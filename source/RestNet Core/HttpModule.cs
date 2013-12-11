using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Reflection;
using System.Security;
using System.Security.Principal;


namespace RestNet
{

    public class HttpModule : IHttpModule
    {
        private Persephone.Processing.Pipeline.PipelineManager<IRestNetAuth> authClass = null;
        private IPrincipal _userPrincipal = null;
        private System.Collections.Specialized.NameValueCollection customHeaders = new System.Collections.Specialized.NameValueCollection();

        public HttpModule()
        { }


        #region IHttpModule Members

        public void Dispose()
        {
            // TODO - DO NOTHING FOR NOW
            //throw new Exception("The method or operation is not implemented.");
        }

        public void Init(HttpApplication context)
        {

            context.AuthenticateRequest += new EventHandler(this.OnAuthenticateRequest);
            context.PostAuthenticateRequest += new EventHandler(this.OnPostAuthenticateRequest);

            context.BeginRequest += new EventHandler(this.OnBeginRequest);

            context.EndRequest += new EventHandler(this.OnEndRequest);
            context.Error += new EventHandler(this.OnError);

            context.PreSendRequestHeaders += new EventHandler(this.OnPreSendRequestHeaders);
        }

        void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            HttpResponse response = ((HttpApplication)sender).Context.Response;
            HttpRequest request = ((HttpApplication)sender).Context.Request;

            if (!UseCrippledClientHeaders(request))
                return;

            response.AddHeader("X-Crippled-Client", "true");

            if (UseCrippledClientEnvelope(request))
            {
                response.AddHeader("X-Crippled-Client-Envelope", "true");
            }

            customHeaders["X-True-Status-Code"] = response.StatusCode.ToString();
            customHeaders["X-True-Status-Text"] = response.StatusDescription;

            for (int x = 0; x < customHeaders.Count; x++)
                response.AddHeader(customHeaders.AllKeys[x], customHeaders[x]);

            response.StatusCode = 200;
        }

        void OnPostAuthenticateRequest(object source, EventArgs eventArgs)
        {
            if (_userPrincipal != null)
            {
                HttpApplication app = (HttpApplication)source;
                app.Context.User = _userPrincipal;
            }
        }

        public void OnError(object source, EventArgs eventArgs)
        {
            HttpApplication app = (HttpApplication)source;
            Exception ex = app.Server.GetLastError();
            while (ex != null && ex is TargetInvocationException)
                ex = ex.InnerException;

            ErrorHandler.SendErrorResponse(new HttpResponseWrapper(app.Response), ex);
            app.Server.ClearError();
        }

        public void OnAuthenticateRequest(object source, EventArgs eventArgs)
        {
            HttpApplication app = (HttpApplication)source;

                
            string authStr = app.Request.Headers["Authorization"];
            string username = string.Empty;
            string password = string.Empty;

            if (authStr == null || authStr.Length == 0)
            {
                // Check to see if anonymous access is allowed
                if (!AllowAnonymousAccess(app))
                    DenyAccess(app);

                // If we survive to here, anonymous access to current method of this resource is allowed
                return;
            }
            else
            {

                // LOOK FOR BASIC AUTH STRING
                authStr = authStr.Trim();
                if (authStr.IndexOf("Basic", 0) != 0)
                {
                    // Don't understand this header...we'll pass it along and 
                    // assume someone else will handle it
                    DenyAccess(app);
                    return;
                }

                // DECRYPT AND UNSPIN USER AND PASSWORD
                string encodedCredentials = authStr.Substring(6);

                byte[] decodedBytes = Convert.FromBase64String(encodedCredentials);
                string s = new ASCIIEncoding().GetString(decodedBytes);

                string[] userPass = s.Split(new char[] { ':' });

                username = userPass[0];
                password = userPass[1];
            }


            string cacheKey = string.Format("authentication.principal.{0}.{1}", username, password.GetHashCode());
            _userPrincipal = (GenericPrincipal)Cache.Get(cacheKey);
            if (_userPrincipal == null)
            {

                // USE THE PIPELINE TO INSTANCE THE RestNet_Authentication CLASS
                if (authClass == null)
                    GetAuthClass();

                try
                {
                    _userPrincipal = authClass.ProcessAuthentication(username, password);
                    //app.Context.User = _userPrincipal;
                    if (!_userPrincipal.Identity.IsAuthenticated)
                        DenyAccess(app);
                }
                catch (HttpException ex)
                {
                    DenyAccess(app, ex);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // DenyAccess already killed the request
                    return;
                }
                catch (Exception ex)
                {
                    RestNet.ErrorHandler.SendErrorResponse(app.Context.Response, ex);
                }

                if(_userPrincipal != null)
                    Cache.Set(cacheKey, _userPrincipal, null, null, true, TimeSpan.FromMinutes(15));
            }
        }

