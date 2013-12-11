using System;
using System.Collections.Generic;
using System.Text;

namespace RestNet
{
    public class Utilities
    {
        public static void HandleErrors(System.Web.HttpResponse response, Exception e)
        {
            RestNet.ErrorHandler.SendErrorResponse(response, e);
        }

        public static void HandleErrors(System.Web.HttpResponse response, Exception e, string errorTemplate)
        {

        }

        public static void SetRootAttribute(System.Xml.XmlDocument xmlDocument, string attributeName, string attributeValue)
        {

            if (xmlDocument.DocumentElement.Attributes[attributeName] == null)
                xmlDocument.DocumentElement.Attributes.Append(xmlDocument.CreateAttribute(attributeName));
            xmlDocument.DocumentElement.Attributes[attributeName].Value = attributeValue;
        }

        public static System.Xml.XmlDocument GetXmlDocumentFromFile(string filename)
        {
            System.Xml.XmlDocument xmlOutputDoc = new System.Xml.XmlDocument();
            xmlOutputDoc.Load(GetFullXmlFilename(filename));
            return xmlOutputDoc;
        }

        public static string GetXmlStringFromFile(string filename)
        {
            return ReadTextFile(GetFullXmlFilename(filename));
        }

        public static string GetFullXmlFilename(string filename)
        {
            return System.Web.HttpContext.Current.Server.MapPath(string.Format("~/bin/xml/{0}", filename));
        }

        public static string ReadTextFile(string filename)
        {
            return System.IO.File.ReadAllText(filename);
        }

        public static void SetAttribute(System.Xml.XmlNode node, string attributeName, string attributeValue)
        {

            if (node.Attributes[attributeName] == null)
                node.Attributes.Append(node.OwnerDocument.CreateAttribute(attributeName));
            node.Attributes[attributeName].Value = attributeValue;
        }

        public static string StreamToString(System.IO.Stream s)
        {
            const int chunkSize = 262144;
            int bytesRead = 0;
            string outputString = string.Empty;

            byte[] buff = new byte[chunkSize];

            s.Position = 0;
            while (s.Position < s.Length)
            {
                bytesRead = s.Read(buff, 0, buff.Length);
                outputString += System.Text.Encoding.ASCII.GetString(buff);
            }
            return outputString;
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
        public static System.Xml.XmlDocument XslTransform(System.Xml.XPath.IXPathNavigable xmlSourceDoc, System.Xml.Xsl.XslCompiledTransform xsltDoc)
        {
            System.IO.StringWriter sResult = new System.IO.StringWriter();
            System.Xml.XmlDocument outputDoc = new System.Xml.XmlDocument();


            xsltDoc.Transform(xmlSourceDoc, null, sResult);


            outputDoc.LoadXml(sResult.ToString());

            return outputDoc;
        }



    }
}
