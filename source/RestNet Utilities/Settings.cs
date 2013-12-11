using System;
using System.Collections.Generic;
using System.Text;

namespace RestNet
{
    public static class Settings
    {
        // cache settings for a long time, as they're in web.config and will be cleared automatically if it changes
        private static TimeSpan cacheDuration = TimeSpan.FromHours(12);

        public static string Get(string key)
        {
            return Get(key, null);
        }

        public static string Get(string key, string defaultValue)
        {
            string output = (string) Cache.Get(key);
            if (output != null)
                return output;

            output = System.Configuration.ConfigurationManager.AppSettings.Get(key);
            
/*            var Response = System.Web.HttpContext.Current.Response;

            Response.Write("<html><body><h1>AppSettings</h1>\n<ol>");
            var appSettings = System.Web.HttpContext.Current.Server.MapPath("~/config/appSettings.config");
            Response.Write(string.Format("<li>{0} exists? {1}</li>\n", appSettings, System.IO.File.Exists(appSettings)));
            foreach (var k in System.Configuration.ConfigurationManager.AppSettings.AllKeys)
            {
                Response.Write(string.Format("<li>[{0}] = [{1}]</li>\n", k, System.Configuration.ConfigurationManager.AppSettings.Get(k)));
            }
            Response.Write("</ol></body></html>");
            Response.End();
*/
            if (output == null)
                output = defaultValue;

            if (output != null)
            {
                Cache.Set(key, output, cacheDuration);
            }
            return output;
        }

        public static string GetConnectionString(string key)
        {
            string output = (string) Cache.Get("connectionstring." + key);
            if (output != null)
                return output;

            System.Configuration.Configuration webConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            System.Configuration.ConnectionStringSettings connString = webConfig.ConnectionStrings.ConnectionStrings[key];

            if (connString != null)
            {
                output = connString.ConnectionString;
                Cache.Set("connectionstring." + key, output, cacheDuration);
            }
            return output;
        }

    }
}
