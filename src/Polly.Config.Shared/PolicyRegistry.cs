// ***********************************************************************
// Assembly         : Polly
// Author           : bruno
// Created          : 07-30-2016
//
// Last Modified By : bruno
// Last Modified On : 07-30-2016
// ***********************************************************************
// <copyright file="PolicyRegistry.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Polly.Caching;
using Polly.Metrics;
using System;
using System.Collections.Generic;
#if !PORTABLE
using System.Configuration;
#endif
using System.Text;

namespace Polly.Configuration
{
    /// <summary>
    /// Class PolicyRegistry.
    /// </summary>
    public static partial class PolicyRegistry
    {
        private static readonly Dictionary<string, Policy> _policies = new Dictionary<string, Policy>();
        private static readonly object _lock = new object();

#if !PORTABLE
        /// <summary>
        /// Resolves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public static Policy Resolve(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (_policies.ContainsKey(name)) return _policies[name];

            var section = (PollyConfigurationSection)ConfigurationManager.GetSection(PollyConfigurationSection.SectionName);
            if (section != null)
            {
                foreach(PolicyElement policyDef in section.Policies)
                {
                    if (policyDef.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        var policy = CreatePolicy(policyDef);
                        if (section.UseMetrics) policy = policy.UseMetrics();
                        lock (_lock)
                        {
                            if (!_policies.ContainsKey(name))
                            {
                                _policies.Add(name, policy);
                            }
                        }
                        return policy;
                    }
                }
            }
            return null; // TODO: REVIEW: PolicyNotFoundException
        }

        /// <summary>
        /// Resolves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Policy.</returns>
        public static IEnumerable<Policy> ResolveAll()
        {
            var section = (PollyConfigurationSection)ConfigurationManager.GetSection(PollyConfigurationSection.SectionName);
            if (section != null)
            {
                foreach(PolicyElement policyDef in section.Policies)
                {
                    if (!_policies.ContainsKey(policyDef.Key))
                    {
                        var policy = CreatePolicy(policyDef);
                        if (section.UseMetrics) policy = policy.UseMetrics();
                        lock(_lock)
                        {
                            if (!_policies.ContainsKey(policyDef.Key))
                            {
                                _policies.Add(policyDef.Key, policy);
                            }
                        }
                    }
                }
            }
            return _policies.Values; // TODO: REVIEW: PolicyNotFoundException
        }
        
