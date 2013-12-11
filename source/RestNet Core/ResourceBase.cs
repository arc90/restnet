using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Web;

namespace RestNet
{
    public abstract class ResourceBase : IHttpHandler
    {
        public ResourceBase() : this(null)
        {
        }

        public ResourceBase(HttpContextBase context)
        {
            Context = context ?? new HttpContextWrapper(HttpContext.Current);
            RegisterRepresentationTypes();
        }

        private HttpContextBase _contextBase = null;
        private RequestMethod _method;
        private DateTime _timer = DateTime.Now;
        //private int _httpStatus = 200;

        //public int HttpStatus
        //{
        //    get { return _httpStatus; }
        //    set { _httpStatus = value; }
        //}

        public abstract string Name { get; }

        //public abstract string[] ViewerRoles { get; }
        //public abstract string[] EditorRoles { get; }

        /// <summary>
        /// Returns ContentType header value without parameters
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        protected static string GetBaseContentType(string contentType)
        {
            if (contentType == null || contentType == string.Empty)
                return string.Empty;

            return contentType.Split(';')[0];
        }

        private PagingCriteria _pagingCriteria = null;

        protected void AddResponsePagingHeaders(PagingCriteria pagingCriteria)
        {
            Context.Response.AddHeader("X-Total-Item-Count", pagingCriteria.TotalItems.ToString());
            Context.Response.AddHeader("X-Items-Per-Page", pagingCriteria.ItemsPerPage.ToString());
            Context.Response.AddHeader("X-Page", pagingCriteria.Page.ToString());
        }

        protected PagingCriteria GetPagingCriteria()
        {
            if (_pagingCriteria != null)
                return _pagingCriteria;

            string pageNumberString = "0", itemsPerPageString = "0";
            int pageNumber = 0, itemsPerPage = 0;

            if (Context.Request.QueryString["page"] != null)
                pageNumberString = Context.Request.QueryString["page"];
            else
                if (Context.Request.Headers["X-Page"] != null)
                    pageNumberString = Context.Request.Headers["X-Page"];

            if (Context.Request.QueryString["itemsPerPage"] != null)
                itemsPerPageString = Context.Request.QueryString["itemsPerPage"];
            else
                if (Context.Request.Headers["X-Items-Per-Page"] != null)
                    itemsPerPageString = Context.Request.Headers["X-Items-Per-Page"];

            if (!int.TryParse(pageNumberString, out pageNumber) || !int.TryParse(itemsPerPageString, out itemsPerPage))
                throw RestNet.ErrorHandler.HttpBadRequest("One or more of the results paging parameters are not valid. 'page' or 'itemsPerPage' in the query string or 'X-Page' or 'X-Items-Per-Page' in the headers are not valid integers.");

            _pagingCriteria = new PagingCriteria(pageNumber, itemsPerPage, 0);

            return _pagingCriteria;
        }

        protected int ElapsedTime
        {
            get
            {
                return DateTime.Now.Subtract(_timer).Milliseconds;
            }
        }

        //private string[] getResourceRolePermissions(string method)
        //{
        //    return AuthUtils.getResourceRolePermissions(Name, method);
        //}

        //public bool IsUserInRole(string roleKey)
        //{

        //    return AuthUtils.IsUserInRole(roleKey);
        //}

        public static bool IsUserInRole(System.Security.Principal.IPrincipal user, string roleKey)
        {

            System.Collections.Specialized.NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            for (int x = 0; x < appSettings.Count; x++)
            {
                string thisKey = appSettings.Keys[x];
                if (string.Compare(thisKey, roleKey, true) == 0)
                {
                    string[] roleGroups = Settings.Get(thisKey).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int y = 0; y < roleGroups.Length; y++)
                    {
                        if (roleGroups[y] != null && roleGroups[y] != string.Empty)
                            if (user.IsInRole(roleGroups[y]))
                                return true;
                    }
                }
            }
            return false;
        }

        private System.Collections.Specialized.NameValueCollection _roles = new System.Collections.Specialized.NameValueCollection();

        public static string[] GetUserGroups(System.Security.Principal.IPrincipal user)
        {
            // HACK: will break when we add other RestNet principals
            if (user is RestNet.LdapPrincipal)
            {
                return ((RestNet.LdapPrincipal)user).Roles;
            }
            else
            {
                return System.Web.Security.Roles.GetRolesForUser(user.Identity.Name);
            }
        }

