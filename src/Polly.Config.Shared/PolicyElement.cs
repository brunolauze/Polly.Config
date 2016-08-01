// ***********************************************************************
// Assembly         : Polly
// Author           : bruno
// Created          : 07-30-2016
//
// Last Modified By : bruno
// Last Modified On : 07-30-2016
// ***********************************************************************
// <copyright file="PolicyElement.cs" company="">
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
    /// Class PolicyElement.
    /// </summary>
    internal class PolicyElement : ConfigurationElement
    {
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
        /// Gets the policies.
        /// </summary>
        /// <value>The policies.</value>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public PolicyItemElementCollection PolicyItems
        {
            get
            {
                return (PolicyItemElementCollection)base[""];
            }
        }
    }
}
#endif