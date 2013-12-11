using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Security;

namespace RestNet
{
    public class LdapAuthentication
    {
        private string _path;
        private string _filterAttribute;
        private DirectoryEntry _entry;

        public LdapAuthentication(string ldapPath)
        {
            _path = ldapPath;
        }

        public virtual bool GetPasswordAndRoles(string domainAndUsername, ref string password, ref string[] roles)
        {
            string[] pathItems = domainAndUsername.Split(new char[] { '\\' });
            if (pathItems.Length < 2)
            {
                throw new SecurityException(@"LDAP Authentication requires DOMAIN\username logins");
            }
            string username = pathItems[1];
            this._entry = new DirectoryEntry(this._path, domainAndUsername, password, AuthenticationTypes.Secure);
            try
            {
                object obj = this._entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher(this._entry);
                search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();
                if (result == null)
                {
                    return false;
                }
                this._path = result.Path;
                this._entry.Path = this._path;
                this._filterAttribute = result.Properties["cn"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new SecurityException("Error authenticating user. " + ex.Message, ex);
            }
            roles = this.GetGroups();
            return true;
        }


        public string[] GetGroups()
        {
            DirectorySearcher search = new DirectorySearcher(this._entry);
            search.PropertiesToLoad.Add("memberOf");
            StringBuilder groupNames = new StringBuilder();
            try
            {
                SearchResult result = search.FindOne();
                int propertyCount = result.Properties["memberOf"].Count;
                for (int propertyCounter = 0; propertyCounter <= propertyCount - 1; propertyCounter++)
                {
                    string dn = result.Properties["memberOf"][propertyCounter].ToString();
                    int equalsIndex = dn.IndexOf("=", 1);
                    int commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {
                        return null;
                    }
                    groupNames.Append(dn.Substring(equalsIndex + 1, (commaIndex - equalsIndex) - 1));
                    groupNames.Append("|");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining group names. " + ex.Message);
            }
            return groupNames.ToString(0, Math.Max(0, groupNames.Length - 1)).Split(new char[] { '|' });
        }
    }
}