        /// <summary>
        /// Creates the policy.
        /// </summary>
        /// <param name="policyDef">The policy definition.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">
        /// The policy items cannot start with thenhandle type
        /// or
        /// The policy items cannot start with fallback type
        /// or
        /// The policy items cannot start with retry type
        /// or
        /// The policy items cannot start with circuitbreaker type
        /// </exception>
        private static PolicyBuilder CreatePolicy(PolicyElement policyDef)
        {
            PolicyBuilder policy = null;
            foreach(PolicyItemElement item in policyDef.PolicyItems)
            {
                switch(item.Type)
                {
                    case "handle":
                        policy = ProcessHandle(item, policy);
                        break;
                    case "handleresult":
                        break;
                    case "fallback":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with fallback type");
                        policy = ProcessFallback(item, policy);
                        break;
                    case "timeout":
                        policy = ProcessTimeout(item, policy);
                        break;
                    case "throttle":
                        policy = ProcessThrottle(item, policy);
                        break;
                    case "retry":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with retry type");
                        policy = ProcessRetry(item, policy);
                        break;
                    case "circuitbreaker":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with circuitbreaker type");
                        policy = ProcessCircuitBreaker(item, policy);
                        break;
                    case "caching":
                        policy = ProcessCaching(item, policy);
                        break;
                    case "latency":
                    if (policy == null) throw new NullReferenceException("The policy items cannot start with latency type");
                        policy = ProcessLatency(item, policy);
                        break;
                    case "custom":
                        policy = ProcessCustom(item, policy);
                        break;
                    default:
                        throw new InvalidOperationException(); //TODO: Invalid Policy Type Exception
                }
            }
            return policy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static PolicyBuilder ProcessLatency(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("timeInMilliseconds")) throw new NullReferenceException($"timeInMilliseconds is missing for latency policy item {item.Key}");
            if (!item.Attributes.ContainsKey("numberOfBuckets")) throw new NullReferenceException($"numberOfBuckets is missing for latency policy item {item.Key}");
            if (!item.Attributes.ContainsKey("bucketDataLength")) throw new NullReferenceException($"bucketDataLength is missing for latency policy item {item.Key}");
            var timeInMillisecondsStr = item.Attributes["timeInMilliseconds"];
            var numberOfBucketsStr = item.Attributes["numberOfBuckets"];
            var bucketDataLengthStr = item.Attributes["bucketDataLength"];
            int timeInMilliseconds;
            int numberOfBuckets;
            int bucketDataLength;
            if (!int.TryParse(timeInMillisecondsStr, out timeInMilliseconds)) throw new NullReferenceException($"timeInMilliseconds is missing for latency policy item {item.Key}");
            if (!int.TryParse(numberOfBucketsStr, out numberOfBuckets)) throw new NullReferenceException($"numberOfBuckets is missing for latency policy item {item.Key}");
            if (!int.TryParse(bucketDataLengthStr, out bucketDataLength)) throw new NullReferenceException($"bucketDataLength is missing for latency policy item {item.Key}");
            return policy.Latency(timeInMilliseconds, numberOfBuckets, bucketDataLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static PolicyBuilder ProcessCustom(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("policyType")) throw new NullReferenceException($"policyType is missing for custom policy item {item.Key}");
            var policyType = Type.GetType(item.Attributes["policyType"]);
            //return policy.Wrap((Policy)Activator.CreateInstance(policyType, new object[] { item.Attributes }));
            return policy;
        }

        /// <summary>
        /// Class ExceptionPolicyWrapper.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        internal class ExceptionPolicyWrapper<TException> : IPolicyWrapper
            where TException : Exception
        {
            /// <summary>
            /// Executes the specified policy.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            public PolicyBuilder Handle(PolicyBuilder policy)
            {
                return AddException(policy);
            }

            /// <summary>
            /// Adds the exception.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            private PolicyBuilder AddException(PolicyBuilder policy)
            {
                if (policy == null) return Policy.Handle<TException>();
                else return policy.Or<TException>();
            }
        }

        /// <summary>
        /// Interface IPolicyWrapper
        /// </summary>
        internal interface IPolicyWrapper
        {
            /// <summary>
            /// Handles the specified policy.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            PolicyBuilder Handle(PolicyBuilder policy);
        }
        
        /// <summary>
        /// Processes the handle.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$exceptionType is missing for handle policy item {item.Key}</exception>
        /// <exception cref="System.TypeLoadException">Type {exceptionTypeStr} cannot be resolved</exception>
        private static PolicyBuilder ProcessHandle(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("exceptionType")) throw new NullReferenceException($"exceptionType is missing for handle policy item {item.Key}");
            var exceptionTypeStr = item.Attributes["exceptionType"];
            var type = Type.GetType(exceptionTypeStr);
            if (type != null)
            {
                  return ((IPolicyWrapper)Activator.CreateInstance(typeof(ExceptionPolicyWrapper<>).MakeGenericType(type))).Handle(policy);
            }
            throw new TypeLoadException($"Type {exceptionTypeStr} cannot be resolved");
        }
        
