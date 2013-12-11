using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Persephone.Processing.Pipeline
{
    /// <summary>
    /// A collection of IFilters.
    /// This is a TypeDef for a List.
    /// </summary>
    public class FilterChain<T> : List<IHttpHandler>
    {
        /// <summary>
        /// Processes the data object by calling Process on each step in the step list
        /// passing in the same data object for each call.
        /// </summary>
        /// <param name="data">The data object to process.</param>
        /// <param name="stopOnFailure">A Flag to indicate if the processing should stop if any of the
        /// steps return false.</param>
        /// <returns>True if all the processors returned true, false otherwise.</returns>
        internal void ProcessRequest(HttpContext context)
        {
            foreach (IHttpHandler processor in this)
            {
                if (processor != null && context.Response.IsClientConnected)
                    processor.ProcessRequest(context);
            }
        }
    }


    public class AuthChain<T> : List<RestNet.IRestNetAuth>
    {
        /// <summary>
        /// Processes the data object by calling Process on each step in the step list
        /// passing in the same data object for each call.
        /// </summary>
        /// <param name="data">The data object to process.</param>
        /// <param name="stopOnFailure">A Flag to indicate if the processing should stop if any of the
        /// steps return false.</param>
        /// <returns>True if all the processors returned true, false otherwise.</returns>
        internal void ProcessAuthorization(string username, string password)
        {
            foreach (RestNet.IRestNetAuth authenticator in this)
            {
                if (authenticator != null)
                    authenticator.AuthenticateUser(username, password);
            }
        }
    }

}