        private bool AllowAnonymousAccess(HttpApplication app)
        {
            // for non-RestNet requests - aspx, images, etc. bypass security by always allowing
            if (!IsRestNetRequest(app))
                return true;

            IPrincipal prevUser = app.Context.User;
            IPrincipal prevPrincipal = _userPrincipal;

            try
            {
                // Create temporary anonymous user so all our security methods work
                RestNetUser user = new RestNetUser("Anonymous", string.Empty, "Restnet.RestNetUser", false, new string[] { });
                _userPrincipal = new GenericPrincipal(user, new string[] { });
                app.Context.User = _userPrincipal;

                string resourceName = app.Context.Request.QueryString["resourceName"];
                if(resourceName == null)
                    resourceName = GetResourceNameFromUrl(app.Request);


                bool allowAnonymous = RestNet.AuthUtils.UserHasRightsToThisMethod(resourceName, app.Context.Request.HttpMethod, new HttpContextWrapper(app.Context));
                return allowAnonymous;
            }
            catch
            {
                throw;
            }
            finally
            {
                // restore previous user
                _userPrincipal = prevPrincipal;
                app.Context.User = prevUser;
            }
        }

        private void GetAuthClass()
        {
            authClass = Persephone.Processing.Pipeline.PipelineFactory.CreateAuthFromConfiguration<IRestNetAuth>("RestNet_Authentication");
            if (authClass == null || authClass.Auths.Count == 0)
                throw new Exception("Error loading RestNet_Authentication instance");
        }

        public void OnBeginRequest(object source, EventArgs eventArgs)
        {
            // DO NOTHING FOR NOW
            HttpApplication app = (HttpApplication)source;
            if (app.Context != null)
            {
                if (IsRestNetRequest(app))
                {

                    // CHECK FOR FLASH CUSTOM AUTH HEADER AND RE-WRITE TO NORMAL AUTH HEADER
                    string authStr = app.Request.Headers["X-Authorization"];
                    if (authStr != null && authStr.Length > 0)
                    {
                        // PUT HEADERS COLLECTION IN GOD MODE TO SWAP AUTH HEADERS

                        // CREATE A NEW ARRAY LIST WHICH WILL CONTAIN OUR NEW HEADER
                        // THE REFLECTION BaseSet METHOD NEEDS AN ARRAY LIST FOR THE ADD METHOD OF THE REQUEST HEADERS COLLECTION
                        System.Collections.ArrayList list = new System.Collections.ArrayList();
                        list.Add(authStr);

                        Type t = app.Request.Headers.GetType();
                        lock (app.Request.Headers)
                        {
                            t.InvokeMember("MakeReadWrite", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, app.Request.Headers, null);
                            t.InvokeMember("InvalidateCachedArrays", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, app.Request.Headers, null);
                            t.InvokeMember("BaseSet", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, app.Request.Headers, new object[] { "Authorization", list });
                            t.InvokeMember("MakeReadOnly", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, app.Request.Headers, null);
                        }
                    }

                    // CHECK FOR CRIPPLED CLIENT VERB METHOD HEADER AND RE-TOOL CONTEXT ACCORDINGLY
                    // TODO - CRIPPLED CLIENT SUPPORT - MAY NEED TO SUPPORT X-Rest-Method AS WELL                    // CHECK FOR CRIPPLED CLIENT VERB METHOD HEADER AND RE-TOOL CONTEXT ACCORDINGLY
                    if (app.Request.Headers["X-Method"] != null)
                    {
                        string newVerb = app.Request.Headers["X-Method"];
                        app.Request.GetType().GetField("_httpMethod", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app.Request, newVerb);
                    }
                }
            }
        }

        private bool UseCrippledClientHeaders(HttpRequest request)
        {
            string useCrippledClientHeaders = request.Headers["X-Crippled-Client"];
            return (useCrippledClientHeaders == "true" || useCrippledClientHeaders == "yes");
        }

        private bool UseCrippledClientEnvelope(HttpRequest request)
        {
            string useCrippledClientEnvelope = request.Headers["X-Crippled-Client-Envelope"];
            return (useCrippledClientEnvelope == "true" || useCrippledClientEnvelope == "yes");
        }

        private static string GetRestNetSecurity(HttpRequest request)
        {
            return Settings.Get(GetResourceNameFromUrl(request));
        }

        private static string GetResourceNameFromUrl(HttpRequest request)
        {
            return request.Url.Segments[request.Url.Segments.Length - 1];
        }

        private static bool IsRestNetRequest(HttpApplication app)
        {
            if (app.Request.Path.EndsWith(".rnx", StringComparison.InvariantCultureIgnoreCase))
                return true;

            // it's not a RestNet request, but let's see if RestNet security is defined on it
            string restnetSecurity = GetRestNetSecurity(app.Request);
            if (restnetSecurity != null && restnetSecurity != string.Empty)
                return true;

            return false;
        }


        public void OnEndRequest(object source, EventArgs eventArgs)
        {
            // We add the WWW-Authenticate header here, so if an authorization 
            // fails elsewhere than in this module, we can still request authentication 
            // from the client.
            HttpApplication app = (HttpApplication)source;

            if (app.Response.StatusCode == 401)
            {
                DenyAccess(app);
            }
        }

        private void DenyAccess(HttpApplication app, HttpException ex)
        {
            _userPrincipal = null;
            string realm = Settings.Get("Authentication_Realm");
            string val = String.Format("Basic Realm=\"{0}\"", realm);
            app.Response.AppendHeader("WWW-Authenticate", val);
            RestNet.ErrorHandler.SendErrorResponse(app.Response, ex);
        }

        private void DenyAccess(HttpApplication app)
        {
            DenyAccess(app, new HttpException(401, "Access Denied"));
        }
        #endregion
    }
}
