using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Xml.Xsl;
using System.IO;

namespace RestNet
{
    public static class Utilities
    {
        public static void SetRootAttribute(System.Xml.XmlDocument xmlDocument, string attributeName, string attributeValue)
        {

            if (xmlDocument.DocumentElement.Attributes[attributeName] == null)
                xmlDocument.DocumentElement.Attributes.Append(xmlDocument.CreateAttribute(attributeName));
            xmlDocument.DocumentElement.Attributes[attributeName].Value = attributeValue;
        }

        public static System.Xml.XmlDocument GetXmlDocumentFromFile(string filename, bool resolveExternalUrls)
        {
            string fullPath = GetFullXmlFilename(filename, "XML");
            return GetXmlDocumentFromFileByPath(fullPath, resolveExternalUrls);
        }

        public static System.Xml.XmlDocument GetXsdDocumentFromFile(string filename)
        {
            string fullPath = GetFullXmlFilename(filename, "Schema");
            return GetXmlDocumentFromFileByPath(fullPath, true);
        }

        public class NonResolvingXmlDocument : XmlDocument
        {
            public NonResolvingXmlDocument()
            {
                this.XmlResolver = null;
            }
        }

        public static System.Xml.XmlDocument GetXmlDocumentFromFileByPath(string fullPath, bool resolveExternalUrls)
        {
            System.Xml.XmlDocument xmlOutputDoc = resolveExternalUrls ? new System.Xml.XmlDocument() : new NonResolvingXmlDocument();

            using (System.IO.FileStream fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                xmlOutputDoc.Load(fs);
                fs.Close();
            }
            return xmlOutputDoc;
        }

        public static string GetXmlStringFromFile(string filename)
        {
            return ReadTextFile(GetFullXmlFilename(filename));
        }

        public static string PrettyPrint(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
                return null;

            try
            {
                using (System.IO.StringWriter sw = new System.IO.StringWriter())
                {
                    System.Xml.XmlNodeReader xmlReader = new System.Xml.XmlNodeReader(xmlDoc);
                    System.Xml.XmlTextWriter xmlWriter = new System.Xml.XmlTextWriter(sw);
                    xmlWriter.Formatting = System.Xml.Formatting.Indented;
                    xmlWriter.Indentation = 4;
                    xmlWriter.IndentChar = ' ';
                    xmlWriter.WriteNode(xmlReader, true);
                    return sw.ToString();
                }
            }
            catch (Exception)
            {
                // just return as-is
                return xmlDoc.OuterXml;
            }
        }

        public static string GetFullXmlFilename(string filename)
        {
            return GetFullXmlFilename(filename, "XML");
        }

        public static string GetFullXmlFilename(string filename, string subFolder)
        {
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
                return System.Web.Hosting.HostingEnvironment.MapPath(string.Format("~\\App_Data\\{0}\\{1}", subFolder, filename));
            else
                return System.IO.Path.Combine(Directory.GetCurrentDirectory(), string.Format("..\\..\\App_Data\\{0}\\{1}", subFolder, filename));
        }

        public static string ReadTextFile(string filename)
        {
            string cacheKey = "filecontents:" + filename;
            string fileContents = (string)Cache.Get(cacheKey);
            if (!string.IsNullOrEmpty(fileContents))
            {
                return fileContents;
            }
            fileContents = System.IO.File.ReadAllText(filename);
            string[] filenames = { filename };
            Cache.Set(cacheKey, fileContents, filenames, null);
            return fileContents;
        }

        public static void SetAttribute(System.Xml.XmlNode node, string attributeName, string attributeValue)
        {

            if (node.Attributes[attributeName] == null)
                node.Attributes.Append(node.OwnerDocument.CreateAttribute(attributeName));
            node.Attributes[attributeName].Value = attributeValue;
        }

        /// <summary>
        /// Gets variable from Request Params collection or Headers collection. 
        /// If both are provided, Params (query string, cookies, form, server variables) takes precedence, as it often is used to override headers.
        /// </summary>
        /// <param name="keyName">Name of the parameter</param>
        /// <returns>Value of the parameter</returns>
        public static string GetRequestParam(string keyName)
        {
            System.Web.HttpContext ctx = System.Web.HttpContext.Current;

            // no context, so none of this is available
            if (ctx == null)
                return null;

            string paramValue = ctx.Request.Params[keyName];
            if (paramValue != null && paramValue != string.Empty)
                return paramValue;

            string headerValue = ctx.Request.Headers[keyName];
            if (headerValue != null && paramValue != string.Empty)
                return headerValue;

            return (headerValue == null && paramValue == null) ? null : string.Empty;
        }

