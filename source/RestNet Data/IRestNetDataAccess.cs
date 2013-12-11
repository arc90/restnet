using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using RestNet.Data;

namespace RestNet.Data
{
    public interface IDataAccessLayer
    {

        XmlDocument Create(string connection, string contentType, XmlDocument Body, string createdBy);
        XmlDocument Create(string connection, string contentType, XmlDocument Body, string createdBy, out string id);
        XmlDocument Create(SqlTransaction oTran, string id, string contentType, XmlDocument Body, string createdBy);
        


        void Update(string connection, string id, string contentType, XmlDocument Body, string updatedBy);
        void Update(SqlTransaction oTran, string id, string contentType, XmlDocument Body, string updatedBy);


        XmlDocument Retrieve(string connection, string id, string contentType);
        XmlDocument Retrieve(string connection, string id, string contentType, DeletedOptions deleted);

        XmlDocument Retrieve(string connection, XmlDocument SearchIds, string contentType);
        XmlDocument Retrieve(string connection, XmlDocument SearchIds, string contentType, DeletedOptions deleted);


        XmlDocument Search(string connection, string query, string contentType, string rootNode, bool rootsOnly);
        XmlDocument Search(string connection, string query, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted);
        XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType, string rootNode, bool rootsOnly, SqlFullTextConjunctionType conjunction);
        XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType, string rootNode, bool rootsOnly, SqlFullTextConjunctionType conjunction, DeletedOptions deleted);
        XmlDocument Search(string connection, string xPathFormat, string[] queryValues, string contentType, string rootNode, bool rootsOnly);
        XmlDocument Search(string connection, string xPathFormat, string[] queryValues, string contentType, string rootNode, bool rootsOnly, DeletedOptions deleted);


        void Delete(string connection, string id, string contentType, DeletedOptions deleted, string deletedBy);
        void Delete(SqlTransaction oTran, string id, string contentType, DeletedOptions deleted, string deletedBy);

        //XmlDocument Head(string connection, string id, string contentType);
    

    }
}
