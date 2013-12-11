using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace RestNet
{
    public abstract class FilterBase : IHttpHandler
    {
        public IHttpHandler _nextFilter = null;
        public FilterBase(IHttpHandler nextFilter)
        {
            _nextFilter = nextFilter;
        }

        protected void CallNextFilter(HttpContext context)
        {
            if (context == null)
                return;

            if (_nextFilter == null)
                return;

            if (!context.Response.IsClientConnected)
                return;

            _nextFilter.ProcessRequest(context);
        }

        #region IHttpHandler Members
        // each filter will have to report whether or not it's threadsafe (aka reusable). Essentially, IsReusable = no instance variables and vice versa.
        public abstract bool IsReusable{ get; }

        public virtual void ProcessRequest(HttpContext context)
        {
            CallNextFilter(context);
        }

        #endregion
    }
}
