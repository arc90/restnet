using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections.Specialized;

namespace RestNet
{
    interface IRestNetDataAccess
    {
        XmlDocument Retrieve(string connection, string id, string contentType);
        XmlDocument Retrieve(string connection, string id, string contentType, bool deleted);

        XmlDocument Search(string connection, string qryTerm, string contentType);
        XmlDocument Search(string connection, string qryTerm, string contentType, bool deleted);

        XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType);
        XmlDocument Search(string connection, NameValueCollection qryTerms, string contentType, bool deleted);

        XmlDocument Search(string connection, XmlDocument qryTerms, string contentType);
        XmlDocument Search(string connection, XmlDocument qryTerms, string contentType, bool deleted);
    }
}
