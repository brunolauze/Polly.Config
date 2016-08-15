// ***********************************************************************
// Assembly         : Polly
// Author           : bruno
// Created          : 07-30-2016
//
// Last Modified By : bruno
// Last Modified On : 07-30-2016
// ***********************************************************************
// <copyright file="PollyConfigurationSection.cs" company="">
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
    /// Class PollyConfigurationSection.
    /// </summary>
    internal class PollyConfigurationSection : ConfigurationSection
    {
        public const string SectionName = "polly";

        /// <summary>
        /// Gets the policies.
        /// </summary>
        /// <value>The policies.</value>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public PolicyElementCollection Policies
        {
            get
            {
                return (PolicyElementCollection)base[""];
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use metrics].
        /// </summary>
        /// <value><c>true</c> if [use metrics]; otherwise, <c>false</c>.</value>
        [ConfigurationProperty("useMetrics", IsRequired = false, DefaultValue = false)]
        public bool UseMetrics
        {
            get { return (bool)base["useMetrics"]; }
            set { base["useMetrics"] = value; }
        }

    }
}
#endif