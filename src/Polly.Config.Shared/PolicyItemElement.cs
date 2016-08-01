// ***********************************************************************
// Assembly         : Polly
// Author           : bruno
// Created          : 07-30-2016
//
// Last Modified By : bruno
// Last Modified On : 07-30-2016
// ***********************************************************************
// <copyright file="PolicyItemElement.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#if !PORTABLE
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Polly.Configuration
{
    /// <summary>
    /// Class PolicyItemElement.
    /// </summary>
    internal class PolicyItemElement : ConfigurationElement
    {
        private readonly IDictionary<string, string> _attributes;


        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyElement"/> class.
        /// </summary>
        public PolicyItemElement()
        {
            _attributes = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key
        {
            get
            {
                return (string)base["key"];
            }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)base["type"];
            }
            set
            {
                base["type"] = value;
            }
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <value>The attributes.</value>
        public IDictionary<string, string> Attributes { get { return _attributes; } }

        /// <summary>
        /// Called when [deserialize unrecognized attribute].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            Attributes.Add(name, value);
            return true;
        }
    }
}
#endif