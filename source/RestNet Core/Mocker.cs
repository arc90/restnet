    using System;
using System.Collections.Generic;
using RestNet;

namespace RestNet
{
    public class Mocker : ResourceBase
    {
        private System.Collections.Specialized.NameValueCollection mockDocs = new System.Collections.Specialized.NameValueCollection();
        private System.Collections.Generic.Dictionary<string, int> statusCodes = new Dictionary<string, int>();

        public override string Name
        {
            get { return "Mock Resource"; }
        }

        private void ThrowMockInfoParamInvalid()
        {
            throw ErrorHandler.HttpBadRequest("Must pass a _mocks parameter to the mock resource. It must be of form {verb1|verb2},{type1|type2},{file};{next mock...} e.g. \"_mocks=GET|POST,text/html,mock_doc.xhtml;PUT,application/xml|text/csv,different_doc.csv\", etc.");
        }

        protected override void RegisterRepresentationTypes()
        {
            string mockInfo = Context.Request["_mocks"];
            if (!IsValid(mockInfo))
                ThrowMockInfoParamInvalid();
            
            string[] mocks = mockInfo.Split(';');
            for (int x = 0; x < mocks.Length; x++)
            {
                string[] parts = mocks[x].Split(',');
                if (parts.Length < 3 || parts.Length > 4)
                    ThrowMockInfoParamInvalid();

                string[] methods = parts[0].Split('|');
                string[] types = parts[1].Split('|');
                string sourceFilename = parts[2];
                string statusCodeString = parts.Length > 3 ? parts[3] : "200";
                int statusCode = 200;
                int.TryParse(statusCodeString, out statusCode);

                for (int y = 0; y < methods.Length; y++)
                {
                    for (int z = 0; z < types.Length; z++)
                    {
                        types[z] = ExpandTypes(types[z]);
                        mockDocs[methods[y].ToLower() + ":" + types[z]] = GetFullMockFilename(sourceFilename);
                        statusCodes[methods[y].ToLower() + ":" + types[z]] = statusCode;
                        string contentType = GetOrFakeContentType(methods[y], Context.Request.ContentType);
                        AddRepresentationType((RequestMethod)Enum.Parse(typeof(RequestMethod), methods[y], true), contentType, types[z]);
                        if (Context.Request.QueryString["_showMockFilename"] == "1")
                            Context.Response.Write(string.Format("Method: {0}, Type: {1}, Filename: {2}\n", methods[y], types[z], GetFullMockFilename(sourceFilename)));
                    }
                }
            }

        }

        private string GetOrFakeContentType(string method, string contentType)
        {
            if (contentType != string.Empty)
                return contentType;

            if (method == "POST" || method == "PUT")
                return "doesnt_matter_for_mocks";
            else
                return string.Empty;
        }

        private string ExpandTypes(string shortType)
        {
            switch (shortType)
            {
                case "html":
                    return "text/html";
                case "xhtml":
                    return "application/xhtml+xml";
                case "json":
                    return "application/json";
                case "text":
                    return "text/plain";
                case "xml":
                    return "application/xml";
                default:
                    return Settings.Get("RestNet.Mocker.Representations." + shortType, shortType);
            }
        }

        public override int Head()
        {
            return Get(GetResponseType(RequestMethod.Head));
        }

        public override int Put(string contentType)
        {
            return Get(contentType);
        }

        public override int Post(string contentType)
        {
            return Get(contentType);
        }
        public override int Options()
        {
            return Get(GetResponseType(RequestMethod.Options));
        }
        public override int Delete()
        {
            return Get(GetResponseType(RequestMethod.Delete));
        }
        public override int Trace()
        {
            return Get(GetResponseType(RequestMethod.Trace));
        }
        public override int Connect()
        {
            return Get(GetResponseType(RequestMethod.Connect));
        }

        public override int GetMyRepresentation(string representationContentType)
        {
            string key = Context.Request.HttpMethod.ToLower() + ":" + representationContentType;
            string filename = mockDocs[key];
            if (Context.Request.QueryString["_showMockFilename"] == "1")
            {
                Context.Response.ContentType = "text/plain";
                Context.Response.Write(filename);
                return 200;
            }
            Context.Response.AddHeader("X-Mock-Filename", filename);
            if (!System.IO.File.Exists(filename))
                throw RestNet.ErrorHandler.HttpResourceInstanceNotFound(this.Name, filename);
            Context.Response.WriteFile(filename);
            return statusCodes[key];
        }

        private bool IsValid(string representationTypes)
        {
            if (representationTypes == null || representationTypes == string.Empty)
                return false;

            return true;
        }

        private string GetFullMockFilename(string filename)
        {
            return System.Web.Hosting.HostingEnvironment.MapPath(string.Format("~/App_Data/mocks/{0}", filename));
        }

    }
}
