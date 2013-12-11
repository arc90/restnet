using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Security;

namespace RestNet
{
    public interface IRestNetAuth
    {

        GenericPrincipal AuthenticateUser(string username, string password);

    }
}
