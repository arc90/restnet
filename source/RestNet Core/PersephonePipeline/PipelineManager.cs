using System;

namespace Persephone.Processing.Pipeline
{
    /// <summary>
    /// Represents a series of processing step to perform
    /// on a data object. Each step implements the IStep
    /// interface.
    /// </summary>
    public class PipelineManager<T>
    {
        private FilterChain<T> filters;
        private System.Web.IHttpHandler processor;

        private AuthChain<T> auths;
        private RestNet.IRestNetAuth authenticator;

        /// <summary>
        /// Creates and instance of the class.
        /// </summary>
        public PipelineManager()
        {
            filters = new FilterChain<T>();
        }

        /// <summary>
        /// Creates an instance of the class using the specified ISteps.
        /// </summary>
        /// <param name="pipeline">The collection of ISteps to use for the pipeline.</param>
        public PipelineManager(FilterChain<T> filters)
        {
            this.filters = filters;
        }

        public PipelineManager(AuthChain<T> auths)
        {
            this.auths = auths;

        }


        /// <summary>
        /// Readonly property exposing the steps used by
        /// the Pipeline.
        /// </summary>
        public FilterChain<T> Filters
        {
            get { return filters; }
        }

        public AuthChain<T> Auths
        {
            get { return auths; }
        }

        /// <summary>
        /// Gets or sets the processor associated with the pipeline.
        /// </summary>
        public System.Web.IHttpHandler Processor
        {
            get { return processor; }
            set { processor = value; }
        }

        public RestNet.IRestNetAuth Authenticator
        {
            get { return authenticator; }
            set { authenticator = value; }
        }


        /// <summary>
        /// Processes the data object by calling Execute on each step in the step list
        /// passing in the same data object for each call.
        /// </summary>
        /// <param name="data">The data object to process.</param>
        /// <param name="stopOnFailure">A Flag to indicate if the processing should stop if any of the
        /// steps return false.</param>
        /// <returns>True if all the processors returned true, false otherwise.</returns>
        public bool ProcessFilter(System.Web.HttpContext context)
        {
            filters.ProcessRequest(context);
            if (processor != null)
                processor.ProcessRequest(context);

            return true;
        }


        public System.Security.Principal.GenericPrincipal ProcessAuthentication(string username, string password)
        {
            return auths[0].AuthenticateUser(username, password);
        }

    }
}
