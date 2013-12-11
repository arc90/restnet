using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace RestNet
{
    public static class ErrorHandler
    {
        public static string StatusCodeMessage(int httpStatusCode)
        {
            switch (httpStatusCode)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing (WebDAV)";

                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status (WebDAV)";

                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 306: return "Switch Proxy";
                case 307: return "Temporary Redirect";

                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-URI Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 422: return "Unprocessable Entity (WebDAV)";
                case 423: return "Locked (WebDAV)";
                case 424: return "Failed Dependency (WebDAV)";
                case 425: return "Unordered Collection";
                case 426: return "Upgrade Required";
                case 449: return "Retry With";

                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Temporarily Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "HTTP Version Not Supported";
                case 506: return "Variant Also Negotiates";
                case 507: return "Insufficient Storage (WebDAV)";
                case 509: return "Bandwidth Limit Exceeded";
                case 510: return "Not Extended";
            }

            return "Unknown Event";
        }


        /// <summary>
        /// Generic problem with the request
        /// </summary>
        /// <param name="message">Message to display to the user</param>
        /// <returns>400 Bad Request exception</returns>
        public static Exception HttpBadRequest(string message)
        {
            return HttpBadRequest(message, null);
        }

        /// <summary>
        /// Generic problem with the request
        /// </summary>
        /// <param name="message">Message to display to the user</param>
        /// <param name="ex">Root exception</param>
        /// <returns>400 Bad Request exception</returns>
        public static Exception HttpBadRequest(string message, Exception ex)
        {
            return new HttpException(400, message, ex);
        }

        public static Exception HttpUnauthorized()
        {
            return HttpUnauthorized("You are not authorized to use this resource.");
        }

        public static Exception HttpUnauthorized(string message)
        {
            return new HttpException(401, message);
        }

        public static Exception HttpNotAllowed()
        {
            return HttpNotAllowed("This method or operation is not allowed.");
        }

        public static Exception HttpNotAllowed(string message)
        {
            return new HttpException(403, message);
        }

        public static Exception HttpNotAllowed(string message, System.Exception ex)
        {
            return new HttpException(403, message, ex);
        }

        public static Exception HttpNotImplemented(string methodName)
        {
            return new HttpException(405, string.Format("This resource does not implement the {0} method", methodName));
        }

        public static Exception HttpResourceUnknown(string resourceName)
        {
            return new HttpException(404, string.Format("The requested resource '{0}' does not exist or cannot be accessed here.", resourceName));
        }

        public static Exception HttpResourceInstanceNotFound(string resourceName, string resourceId)
        {
            return new HttpException(404, string.Format("{0} resource {1} does not exist, or you do not have access to it.", resourceName, (resourceId == null || resourceId == string.Empty) ? "(blank)" : resourceId));
        }

        public static Exception HttpConflict(string resourceName, string resourceId, Uri conflictingResource)
        {
            return new HttpException(409, string.Format("{0} resource {1} conflicts with existing resource at {2}.", resourceName, resourceId, conflictingResource.AbsoluteUri));
        }

        public static Exception HttpRepresentationNotValid(string resourceName)
        {
            return new HttpException(415, string.Format("Not a valid {0} representation.", resourceName));
        }

        public static Exception HttpRepresentationNotValid(string resourceName, string validationMessage)
        {
            return new HttpException(415, string.Format("Not a valid {0} representation. {1}", resourceName, validationMessage));
        }

        public static Exception HttpFilterUnknown(string filterName)
        {
            return new HttpException(501, string.Format("The application filter identified by '{0}' does not exist or cannot be loaded.", filterName));
        }

        public static Exception HttpConfigurationError(string message)
        {
            return new HttpException(500, string.Format("This application is not configured correctly. {0}", message));
        }

        /// <summary>
        /// Thrown when a resource is called with a method+representation combination it does not support
        /// </summary>
        /// <param name="resourceName">Name of the resource</param>
        /// <param name="methodName">Name of the method</param>
        /// <param name="representationType">Name of the representation type</param>
        /// <returns>415 Unsupported Media Type HttpException</returns>
        /// <example>POSTing application/pdf to a resource that only accepts GET of application/pdf, or POST of application/text+xml</example>
        public static Exception HttpRepresentationMethodMismatch(string resourceName, string methodName, string representationType)
        {
            return new HttpException(415, string.Format("The {0} resource's {1} method does not accept representations of type '{2}'", resourceName, methodName, representationType));
        }

        public static void SendErrorResponse(HttpResponse response, System.Exception e)
        {
            SendErrorResponse(new HttpResponseWrapper(response), e);
        }

        public static void SendErrorResponse(HttpResponseBase response, System.Exception e)
        {
            HttpException he = null;
            if (e is HttpException)
                he = (HttpException)e;
            else if (e is NullReferenceException)
            {
                NullReferenceException nre = (NullReferenceException)e;
                he = new HttpException(500, "NullReferenceException - The system tried to use an object with no value.", e);
            }
            else
                he = new HttpException(500, e.GetBaseException().Message, e);

            SendErrorResponse(response, he);
        }

        public static void SendErrorResponse(HttpResponse response, System.Web.HttpException he)
        {
            SendErrorResponse(new HttpResponseWrapper(response), he);
        }

        public static void SendErrorResponse(HttpResponseBase response, System.Web.HttpException he)
        {
            SendErrorResponse(response, he, string.Empty);
        }

        public static void SendErrorResponse(HttpResponse response, System.Web.HttpException he, string moduleVersion)
        {
            SendErrorResponse(new HttpResponseWrapper(response), he, moduleVersion);
        }

        public static void SendErrorResponse(HttpResponseBase response, System.Web.HttpException he, string moduleVersion)
        {
            int httpStatusCode = he.GetHttpCode();
            string msgType = StatusCodeMessage(httpStatusCode);

            string errorDoc = null;
            try
            {
                errorDoc = Utilities.GetXmlStringFromFile(Settings.Get(("ErrorTemplate")));                
            }
            catch (Exception ex)
            {
                // use default template if we can't find the requested one
                if (string.IsNullOrEmpty(errorDoc))
                {
                    errorDoc =
                        "<?xml version=\"1.0\" encoding=\"utf-8\"?><!DOCTYPE html PUBLIC \"-/W3C/DTD XHTML 1.0 Transitional/EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns = \"http:/www.w3.org/1999/xhtml\"><head><title>Application Error - {0} {1}</title></head><body style=\"font-family:Arial; font-size:10pt; color:#330000\"><h1>{0} {1}</h1><div class=\"error_message\">{2}</div><b><span class=\"error_location\">{4}.{3}</span></b><!--\n{5}\n--><br/><br/>In addition, the error page could not be loaded, so this default page was used instead.\n<!--THE FOLLOWING EXCEPTION OCCURRED WHILE LOADING THE ERROR TEMPLATE:\n" +
                        ex.Message + "\n--><hr/><i>RestNet Application {6}</i></body></html>";
                }
            }

            var url = (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Url != null) ? string.Format(" ({0})", HttpContext.Current.Request.Headers["X-REWRITE-URL"] ?? HttpContext.Current.Request.Url.PathAndQuery) : string.Empty;

            System.Reflection.MethodBase targetSite = he.GetBaseException().TargetSite;
            string msg = string.Format(errorDoc,
                httpStatusCode,
                msgType,
                he.Message + url,
                targetSite == null ? string.Empty : targetSite.Name,
                targetSite == null ? string.Empty : targetSite.ReflectedType.FullName,
                System.Web.HttpUtility.HtmlEncode(he.GetBaseException().StackTrace),
                moduleVersion);

            // Set logging level differently for client vs. server errors.
            // 100, 200, 300 series should never really appear, but just in case I've included them here
            // also, don't bother including full exceptions for < 500, as they're likely to just duplicate the message
            switch ((int)(httpStatusCode / 100))
            {
                case 0:
                case 1:
                case 2:
                    Logging.Debug(he.Message + url);
                    break;
                case 3:
                    Logging.InfoFormat("{0} - {1}", httpStatusCode, he.Message + url);
                    break;
                case 4:
                    if (httpStatusCode == 401)
                        // 401 challenge isn't really worthy of a warning
                        Logging.DebugFormat("{0} - {1}", httpStatusCode, he.Message + url);
                    else
                        Logging.WarnFormat("{0} - {1}", httpStatusCode, he.Message + url);
                    break;
                default:
                    Logging.Error(string.Format("{0} - {1}", httpStatusCode, he.Message + url), he);
                    break;
            }
            ReturnError(response, httpStatusCode, msg);



        }

        public static void ReturnError(HttpResponse response, int httpStatusCode, string msg)
        {
            ReturnError(new HttpResponseWrapper(response), httpStatusCode, msg);
        }

        public static void ReturnError(HttpResponseBase response, int httpStatusCode, string msg)
        {
            response.Clear();
            response.StatusCode = httpStatusCode;
            response.Write(msg);
            try
            {
                response.End();
            }
            catch (NullReferenceException)
            {
                // TODO: If request running in HttpSimulator, the added ApplicationInstance seems to cause NRE here, so just ignore it.
            }
        }
    }
}
