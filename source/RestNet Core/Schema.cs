using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using RestNet.Data;

namespace RestNet
{
    public class Schema_what_the_hell : ICloneable
    {
        # region Properties
        public System.Xml.XmlDocument XmlSchemaDoc = null;
        private int _id = 0;
        private bool _rawSchema = false;
        private string _validationMessages = string.Empty;
        private string _elementName = null;
        private SqlDataAccessLayer dal = new SqlDataAccessLayer();

        string[] _roles = null;

        # endregion Properties

        private Schema_what_the_hell(string[] myRoles)
        {
            Roles = myRoles;
        }

        public Schema_what_the_hell(string contentType, string[] myRoles) : this(contentType, myRoles, false)
        {
        }

        public Schema_what_the_hell(string contentType, string[] myRoles, bool rawSchema)
        {
            _rawSchema = rawSchema;
            int schemaId = Schema_what_the_hell.GetIdFromName(contentType);
            
            _elementName = contentType;
            Roles = myRoles;
            if (schemaId > 0)
                RetrieveSchemaDetails(schemaId);
        }

        private static int GetIdFromName(string contentType)
        {
            string cacheKey = string.Format("schema.id.{0}", contentType);
            string schemaIdString = (string)RestNet.Cache.Get(cacheKey);
            int schemaId = 0;
            if (schemaIdString == null || schemaIdString == string.Empty || !int.TryParse(schemaIdString, out schemaId))
            {
                System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("getSchemaId");
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@contentType", contentType);
                schemaId = RestNet.Data.SqlDataAccessLayer.GetScalarOut(Settings.GetConnectionString("core"), cmd, "@schemaId");
                if (schemaId == 0)
                    throw RestNet.ErrorHandler.HttpResourceInstanceNotFound("Schema", contentType);

                RestNet.Cache.Set(cacheKey, schemaId.ToString());
            }
            return schemaId;
        }

        //public Schema(int id, string[] myRoles)
        //{
        //    Roles = myRoles;
        //    RetrieveSchemaDetails(id);
        //}

        public string ElementName
        {
            get
            {
                return _elementName;
            }
        }

        private string[] Roles
        {
            get
            {
                return _roles;
            }
            set
            {
                if (value == null || value.Length == 0)
                    throw RestNet.ErrorHandler.HttpNotAllowed("You are not authorized to access schema records");

                _roles = value;
            }
        }

/*
        public bool Validate(XmlDocument xmlDoc, string schemaUrl)
        {
            if (xmlDoc == null || schemaUrl == null || schemaUrl == string.Empty)
                return false;

            _validationMessages = string.Empty;
            XmlReaderSettings settings = CreateSchemaReaderSettings(ValidationCallback, schemaUrl);
            using (XmlNodeReader xnr = new XmlNodeReader(xmlDoc))
            {
                using (XmlReader xr = XmlReader.Create(xnr, settings))
                {
                    while (xr.Read()) ;
                    xr.Close();
                }
            }

            if(_validationMessages != string.Empty)
                throw new Exception(string.Format("<ul id=\"schemaErrors\"><li>{0}</li></ul><p>This record must match <a href=\"{1}\">this schema</a>.</p>", _validationMessages.Replace("\n", "<br/>\n"), schemaUrl));

            return true;
        }
*/

        public bool Validate(XmlDocument xmlDoc, bool doValidation)
        {
            
            if (xmlDoc == null)
                return false;

            if (XmlSchemaDoc == null)
                return false;

            // create schemaset, add contact schema to it
            //XmlSchemaSet sc = new XmlSchemaSet();
            //sc.Add("urn:contact-schema", XmlReader.Create(new System.IO.MemoryStream(Encoding.ASCII.GetBytes(XmlSchemaDoc.OuterXml))));
            _validationMessages = string.Empty;

            if (doValidation)
            {
                XmlReaderSettings settings = CreateSchemaReaderSettings(ValidationCallback, XmlSchemaDoc);
               using (XmlNodeReader xnr = new XmlNodeReader(xmlDoc))
                {
                    using (XmlReader xr = XmlReader.Create(xnr, settings))
                    {
                        while (xr.Read()) ;
                        xr.Close();
                    }
                }
            }

            if (_validationMessages == string.Empty)
            {

                // we passed schema validation, or we skipped it, but we need to force any fixed attributes into the data doc, as they may be absent
                ApplyFixedAttributes(XmlSchemaDoc, xmlDoc);
                return true;
            }
            else
                // TODO: Can't hardcode this to /schemas/contact! Need to let schema resource itself figure out what rights are needed. Right now
                // /schemas/contact is rewritten to /schemas/1?SchemaType=Contacts, which is used to find Security.View.Contacts permissions. If
                // we use /schemas/1, we don't know which permissions to look up and 403 is thrown, hence the current hardcoding.
                throw new Exception(string.Format("<ul id=\"schemaErrors\"><li>{0}</li></ul><p>This record must match <a href=\"/starling/schemas/{1}?accessType=edit\">this schema</a>.</p>", _validationMessages.Replace("\n", "<br/>\n"), "contact"));
        }

