using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace Persephone.Processing.Pipeline
{
    /// <summary>
    /// A configuration class for handling Pipeline data.
    /// </summary>
    class PipelineConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        public PipelineConfigurationSection()
            : base()
        { }

        /// <summary>
        /// A collection of Pipeline step definitions.
        /// </summary>
        [ConfigurationProperty("filters", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(PipelineHandlerTypeCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public PipelineHandlerTypeCollection Processors
        {
            get
            {
                PipelineHandlerTypeCollection processorCollection =
                (PipelineHandlerTypeCollection)base["filters"];
                return processorCollection;
            }
        }
    }

    /// <summary>
    /// A collection of Pipeline handler type definitions.
    /// </summary>
    class PipelineHandlerTypeCollection : ConfigurationElementCollection
    {
        public PipelineHandlerTypeCollection()
            : base()
        { }

        /// <summary>
        /// Readonly property specifying how the collection is mapped between configuration files. 
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        /// <summary>
        /// Creates a new element for the collection.
        /// </summary>
        /// <returns>The newly created element.</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new PipelineHandlerConfigurationElement();
        }

        /// <summary>
        /// Returns the key value for an element in the collection.
        /// </summary>
        /// <param name="element">The target element from which to extract the key.</param>
        /// <returns>The key for the target.</returns>
        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((PipelineHandlerConfigurationElement)element).HandlerType;
        }

        /// <summary>
        /// Retrieves the item at the specified index in the collection.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>The element at the specified index.</returns>
        public PipelineHandlerConfigurationElement this[int index]
        {
            get
            {
                return (PipelineHandlerConfigurationElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Retrieves the item from the collection based on the specified key.
        /// </summary>
        /// <param name="stepType">The key of the item in the collection.</param>
        /// <returns>The element with the specified key.</returns>
        new public PipelineHandlerConfigurationElement this[string stepType]
        {
            get
            {
                return (PipelineHandlerConfigurationElement)BaseGet(stepType);
            }
        }

        /// <summary>
        /// retruns the index of the specified item in the collection.
        /// </summary>
        /// <param name="step">The element to search for in the collection.</param>
        /// <returns>The index of the element, -1 if the element is not found.</returns>
        public int IndexOf(PipelineHandlerConfigurationElement step)
        {
            return BaseIndexOf(step);
        }

        /// <summary>
        /// Adds the specified PipelineStepConfigurationElement to the collection.
        /// </summary>
        /// <param name="step">The PipelineStepConfigurationElement to add to the collection.</param>
        public void Add(PipelineHandlerConfigurationElement step)
        {
            BaseAdd(step);
        }

        /// <summary>
        /// Adds a ConfigurationElement to the collection.
        /// </summary>
        /// <param name="element">The ConfigurationElement to add to the collection.</param>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        /// <summary>
        /// Removes the specified element form the collection.
        /// </summary>
        /// <param name="step">The element to remove.</param>
        public void Remove(PipelineHandlerConfigurationElement step)
        {
            if (BaseIndexOf(step) >= 0)
                BaseRemove(step.HandlerType);
        }

        /// <summary>
        /// Removes the element at the specified index from the collection.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        /// <summary>
        /// Removes the element with the specified key from the collection.
        /// </summary>
        /// <param name="stepType">The key of the element to remove.</param>
        public void Remove(string stepType)
        {
            BaseRemove(stepType);
        }

        /// <summary>
        /// Removes all the elements form the collection.
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }
    }

    /// <summary>
    /// Contains the type information for a single pipeline handler
    /// </summary>
    class PipelineHandlerConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        public PipelineHandlerConfigurationElement()
            : base()
        { }

        /// <summary>
        /// Crerates an instance of the class specifying the type name.
        /// </summary>
        /// <param name="type">The type name of the pipeline step.</param>
        public PipelineHandlerConfigurationElement(string type)
            : this()
        {
            this.HandlerType = type;
        }

        /// <summary>
        /// The type name of teh pipeline step.
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true, IsKey = true)]
        public string HandlerType
        {
            get
            {
                return (string)this["type"];
            }
            set
            {
                this["type"] = value;
            }
        }
    }
}
