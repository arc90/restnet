using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;

namespace RestNet
{
    public class LdapPrincipal : GenericPrincipal
    {
        #region IPrincipal Members
        public LdapPrincipal(IIdentity user, string[] roles) : base(user, roles)
        {
            _roles = roles;
        }

        public override IIdentity Identity
        {
            get { return base.Identity; }
        }

        public override bool IsInRole(string role)
        {
            return Array.Exists<string>(_roles, delegate(string testString) { return role.Equals(testString, StringComparison.InvariantCultureIgnoreCase); });
        }

        #endregion

        private string[] _roles;

        public string[] Roles
        {
            get
            {
                return _roles;
            }
        }
    }
}
