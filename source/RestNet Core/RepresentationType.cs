using System;
using System.Collections.Generic;
using System.Text;

namespace RestNet
{
    public enum RequestMethod
    {
        Get = 1,
        Head = 2,
        Post = 3,
        Put = 4,
        Options = 5,
        Delete = 6,
        Trace = 7,
        Connect = 8
    }

    public class RepresentationType
    {
        private RequestMethod _method;
        private string _contentType = null;
        private string _accept = null;

        public RepresentationType(RequestMethod method, string contentType, string accept)
        {
            if (method == RequestMethod.Post || method == RequestMethod.Put)
            {
                // must have a body, and therefore a content type
                if(contentType == null || contentType == string.Empty)
                    throw new ArgumentNullException("contentType", "Cannot create a RepresentationType for POST/PUT without specifying Content-Type");
            }
            Method = method;
            Accept = accept;
            ContentType = contentType;
        }

        public RequestMethod Method
        {
            get
            {
                return _method;
            }
            set
            {
                _method = value;
            }
        }

        public string Accept
        {
            get
            {
                return _accept;
            }
            set
            {
                _accept = value;
            }
        }

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }
    }
}
