using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Xml.Schema;


namespace RestNet
{
    public class Schema
    {
        private XmlDocument _schemaDoc;
        private ArrayList _validationMessages;
        private string _sourceUrl;

        public Schema(string sourceUrl)
        {
            _schemaDoc = RestNet.Utilities.GetXsdDocumentFromFile(sourceUrl);
            _validationMessages = new ArrayList(0);
            _sourceUrl = RestNet.Utilities.GetFullXmlFilename(sourceUrl, "Schema"); ;
        }

        public Schema(XmlDocument xmlSchemaDoc, string sourceUrl)
        {
            if (xmlSchemaDoc == null)
                throw new NullReferenceException("A valid xml schema doc must be passed in.");

            _schemaDoc = xmlSchemaDoc;
            _validationMessages = new ArrayList(0);
            _sourceUrl = RestNet.Utilities.GetFullXmlFilename(sourceUrl, "Schema"); ;
        }

        public bool Validate(XmlDocument xmlDoc)
        {
            var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    ProhibitDtd = false,
                    ValidationFlags =
                        XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation
                };
            settings.Schemas.Add(string.Empty, _sourceUrl);
            settings.ValidationEventHandler += ValidationCallback;
            using (var xnr = new XmlNodeReader(xmlDoc))
            {
                using (XmlReader xr = XmlReader.Create(xnr, settings))
                {
                    while (xr.Read()) ;
                    xr.Close();
                }
            }
            return _validationMessages.Count == 0;
        }

        //public bool XXValidate(XmlDocument xmlDoc)
        //{
        //    if (xmlDoc == null)
        //        throw new NullReferenceException("A valid xml doc must be passed in.");

        //    _validationMessages = new ArrayList(0);

        //    GetIncludedSchemas(_schemaDoc);

        //    XmlReaderSettings settings = CreateSchemaReaderSettings(ValidationCallback, _schemaDoc);

        //    using (XmlNodeReader xnr = new XmlNodeReader(xmlDoc))
        //    {
        //        using (XmlReader xr = XmlReader.Create(xnr, settings))
        //        {
        //            while (xr.Read()) ;
        //            xr.Close();
        //        }
        //    }

        //    if (_validationMessages.Count == 0)
        //        return true;
        //    else
        //        return false;
        //}

        private void ValidationCallback(object sender, ValidationEventArgs args)
        {
            _validationMessages.Add(args.Message + "\n");
        }

        private XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, string schemaUri)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            return CreateSchemaReaderSettings(validationDelegate, XmlReader.Create(schemaUri, settings));
        }

        private XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, XmlDocument xmlSchemaDoc)
        {
            XmlNodeReader xnr = new XmlNodeReader(xmlSchemaDoc);
            return CreateSchemaReaderSettings(validationDelegate, xnr);
        }

        private XmlReaderSettings CreateSchemaReaderSettings(ValidationEventHandler validationDelegate, XmlReader xrSchemaReader)
        {

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationEventHandler += new ValidationEventHandler(validationDelegate);
            using (xrSchemaReader)
            {
                settings.Schemas.Add(null, xrSchemaReader);
                xrSchemaReader.Close();
            }
            return settings;
        }

        //public void GetIncludedSchemas(XmlNode schemaNode)
        //{
        //    foreach (XmlNode inc in SelectNodes(schemaNode, ".//xs:include"))
        //    {
        //        var includedSchemaUrl = new Uri(inc.Attributes["schemaLocation"].Value, UriKind.RelativeOrAbsolute);

        //        var resolvedUrl =
        //            (System.IO.Path.Combine(_sourceFolder.OriginalString, includedSchemaUrl.OriginalString));

        //        XmlDocument xmlIncludedSchema = GetFromFile(resolvedUrl);

        //        // can't just replace node, otherwise we'll have schema node inside another
        //        // so delete the include, then append all the new kids after the old ones
        //        XmlDocumentFragment newKids = _schemaDoc.CreateDocumentFragment();
        //        newKids.InnerXml = xmlIncludedSchema.DocumentElement.InnerXml;
        //        XmlNode replacedNode = _schemaDoc.DocumentElement.InsertAfter(newKids, inc);
        //        inc.ParentNode.RemoveChild(inc);

        //        // recurse
        //        GetIncludedSchemas(replacedNode);
        //    }
        //}

        public static XmlNodeList SelectNodes(XmlDocument xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlSource.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectNodes(xPath, nsmgr);
        }

        public static XmlNodeList SelectNodes(XmlNode xmlSource, string xPath)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager((xmlSource is XmlDocument) ? ((XmlDocument)xmlSource).NameTable : xmlSource.OwnerDocument.NameTable);
            nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            return xmlSource.SelectNodes(xPath, nsmgr);
        }

        public ArrayList ValidationMessages
        {
            get { return _validationMessages; }
        }

        public string ValidationMessagesAsString(string delimiter)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _validationMessages.Count; i++)
            {
                sb.Append(_validationMessages[i] + delimiter);
            }

            return sb.ToString();
        }

        public static XmlDocument GetFromFile(string SchemaDocName)
        {
            return RestNet.Utilities.GetXsdDocumentFromFile(SchemaDocName);
        }
    }
}