        public static string[] GetUserRoles(System.Security.Principal.IPrincipal user, string roleKey)
        {
            // HACK - This code is terrible. Rewrite!
            string allRoles = Settings.Get(roleKey);
            string output = string.Empty;

            //throw new Exception("roleKey:" + roleKey);

            if (allRoles != null && allRoles != string.Empty)
            {
                string[] roleArray = allRoles.Split(';');
                StringBuilder sb = new StringBuilder();
                for (int x = 0; x < roleArray.Length; x++)
                {
                    if (IsUserInRole(user, roleKey + "." + roleArray[x]))
                    {
                        //sb.AppendFormat("{0};", roleArray[x]);
                        if (sb.Length > 0)
                        {
                            sb.Append(";" + roleArray[x]);
                        }
                        else
                        {
                            sb.Append(roleArray[x]);
                        }
                    }
                }
                output = sb.ToString();
            }
            return output.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetMyRoles(string roleKey)
        {
            if (_roles[roleKey] != null)
                return _roles[roleKey].Split(';');

            _roles[roleKey] = string.Join(";", ResourceBase.GetUserRoles(User, roleKey));
            return _roles[roleKey].Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool CanUserView(string dataName)
        {
            return IsUserInRole(User, string.Format("Security.View.{0}", dataName));
        }

        public bool CanUserEdit(string dataName)
        {
            return IsUserInRole(User, string.Format("Security.Edit.{0}", dataName));
        }




        public HttpContextBase Context
        {
            get
            {
                return _contextBase;
            }
            set
            {
                if (value == null)
                    throw new HttpException(500, "No web server context. Execution cannot continue.");
                _contextBase = value;
            }
        }

        private System.Security.Principal.IPrincipal _user = null;
        /// <summary>
        /// Authenticated name of current user
        /// </summary>
        public System.Security.Principal.IPrincipal User
        {
            get
            {
                if (_user == null)
                {
                    _user = Context.User;
                }
                return _user;
            }
        }

        public RequestMethod RequestMethod
        {
            get
            {
                if (_method == 0)
                    _method = (RequestMethod)Enum.Parse(typeof(RequestMethod), _contextBase.Request.HttpMethod, true);
                return _method;
            }
        }

        private System.Collections.Generic.List<RepresentationType> _representationTypes = new List<RepresentationType>();

        # region Properties
        protected System.Collections.Generic.List<RepresentationType> RepresentationTypes
        {
            get
            {
                return _representationTypes;
            }
        }

        protected void AddRepresentationType(RequestMethod method, string contentType, string accept)
        {
            RepresentationType repType = new RepresentationType(method, contentType, accept);
            RepresentationTypes.Add(repType);
        }

        protected void AddRepresentationType(RequestMethod[] method, string[] contentType, string[] accept)
        {
            for (int m = 0; m < method.Length; m++)
            {
                for (int a = 0; a < accept.Length; a++)
                {
                    if (contentType != null)
                    {
                        for (int c = 0; c < contentType.Length; c++)
                        {
                            AddRepresentationType(method[m], contentType[c], accept[a]);
                        }
                    }
                    else
                        AddRepresentationType(method[m], null, accept[a]);
                }
            }
        }   

        protected bool HasRepresentationType(string httpMethod, string contentType, string accept)
        {
            RequestMethod method = (RequestMethod)Enum.Parse(typeof(RequestMethod), httpMethod, true);
            return HasRepresentationType(method, contentType, accept);
        }

        protected bool HasRepresentationType(RequestMethod httpMethod, string contentType, string accept)
        {   
            //bool containsTest = RepresentationTypes.Contains(new RepresentationType(httpMethod, contentType));
            for (int x = 0; x < RepresentationTypes.Count; x++)
            {
                if (httpMethod == RepresentationTypes[x].Method 
                    && (RepresentationTypes[x].ContentType == null || IsSameContentType(contentType, RepresentationTypes[x].ContentType, true))
                    && (accept == null || accept.StartsWith("*/*") || RepresentationTypes[x].Accept == null || IsSameContentType(accept, RepresentationTypes[x].Accept, true))
                    )
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if two HTTP content type headers refer to the same underlying type
        /// </summary>
        /// <param name="contentType1"></param>
        /// <param name="contentType2"></param>
        /// <returns></returns>
        /// <example>"application/x-www-form-urlencoded" and "application/x-www-form-urlencoded; charset=UTF-8" are the same content type. "charset=UTF-8" is a parameter that does not impact this comparison</example>
        protected bool IsSameContentType(string contentType1, string contentType2, bool ignoreParameters)
        {
            string[] params1 = contentType1.Split(';');
            string[] params2 = contentType2.Split(';');

            // force wildcard types to match
            if (params1[0] == "*/*")
                params2[0] = "*/*";
            else if (params2[0] == "*/*")
                params1[0] = "*/*";

            if (ignoreParameters)
            {
                return params1[0].Equals(params2[0], StringComparison.InvariantCultureIgnoreCase);
            }

            // type/subtype differs, or different number of parameters, therefore they don't match
            if(!params1[0].Equals(params2[0], StringComparison.InvariantCultureIgnoreCase) || params1.Length != params2.Length)
                return false;

            // same number of parameters, so can do everything in one loop
            SortedList<string, string> sortedParams1 = new SortedList<string, string>(params1.Length - 1);
            SortedList<string, string> sortedParams2 = new SortedList<string, string>(params2.Length - 1);

            // load into sorted list
            for (int x = 1; x < params1.Length; x++)
            {
                string[] thisParam = params1[x].Split('=');
                sortedParams1.Add(thisParam[0].Trim(), thisParam.Length == 1 ? string.Empty : thisParam[1].Trim());
                thisParam = params2[x].Split('=');
                sortedParams2.Add(thisParam[0].Trim(), thisParam.Length == 1 ? string.Empty : thisParam[1].Trim());
            }

            // if keys differ, param is missing from one of them. If values differ, param values are different
            for (int x = 0; x < sortedParams1.Count; x++)
            {
                if (!sortedParams1.Keys[x].Equals(sortedParams2.Keys[x], StringComparison.InvariantCultureIgnoreCase) || !sortedParams1.Values[x].Equals(sortedParams2.Values[x], StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        protected RepresentationType DefaultRepresentationType
        {
            get
            {
                return RepresentationTypes[0];
            }
        }

        # endregion Properties

        protected abstract void RegisterRepresentationTypes();
        public abstract int GetMyRepresentation(string representationContentType);

        /// <summary>
        /// Handles content-type negotiation for response
        /// </summary>
        /// <returns></returns>
        protected string GetResponseType(RequestMethod method)
        {
            // HACK: GetResponseType and GetBestResponseType probably suck and should be rewritten. Too scrambled to look at it anymore now
            string accept = Context.Request["ContentType"];
            if (accept != null && accept != string.Empty)
                Logging.Warn("The request parameter 'ContentType' has been deprecated. Replace with '_Accept' ASAP!");
            else
                accept = Context.Request["_Accept"];

            if (accept != null && accept != string.Empty)
            {
                // client has requested specific type
                return GetBestResponseType(method, accept);
            }

            // see if one of the Accept header values are supported
            if (Context.Request.AcceptTypes != null)
            {
                for (int x = 0; x < Context.Request.AcceptTypes.Length; x++)
                {
                    if (HasRepresentationType(Context.Request.HttpMethod, Context.Request.ContentType, Context.Request.AcceptTypes[x]))
                    {
                        // a matching representation is available, so return it
                        return GetBestResponseType(method, Context.Request.AcceptTypes[x]);
                    }
                }
            }

            // if all else fails, just return the default representation
            // TODO: Or would it be more RESTful to fail?
            return GetBestResponseType(method, RepresentationTypes[0].Accept);
        }

        private string GetBestResponseType(RequestMethod method, string acceptType)
        {
            //bool containsTest = RepresentationTypes.Contains(new RepresentationType(httpMethod, contentType));
            for (int x = 0; x < RepresentationTypes.Count; x++)
            {
                if (method == RepresentationTypes[x].Method && IsSameContentType(acceptType, RepresentationTypes[x].Accept, false))
                    return RepresentationTypes[x].Accept;
            }
            for (int x = 0; x < RepresentationTypes.Count; x++)
            {
                if (method == RepresentationTypes[x].Method && IsSameContentType(acceptType, RepresentationTypes[x].Accept, true))
                    return RepresentationTypes[x].Accept;
            }
            throw RestNet.ErrorHandler.HttpRepresentationMethodMismatch(this.Name, method.ToString(), acceptType);
        }

        //public int SendRepresentation()
        //{
        //    return SendRepresentation(GetResponseType());
        //}

        public int SendRepresentation(string representationContentType)
        {
            // allow ability to override content-type header in response
            if (Context.Request["ForceContentTypeHeader"] != null && Context.Request["ForceContentTypeHeader"].Length != 0)
                return SendRepresentation(representationContentType, Context.Request["ForceContentTypeHeader"]);
            else
                return SendRepresentation(representationContentType, representationContentType);
        }

        /// <summary>
        /// Sends a representation of the specified type back to the client's Response stream
        /// </summary>
        /// <param name="representationContentType">Representation content type requested - text/html, application/xhtml+xml, etc.</param>
        /// <param name="forceContentTypeHeader">Alternate content-type string to override the actual content type in the 'Content-Type:' header string</param>
        /// <returns>HTTP status code - 200, 404, 500, etc.</returns>
        /// <example>SendRepresentation("application/contactRecord+xml", "text/plain"). This lets browser display this XML representation as plain text.</example>
        private int SendRepresentation(string representationContentType, string forceContentTypeHeader)
        {
            GetRepresentation(representationContentType);
            return FinishResponse(forceContentTypeHeader);
        }

        protected int FinishResponse(string representationContentType)
        {
            string forceContentTypeHeader = Context.Request["ForceContentTypeHeader"];
            forceContentTypeHeader = forceContentTypeHeader == null || forceContentTypeHeader == string.Empty ? representationContentType : forceContentTypeHeader;

            //Context.Response.StatusCode = HttpStatus;
            if (!HeadersWritten)
            {
                Context.Response.StatusDescription = ErrorHandler.StatusCodeMessage(Context.Response.StatusCode);

                // if overridden already, don't change it
                if (Context.Response.ContentType == "text/html")
                    Context.Response.ContentType = forceContentTypeHeader;
            }
            // RestNet.Logging.InfoFormat("  END {0} - {1} - {2} ({3} {4})", Context.Request.HttpMethod, Context.Request.Url.AbsoluteUri, DateTime.Now.ToUniversalTime(), HttpStatus, Context.Response.StatusDescription);

            //Context.Response.End();
            return Context.Response.StatusCode;
        }

        public virtual int GetRepresentation()
        {
            // return default representation;
            return GetRepresentation(Context.Request.ContentType);
        }

        public virtual int GetRepresentation(string representationContentType)
        {
            // if no particular type requested, default to first one
            if (representationContentType == null || representationContentType == string.Empty)
                representationContentType = RepresentationTypes[0].ContentType;

            if (!HasRepresentationType(Context.Request.HttpMethod, Context.Request.ContentType, representationContentType))
            {
                throw new HttpException(415, string.Format("This resource is not configured to return a representation of type '{0}' to HTTP {1} requests.", representationContentType, Context.Request.HttpMethod));
            }

            return GetMyRepresentation(representationContentType);
        }

        protected void OutputStream(string stringRepresentation)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(stringRepresentation.Length);
            Context.Response.OutputStream.Write(System.Text.Encoding.ASCII.GetBytes(stringRepresentation.ToCharArray()), 0, stringRepresentation.Length);
        }

        protected void OutputStream(System.Xml.XmlDocument xmlDoc)
        {
            xmlDoc.Save(Context.Response.OutputStream);
        }

        //public override string ToString()
        //{
        //    return ToString(string.Empty);
        //}

        //public virtual string ToString(string representationContentType)
        //{
        //    using (System.IO.Stream s = GetRepresentation(representationContentType))
        //    {
        //        if (s == null)
        //            return string.Empty;

        //        byte[] buff = new byte[s.Length];
        //        s.Position = 0;

        //        int bytesRead = s.Read(buff, 0, buff.Length);
        //        return System.Text.Encoding.ASCII.GetString(buff);
        //    }
        //}

        private int DoHttpMethod(string methodName)
        {
            bool skipLogging = Settings.Get("RestNet.SkipRequestLoggingOn", string.Empty).Contains(this.GetType().Name);

            if (!skipLogging)
                RestNet.Logging.DebugFormat("START {0} - {1} - {2} in - {3}",
                    methodName,
                    _contextBase.Request.Url.AbsoluteUri,
                    _contextBase.Request.ContentLength,
                    DateTime.Now.ToUniversalTime());

            // default to 500. If method succeeds, will be set to something else
            int httpStatusCode = 500;
            RequestMethod method = this.RequestMethod;
            string responseType = GetResponseType(method);

            try
            {

                // CHECK TO SEE IF USER HAS PERMISSIONS TO USE THIS METHOD
                if (!AuthUtils.UserHasRightsToThisMethod(_contextBase.Request.QueryString["resourceName"], methodName, _contextBase))
                    throw ErrorHandler.HttpUnauthorized();

                switch (methodName)
                {
                    case "GET":
                        httpStatusCode = Get(responseType);
                        break;
                    case "HEAD":
                        httpStatusCode = Head();
                        break;
                    case "PUT":
                        httpStatusCode = Put(responseType);
                        break;
                    case "POST":
                        // TODO: Should PUT work the same way? This is part of the whole representation improvement refactoring (http://trac.arc/LibrariesandFrameworks/ticket/9)
                        if (HasRepresentationType(method, _contextBase.Request.ContentType, responseType))
                            httpStatusCode = Post(responseType);
                        else
                            throw RestNet.ErrorHandler.HttpRepresentationMethodMismatch(this.Name, methodName, responseType);
                        break;
                    case "OPTIONS":
                        httpStatusCode = Options();
                        break;
                    case "DELETE":
                        httpStatusCode = Delete();
                        break;
                    case "TRACE":
                        httpStatusCode = Trace();
                        break;
                    case "CONNECT":
                        httpStatusCode = Connect();
                        break;
                    default:
                        throw RestNet.ErrorHandler.HttpNotImplemented(Context.Request.HttpMethod);
                }
                //HttpStatus = httpStatusCode;
                if (!HeadersWritten)
                    Context.Response.StatusCode = httpStatusCode;
            }
            finally
            {
                if (!skipLogging)
                    RestNet.Logging.DebugFormat("END {0} - {1} - {2} - {3} - {4}",
                        methodName,
                        httpStatusCode,
                        _contextBase.Request.Url.AbsoluteUri,
                        responseType,
                        DateTime.Now.ToUniversalTime());
            }

            return httpStatusCode;
        }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        protected bool HeadersWritten { get; private set; }

        public void ProcessRequest(HttpContext context)
        {
            ProcessRequest(new HttpContextWrapper(context));
        }

        public void ProcessRequest(HttpContextBase context)
        {
            try
            {
                // will be null for HttpSimulator
                if (context.ApplicationInstance == null)
                    context.ApplicationInstance = new HttpApplication();

                context.ApplicationInstance.PreSendRequestHeaders += new EventHandler(ApplicationInstance_PreSendRequestHeaders);
                DoHttpMethod(Context.Request.HttpMethod);
            }
            catch (System.Threading.ThreadAbortException)
            {
                Context.Server.ClearError();
            }
            catch (Exception e)
            {
                RestNet.ErrorHandler.SendErrorResponse(context.Response, e);
            }
        }

        void ApplicationInstance_PreSendRequestHeaders(object sender, EventArgs e)
        {
            HeadersWritten = true;
        }

        #endregion

        # region HTTP Methods
        public virtual int Get(string responseType)
        {
            return SendRepresentation(responseType);
        }

        public virtual int Head()
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("HEAD");
        }

        public virtual int Put(string responseType)
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("PUT");
        }

        public virtual int Post(string responseType)
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("POST");
        }

        public virtual int Options()
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("OPTIONS");
        }

        public virtual int Delete()
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("DELETE");
        }

        public virtual int Delete(string requestBody, string requestType)
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("DELETE");
        }

        public virtual int Trace()
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("TRACE");
        }

        public virtual int Connect()
        {
            throw RestNet.ErrorHandler.HttpNotImplemented("CONNECT");
        }

        # endregion HTTP Methods



    }
}
