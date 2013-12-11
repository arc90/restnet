using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Security;
using System.Security.Principal;

namespace RestNet
{
    public class RestNetUser : IIdentity
    {
        private string _authType = string.Empty;
        private string _name = string.Empty;
        private string _pass = string.Empty;
        private bool _authenticated = false;
        private string[] _roles = null;

        public RestNetUser(string username, string password, string authType, bool authenticated, string[] roles)
        {

            _name = username;
            _pass = password;
            _authType = authType;
            _authenticated = authenticated;
            _roles = roles;
        }

        public string Password
        {
            get { return _pass; }
        }

        public string[] Roles
        {
            get { return _roles; }
        }

        public bool isInRole(string whichRole)
        {
            if (string.Join("", Roles).Contains(whichRole))
                return true;
            
            return false;
            
        }

        #region IIdentity Members

        public string AuthenticationType
        {
            get { return _authType; }
        }

        public bool IsAuthenticated
        {
            get { return _authenticated; }
        }

        public string Name
        {
            get { return _name; }
        }

        #endregion
    }
}
