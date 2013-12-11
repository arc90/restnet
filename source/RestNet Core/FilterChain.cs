using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace RestNet
{
    public enum FilterLocation
    {
        Before = 1,
        After = 2
    }

    /// <summary>
    /// A collection of IFilters.
    /// This is a TypeDef for a List.
    /// </summary>
    public class FilterChain : IHttpHandler
    {
        private List<IHttpHandler> _beforeFilters = null;
        private List<IHttpHandler> _afterFilters = null;

        public FilterChain()
        {
        }

        public FilterChain(List<IHttpHandler> beforeFilters, List<IHttpHandler> afterFilters)
        {
            _beforeFilters = beforeFilters;
            _afterFilters = afterFilters;
        }

        public void AddFilter(FilterLocation filterLocation, IHttpHandler newFilter)
        {
            ((filterLocation == FilterLocation.Before) ? _beforeFilters : _afterFilters).Add(newFilter);
        }

        public void RemoveFilter(FilterLocation filterLocation, IHttpHandler filterToRemove)
        {
            ((filterLocation == FilterLocation.Before) ? _beforeFilters : _afterFilters).Remove(filterToRemove);
        }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            // execute before filters
            if (_beforeFilters != null)
            {
                _beforeFilters.ForEach(delegate(IHttpHandler thisFilter) { thisFilter.ProcessRequest(context); });
            }

            // execute request
            ProcessRequest(context);

            // execute after filters
            if (_afterFilters != null)
            {
                _afterFilters.ForEach(delegate(IHttpHandler thisFilter) { thisFilter.ProcessRequest(context); });
            }
        }

        #endregion
    }
}
