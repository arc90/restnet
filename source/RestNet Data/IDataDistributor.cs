using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using RestNet.Data;


namespace RestNet.Data
{
    public interface IDataDistributor : IDisposable
    {

        XmlDocument Create(string[] dataStores, string contentType, XmlDocument Body, string createdBy);

        XmlDocument Update(string[] dataStores, string id, string contentType, XmlDocument Body, string updatedBy);

        XmlDocument Retrieve(string[] dataStores, string id, string contentType);
        XmlDocument Retrieve(string[] dataStores, string id, string contentType, RestNet.Data.DeletedOptions deleted);

        XmlDocument Search(string[] dataStores, string qryTerm, string contentType);
        XmlDocument Search(string[] dataStores, string qryTerm, string contentType, RestNet.Data.DeletedOptions deleted);
        XmlDocument Search(string[] dataStores, NameValueCollection qryTerms, string contentType, RestNet.Data.SqlFullTextConjunctionType conjunction);
        XmlDocument Search(string[] dataStores, NameValueCollection qryTerms, string contentType, RestNet.Data.SqlFullTextConjunctionType conjunction, RestNet.Data.DeletedOptions deleted);

        void Delete(string[] dataStores, string id, string contentType, RestNet.Data.DeletedOptions deleted, string deletedBy);
    }
}
