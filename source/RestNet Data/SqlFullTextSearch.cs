using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RestNet.Data
{
    public static class SqlFullTextSearch
    {
        public static string GenerateSearchClause(string[] queryTerms, SqlFullTextConjunctionType conj)
        {
            if (queryTerms == null || queryTerms.Length == 0)
                return string.Empty;

            if (queryTerms.Length == 1)
                return queryTerms[0];

            string conjunction = Enum.GetName(typeof(SqlFullTextConjunctionType), conj).ToLower();

            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < queryTerms.Length; x++)
            {
                string thisValue = queryTerms[x].Replace("\"", string.Empty);
                if (thisValue != string.Empty)
                    sb.AppendFormat("\"{0}\" {1} ", thisValue, conjunction);
            }

            return sb.ToString(0, sb.Length == 0 ? 0 : sb.Length - conjunction.Length - 2);
        }

        public static string GetFieldSearchClause(string fieldName, string fieldValue)
        {
            return string.Format("\"{0} {1} {0}\"", fieldName, fieldValue);
        }

        public static string GetFieldSearchClause(System.Xml.XmlNode field)
        {
            return GetFieldSearchClause(field.Name, field.InnerText);
        }

        public static string GetFieldSearchClause(System.Xml.XmlNodeList fieldList, RestNet.Data.SqlFullTextConjunctionType conjunctionType)
        {
            StringBuilder sb = new StringBuilder();
            string conjunctionString = Enum.GetName(typeof(SqlFullTextConjunctionType), conjunctionType);

            if (fieldList != null)
            {
                sb.Append("(");
                for (int x = 0; x < fieldList.Count; x++)
                {
                    XmlNode thisField = fieldList[x];
                    if (thisField.InnerText != string.Empty)
                        sb.AppendFormat("{0} {1} ", GetFieldSearchClause(thisField), conjunctionString);
                }
                // remove trailing AND or OR
                if (sb.Length > conjunctionString.Length)
                    sb.Remove(sb.Length - conjunctionString.Length - 1, conjunctionString.Length+1);

                sb.Append(")");
            }
            return (sb.Length == 2) ? string.Empty : sb.ToString();
        }
    }

    public enum SqlFullTextConjunctionType
    {
        AND = 1,
        OR = 2
    }
}
