using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace RestNet
{
    public static class AuthUtils
    {


        public static bool UserHasRightsToThisMethod(string resourceName, string method, System.Web.HttpContextBase context)
        {
            if (resourceName == null || resourceName == string.Empty)
                throw RestNet.ErrorHandler.HttpBadRequest("No resource name was specified");    

            string[] methodRoles = getResourceRolePermissions(resourceName, method);
            string securityType = Settings.Get("RestNetSecurityType", "role").ToLower();

            if (methodRoles != null)
            {
                for (int i = 0; i < methodRoles.Length; i++)
                {
                    // anonymous
                    if (methodRoles[i] == "*")
                        return true;

                    if (IsUserInRole(securityType, methodRoles[i], context))
                        return true;
                }
            }
            return false;
        }

        public static string[] getResourceRolePermissions(string resourceName, string method)
        {
            string allVerbs = Settings.Get(resourceName);
            if (allVerbs == null || allVerbs.Length == 0)
                throw ErrorHandler.HttpBadRequest("Could not find Resource Permissions for " + resourceName);

            string[] verbs = allVerbs.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            for (int iVerbs = 0; iVerbs < verbs.Length; iVerbs++)
            {
                string[] thisVerb = verbs[iVerbs].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (thisVerb[0].ToUpper().Trim() == method)
                    return thisVerb[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            }

            return null;
        }

        public static bool IsUserInRole(string securityType, string roleKey, System.Web.HttpContextBase context)
        {
            // anonymous user
            if (context.User == null)
                return false;

            switch (securityType)
            {
                case "role":
                    return ResourceBase.IsUserInRole(context.User, roleKey) || context.User.IsInRole(roleKey);
                case "group":
                    RestNetUser rUser = (RestNetUser)context.User.Identity;
                    return rUser.isInRole(roleKey);
                case "none":
                    return true;
                default:
                    throw ErrorHandler.HttpConfigurationError("The RestNetSecurityType setting in web.config contains an unknown value. Valid values are 'role', 'group', or 'none'");
            }
        }
    }
}
