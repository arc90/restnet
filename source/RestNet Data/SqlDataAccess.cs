using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Xml;
using System.Collections.Specialized;
using RestNet.Data;

namespace RestNet.Data
{
    public class SqlDataAccessLayer: IDataAccessLayer, IDisposable
    {

        #region PUBLIC INTERFACE


        //public static string createSqlConnectionString(string dbserver, string dbvolume, string dbuser, string dbpwd)
        //{
        //    string rtn = "Server=" + dbserver + ";Database=" + dbvolume + ";" +
        //                "uid=" + dbuser + ";pwd=" + dbpwd + ";Trusted_Connection=false";

        //    return rtn;
        //}

        //public static string createSqlConnectionString(string dbserver, string dbvolume, string dbuser, string dbpwd, bool trusted)
        //{
            

        //    string rtn = "Server=" + dbserver + ";Database=" + dbvolume + ";" +
        //                "uid=" + dbuser + ";pwd=" + dbpwd + ";Trusted_Connection=" + trusted.ToString();

        //    return rtn;
        //}


        public XmlDocument Create(string connection, string contentType, XmlDocument body, string createdBy)
        {
            return insertXmlRecord(connection, contentType, body, createdBy);
        }

        public XmlDocument Create(string connection, string contentType, XmlDocument body, string createdBy, out string id)
        {
            XmlDocument xd = insertXmlRecord(connection, contentType, body, createdBy);

            if (xd != null)
            {
                id = xd.Attributes["id"].Value.ToString();
            }
            else
            {
                id = string.Empty;
            }

            return xd;
        }

        public XmlDocument Create(SqlTransaction oTran, string id, string contentType, XmlDocument body, string createdBy)
        {
            int keyID = 0;
            if (!int.TryParse(id,out keyID))
            {
                // SHOULD WE ASSUME NEW CORE THING HERE? OR SHOULD WE THROW AN EXCEPTION?
            }
            XmlDocument xd = insertXmlRecord(oTran, keyID, contentType, body, createdBy);
            return xd;
        }


        public XmlDocument Assert(SqlTransaction oTran, ref int id, string contentType, XmlDocument body, string assertedBy)
        {
            return assertXmlRecord(oTran, ref id, contentType, body, assertedBy);
        }

        public void Update(string connection, string id, string contentType, XmlDocument xmlData, string updatedBy)
        {
            updateXmlRecord(connection, id, contentType, xmlData, updatedBy);
        }

        public void Update(SqlTransaction oTran, string id, string contentType, XmlDocument xmlData, string updatedBy)
        {
            updateXmlRecord(oTran, id, contentType, xmlData, updatedBy);
        }


        public XmlDocument Retrieve(string connection, string id, string contentType)
        {
            return getXmlRecord(connection, id, contentType, DeletedOptions.notDeleted );
        }

        public XmlDocument Retrieve(string connection, string id, string contentType, DeletedOptions deleted)
        {
            return getXmlRecord(connection, id, contentType, deleted);
        }


        public XmlDocument Retrieve(string connection, XmlDocument SearchIds, string contentType)
        {
            return getXmlRecords(connection, SearchIds, contentType, DeletedOptions.notDeleted);
        }

        public XmlDocument Retrieve(string connection, XmlDocument SearchIds, string contentType, DeletedOptions deleted)
        {
            return getXmlRecords(connection, SearchIds, contentType, deleted);
        }



        public XmlDocument Search(string connection, string query, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted)
        {
            return searchXmlRecord(connection, query, contentType, rootNode, rootsOnly, deleted);
        }

        public XmlDocument Search(string connection, string query, string contentType, string rootNode, bool rootsOnly)
        {
            return searchXmlRecord(connection, query, contentType, rootNode, rootsOnly, DeletedOptions.notDeleted);
        }

        public XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType, string rootNode, bool rootsOnly, SqlFullTextConjunctionType conjunction)
        {
            return searchXmlRecord(connection, qryTerms, contentType, rootNode, rootsOnly, conjunction, DeletedOptions.notDeleted);
        }