        private static XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, string schemaUri)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            //settings.NameTable = new NameTable();
            //settings.NameTable.Add("xs");
            settings.ProhibitDtd = false;
            return CreateSchemaReaderSettings(validationDelegate, XmlReader.Create(schemaUri, settings));
        }

        private static XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, XmlDocument xmlSchemaDoc)
        {
            XmlNodeReader xnr = new XmlNodeReader(xmlSchemaDoc);
            return CreateSchemaReaderSettings(validationDelegate, xnr);
        }

        private static XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, XmlReader xrSchemaReader)
        {
            
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationEventHandler += new ValidationEventHandler(validationDelegate);
            //settings.NameTable = new NameTable();
            //settings.NameTable.Add("xs");
            using (xrSchemaReader)
            {
                settings.Schemas.Add(null, xrSchemaReader);
                xrSchemaReader.Close();
            }
            return settings;
        }

        public static void ApplyFixedAttributes(XmlDocument xmlSchemaDoc, XmlDocument xmlDataDoc)
        {
            // find the fixed attributes
            foreach (XmlAttribute thisFixedAttribute in Schema_what_the_hell.SelectNodes(xmlSchemaDoc, "//xs:attribute/@fixed"))
            {
                string attributeName = thisFixedAttribute.OwnerElement.Attributes["name"].Value;

                // find out the type this element applies to
                string typeElementName = thisFixedAttribute.OwnerElement.ParentNode.Attributes["name"].Value;
                // find out the name of that element
                string elementName = Schema_what_the_hell.SelectSingleNode(xmlSchemaDoc, "//xs:element[@type='" + typeElementName + "']").Attributes["name"].Value;

                // go back to the data doc. If it's present, the schema has validated it, so simply append if absent
                XmlNode dataNode = xmlDataDoc.SelectSingleNode("//" + elementName);
                if (dataNode != null && dataNode.Attributes[attributeName] == null)
                {
                    dataNode.Attributes.Append(dataNode.OwnerDocument.CreateAttribute(attributeName));
                    dataNode.Attributes[attributeName].Value = thisFixedAttribute.Value;
                }
            }
        }

        public static string GetFieldCaption(XmlDocument xmlSchemaDoc, string fieldName)
        {
            XmlNode thisField = SelectSingleNode(xmlSchemaDoc, "//xs:element[@name='{0}']/xs:annotation/xs:documentation[@source='label']");
            return thisField == null ? fieldName : thisField.InnerText;
        }

        private void ValidationCallback(object sender, ValidationEventArgs args)
        {
            _validationMessages += args.Message + "\n";
        }
        /*        public Contact(int id, string lastName, string firstName)
                {
                    ID = id;
                    LastName = lastName;
                    FirstName = firstName;
                }
        */

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        private void RetrieveSchemaDetails(int id)
        {
            ID = id;

            SetMasterCacheDependency(false);
            
            string cacheKey = string.Format("schema.XmlSchemaDoc.{0}.{1}.{2}", id, _rawSchema, string.Join(".", Roles));
            XmlSchemaDoc = (XmlDocument)RestNet.Cache.Get(cacheKey);
            if (XmlSchemaDoc == null)
            {
                XmlSchemaDoc = dal.Retrieve(RestNet.Settings.GetConnectionString("core"), id.ToString(), "schema");
                // HACK: Should use real schema document and do includes properly
                if (!_rawSchema)
                {
                    GetIncludedSchemas(XmlSchemaDoc);
                    RestrictByRole(this, Roles);
                }
                string[] cacheDependencyKeys = new string[1];
                cacheDependencyKeys[0] = string.Format("schema.{0}", id);
                RestNet.Cache.Set(cacheKey, XmlSchemaDoc, null, cacheDependencyKeys);
            }
        }

        public void GetIncludedSchemas(XmlNode schemaNode)
        {
            foreach (XmlNode inc in SelectNodes(schemaNode, ".//xs:include"))
            {
                XmlDocument xmlIncludedSchema = new XmlDocument();
                string schemaLocation = inc.Attributes["schemaLocation"].Value;
                if (schemaLocation.StartsWith("#"))
                    xmlIncludedSchema = new Schema_what_the_hell(schemaLocation.Remove(0, 1), Roles).XmlSchemaDoc;
                else
                    xmlIncludedSchema.Load(inc.Attributes["schemaLocation"].Value);

                // can't just replace node, otherwise we'll have schema node inside another
                // so delete the include, then append all the new kids after the old ones
                XmlDocumentFragment newKids = XmlSchemaDoc.CreateDocumentFragment();
                newKids.InnerXml = xmlIncludedSchema.DocumentElement.InnerXml;
                XmlNode replacedNode = XmlSchemaDoc.DocumentElement.InsertAfter(newKids, inc);
                inc.ParentNode.RemoveChild(inc);

                // recurse
                GetIncludedSchemas(replacedNode);
            }
        }

        public void Save(System.Security.Principal.IPrincipal updatedBy)
        {
            if (this.XmlSchemaDoc == null || this.XmlSchemaDoc.DocumentElement == null)
                throw new Exception("Schema record does not contain XML content and cannot be saved.");

            if (ID == 0)
                dal.Create(RestNet.Settings.GetConnectionString("core"), "schema", this.XmlSchemaDoc, updatedBy.Identity.Name);
            else
                dal.Update(RestNet.Settings.GetConnectionString("core"), ID.ToString(), "schema", this.XmlSchemaDoc, updatedBy.Identity.Name);

            SetMasterCacheDependency(true);
        }

        private void SetMasterCacheDependency(bool forceUpdate)
        {
            // clear any cached objects by updating the core dependency
            string cacheKey = string.Format("schema.{0}", ID);
            if (forceUpdate || Cache.Get(cacheKey) == null)
                Cache.Set(cacheKey, DateTime.Now.Ticks.ToString());
        }


        public static XmlNodeList SelectNodes(XmlDocument xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlSource.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectNodes(xPath, nsmgr);
        }

        public static XmlNodeList SelectNodes(XmlNode xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager((xmlSource is XmlDocument) ? ((XmlDocument) xmlSource).NameTable : xmlSource.OwnerDocument.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectNodes(xPath, nsmgr);
        }

        public static XmlNode SelectSingleNode(XmlDocument xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlSource.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectSingleNode(xPath, nsmgr);
        }

        public static XmlNode SelectSingleNode(XmlNode xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlSource.OwnerDocument.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectSingleNode(xPath, nsmgr);
        }

        public static void RestrictByRole(Schema_what_the_hell sourceSchema, string[] myRoles)
        {
            string storeList = "|" + string.Join("|", myRoles) + "|";
            string datastoreXpath = "//xs:complexType/xs:attribute[@name='datastore']";

            // find all complex types with a datastore attribute
            XmlNodeList typeNodes = Schema_what_the_hell.SelectNodes(sourceSchema.XmlSchemaDoc, datastoreXpath);

            // if no datastore attribute, this schema isn't secured at all, so bypass checking
            if (typeNodes == null || typeNodes.Count == 0)
                return;

            foreach (XmlNode typeNode in typeNodes)
            {
                // get the datastore value
                string storeName = typeNode.Attributes["fixed"].Value;
                if (!storeList.Contains(storeName))
                {
                    // User not allowed to see this datastore so find any elements of this type and remove them
                    string typeName = typeNode.ParentNode.Attributes["name"].Value;
                    foreach (XmlNode elementNode in Schema_what_the_hell.SelectNodes(sourceSchema.XmlSchemaDoc, string.Format("//xs:element[@type='{0}']", typeName)))
                    {
                        elementNode.ParentNode.RemoveChild(elementNode);
                    }
                }

            }

            // We've removed all elements user shouldn't see. Now remove any unused types, as they probably refer to deleted elements.
            foreach (XmlNode typeNode in Schema_what_the_hell.SelectNodes(sourceSchema.XmlSchemaDoc, "/xs:schema/xs:simpleType | /xs:schema/xs:complexType"))
            {
                string typeName = typeNode.Attributes["name"].Value;
                if (Schema_what_the_hell.SelectNodes(sourceSchema.XmlSchemaDoc, string.Format("//xs:element[@type='{0}']", typeName)).Count == 0)
                {
                    // type is not in use, so delete it
                    typeNode.ParentNode.RemoveChild(typeNode);
                }
            }


            if (Schema_what_the_hell.SelectNodes(sourceSchema.XmlSchemaDoc, datastoreXpath).Count == 0)
                throw RestNet.ErrorHandler.HttpNotAllowed("You are not authorized to use this resource.");
        }

        #region ICloneable Members

        public object Clone()
        {
            Schema_what_the_hell clonedSchema = new Schema_what_the_hell(Roles);
            clonedSchema.XmlSchemaDoc = (XmlDocument) this.XmlSchemaDoc.Clone();
            clonedSchema.ID = _id;
            return clonedSchema;
        }

        #endregion

        /// <summary>
        /// Returns a sample XML document based on a schema. Sample document has NO data, but correct structure.
        /// </summary>
        /// <param name="contentType">Content type of the schema</param>
        /// <param name="roles">Array of data roles to enforce schema security</param>
        /// <returns>XmlDocument object that matches the schema but contains no values or attributes</returns>
        public static XmlDocument GetSampleDocument(string contentType, string[] roles, bool includeAttributes)
        {
            string cacheKey = string.Format("schema.sampledoc.{0}.{1}", contentType, string.Join(".", roles));
            XmlDocument sampleDoc = (XmlDocument)Cache.Get(cacheKey);
            if (sampleDoc == null)
            {
                Schema_what_the_hell s = new Schema_what_the_hell(contentType, roles);
                sampleDoc = GetSampleDocument(s.XmlSchemaDoc, includeAttributes);
                Cache.Set(cacheKey, sampleDoc);
            }
            return sampleDoc;
        }

        /// <summary>
        /// Returns a sample XML document based on a schema. Sample document has NO data, but correct structure.
        /// </summary>
        /// <param name="xmlSchemaDoc">XSD to use as source</param>
        /// <returns>XmlDocument object that matches the schema but contains no values or attributes</returns>
        public static XmlDocument GetSampleDocument(XmlDocument xmlSchemaDoc, bool includeAttributes)
        {
            // HACK: This currently makes a lot of assumptions about the schema structure
            XmlDocument outputDoc = new XmlDocument();
            XmlNode rootSchemaNode = Schema_what_the_hell.SelectSingleNode(xmlSchemaDoc, "/xs:schema/xs:element");
            XmlNode rootDocNode = outputDoc.AppendChild(outputDoc.CreateElement(rootSchemaNode.Attributes["name"].Value));
            foreach (XmlNode requiredAttribute in Schema_what_the_hell.SelectNodes(rootSchemaNode, "./xs:complexType/xs:attribute[@use='required']"))
            {
                switch (requiredAttribute.Attributes["type"].Value)
                {
                    case "xs:integer":
                        Utilities.SetAttribute(rootDocNode, requiredAttribute.Attributes["name"].Value, "0");
                        break;
                    default:
                        break;
                }
            }
            foreach (XmlNode childNode in Schema_what_the_hell.SelectNodes(rootSchemaNode, "./xs:complexType/xs:all/xs:element | ./xs:complexType/xs:sequence/xs:element | ./xs:complexType/xs:choice/xs:element"))
            {
                AppendSampleNode(outputDoc.DocumentElement, childNode, includeAttributes);
            }
            return outputDoc;
        }

        /// <summary>
        /// Recursive function to walk schema element and type hierarchy and append empty elements to sample doc
        /// </summary>
        /// <param name="newDocParent">Current node in sample doc</param>
        /// <param name="schemaParent">Current node in schema doc</param>
        private static void AppendSampleNode(XmlNode newDocParent, XmlNode schemaParent, bool includeAttributes)
        {
            XmlNode newDocChild = newDocParent.AppendChild(newDocParent.OwnerDocument.CreateElement(schemaParent.Attributes["name"].Value));
            if (includeAttributes)
            {
                XmlNode minOccurs = schemaParent.Attributes["minOccurs"];
                XmlNode maxOccurs = schemaParent.Attributes["maxOccurs"];
                if (minOccurs != null)
                    newDocChild.Attributes.Append((XmlAttribute)newDocParent.OwnerDocument.ImportNode(minOccurs, false));
                if (maxOccurs != null)
                    newDocChild.Attributes.Append((XmlAttribute)newDocParent.OwnerDocument.ImportNode(maxOccurs, false));
            }

            string typeName = schemaParent.Attributes["type"].Value;
            if (typeName.StartsWith("xs:"))
            {
                // simple field, so stop recursion
                return;
            }

            foreach (XmlNode schemaChild in Schema_what_the_hell.SelectNodes(schemaParent, string.Format("//xs:complexType[@name='{0}']/xs:all/xs:element | //xs:complexType[@name='{0}']/xs:sequence/xs:element | //xs:complexType[@name='{0}']/xs:choice/xs:element", typeName)))
            {
                AppendSampleNode(newDocChild, schemaChild, includeAttributes);
            }
        }
    }
 
}
