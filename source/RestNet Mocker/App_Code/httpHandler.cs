using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using RestNet;

namespace RestNetMocker
{
    public class HttpHandler : System.Web.IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {


            string resourceName = context.Request.QueryString["resourceName"];
            string resourceData = context.Request.QueryString["resourceData"];

            if (resourceName == null || resourceName == string.Empty)
                throw RestNet.ErrorHandler.HttpBadRequest("No resource name was specified");

            if (context.Request.QueryString["_rewriteTest"] != null)
            {
                context.Response.ContentType = "text/plain";
                context.Response.Write(
                    string.Format(" Original URL: {0}\nRewritten URL: {1}\n resourceName: {2}\n resourceData: {3}\n        query: {4}",
                        context.Request.Headers["X-REWRITE-URL"],
                        context.Request.Url.PathAndQuery,
                        resourceName,
                        resourceData,
                        string.Join("&\n               ", context.Request.QueryString.ToString().Split('&'))
                        ));
                context.Response.End();
                return;
            }

            if (context.Request.Headers["X-Crippled-Client-Envelope"] == "true")
                context.Response.Filter = new arcHelpers.CrippledClientEnvelopeHttpFilter(context.Response.ContentEncoding, context.Response.Filter);

            Persephone.Processing.Pipeline.PipelineManager<IHttpHandler> rr = Persephone.Processing.Pipeline.PipelineFactory.CreateFromConfiguration<IHttpHandler>(resourceName.Replace('/', '_'));
            if (rr == null || rr.Filters.Count == 0)
                throw RestNet.ErrorHandler.HttpResourceUnknown(resourceName);

            rr.ProcessFilter(context);

        }

        #endregion
    }
}