        public XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType, string rootNode, bool rootsOnly, SqlFullTextConjunctionType conjunction, DeletedOptions deleted)
        {
            return searchXmlRecord(connection, qryTerms, contentType, rootNode, rootsOnly, conjunction, deleted);
        }

        public XmlDocument Search(string connection, string xPathFormat, string[] queryValues, string contentType, string rootNode, bool rootsOnly)
        {
            return searchXmlRecord(connection, xPathFormat, queryValues, contentType, rootNode, rootsOnly, DeletedOptions.notDeleted);
        }

        public XmlDocument Search(string connection, string xPathFormat, string[] queryValues, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted)
        {
            return searchXmlRecord(connection, xPathFormat, queryValues, contentType, rootNode, rootsOnly, deleted);
        }

        public void Delete(string connection, string id, string contentType, DeletedOptions deleted, string deletedBy)
        {
            deleteXmlRecord(connection, id, contentType, deleted, deletedBy);
        }

        public void Delete(SqlTransaction oTran, string id, string contentType, DeletedOptions deleted, string deletedBy)
        {
            deleteXmlRecord(oTran, id, contentType, deleted, deletedBy);
        }

        //public static XmlDocument HEAD(string connection, string id, string contentType)
        //{
            
        //    // TODO
        //    throw new Exception("HEAD is not yet implemented");
        //}

        #endregion


        #region PRIVATE METHODS

        private static XmlDocument insertXmlRecord(string connection, string contentType, XmlDocument newXd, string createdBy)
        {
            if (connection == null || connection.Length == 0)
                throw new Exception("Database connection string was not provided.");

            using (SqlConnection conn = new SqlConnection(connection))
                insertXmlRecord(new SqlCommand("createXmlContent", conn), 0, contentType, newXd, createdBy);

            return newXd;

        }

        private XmlDocument assertXmlRecord(SqlTransaction oTran, ref int id, string contentType, XmlDocument body, string assertedBy)
        {
            if (oTran == null)
            {
                throw new Exception("Transaction object cannot be null");
            }

            if (contentType.Length <= 0)
            {
                throw new Exception("contentType must be specified");
            }

            try
            {
                using (SqlCommand oCmd = new SqlCommand("assertXmlContent", oTran.Connection))
                {
                    oCmd.Transaction = oTran;
                    oCmd.CommandType = CommandType.StoredProcedure;

                    oCmd.Parameters.Add("@id", SqlDbType.Int);
                    oCmd.Parameters["@id"].Direction = ParameterDirection.InputOutput;

                    if (id > 0)
                        oCmd.Parameters["@id"].Value = id;

                    oCmd.Parameters.AddWithValue("@contentType", contentType);
                    if (body != null)
                    {
                        AddXmlParameter("@xmlData", oCmd, body);
                        AddXmlParameter("@xmlSearch", oCmd, RestNet.Utilities.GetSearchableXmlDoc(body));
                    }
                    else
                    {
                        oCmd.Parameters.AddWithValue("@xmlData", DBNull.Value);
                        oCmd.Parameters.AddWithValue("@xmlSearch", DBNull.Value);
                    }

                    oCmd.Parameters.AddWithValue("@assertedBy", assertedBy);
                    oCmd.Parameters.Add("@assertedAt", SqlDbType.DateTime);
                    oCmd.Parameters["@assertedAt"].Direction = ParameterDirection.Output;


                    //oCmd.Connection.Open();
                    oCmd.ExecuteNonQuery();

                    if (id <= 0)
                    {
                        id = (int)oCmd.Parameters["@ID"].Value;
                    }

                    SetRootAttribute(body, "id", id.ToString());
//                    SetRootAttribute(newXd, "createdBy", createdBy);
//                    SetRootAttribute(newXd, "updatedBy", createdBy);
                    //SetRootAttribute(newXd, "createdAt", XmlConvert.ToString((DateTime)oCmd.Parameters["@createdAt"].Value, XmlDateTimeSerializationMode.Local));
                    //SetRootAttribute(newXd, "updatedAt", XmlConvert.ToString((DateTime)oCmd.Parameters["@createdAt"].Value, XmlDateTimeSerializationMode.Local));
//                    SetRootAttribute(newXd, "createdAt", ((DateTime)oCmd.Parameters["@createdAt"].Value).ToUniversalTime().ToString("s"));
//                    SetRootAttribute(newXd, "updatedAt", ((DateTime)oCmd.Parameters["@createdAt"].Value).ToUniversalTime().ToString("s"));

                    //oCmd.Connection.Close();
                }
                return body; ;


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private static XmlDocument insertXmlRecord(SqlTransaction oTran, int id, string contentType, XmlDocument newXd, string createdBy)
        {
            if (oTran == null)
                throw new Exception("Transaction object cannot be null");

            insertXmlRecord(new SqlCommand("createXmlContent", oTran.Connection, oTran), id, contentType, newXd, createdBy);
            return newXd;
        }

        private static void insertXmlRecord(SqlCommand oCmd, int id, string contentType, XmlDocument newXmlDoc, string createdBy)
        {
            if (contentType == null || contentType.Length == 0)
                throw new Exception("No content-type was specified to update.");

            if (newXmlDoc == null)
                throw new Exception("xml body cannot be null");

            using (oCmd)
            {
                oCmd.CommandType = CommandType.StoredProcedure;

                oCmd.Parameters.AddWithValue("@contentType", contentType);
                AddXmlParameter("@xmlData", oCmd, newXmlDoc);
                AddXmlParameter("@xmlSearch", oCmd, RestNet.Utilities.GetSearchableXmlDoc(newXmlDoc));
                oCmd.Parameters.AddWithValue("@createdBy", createdBy);
                oCmd.Parameters.Add("@createdAt", SqlDbType.DateTime);
                oCmd.Parameters["@createdAt"].Direction = ParameterDirection.Output;

                oCmd.Parameters.Add("@id", SqlDbType.Int);
                if (id > 0)
                    oCmd.Parameters["@id"].Value = id;
                else
                    oCmd.Parameters["@id"].Direction = ParameterDirection.Output;

                oCmd.ExecuteNonQuery();

                if (id <= 0)
                    id = (int)oCmd.Parameters["@ID"].Value;

                SetRootAttribute(newXmlDoc, "id", id.ToString());
                SetRootAttribute(newXmlDoc, "createdBy", createdBy);
                SetRootAttribute(newXmlDoc, "updatedBy", createdBy);
                SetRootAttribute(newXmlDoc, "createdAt", ((DateTime)oCmd.Parameters["@createdAt"].Value).ToUniversalTime().ToString("s"));
                SetRootAttribute(newXmlDoc, "updatedAt", ((DateTime)oCmd.Parameters["@createdAt"].Value).ToUniversalTime().ToString("s"));
            }
        }

        private static void SetRootAttribute(XmlDocument newXd, string attributeName, string attributeValue)
        {
            if (newXd == null)
                return;

            if (newXd.DocumentElement.Attributes[attributeName] == null)
                newXd.DocumentElement.Attributes.Append(newXd.CreateAttribute(attributeName));
            newXd.DocumentElement.Attributes[attributeName].Value = attributeValue;
        }

        private static bool deleteXmlRecord(string connection, string keyId, string contentType, DeletedOptions deleted, string updatedBy)
        {
            bool retVal = false;
            try
            {
                using (SqlCommand oCmd = new SqlCommand("deleteXmlContent", new SqlConnection(connection)))
                {
                    oCmd.CommandType = CommandType.StoredProcedure;
                    oCmd.Parameters.AddWithValue("@id", keyId);
                    oCmd.Parameters.AddWithValue("@deleted", deleted);
                    oCmd.Parameters.AddWithValue("@updatedBy", updatedBy);
                    oCmd.Connection.Open();

                    retVal = oCmd.ExecuteNonQuery() != 0;
                    oCmd.Connection.Close();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        private static bool deleteXmlRecord(SqlTransaction oTran, string keyId, string contentType, DeletedOptions deleted, string updatedBy)
        {
            bool retVal = false;
            try
            {
                using (SqlCommand oCmd = new SqlCommand("deleteXmlContent", oTran.Connection))
                {
                    oCmd.Transaction = oTran;
                    oCmd.CommandType = CommandType.StoredProcedure;
                    oCmd.Parameters.AddWithValue("@id", keyId);
                    oCmd.Parameters.AddWithValue("@deleted", deleted);
                    oCmd.Parameters.AddWithValue("@updatedBy", updatedBy);

                    retVal = oCmd.ExecuteNonQuery() != 0;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        private static void updateXmlRecord(string connection, string keyId, string contentType, XmlDocument oXd, string updatedBy)
        {
            if (connection == null || connection.Length == 0)
                throw new Exception("Database connection string was not provided.");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                updateXmlRecord(new SqlCommand("updateXmlContent", conn), keyId, contentType, oXd, updatedBy);
                conn.Close();
            }
        }


        private static void updateXmlRecord(SqlTransaction oTran, string keyId, string contentType, XmlDocument oXd, string updatedBy)
        {
            if (oTran == null)
                throw new Exception("Database transaction was not provided.");

            updateXmlRecord(new SqlCommand("updateXmlContent", oTran.Connection, oTran), keyId, contentType, oXd, updatedBy);
        }

        private static void updateXmlRecord(SqlCommand oCmd, string keyId, string contentType, XmlDocument oXd, string updatedBy)
        {
            if (keyId == null || keyId.Length == 0)
                throw new Exception("No ID was specified indicating record to update.");

            if (contentType == null || contentType.Length == 0)
                throw new Exception("No content-type was specified to update.");

            using (oCmd)
            {
                oCmd.CommandType = CommandType.StoredProcedure;

                oCmd.Parameters.AddWithValue("@id", keyId);
                oCmd.Parameters.AddWithValue("@contentType", contentType);
                AddXmlParameter("@xmlData", oCmd, oXd);
                AddXmlParameter("@xmlSearch", oCmd, RestNet.Utilities.GetSearchableXmlDoc(oXd));
                oCmd.Parameters.AddWithValue("@updatedBy", updatedBy);
                oCmd.Parameters.Add("@updatedAt", SqlDbType.DateTime);
                oCmd.Parameters["@updatedAt"].Direction = ParameterDirection.Output;

                oCmd.ExecuteNonQuery();

                SetRootAttribute(oXd, "id", keyId);
                SetRootAttribute(oXd, "updatedBy", updatedBy);
                SetRootAttribute(oXd, "updatedAt", XmlConvert.ToString((DateTime)oCmd.Parameters["@updatedAt"].Value, XmlDateTimeSerializationMode.Local));
            }
        }

        private static void AddXmlParameter(string paramName, SqlCommand oCmd, XmlDocument oXd)
        {
            using (XmlNodeReader xnr = new XmlNodeReader(oXd))
            {
                System.Data.SqlTypes.SqlXml sx = new System.Data.SqlTypes.SqlXml(xnr);

                oCmd.Parameters.Add(paramName, SqlDbType.Xml);
                oCmd.Parameters[paramName].Value = sx;
            }
        }


        private static XmlDocument searchXmlRecord(string connection, string query, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted)
        {
            if (connection == null || connection.Length <= 0)
            {
                throw new Exception("missing or invalid connection string");
            }
            if (query == null)
            {
                query = string.Empty;
            }
            if (contentType.Length == 0)
            {
                throw new Exception("contentType must be specified");
            }

            // HACK
            if (deleted == RestNet.Data.DeletedOptions.all)
            {
                throw new Exception("return deleted and non-deleted (all) not implemented");
            }

            try
            {
                query = RestNet.Utilities.SearchableString(query);
                Ewbi.FullTextSearch ftsHelper = new Ewbi.FullTextSearch(query, Ewbi.FullTextSearchOptions.None);


                string SQL = "searchXmlContent";
                StringBuilder sb = new StringBuilder();
                XmlDocument xdOutput = new XmlDocument { XmlResolver = null };

                using (SqlCommand oCmd = new SqlCommand(SQL, new SqlConnection(connection)))
                {
                    oCmd.CommandType = CommandType.StoredProcedure;
                    oCmd.Parameters.AddWithValue("@contentType", contentType);
                    oCmd.Parameters.AddWithValue("@rootsOnly", rootsOnly);

                    oCmd.Parameters.AddWithValue("@query", ftsHelper.NormalForm);
                    oCmd.Parameters.AddWithValue("@deleted", deleted);
                    oCmd.Connection.Open();

                    System.Data.SqlClient.SqlDataReader oRdr = oCmd.ExecuteReader();


                    if (!rootsOnly)
                    {
                        // SPROC RETURNS RECORD SET OF 1 COL WITH XML DOC IN EACH COLUMN
                        sb.AppendFormat("<{0} query=\"{1}\">", rootNode, query);
                        //xdOutput.AppendChild(xdOutput.CreateElement(rootNode));
                        while (oRdr.Read())
                        {
                            sb.Append(oRdr.GetValue(0));
                        }
                        sb.AppendFormat("</{0}>", rootNode);
                    }
                    else
                    {
                        // SPROC RETURNS WELL-FORMED XML DOC IN COL 0 WHICH MIGHT SPAN MULTIPLE ROWS

                        while (oRdr.Read())
                        {
                            sb.Append(oRdr.GetValue(0));
                        }

                    }


                    oRdr.Close();
                    oCmd.Connection.Close();
                }

                if (sb.Length == 0)
                {
                    sb.Append("<" + rootNode + "/>");
                }
                xdOutput.LoadXml(sb.ToString());


                // save the query
                xdOutput.DocumentElement.Attributes.Append(xdOutput.CreateAttribute("query"));
                xdOutput.DocumentElement.Attributes["query"].Value = query;

                xdOutput.DocumentElement.Attributes.Append(xdOutput.CreateAttribute("results"));
                xdOutput.DocumentElement.Attributes["results"].Value = xdOutput.DocumentElement.ChildNodes.Count.ToString();

                return xdOutput;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static XmlDocument searchXmlRecord(string connection, string xPathFormat, string[] queryValues, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted)
        {
            if (connection == null || connection.Length <= 0)
            {
                throw new Exception("missing or invalid connection string");
            }

            if (xPathFormat == null || xPathFormat.Length == 0)
            {
                throw new Exception("missing or invalid xPath input");
            }

            if (queryValues == null || queryValues.Length == 0)
            {
                throw new Exception("missing or invalid query values input");
            }

            if (contentType.Length == 0)
            {
                throw new Exception("missing or invalid query parameters collection");
            }

            // HACK
            if (deleted == RestNet.Data.DeletedOptions.all)
            {
                throw new Exception("return deleted and non-deleted (all) not implemented");
            }

            

            string SQL = "searchXmlContentXpath";
            StringBuilder sb = new StringBuilder();
            XmlDocument xdOutput = new XmlDocument { XmlResolver = null };

            // convert to lowercase and remove accents
            string[] cleanQueryValues = RestNet.Utilities.SearchableString(queryValues);
            string finalXPath = SqlXpathSearch.GenerateXpathFromFormat(xPathFormat, cleanQueryValues);

            System.Collections.Specialized.NameValueCollection ftsTerms = new NameValueCollection();
            for (int x = 0; x < cleanQueryValues.Length; x++)
                cleanQueryValues[x] = string.Format("\"{0}\"", cleanQueryValues[x]);
            char[] tab = { '\t' };
            string ftsRawSearch = string.Join(" or ", string.Join("\t", cleanQueryValues).Replace("\"\"", string.Empty).Split(tab, StringSplitOptions.RemoveEmptyEntries));
            Ewbi.FullTextSearch ftsHelper = new Ewbi.FullTextSearch(ftsRawSearch, Ewbi.FullTextSearchOptions.None);

            using (SqlCommand oCmd = new SqlCommand(SQL, new SqlConnection(connection)))
            {
                oCmd.CommandType = CommandType.StoredProcedure;
                oCmd.Parameters.AddWithValue("@contentType", contentType);
                oCmd.Parameters.AddWithValue("@rootsOnly", rootsOnly);

                oCmd.Parameters.AddWithValue("@contains", ftsHelper.NormalForm);
                oCmd.Parameters.AddWithValue("@xPath", string.Empty); //finalXPath);

                oCmd.Parameters.AddWithValue("@deleted", deleted);
                oCmd.Connection.Open();

                System.Data.SqlClient.SqlDataReader oRdr = oCmd.ExecuteReader();


                if (!rootsOnly)
                {
                    // SPROC RETURNS RECORD SET OF 1 COL WITH XML DOC IN EACH COLUMN
                    // TODO: Escape quotes so this is valid XML
                    sb.AppendFormat("<{0} query=\"{1}\">", rootNode, SqlXpathSearch.GenerateXpathFromFormat(xPathFormat, cleanQueryValues));
                    //xdOutput.AppendChild(xdOutput.CreateElement(rootNode));
                    while (oRdr.Read())
                    {
                        sb.Append(oRdr.GetValue(0));
                    }
                    sb.AppendFormat("</{0}>", rootNode);
                }
                else
                {
                    // SPROC RETURNS WELL-FORMED XML DOC IN COL 0 WHICH MIGHT SPAN MULTIPLE ROWS

                    while (oRdr.Read())
                    {
                        sb.Append(oRdr.GetValue(0));
                    }

                }


                oRdr.Close();
                oCmd.Connection.Close();
            }

            if (sb.Length == 0)
            {
                sb.Append("<" + rootNode + "/>");
            }
            xdOutput.LoadXml(sb.ToString());

            RestNet.Logging.DebugFormat("SqlDataAccess.searchXmlRecord - {6} records\r\nSearch SQL: {0} @contentType='{1}', @rootsOnly='{2}', @contains='{3}', @xPath='{4}', @deleted={5}\r\nSearch Results:{7}",
                SQL, contentType, rootsOnly, ftsHelper.NormalForm, string.Empty, deleted, xdOutput.DocumentElement.ChildNodes.Count, xdOutput.OuterXml);

            return xdOutput;

        }

        private static XmlDocument searchXmlRecord(string connection, NameValueCollection queryParams, string contentType, string rootNode, bool rootsOnly, SqlFullTextConjunctionType conjunction, DeletedOptions deleted)
        {

            if (queryParams == null || queryParams.Count == 0)
                throw new Exception("No search parameters were provided.");

            NameValueCollection oNvc = SqlXpathSearch.GenerateXpathStructsFromNVC(queryParams, conjunction);
            if (oNvc == null)
                throw new Exception("Error generating search parameter collection.");

            return searchXmlRecord(connection, oNvc.Keys[0], oNvc[0].Split('|'), contentType, rootNode, rootsOnly, deleted);
        }



        private static XmlDocument getXmlRecord(string connection, string keyId, string contentType, DeletedOptions deleted)
        {

            if (connection.Length <= 0)
            {
                throw new Exception("missing or invalid connection string");
            }
            if (keyId.Length <= 0)
            {
                throw new Exception("missing or invalid keyID");
            }
            if (contentType.Length <= 0)
            {
                throw new Exception("contentType must be specified");
            }

            try
            {
                XmlDocument xdOutput = new XmlDocument { XmlResolver = null };
                StringBuilder sb = new StringBuilder();

                using (SqlCommand oCmd = new SqlCommand("getXmlRecord", new SqlConnection(connection)))
                {
                    oCmd.CommandType = CommandType.StoredProcedure;

                    oCmd.Parameters.AddWithValue("@id", keyId);
                    oCmd.Parameters.AddWithValue("@contentType", contentType);
                    oCmd.Parameters.AddWithValue("@deleted", deleted);

                    oCmd.Connection.Open();

                    System.Data.SqlClient.SqlDataReader oRdr = oCmd.ExecuteReader();



                    if (!oRdr.HasRows)
                    {
                        return null;
                    }

                    while (oRdr.Read())
                    {
                        System.Data.SqlTypes.SqlXml sx = oRdr.GetSqlXml(0);
                        //XmlReader xr = sx.CreateReader();
                        //xr.Read();

                        sb.Append(sx.Value);
                    }


                    oRdr.Close();
                    oCmd.Connection.Close();
                }

                xdOutput.LoadXml(sb.ToString());

                return xdOutput;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private static XmlDocument getXmlRecords(string connection, XmlDocument searchIds, string contentType, DeletedOptions deleted)
        {

            if (connection == null || connection.Length <= 0)
            {
                throw new Exception("missing or invalid connection string");
            }
            if (searchIds == null)
            {
                throw new Exception("missing or invalid searchIds Xml");
            }
            if (contentType.Length <= 0)
            {
                throw new Exception("contentType must be specified");
            }

            XmlDocument xdOutput = new XmlDocument { XmlResolver = null };
            string rootNode = contentType + "s";

            // no IDs, so save time by aborting now
            if (!searchIds.DocumentElement.HasChildNodes)
            {
                xdOutput.LoadXml(string.Format("<{0}/>", rootNode));
                return xdOutput;
            }

            try
            {
                StringBuilder sbXml = new StringBuilder();

                using (SqlCommand oCmd = new SqlCommand("getXmlRecords", new SqlConnection(connection)))
                {
                    oCmd.CommandType = CommandType.StoredProcedure;

                    using (XmlNodeReader xnr = new XmlNodeReader(searchIds))
                    {
                        SqlXml sx = new SqlXml(xnr);
                        
                        oCmd.Parameters.Add("@ids", SqlDbType.Xml);
                        oCmd.Parameters["@ids"].Value = sx;
                    }

                    oCmd.Parameters.AddWithValue("@contentType", contentType);
                    oCmd.Parameters.AddWithValue("@version", null);
                    oCmd.Parameters.AddWithValue("@deleted", deleted);

                    oCmd.Connection.Open();

                    System.Data.SqlClient.SqlDataReader oRdr = oCmd.ExecuteReader();

                    if (!oRdr.HasRows)
                    {
                        sbXml.Append("<" + rootNode + "/>");
                    }
                    else
                    {
                        sbXml.Append("<" + rootNode + ">");
                        while (oRdr.Read())
                        {
                            System.Data.SqlTypes.SqlXml sx = oRdr.GetSqlXml(0);
                            //XmlReader xr = sx.CreateReader();
                            //xr.Read();

                            sbXml.Append(sx.Value);
                        }
                        sbXml.Append("</" + rootNode + ">");

                        oRdr.Close();
                        oCmd.Connection.Close();
                    }
                }

                xdOutput.LoadXml(sbXml.ToString());

                return xdOutput;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static int GetScalarOut(string connection, SqlCommand cmd, string outParamName)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.Parameters.Add(new SqlParameter(outParamName, 0));
                cmd.Parameters[outParamName].Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                int outParamValue = cmd.Parameters[outParamName].Value == System.DBNull.Value ? 0 : Convert.ToInt32(cmd.Parameters[outParamName].Value);
                conn.Close();
                return outParamValue;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            //throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