        public static string StreamToString(System.IO.Stream s)
        {
            using (StreamReader reader = new StreamReader(s))
            {
                return reader.ReadToEnd();
            }
        }

        /*        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, System.Xml.Xsl.XslCompiledTransform xsltDoc)
                {
                    System.IO.MemoryStream outputStream = new System.IO.MemoryStream();
                    System.Xml.XmlDocument outputDoc = new System.Xml.XmlDocument();

                    xsltDoc.Transform(xmlSourceDoc, null, outputStream);
                    if (outputStream.Length != 0)
                        outputStream.Position = 0;

                    // if it's a node, it's a partial doc
                    if (xmlSourceDoc is System.Xml.XmlDocument)
                        outputDoc.Load(outputStream);
                    else
                    {
                        byte[] buff = new byte[outputStream.Length];
                        outputStream.Read(buff, 0, buff.Length);
                        System.Text.StringBuilder sb = new StringBuilder(buff.Length);
                        System.Web.HttpContext.Current.Response.OutputStream.Write(buff, 0, buff.Length);
                        System.Web.HttpContext.Current.Response.End();
                        return null;
                        sb.Append(Encoding.ASCII.GetChars(buff));
                        sb.Replace("?", string.Empty);
                        outputDoc.LoadXml("<html>" + sb.ToString() + "</html>");
                    }
                    return outputDoc;
                }
        */

        public static System.Xml.Xsl.XslCompiledTransform GetXslCompiledTransform(string xsltFilename)
        {
            System.Xml.Xsl.XslCompiledTransform xslTransform = (System.Xml.Xsl.XslCompiledTransform)RestNet.Cache.Get(xsltFilename);
            if (xslTransform == null)
            {

                xslTransform = new System.Xml.Xsl.XslCompiledTransform();
                xslTransform.Load(new XmlTextReader(xsltFilename),  XsltSettings.TrustedXslt, new XmlUrlResolver());
                string[] fnTemp = { xsltFilename };
                RestNet.Cache.Set(xsltFilename, xslTransform, fnTemp, null, true, TimeSpan.MaxValue);
            }
            return xslTransform;
        }

        public static System.Xml.Xsl.XslCompiledTransform GetXslCompiledTransform(XmlDocument xsltDocument)
        {
            System.Xml.Xsl.XslCompiledTransform xslTransform = new System.Xml.Xsl.XslCompiledTransform();
            xslTransform.Load(xsltDocument, XsltSettings.TrustedXslt, new XmlUrlResolver());
            return xslTransform;
        }