        /// <summary>
        /// Processes the timeout.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$timeoutInMilliseconds or timeoutInSeconds are missing or invalid for timeout policy item {item.Key}</exception>
        private static PolicyBuilder ProcessTimeout(PolicyItemElement item, PolicyBuilder policy)
        {
            TimeSpan timeout = default(TimeSpan);
            var timeoutFound = false;
            if (item.Attributes.ContainsKey("timeoutInMilliseconds"))
            {
                var timeoutInMillisecondsStr = item.Attributes["timeoutInMilliseconds"];
                int timeoutInMilliseconds;
                if (int.TryParse(timeoutInMillisecondsStr, out timeoutInMilliseconds))
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
                    timeoutFound = true;
                }
            }
            else if (item.Attributes.ContainsKey("timeoutInSeconds"))
            {
                var timeoutInSecondStr = item.Attributes["timeoutInSeconds"];
                int timeoutInSeconds;
                if (int.TryParse(timeoutInSecondStr, out timeoutInSeconds))
                {
                    timeout = TimeSpan.FromSeconds(timeoutInSeconds);
                    timeoutFound = true;
                }
            }
            if (!timeoutFound)
            {
                throw new NullReferenceException($"timeoutInMilliseconds or timeoutInSeconds are missing or invalid for timeout policy item {item.Key}");
            }
            if (policy == null) return Policy.Timeout(timeout);
            return policy.Timeout(timeout);
        }

        /// <summary>
        /// Processes the throttle.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static PolicyBuilder ProcessThrottle(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("maxParallelization")) throw new NullReferenceException($"maxParallelization is missing for throttle policy item {item.Key}");
            var maxParallelizationStr = item.Attributes["maxParallelization"];
            int maxParallelization;
            if (!string.IsNullOrEmpty(maxParallelizationStr))
            {
                if (int.TryParse(maxParallelizationStr, out maxParallelization))
                {
                    if (item.Attributes.ContainsKey("maxQueuedActions"))
                    {
                        var maxQueuedActionsStr = item.Attributes["maxQueuedActions"];
                        int maxQueuedActions;
                        if (int.TryParse(maxQueuedActionsStr, out maxQueuedActions))
                        {
                            if (policy == null) return Policy.Throttle(maxParallelization, maxQueuedActions);
                            return policy.Throttle(maxParallelization, maxQueuedActions);
                        }
                    }
                    if (policy == null) return Policy.Throttle(maxParallelization);
                    return policy.Throttle(maxParallelization);
                }
            }
            throw new NullReferenceException($"retryCount is missing for retry policy item {item.Key}");
        }

        /// <summary>
        /// Process the circuit breaker policy item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        /// <returns></returns>

        private static PolicyBuilder ProcessCircuitBreaker(PolicyItemElement item, PolicyBuilder policy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes the caching.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">
        /// $cachingProvider is missing for handle policy item {item.Key}
        /// or
        /// $cachingProvider is missing for handle policy item {item.Key}
        /// </exception>
        /// <exception cref="System.TypeLoadException">$Type {cachingProviderStr} cannot be resolved</exception>
        private static PolicyBuilder ProcessCaching(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("cachingProvider")) throw new NullReferenceException($"cachingProvider is missing for handle policy item {item.Key}");
            var cachingProviderStr = item.Attributes["cachingProvider"];
            if (string.IsNullOrEmpty(cachingProviderStr)) throw new NullReferenceException($"cachingProvider is missing for handle policy item {item.Key}");
            if (cachingProviderStr.Equals("memory", StringComparison.OrdinalIgnoreCase))
            {
                if (policy == null) return Policy.Cache();
                return policy.Cache();
            }
            var type = Type.GetType(cachingProviderStr);
            if (type != null)
            {
                var cacheProvider = (IResultCacheProvider)Activator.CreateInstance(type);

                if (policy == null) return Policy.Cache(cacheProvider);
                return policy.Cache(cacheProvider);
            }
            throw new TypeLoadException($"Type {cachingProviderStr} cannot be resolved");
        }



        /// <summary>
        /// Processes the retry.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$retryCount is missing for handle policy item {item.Key}</exception>
        private static PolicyBuilder ProcessRetry(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("retryCount")) throw new NullReferenceException($"retryCount is missing for retry policy item {item.Key}");
            var retryCountStr = item.Attributes["retryCount"];
            int retryCount;
            if (!string.IsNullOrEmpty(retryCountStr))
            {
                if (int.TryParse(retryCountStr, out retryCount))
                {
                    return policy.Retry(retryCount);
                }
                else if (retryCountStr.Equals("forever", StringComparison.OrdinalIgnoreCase))
                {
                    return policy.RetryForever();
                }
            }
            throw new NullReferenceException($"retryCount is missing for retry policy item {item.Key}");
        }



        private static PolicyBuilder ProcessFallback(PolicyItemElement item, PolicyBuilder policy)
        {
            if (!item.Attributes.ContainsKey("value")) throw new NullReferenceException($"value is missing for fallback policy item {item.Key}");
            var valueStr = item.Attributes["value"];
            if (!string.IsNullOrEmpty(valueStr))
            {
                if (valueStr.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return policy.Fallback<object>(() => null);
                }

                var valueType = item.Attributes.ContainsKey("valueType") ? item.Attributes["valueType"] : "string";
                if (valueType == "int")
                {
                    int value;
                    if (int.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<int>(value);
                    }
                }
                else if (valueType == "double")
                {
                    double value;
                    if (double.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<double>(value);
                    }
                }
                else if (valueType == "float")
                {
                    float value;
                    if (float.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<float>(value);
                    }
                }
                else if (valueType == "decimal")
                {
                    decimal value;
                    if (decimal.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<decimal>(value);
                    }
                }
                else if (valueType == "long")
                {
                    long value;
                    if (long.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<long>(value);
                    }
                }
                else if (valueType == "short")
                {
                    short value;
                    if (short.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<short>(value);
                    }
                }
                else if (valueType == "sbyte")
                {
                    sbyte value;
                    if (sbyte.TryParse(valueStr, out value))
                    {
                        return policy.Fallback<sbyte>(value);
                    }
                }
                else if (valueType == "string")
                {
                    return policy.Fallback<string>(() => valueStr);
                }
            }
            throw new NullReferenceException($"retryCount is missing for retry policy item {item.Key}");
        }

#endif
    }
}
