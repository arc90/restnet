using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace RestNet.Data
{
    public static class SqlXpathSearch
    {

        /// <summary>
        /// Generates an XPath string-format statement and associated list of values for use in searching XML documents
        /// </summary>
        /// <param name="oNvc">Collection of XML field names and arrays of associated values, separated by pipe</param>
        /// <param name="conj">Conjunction type - AND or OR</param>
        /// <returns>NameValueCollection with single entry. Key is the string format. Value is the pipe-delimited list of values</returns>
        public static NameValueCollection GenerateXpathStructsFromNVC(NameValueCollection oNvc, SqlFullTextConjunctionType conj)
        {
            return GenerateXpathStructsFromNVC(oNvc, conj, false);
        }

        /// <summary>
        /// Generates an XPath string-format statement and associated list of values for use in searching XML documents
        /// </summary>
        /// <param name="oNvc">Collection of XML field names and arrays of associated values, separated by pipe</param>
        /// <param name="conj">Conjunction type - AND or OR</param>
        /// <param name="reverseIntent">If true, wraps XPath in "not()" function, causing it to exclude instead of include</param>
        /// <returns>NameValueCollection with single entry. Key is the string format. Value is the pipe-delimited list of values</returns>
        public static NameValueCollection GenerateXpathStructsFromNVC(NameValueCollection oNvc, SqlFullTextConjunctionType conj, bool reverseIntent)
        {
            if (oNvc == null || oNvc.Count == 0)
                return null;

            string sConj = conj == SqlFullTextConjunctionType.AND ? "and" : "or";

            StringBuilder fields = new StringBuilder();
            StringBuilder sValues = new StringBuilder();

            int iValCount = 0;
            char[] separator = { '|' };

            for (int i = 0; i < oNvc.Count; i++)
            {

                //if (oNvc[i].Contains("|"))
                //{
                //PROC ARRAY FIELD
                StringBuilder sbThisField = new StringBuilder();
                string[] vals = oNvc[i] == null ? string.Empty.Split('|') : oNvc[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);

                if (vals.Length != 0)
                {
                    sbThisField.AppendFormat(".{0}[", oNvc.Keys[i]);
                    for (int x = 0; x < vals.Length; x++)
                    {
                        string thisValue = vals[x].Replace("\"", "&quot;");
                        int asteriskLocation = thisValue.IndexOf('*');
                        if (asteriskLocation != -1)
                        {
                            // starts with
                            //sbThisField.AppendFormat("starts-with(., \"{{{0}}}\") or ", iValCount);
                            sbThisField.AppendFormat("starts-with(translate(., \"{{{0}}}\", \"{{{1}}}\"), \"{{{1}}}\") or ", iValCount, iValCount + 1);
                            // HACK: We do *NOT* chop off asterisk, as this is fed to FTS as well. Instead, we'll chop them
                            // when we format the XPath string and its values together
                        }
                        else
                        {
                            // exact match, but still must deal with case sensitivity
                            //sbThisField.AppendFormat("text()=\"{{{0}}}\" or ", iValCount);
                            sbThisField.AppendFormat("translate(., \"{{{0}}}\", \"{{{1}}}\") = \"{{{1}}}\" or ", iValCount, iValCount + 1);
                        }
                        sValues.AppendFormat("{0}|", thisValue.ToUpper());
                        sValues.AppendFormat("{0}|", thisValue.ToLower());
                        iValCount += 2;
                    }
                    sbThisField.Length = sbThisField.Length - 4;
                    sbThisField.Append("]");
                    fields.AppendFormat("({0}) {1} ", sbThisField.ToString(), sConj);
                }
            }

            if (fields.Length > 0)
                fields.Length = fields.Length - (2 + sConj.Length);

            string xPathFormat = string.Format(reverseIntent ? "/contacts/contact[not({0})]" : "/.[{0}]", fields.ToString());

            NameValueCollection result = new NameValueCollection();

            result.Add(xPathFormat, sValues.ToString());

            return result;
        }


        public static string GenerateXpathFormat(string[] queryFields)
        {

            if (queryFields == null || queryFields.Length == 0 )
                return string.Empty;


            StringBuilder sb = new StringBuilder();
            
            for (int x = 0; x < queryFields.Length; x++)
            {
                sb.Append("//" + queryFields[x] + "/text()=\"{" + x.ToString() + "}\" and ");
            }

            if (queryFields.Length > 1)
            {
                return "(" + sb.ToString(0, sb.Length - 5) + ")";
            }
            else
            {
                return sb.ToString(0, sb.Length - 5);
            }
        }



        public static string GenerateXpathFromFormat(string xPathFormat, string[] xPathValues)
        {
            // must escape quotes
            // HACK: And we simply delete any asterisks here. See comment in GenerateXpathStructsFromNVC above for reason why.
            string[] xPathEscapedValues = new string[xPathValues.Length];
            for (int x = 0; x < xPathValues.Length; x++)
            {
                xPathEscapedValues[x] = xPathValues[x].Replace("\"", "&quot;").Replace("*", string.Empty);
            }

            return string.Format(xPathFormat, xPathEscapedValues);
        }

        public static void FilterDocOnXPath(System.Xml.XmlDocument sourceDoc, string xPathRemove)
        {
            System.Xml.XmlNodeList mismatches = sourceDoc.SelectNodes(xPathRemove);
            //RestNet.Logging.Debug(string.Format("SqlXpathSearch.FilterDocOnXPath - Removing {0} records from {1} with removal XPath [{2}]", mismatches.Count, sourceDoc.DocumentElement.ChildNodes.Count, xPathRemove));
            for (int x = 0; x < mismatches.Count; x++)
            {
                mismatches[x].ParentNode.RemoveChild(mismatches[x]);
            }
        }


    }
}
