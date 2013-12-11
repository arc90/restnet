using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RestNet
{
    public class SchemaResource : ResourceBase
    {
        private enum FieldInsertType
        {
            Before = 1,
            After = 2,
            Child = 3
        }

        protected Schema_what_the_hell _schema = null;
        public string ContentType = null;
        public string SchemaName = null;

        public override string Name
        {
            get { return "Schema"; }
        }

        protected SchemaResource()
        {
            string[] myRoles = null;
            string accessType = "View";
            if (Context != null && Context.Request["accessType"] != null && Context.Request["accessType"].Length != 0)
                accessType = Context.Request["accessType"];

            if (Context != null)
                myRoles = GetMyRoles("Security." + accessType + "." + Context.Request["schemaType"]);

            Constructor(myRoles);
        }

        public SchemaResource(string[] myRoles)
        {
            Constructor(myRoles);
        }

        private void Constructor(string[] myRoles)
        {
            if (Context != null)
            {
                if (RequestMethod == RestNet.RequestMethod.Get || RequestMethod == RestNet.RequestMethod.Head || RequestMethod == RestNet.RequestMethod.Post || RequestMethod == RestNet.RequestMethod.Put)
                {
                    bool rawSchema = (Context.Request.Params["rawSchema"] == "yes");
                    SchemaName  = Context.Request.QueryString["schemaName"];
                    _schema = SchemaResource.GetSchema(SchemaName, myRoles, rawSchema);

                    if (_schema.XmlSchemaDoc == null)
                        throw RestNet.ErrorHandler.HttpResourceInstanceNotFound(Name, SchemaName);
                }
            }
        }

        public static Schema_what_the_hell GetSchema(string contentType, string[] roles)
        {
            return GetSchema(contentType, roles, false);
        }

        public static Schema_what_the_hell GetSchema(string contentType, string[] roles, bool rawSchema)
        {
            string cacheKey = string.Format("schema.{0}.{1}.{2}", contentType, rawSchema, string.Join(".", roles));
            Schema_what_the_hell s = (Schema_what_the_hell)RestNet.Cache.Get(cacheKey);
            if (s == null)
            {
                s = new Schema_what_the_hell(contentType, roles, rawSchema);
                RestNet.Cache.Set(cacheKey, s);
            }
            return s;
        }

        public static XmlDocument GetSchemaXml(string contentType, string[] roles)
        {
            return GetSchema(contentType, roles).XmlSchemaDoc;
        }

        //public static XmlDocument GetSchemaXml(int ID, string[] roles)
        //{
        //    return GetSchema(ID, roles).XmlSchemaDoc;
        //}

        public SchemaResource(Schema_what_the_hell schema)
        {
            _schema = schema;
        }

        protected override void RegisterRepresentationTypes()
        {
            // don't support HTML version (form) because it generates a create form based on user's view rights, not their edit rights
            // so they'll see fields they can't actually edit, and drill-down links will not work, as they go to real createform which
            // will reject the requests
            string[] contentTypes = { "application/xml", "application/sample.data+xml" };
            AddRepresentationType(new RequestMethod[] { RestNet.RequestMethod.Get }, null, contentTypes);
            AddRepresentationType(RestNet.RequestMethod.Post, "application/x-www-form-urlencoded", "application/x-www-form-urlencoded");
        }   

        public override System.IO.Stream GetMyRepresentation(string representationContentType)
        {
            if (_schema.XmlSchemaDoc == null)
                throw RestNet.ErrorHandler.HttpResourceInstanceNotFound(Name, SchemaName);

            switch (representationContentType)
            {
                case "application/sample.data+xml":
                    return OutputStream(Schema_what_the_hell.GetSampleDocument(_schema.XmlSchemaDoc, Utilities.GetRequestParam("IncludeAttributes") == "yes"));
                case "application/xml":
                    //if (Context.Request.Params["sample"] != null)
                    //{
                    //    // generate sample doc
                    //    return OutputStream(_schema.SampleDocument());
                    //}
                    //else
                        return OutputStream(_schema.XmlSchemaDoc);
                //case "text/html":
                //    return OutputStream(SchemaResource.GetHtmlForm(_schema));
                default:
                    // Unknown representation MIME type. This should be impossible to reach! RestNetBase class should validate.
                    throw new NotImplementedException("Unknown representation MIME type. This should be impossible to reach! RestNetBase class should validate.");
            }
        }

        public override int Post(string responseType)
        {
            return Put(responseType);
        }

        public override int Put(string responseType)
        {
            if (!User.Identity.IsAuthenticated)
                throw RestNet.ErrorHandler.HttpNotAllowed("You cannot modify this resource when you are not authenticated by the system");

            switch (responseType)
            {
                case "application/x-www-form-urlencoded":
                    Schema_what_the_hell oldSchema = new Schema_what_the_hell(Context.Request.QueryString["schemaName"], GetMyRoles("Security.Edit.Schemas"));
                    // First make sure it's well-formed
                    bool didSomething = false;
                    if (Context.Request.Form["schema"] != null && Context.Request.Form["schema"] != string.Empty)
                    {
                        ValidateNewSchema(Context.Request.Form["schema"], oldSchema);
                        didSomething = true;
                    }
                    if (Context.Request.Form["fieldRequest"] != null && Context.Request.Form["fieldRequest"] != string.Empty)
                    {
                        Schema_what_the_hell oldSchemaRaw = new Schema_what_the_hell(Context.Request.QueryString["schemaName"], GetMyRoles("Security.Edit.Schemas"), true);
                        AddField(Context.Request.Form["fieldRequest"], oldSchema, oldSchemaRaw);
                        didSomething = true;
                    }
                    if (didSomething)
                    {
                        if (Context.Request.Params["TestOnly"] != "yes")
                            oldSchema.Save(this.User);
                        Context.Response.ContentType = "application/xml";
                        Context.Response.Write(oldSchema.XmlSchemaDoc.OuterXml);
                    }
                    else
                        throw RestNet.ErrorHandler.HttpRepresentationNotValid(Context.Request.QueryString["schemaName"], "The request you made was not understood or is not supported.");
                    break;
                default:
                    RestNet.ErrorHandler.ReturnError(Context.Response, 415, string.Format("Somehow a representation of type {0} made it past RestNet validation. This is a bug. Please notify the system administrator.", Context.Request.ContentType));
                    break;
            }

            return 200;
        }

        private void AddField(string fieldRequestContents, Schema_what_the_hell oldSchema, Schema_what_the_hell oldSchemaRaw)
        {
            XmlDocument fieldRequestDoc = new XmlDocument();
            string currentElement = string.Empty;
            try
            {
                fieldRequestDoc.LoadXml(fieldRequestContents);

                // TODO: this should be validated against FieldRequest schema
                currentElement = "FieldRequest";
                XmlNode fieldRequest = fieldRequestDoc.SelectSingleNode("/FieldRequest");

                currentElement = "Name";
                string fieldCaption = fieldRequest.SelectSingleNode("Name").InnerText;
                
                currentElement = "InternalName";
                XmlNode tempNode = fieldRequest.SelectSingleNode("InternalName");
                string fieldName = tempNode == null ? CreateFieldName(fieldCaption) : tempNode.InnerText;

                currentElement = "Type";
                // HACK: accidentally told Dary to use xs:text. Should be xs:string. This lets both work
                string fieldType = fieldRequest.SelectSingleNode("Type").InnerText.Replace("xs:text", "xs:string");

                XmlNode typeNode = null;
                if (!fieldType.StartsWith("xs:", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] choices = fieldType.Split('|');
                    fieldType = string.Format("picker_{0}", fieldName);
                    typeNode = CreatePickerNode(oldSchema.XmlSchemaDoc, fieldType, fieldCaption, choices);
                }

                currentElement = "ExistingField";
                string existingFieldName = fieldRequest.SelectSingleNode("ExistingField").InnerText;
                XmlNode existingFieldNode = Schema_what_the_hell.SelectSingleNode(oldSchema.XmlSchemaDoc, string.Format("//xs:element[@name='{0}']", existingFieldName));
                if (existingFieldNode == null)
                    throw new Exception(string.Format("Could not find existing field '{0}'", existingFieldName));
                
                currentElement = "InsertType";
                string insertType = fieldRequest.SelectSingleNode("InsertType").InnerText;
                FieldInsertType fieldInsertType = (FieldInsertType)Enum.Parse(typeof(FieldInsertType), insertType);

                currentElement = "Required";
                string required = fieldRequest.SelectSingleNode("Required").InnerText;
                bool isRequired = required.ToLower() == "yes";

                currentElement = "new";
                // we survived validation, so try to add a field!
                XmlNode newFieldNode = oldSchema.XmlSchemaDoc.CreateElement("xs", "element", "http://www.w3.org/2001/XMLSchema");
                Utilities.SetAttribute(newFieldNode, "name", fieldName);
                Utilities.SetAttribute(newFieldNode, "type", fieldType);
                if(!isRequired)
                    // minOccurs defaults to 1 if not present
                    Utilities.SetAttribute(newFieldNode, "minOccurs", "0");

                currentElement = "documentation";
                AddDocumentationNode(newFieldNode, fieldCaption);

                currentElement = "insert of new";
                switch (fieldInsertType)
                {
                    case FieldInsertType.Before:
                        existingFieldNode.ParentNode.InsertBefore(newFieldNode, existingFieldNode);
                        break;
                    case FieldInsertType.After:
                        existingFieldNode.ParentNode.InsertAfter(newFieldNode, existingFieldNode);
                        break;
                    case FieldInsertType.Child:
                        existingFieldNode.AppendChild(newFieldNode);
                        break;
                }
                if (typeNode != null)
                    existingFieldNode.OwnerDocument.DocumentElement.AppendChild(typeNode);

                currentElement = "validation of new";
                // last step is to try to validate with the new schema. If it blows up, our new schema is not valid
                ValidateNewSchema(oldSchema.XmlSchemaDoc.OuterXml, oldSchema);

                // if we're still here, validation succeeds, so add it to RAW schema (with includes left unresolved)

                newFieldNode = oldSchemaRaw.XmlSchemaDoc.ImportNode(newFieldNode, true);
                existingFieldNode = Schema_what_the_hell.SelectSingleNode(oldSchemaRaw.XmlSchemaDoc, string.Format("//xs:element[@name='{0}']", existingFieldName));

                if (existingFieldNode == null)
                    throw new Exception(string.Format("Could not find existing field '{0}'. Adding fields to imported schemas is not currently supported.", existingFieldName));

                currentElement = "insert of new";
                switch (fieldInsertType)
                {
                    case FieldInsertType.Before:
                        existingFieldNode.ParentNode.InsertBefore(newFieldNode, existingFieldNode);
                        break;
                    case FieldInsertType.After:
                        existingFieldNode.ParentNode.InsertAfter(newFieldNode, existingFieldNode);
                        break;
                    case FieldInsertType.Child:
                        existingFieldNode.AppendChild(newFieldNode);
                        break;
                }

                oldSchema.XmlSchemaDoc = oldSchemaRaw.XmlSchemaDoc;
                if (typeNode != null)
                    oldSchema.XmlSchemaDoc.DocumentElement.AppendChild(oldSchema.XmlSchemaDoc.ImportNode(typeNode, true));
            }
            catch (Exception ex)
            {
                throw RestNet.ErrorHandler.HttpRepresentationNotValid("schema field request", string.Format("Error processing {0} field. {1}", currentElement, ex.Message));
            }
        }

        private static void AddDocumentationNode(XmlNode elementNode, string fieldCaption)
        {
            XmlNode tempNode = elementNode.OwnerDocument.CreateElement("xs", "annotation", "http://www.w3.org/2001/XMLSchema");
            tempNode = elementNode.AppendChild(tempNode);
            tempNode = tempNode.AppendChild(elementNode.OwnerDocument.CreateElement("xs", "documentation", "http://www.w3.org/2001/XMLSchema"));
            Utilities.SetAttribute(tempNode, "source", "label");
            tempNode.InnerText = fieldCaption;
            return;
        }

        private XmlNode CreatePickerNode(XmlDocument ownerDocument, string typeName, string caption, string[] choices)
        {
            string namespaceUri = "http://www.w3.org/2001/XMLSchema";
            XmlNode typeNode = ownerDocument.CreateElement("xs", "simpleType", namespaceUri);
            Utilities.SetAttribute(typeNode, "name", typeName);
            AddDocumentationNode(typeNode, caption);
            XmlNode tempNode = typeNode.AppendChild(ownerDocument.CreateElement("xs", "restriction", namespaceUri));
            Utilities.SetAttribute(tempNode, "base", "xs:string");
            for (int x = 0; x < choices.Length; x++)
            {
                XmlNode choiceNode = ownerDocument.CreateElement("xs", "enumeration", namespaceUri);
                Utilities.SetAttribute(choiceNode, "value", choices[x]);
                tempNode.AppendChild(choiceNode);
            }
            return typeNode;


        }

        private string CreateFieldName(string fieldCaption)
        {
            return fieldCaption.ToLower().Replace(' ', '_');
        }

        private void ValidateNewSchema(string schemaContents, Schema_what_the_hell oldSchema)
        {
            XmlDocument newSchemaDoc = new XmlDocument();
            try
            {
                newSchemaDoc.LoadXml(schemaContents);
            }
            catch(Exception ex)
            {
                throw RestNet.ErrorHandler.HttpRepresentationNotValid("schema", ex.Message);
            }
            // Now see if it passes schema validation
            try
            {
                oldSchema.XmlSchemaDoc = (XmlDocument) newSchemaDoc.Clone();
                oldSchema.GetIncludedSchemas(oldSchema.XmlSchemaDoc);
                XmlDocument testDoc = new XmlDocument();
                testDoc.LoadXml("<test/>");
                oldSchema.Validate(testDoc, true);
            }
            catch (System.Xml.Schema.XmlSchemaException ex)
            {
                throw RestNet.ErrorHandler.HttpRepresentationNotValid("schema", Context.Server.HtmlEncode(ex.Message));
            }
            catch (System.Exception)
            {
                // HACK: This is the Gillen Hack. If a schema exception was thrown (above), then the schema was not valid at all.
                // If we throw another exception, then the data doc may have failed validation, but at least we know the schema
                // was processed okay, and therefore is valid. So XmlSchemaException = bad schema, anything else = good schema.
            }
            finally
            {
                oldSchema.XmlSchemaDoc = newSchemaDoc;
            }
            return;
        }

        /// <summary>
        /// Converts XHTML web form to JSON notation
        /// </summary>
        /// <param name="htmlForm">XML document containing XHTML form representation. Should be generated from SchemaResource.GetHtmlForm</param>
        /// <returns>JSON string representation of the form</returns>
        /// <remarks>Note that to populate form values you must call SchemaResource.SetFormValues on your HTML form before passing it to this method.</remarks>
        public static string GetJsonForm(XmlDocument htmlForm)
        {
            return Utilities.XslTransformToString(htmlForm, Settings.Get("HtmlFormToJsonXslt"));
        }

        public static XmlDocument GetHtmlForm(Schema_what_the_hell schema, string[] myRoles)
        {
            return GetHtmlForm(schema.XmlSchemaDoc, (XmlDocument)null, myRoles);
        }

        public static XmlDocument GetHtmlForm(Schema_what_the_hell schema, XmlDocument xmlDataDocument, string[] myRoles)
        {
            return GetHtmlForm(schema.XmlSchemaDoc, xmlDataDocument, myRoles);
        }

        public static XmlDocument GetHtmlForm(XmlDocument xmlSchemaDoc, string[] myRoles)
        {
            return GetHtmlForm(xmlSchemaDoc, (XmlDocument)null, myRoles);
        }

        public static XmlDocument GetHtmlForm(XmlDocument xmlSchemaDoc, XmlDocument xmlDataDoc, string[] myRoles)
        {
            if (xmlDataDoc != null)
                SetSchemaMinimums(xmlSchemaDoc, xmlDataDoc.DocumentElement);

            string xsltFilename = Utilities.GetFullXmlFilename(Settings.Get("SchemaToFormXslt"));
            System.Xml.Xsl.XslCompiledTransform xslTransform = null;
            xslTransform = RestNet.Utilities.GetXslCompiledTransform(xsltFilename);
            return GetHtmlForm(xmlSchemaDoc, xslTransform, myRoles);
        }


        public static XmlDocument GetHtmlForm(XmlDocument xmlSchemaDoc, System.Xml.Xsl.XslCompiledTransform xslTransform, string[] myRoles)
        {
            string cacheKey = string.Format("htmlform.{0}.{1}", xmlSchemaDoc.OuterXml.GetHashCode(), string.Join(".", myRoles));
            XmlDocument xmlOutput = (XmlDocument)RestNet.Cache.Get(cacheKey);
            if (xmlOutput != null)
                return xmlOutput;

            xmlOutput = Utilities.XslTransform(xmlSchemaDoc, xslTransform);
            if (xmlOutput.SelectNodes("//form/fieldset").Count == 0)
                throw RestNet.ErrorHandler.HttpNotAllowed("You are not authorized to use this form.");

            RestNet.Cache.Set(cacheKey, xmlOutput);
            return xmlOutput;
        }

        public static XmlDocument GetHtmlForm(string contentType, string[] myRoles)
        {
            Schema_what_the_hell s = GetSchema(contentType, myRoles);
            return GetHtmlForm(s, myRoles);
        }

        public static XmlDocument GetHtmlFormPartial(XmlDocument xmlSchemaDoc, string nodeName, string[] myRoles)
        {
            string cacheKey = string.Format("htmlformpartial.{0}.{1}", nodeName, string.Join(".", myRoles));
            XmlDocument xmlOutput = (XmlDocument)RestNet.Cache.Get(cacheKey);
            if (xmlOutput != null)
                return xmlOutput;

            // retrieve full form
            xmlOutput = GetHtmlForm(xmlSchemaDoc, myRoles);

            // find the first node (ends with "_1") of this name. If it doesn't exist, wasn't a valid request anyway, so blow up.
            XmlNode thisNode = xmlOutput.SelectSingleNode(string.Format("//fieldset[@id=\"{0}_1\"]", nodeName));

            if (thisNode == null)
                throw RestNet.ErrorHandler.HttpNotAllowed(string.Format("The form section '{0}' does not exist, or you do not have permissions to use it.", nodeName));

            // change _1 to _0. Numbering is up to the client anyway.
            thisNode.Attributes["id"].Value = thisNode.Attributes["id"].Value.Replace("_1", "_0");
            thisNode.InnerXml = thisNode.InnerXml.Replace(string.Format("-{0}_1-", nodeName), string.Format("-{0}_0-", nodeName));

            // turn into doc, cache, and return it!
            xmlOutput.LoadXml(thisNode.OuterXml);
            RestNet.Cache.Set(cacheKey, xmlOutput);
            return xmlOutput;
        }

        private static void RecurseComplexTypes( XmlDocument targetSchema, XmlNode targetTypeNode, XmlDocument sourceSchema)
        {
            string nodeType = targetTypeNode.Attributes["name"].Value;

            string xPath = ".//xs:element[starts-with(@type, 'xs:') = 0]";
            foreach (XmlNode subTypeNode in Schema_what_the_hell.SelectNodes(targetTypeNode, xPath))
            {
                nodeType = subTypeNode.Attributes["type"].Value;
                xPath = string.Format("//xs:complexType[@name=\"{0}\"]|//xs:simpleType[@name=\"{0}\"]", nodeType);
                if (Schema_what_the_hell.SelectSingleNode(targetSchema, xPath) == null)
                {
                    XmlNode typeNode = Schema_what_the_hell.SelectSingleNode(sourceSchema, xPath);
                    typeNode = targetSchema.DocumentElement.AppendChild(targetSchema.ImportNode(typeNode, true));
                    RecurseComplexTypes(targetSchema, typeNode, sourceSchema);
                }

            }
            return;
        }


        private static System.Xml.Xsl.XslCompiledTransform getXslt()
        {
            return RestNet.Utilities.GetXslCompiledTransform(Settings.Get("SchemaToFormXslt"));
        }

        private static void SetSchemaMinimums(XmlDocument xmlSchemaDoc, XmlNode thisNode)
        {
            int instanceCount = 1;
            for (int x = 0; x < thisNode.ChildNodes.Count; x++)
            {
                XmlNode childNode = thisNode.ChildNodes[x];
                SetSchemaMinimums(xmlSchemaDoc, childNode);
                if (childNode.NextSibling != null && childNode.Name == childNode.NextSibling.Name)
                {
                    instanceCount++;
                    SetSchemaMinOccurs(xmlSchemaDoc, childNode.Name, instanceCount);
                }
                else
                {
                    // if no dupe, reset counter, as there may be multiple dupes under one parent, i.e. <fruit><apple/><apple/><orange/><orange/></fruit>
                    instanceCount = 1;
                }
            }
            //if (instanceCount != 1)
            //{
            //    string xpath = "//joel";
            //    XmlNode thisAttribute = xmlSchemaDoc.SelectSingleNode(xpath);
            //}
        }

        private static void SetSchemaMinOccurs(XmlDocument xmlSchemaDoc, string elementName, int minOccurs)
        {
            string xpath = string.Format("//xs:element[@name='{0}']", elementName);
            XmlNode thisNode = Schema_what_the_hell.SelectSingleNode(xmlSchemaDoc, xpath);
            if (thisNode != null)
            {
                XmlAttribute minOccursAttribute = thisNode.Attributes["minOccurs"];
                if (minOccursAttribute == null)
                {
                    minOccursAttribute = xmlSchemaDoc.CreateAttribute("minOccurs");
                    thisNode.Attributes.Append(minOccursAttribute);
                }
                minOccursAttribute.Value = minOccurs.ToString();
            }

        }
        
        private static void setFormValue(XmlDocument htmlForm, XmlNode oNode,string nodeParent)
        {

            if (oNode == null) { return; }

            if (!oNode.HasChildNodes || oNode.ChildNodes[0].LocalName=="#text")
            {
                
                string nodeValue = string.Empty;

                // GET NODE VALUE
                if (oNode.HasChildNodes)
                {
                    nodeValue = oNode.ChildNodes[0].Value;
                }
                else
                {
                    nodeValue = oNode.Value;
                }

                string nodeName = oNode.LocalName;
                XmlNode thisNode;

                // GET FORM NODE AND FIGURE OUT THE CONTROL TYPE
                string xpathSuffix = nodeParent == "root" ? string.Empty : "-" + nodeParent;

                thisNode = htmlForm.SelectSingleNode(string.Format("//*[@id=\"{0}{1}\"]", nodeName, xpathSuffix));

                if (thisNode != null)
                {
                    switch (thisNode.LocalName)
                    {
                        case "input":
                            switch (thisNode.Attributes["type"].Value)
                            {
                                case "password":
                                case "text":
                                case "hidden":
                                    thisNode.Attributes["value"].Value = nodeValue;
                                    break;

                                case "checkbox":
                                case "radio":
                                    // PUNTED, SO DO NOTHING 
                                    // TODO
                                    //if (oNode.Value == "")
                                    //    {
                                    //        theseNodes[y].Attributes.Append(htmlForm.CreateAttribute("checked"));
                                    //        theseNodes[y].Attributes["checked"].Value = "true";
                                    //    }

                                    break;
                            }
                            break;

                        case "select":

                            // TODO: NEED TO CHECK FOR BOOLEAN TYPES HERE SOMEHOW

                            //string xpath = string.Format("option[@value = '{0}'] | option[. = '{0}']", nodeValue);
                            XmlNode selectedOption = Utilities.SelectSingleNodeParameterized(thisNode, "option[@value=$option] | option[. = $option]", "option", nodeValue);

                            if(selectedOption != null)
                                Utilities.SetAttribute(selectedOption, "selected", "selected");
                            // if it is null, we couldn't find that option in the list anymore
                            break;
                        case "fieldset":
                            // just ignore fieldsets. They'll be handled elsewhere
                            break;

                        default:
                            throw new Exception("setFormValue - dont know what control type this is");

                    }
                }
                else
                {
                    // schema no longer has that node - ignore for now
                }
                    
            }
            else
            {
                bool collection = false;
                // CHECK FOR COLLECTION
                for (int i = 0; i < oNode.ChildNodes.Count; i++)
                {
                    if ((oNode.ChildNodes[i].NextSibling != null) && (oNode.ChildNodes[i].NextSibling.LocalName == oNode.ChildNodes[i].LocalName))
                    {
                        // COLLECTION HERE
                        collection = true;
                        break;
                    }
                    else
                    {
                        string nodePath = nodePath = oNode.LocalName + ((nodeParent == "root") ? string.Empty : "-" + nodeParent);


                        // If many of this node are allowed, element will have number suffix, and xpath for ID will fail

                        // Does the node we're looking for exist?
                        if (htmlForm.SelectSingleNode("//fieldset[@id=\"" + oNode.LocalName + "\"]") == null)
                        {
                            // No it doesn't, so try looking for it with _1 suffix, indicating that multiple copies are allowed
                            // (elements will be named address_1, address_2, address_3, etc.)
                            if (htmlForm.SelectSingleNode("//fieldset[@id=\"" + oNode.LocalName + "_1\"]") != null)
                            {
                                // found it! So do our search for nodename_1 instead of nodename
                                nodePath = oNode.LocalName + "_1" + ((nodeParent == "root") ? string.Empty : "-" + nodeParent);
                            }
                        }

                        // NOT A COLLECTION - RECURSE
                        setFormValue(htmlForm, oNode.ChildNodes[i], nodePath);
                    }
                }

                if (collection)
                {
                    setFormCollectionValues(htmlForm, oNode, nodeParent);
                }

            }

        }

        private static void setFormCollectionValues(XmlDocument htmlForm, XmlNode oNode,string nodeParent)
        {

            if (oNode == null) { return; }

            string nodeName = string.Empty;

            if (nodeParent == "root")
            {
                nodeName = oNode.LocalName;
            }
            else
            {
                nodeName = oNode.LocalName + "-" + nodeParent;
            }

           
            string collectionNodeName = oNode.ChildNodes[0].LocalName;

            for (int i = 0; i < oNode.ChildNodes.Count; i++)
            {

                if (oNode.ChildNodes[i].LocalName != collectionNodeName)
                {
                    setFormValue(htmlForm, oNode.ChildNodes[i], nodeName);
                    break;
                }
                else
                {
                    int icount = i + 1;

                    string newParent = oNode.ChildNodes[i].LocalName + "_" + icount.ToString() + "-" + nodeName;


                    // LOOP THRU COLLECTION CHILDREN HERE
                    for (int x = 0; x < oNode.ChildNodes[i].ChildNodes.Count; x++)
                    {

                        setFormValue(htmlForm, oNode.ChildNodes[i].ChildNodes[x], newParent);

                    }
                }
            }

        }


        public static void SetFormValues(XmlDocument htmlForm, XmlDocument xmlDataDocument)
        {
            
            for (int i = 0; i < xmlDataDocument.DocumentElement.ChildNodes.Count; i++)
            {
                setFormValue(htmlForm, xmlDataDocument.DocumentElement.ChildNodes[i], "root");
            }


            // SET DOC ATTRIBUTES 
            foreach (XmlAttribute thisAttribute in xmlDataDocument.DocumentElement.Attributes)
            {

                XmlNode thisNode = htmlForm.SelectSingleNode("//*[@id=\"" + thisAttribute.Name + "\"]");

                if (thisNode != null && thisNode.Attributes["value"] != null) { thisNode.Attributes["value"].Value = thisAttribute.Value; }
            }

        }
    }
}
