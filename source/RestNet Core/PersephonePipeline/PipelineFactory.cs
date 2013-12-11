using System;
using System.Collections.Generic;
using System.Text;

namespace Persephone.Processing.Pipeline
{
    /// <summary>
    /// Provides a factory for creating Pipeline objects.
    /// </summary>
    public static class PipelineFactory
    {
        /// <summary>
        /// Creates a Pipeline based on the data in the configuration file.
        /// </summary>
        /// <typeparam name="T">The type of the data the pipeline will handle.</typeparam>
        /// <param name="sectionName">The name of the configuration section containing the configuration data.</param>
        /// <returns>A Pipeline that handles the specified type.</returns>
        public static PipelineManager<T> CreateFromConfiguration<T>(string sectionName)
        {
            FilterChain<T> processors = new FilterChain<T>();
            PipelineConfigurationSection section = (PipelineConfigurationSection)System.Configuration.ConfigurationManager.GetSection(sectionName);
            if (section != null)
                foreach (PipelineHandlerConfigurationElement element in section.Processors)
                {
                    processors.Add((System.Web.IHttpHandler)ObjectFactory.Create(element.HandlerType));
                }
            return new PipelineManager<T>(processors);
        }


        /// <summary>
        /// Creates a Pipeline based on the data in the configuration file.
        /// </summary>
        /// <typeparam name="T">The type of the data the pipeline will handle.</typeparam>
        /// <param name="sectionName">The name of the configuration section containing the configuration data.</param>
        /// <returns>A Pipeline that handles the specified type.</returns>
        public static PipelineManager<T> CreateAuthFromConfiguration<T>(string sectionName)
        {
            AuthChain<T> processors = new AuthChain<T>();
            PipelineConfigurationSection section = (PipelineConfigurationSection)System.Configuration.ConfigurationManager.GetSection(sectionName);
            if (section != null)
                foreach (PipelineHandlerConfigurationElement element in section.Processors)
                {
                    processors.Add((RestNet.IRestNetAuth)ObjectFactory.Create(element.HandlerType));
                }
            return new PipelineManager<T>(processors);
        }



        /// <summary>
        /// Internal helper class to instantiate objects from a string type name.
        /// </summary>
        private static class ObjectFactory
        {
            /// <summary>
            /// Creates an object form the specified type name.
            /// </summary>
            /// <param name="typeName">The name of the type to create.</param>
            /// <returns>An instance of the type specified.</returns>
            public static object Create(string typeName)
            {
                Type t = Type.GetType(typeName);
                if (t == null)
                    throw new System.Web.HttpException(500, string.Format("The resource '{0}' does not exist or is not properly configured in this application.", typeName));
                return Activator.CreateInstance(t);
            }
        }
    }
}