        public static string XslTransformToString(System.Xml.XPath.IXPathNavigable xmlSourceDoc, System.Xml.Xsl.XslCompiledTransform stylesheet)
        {
            System.IO.StringWriter sResult = new System.IO.StringWriter();
            stylesheet.Transform(xmlSourceDoc, null, sResult);
            return sResult.ToString();
        }
        public static string XslTransformToString(System.Xml.XPath.IXPathNavigable xmlSourceDoc, string xsltFilename)
        {
            System.Xml.Xsl.XslCompiledTransform stylesheet = GetXslCompiledTransform(GetFullXmlFilename(xsltFilename));
            return XslTransformToString(xmlSourceDoc, stylesheet);
        }

        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, string xsltFilename)
        {
            System.Xml.Xsl.XslCompiledTransform stylesheet = GetXslCompiledTransform(GetFullXmlFilename(xsltFilename));
            return XslTransform(xmlSourceDoc, stylesheet, null);
        }

        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, string xsltFilename, XsltArgumentList xslArgs)
        {
            System.Xml.Xsl.XslCompiledTransform stylesheet = GetXslCompiledTransform(GetFullXmlFilename(xsltFilename));
            return XslTransform(xmlSourceDoc, stylesheet, xslArgs);
        }

        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, System.Xml.Xsl.XslCompiledTransform xsltDoc)
        {
            return XslTransform(xmlSourceDoc, xsltDoc, null);
        }

        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, System.Xml.Xsl.XslCompiledTransform xsltDoc, XsltArgumentList xslArgs)
        {
            System.Xml.XmlDocument outputDoc = new System.Xml.XmlDocument();
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            xsltDoc.Transform(xmlSourceDoc, xslArgs, ms);
            ms.Position = 0;

            outputDoc.Load(ms);

            return outputDoc;
        }

        public static string GetXmlNodeValue(XmlNode startingNode, string xPath)
        {
            XmlNode xmlChildNode = startingNode.SelectSingleNode(xPath);
            if (xmlChildNode == null)
                return string.Empty;
            else
                return xmlChildNode.InnerText;
        }

        /// <summary>
        /// Removes accents and converts to lowercase to create a more searchable version of text.
        /// </summary>
        /// <param name="inputString">Original string array</param>
        /// <returns>A "cleaned" lowercase version of the string array, without accents, umlauts, etc.</returns>
        public static string[] SearchableString(string[] inputString)
        {
            string[] outputString = new string[inputString.Length];
            for (int x = 0; x < outputString.Length; x++)
                outputString[x] = SearchableString(inputString[x]);
            return outputString;
        }

        /// <summary>
        /// Removes accents and converts to lowercase to create a more searchable version of text.
        /// </summary>
        /// <param name="inputString">Original string</param>
        /// <returns>A "cleaned" lowercase version of the string, without accents, umlauts, etc.</returns>
        public static string SearchableString(string inputString)
        {
            String normalizedString = inputString.Normalize(NormalizationForm.FormD).ToLower();

            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark) //UnicodeCategory.OtherPunctuation && uc != UnicodeCategory.ClosePunctuation && uc != UnicodeCategory.Control && uc != UnicodeCategory.FinalQuotePunctuation && uc != UnicodeCategory.InitialQuotePunctuation && uc != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace("  ", " ").Trim();
        }

        public static XmlDocument GetSearchableXmlDoc(XmlDocument sourceDoc)
        {
            XmlDocument outputDoc = (XmlDocument)sourceDoc.Clone();
            MakeNodeSearchable(outputDoc.DocumentElement);
            return outputDoc;
        }

        public static void MakeNodeSearchable(XmlNode thisNode)
        {
            if (thisNode.NodeType == XmlNodeType.Text)
                thisNode.InnerText = SearchableString(thisNode.InnerText);

            // deal with attributes
            if (thisNode.Attributes != null)
            {
                thisNode.Attributes.RemoveAll();
            }

            // recurse through children
            foreach (XmlNode childNode in thisNode.ChildNodes)
                MakeNodeSearchable(childNode);
        }

        public static XmlNodeList SelectNodesParameterized(XmlNode xmlSourceNode, string xPathExpression, string xPathVariableName, string xPathVariableValue)
        {
            return Mvp.Xml.Common.XPath.XPathCache.SelectNodes(xPathExpression, xmlSourceNode, new Mvp.Xml.Common.XPath.XPathVariable(xPathVariableName, xPathVariableValue));
        }

        public static XmlNodeList SelectNodesParameterized(XmlNode xmlSourceNode, string xPathExpression, System.Collections.Specialized.NameValueCollection xPathParameters)
        {
            Mvp.Xml.Common.XPath.XPathVariable[] variables = new Mvp.Xml.Common.XPath.XPathVariable[xPathParameters.Count];
            for (int x = 0; x < xPathParameters.Count; x++)
            {
                variables[x] = new Mvp.Xml.Common.XPath.XPathVariable(xPathParameters.Keys[x], xPathParameters[x]);
            }

            return Mvp.Xml.Common.XPath.XPathCache.SelectNodes(xPathExpression, xmlSourceNode, variables);
        }

        public static XmlNode SelectSingleNodeParameterized(XmlNode xmlSourceNode, string xPathExpression, string xPathVariableName, string xPathVariableValue)
        {
            return Mvp.Xml.Common.XPath.XPathCache.SelectSingleNode(xPathExpression, xmlSourceNode, new Mvp.Xml.Common.XPath.XPathVariable(xPathVariableName, xPathVariableValue));
        }

        public static XmlNode SelectSingleNodeParameterized(XmlNode xmlSourceNode, string xPathExpression, System.Collections.Specialized.NameValueCollection xPathParameters)
        {
            Mvp.Xml.Common.XPath.XPathVariable[] variables = new Mvp.Xml.Common.XPath.XPathVariable[xPathParameters.Count];
            for (int x = 0; x < xPathParameters.Count; x++)
            {
                variables[x] = new Mvp.Xml.Common.XPath.XPathVariable(xPathParameters.Keys[x], xPathParameters[x]);
            }

            return Mvp.Xml.Common.XPath.XPathCache.SelectSingleNode(xPathExpression, xmlSourceNode, variables);
        }

    }
}
