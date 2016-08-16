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
using System.Linq;
using System.Reflection;
#if PORTABLE
using Microsoft.Extensions.Configuration;
#endif
using System.Text;

namespace Polly.Configuration
{
    /// <summary>
    /// Class PolicyRegistry.
    /// </summary>
    public static partial class PolicyRegistry
    {
#if PORTABLE

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IEnumerable<Policy> ResolveAll(IConfigurationRoot configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            var section = configuration.GetSection("polly");
            if (section != null)
            {
                foreach (var item in section.GetChildren())
                {
                    var policy = CreatePolicy(item, item.Key);
                    lock (_lock)
                    {
                        if (!_policies.ContainsKey(item.Key))
                        {
                            _policies.Add(item.Key, policy);
                        }
                    }
                }
            }
            return _policies.Values.ToList();
        }

        /// <summary>
        /// Resolves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public static Policy Resolve(string name, Microsoft.Extensions.Configuration.IConfigurationRoot configuration)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (_policies.ContainsKey(name)) return _policies[name];
            if (configuration == null) throw new ArgumentNullException("configuration");
            var section = configuration.GetSection("polly");
            if (section != null)
            {
                foreach(var item in section.GetChildren())
                {
                    if (item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        var policy = CreatePolicy(item, item.Key);
                        lock(_lock)
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

        private static int GetOrder(IConfigurationSection section)
        {
            var orderStr = section["order"];
            if (string.IsNullOrEmpty(orderStr)) return 0;
            int order;
            if (int.TryParse(orderStr, out order))
            {
                return order;
            }
            return 0;
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
        private static Policy CreatePolicy(Microsoft.Extensions.Configuration.IConfigurationSection policySection, string key)
        {
            Policy policy                                              = null;
            var useMetrics = false;
            var sections = policySection.GetChildren().OrderBy(GetOrder);
            foreach (var item in sections)
            {
                string type = null;
                string exceptionTypeStr = null;
                string retryCountStr = null;
                string timeoutInSecondsStr = null;
                string timeoutInMillisecondsStr = null;
                string maxParallelizationStr = null;
                string maxQueuedActionsStr = null;
                string cacheProviderStr = null;
                foreach (var el in item.GetChildren())
                {
                    if (el.Key == "type") type = el.Value;
                    else if (el.Key == "exceptionType") exceptionTypeStr = el.Value;
                    else if (el.Key == "retryCount") retryCountStr = el.Value;
                    else if (el.Key == "timeoutInSeconds") timeoutInSecondsStr = el.Value;
                    else if (el.Key == "timeoutInMilliseconds") timeoutInMillisecondsStr = el.Value;
                    else if (el.Key == "maxParallelization") maxParallelizationStr = el.Value;
                    else if (el.Key == "maxQueuedActions") maxQueuedActionsStr = el.Value;
                    else if (el.Key == "cacheProvider") cacheProviderStr = el.Value;
                }
                if (string.IsNullOrEmpty(type)) type = item.Key.ToLowerInvariant();

                switch (type)
                {
                    case "handle":
                        policy = ProcessHandle(exceptionTypeStr, policy, key);
                        break;
                    case "handleresult":
                        break;
                    case "timeout":
                        policy = ProcessTimeout(timeoutInMillisecondsStr, timeoutInSecondsStr, policy, key);
                        break;
                    case "throttle":
                        policy = ProcessThrottle(maxParallelizationStr, maxQueuedActionsStr, policy, key);
                        break;
                    case "caching":
                        policy = ProcessCaching(cacheProviderStr, policy, key);
                        break;
                    case "metrics":
                        useMetrics = true;
                        break;
                }
            }
            
            foreach (var item in sections)
            {
                string type = null;
                string exceptionTypeStr = null;
                string retryCountStr = null;
                string timeoutInSecondsStr = null;
                string timeoutInMillisecondsStr = null;
                string maxParallelizationStr = null;
                string maxQueuedActionsStr = null;
                string cacheProviderStr = null;
                string valueStr = null;
                string valueType = null;
                string exceptionsAllowedBeforeBreakingStr = null;
                string durationOfBreakInSecondsStr = null;
                string durationOfBreakInMillisecondsStr = null;
                string exceptionCountLifetimeStr = null;
                string failureThresholdStr = null;
                string samplingDurationInSecondsStr = null;
                string samplingDurationInMillisecondsStr = null;
                string minimumThroughputStr = null;
                string policyTypeStr = null;
                string timeInMillisecondsStr = null;
                string numberOfBucketsStr = null;
                string bucketDataLengthStr = null;
                string valueProviderStr = null;
                var attributes = new Dictionary<string, string>();
                foreach (var el in item.GetChildren())
                {
                    if (el.Key == "type") type = el.Value;
                    else if (el.Key == "exceptionType") exceptionTypeStr = el.Value;
                    else if (el.Key == "retryCount") retryCountStr = el.Value;
                    else if (el.Key == "timeoutInSeconds") timeoutInSecondsStr = el.Value;
                    else if (el.Key == "timeoutInMilliseconds") timeoutInMillisecondsStr = el.Value;
                    else if (el.Key == "maxParallelization") maxParallelizationStr = el.Value;
                    else if (el.Key == "maxQueuedActions") maxQueuedActionsStr = el.Value;
                    else if (el.Key == "value") valueStr = el.Value;
                    else if (el.Key == "valueType") valueType = el.Value;
                    else if (el.Key == "valueProviderType") valueProviderStr = el.Value;
                    else if (el.Key == "cacheProvider") cacheProviderStr = el.Value;
                    else if (el.Key == "exceptionsAllowedBeforeBreaking") exceptionsAllowedBeforeBreakingStr = el.Value;
                    else if (el.Key == "durationOfBreakInSeconds") durationOfBreakInSecondsStr = el.Value;
                    else if (el.Key == "durationOfBreakInMilliseconds") durationOfBreakInMillisecondsStr = el.Value;
                    else if (el.Key == "failureThreshold") failureThresholdStr = el.Value;
                    else if (el.Key == "samplingDurationInSeconds") samplingDurationInSecondsStr = el.Value;
                    else if (el.Key == "samplingDurationInMilliseconds") samplingDurationInMillisecondsStr = el.Value;
                    else if (el.Key == "minimumThroughput") minimumThroughputStr = el.Value;
                    else if (el.Key == "exceptionCountLifetime") exceptionCountLifetimeStr = el.Value;
                    else if (el.Key == "timeInMilliseconds") timeInMillisecondsStr = el.Value;
                    else if (el.Key == "numberOfBuckets") numberOfBucketsStr = el.Value;
                    else if (el.Key == "bucketDataLength") bucketDataLengthStr = el.Value;
                    else if (el.Key == "policyType") policyTypeStr = el.Value;
                    else if (el.Key == "order") continue;
                    else if (!attributes.ContainsKey(el.Key)) attributes.Add(el.Key, el.Value);
                }
                if (string.IsNullOrEmpty(type)) type = item.Key.ToLowerInvariant();
                
                switch(type)
                {
                    case "handle":
                    case "handleresult":
                    case "timeout":
                    case "throttle":
                    case "caching":
                    case "metrics":
                        // Extract Sampling Duration
                        break;
                    case "thenhandle":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with thenhandle type");
                        policy = ProcessThenHandle(exceptionTypeStr, policy, key);
                        break;
                    case "fallback":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with fallback type");
                        policy = ProcessFallback(valueStr, valueType, valueProviderStr, attributes, policy, key);
                        break;
                    case "retry":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with retry type");
                        policy = ProcessRetry(retryCountStr, policy, key);
                        break;
                    case "circuitbreaker":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with circuitbreaker type");
                        if (string.IsNullOrEmpty(exceptionsAllowedBeforeBreakingStr))
                        {
                            if (string.IsNullOrEmpty(failureThresholdStr))
                            {
                                throw new NullReferenceException($"exceptionsAllowedBeforeBreaking or failureThreshold are required with circuitbreaker policy item {key}");
                            }
                            policy = ProcessCircuitBreaker(failureThresholdStr, samplingDurationInSecondsStr, samplingDurationInMillisecondsStr, minimumThroughputStr,  durationOfBreakInSecondsStr, durationOfBreakInMillisecondsStr, policy, key);
                        }
                        else
                        {
                            policy = ProcessCircuitBreaker(exceptionsAllowedBeforeBreakingStr, durationOfBreakInSecondsStr, durationOfBreakInMillisecondsStr, exceptionCountLifetimeStr, policy, key);
                        }
                        break;
                    case "latency":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with latency type");
                        policy = ProcessLatency(timeInMillisecondsStr, numberOfBucketsStr, bucketDataLengthStr, policy, key);
                        break;
                    case "custom":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with a custom policy type");
                        policy = ProcessCustom(policyTypeStr, attributes, policy, key);
                        break;
                    default:
                        throw new InvalidOperationException(); //TODO: Invalid Policy Type Exception
                }
            }
            if (policy == null) throw new NullReferenceException("The policy does not contain any policy definitions");
            policy = policy.WithPolicyKey(key);
            return useMetrics ? policy.UseMetrics(TimeSpan.FromMinutes(2), 12) : policy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="failureThresholdStr"></param>
        /// <param name="samplingDurationInSecondsStr"></param>
        /// <param name="samplingDurationInMillisecondsStr"></param>
        /// <param name="minimumThroughputStr"></param>
        /// <param name="durationOfBreakInSecondsStr"></param>
        /// <param name="durationOfBreakInMillisecondsStr"></param>
        /// <param name="policy"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static Policy ProcessCircuitBreaker(string failureThresholdStr, string samplingDurationInSecondsStr, string samplingDurationInMillisecondsStr, string minimumThroughputStr, string durationOfBreakInSecondsStr, string durationOfBreakInMillisecondsStr, Policy policy, string key)
        {
            int failureThreshold;
            if (int.TryParse(failureThresholdStr, out failureThreshold))
            {
                TimeSpan samplingDuration = TimeSpan.MaxValue;
                int samplingDurationInSeconds;
                int samplingDurationInMilliseconds;
                if (int.TryParse(durationOfBreakInSecondsStr, out samplingDurationInSeconds))
                {
                    samplingDuration = TimeSpan.FromSeconds(samplingDurationInSeconds);
                }
                else if (int.TryParse(durationOfBreakInMillisecondsStr, out samplingDurationInMilliseconds))
                {
                    samplingDuration = TimeSpan.FromSeconds(samplingDurationInMilliseconds);
                }

                TimeSpan durationOfBreak = TimeSpan.MaxValue;
                int durationOfBreakInSeconds;
                int durationOfBreakInMilliseconds;
                if (int.TryParse(durationOfBreakInSecondsStr, out durationOfBreakInSeconds))
                {
                    durationOfBreak = TimeSpan.FromSeconds(durationOfBreakInSeconds);
                }
                else if (int.TryParse(durationOfBreakInMillisecondsStr, out durationOfBreakInMilliseconds))
                {
                    durationOfBreak = TimeSpan.FromSeconds(durationOfBreakInMilliseconds);
                }

                int minimumThroughput = 1;
                int.TryParse(minimumThroughputStr, out minimumThroughput);
                return policy.CircuitBreaker(failureThreshold, samplingDuration, minimumThroughput, durationOfBreak);
            }
            throw new NullReferenceException($"exceptionsAllowedBeforeBreaking or failureThreshold are required with circuitbreaker policy item {key}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exceptionsAllowedBeforeBreakingStr"></param>
        /// <param name="durationOfBreakInSecondsStr"></param>
        /// <param name="durationOfBreakInMillisecondsStr"></param>
        /// <param name="policy"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static Policy ProcessCircuitBreaker(string exceptionsAllowedBeforeBreakingStr, string durationOfBreakInSecondsStr, string durationOfBreakInMillisecondsStr, string exceptionCountLifetimeStr, Policy policy, string key)
        {
            int exceptionsAllowedBeforeBreaking;
            if (int.TryParse(exceptionsAllowedBeforeBreakingStr, out exceptionsAllowedBeforeBreaking))
            {
                TimeSpan durationOfBreak = TimeSpan.MaxValue;
                int durationOfBreakInSeconds;
                int durationOfBreakInMilliseconds;
                if (int.TryParse(durationOfBreakInSecondsStr, out durationOfBreakInSeconds))
                {
                    durationOfBreak = TimeSpan.FromSeconds(durationOfBreakInSeconds);
                }
                else if (int.TryParse(durationOfBreakInMillisecondsStr, out durationOfBreakInMilliseconds))
                {
                    durationOfBreak = TimeSpan.FromSeconds(durationOfBreakInMilliseconds);
                }

                TimeSpan exceptionCountLifetime = TimeSpan.MinValue;

                if (!string.IsNullOrEmpty(exceptionCountLifetimeStr))
                {
                    int exceptionCountLifetimeValue;
                    if (int.TryParse(exceptionCountLifetimeStr, out exceptionCountLifetimeValue))
                    {
                        exceptionCountLifetime = TimeSpan.FromSeconds(exceptionCountLifetimeValue);
                    }
                }

                return policy.CircuitBreaker(exceptionsAllowedBeforeBreaking, durationOfBreak, exceptionCountLifetime);
            }
            throw new NullReferenceException($"exceptionsAllowedBeforeBreaking is missing for circuit breaker policy {key}");
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
            public Policy Handle(Policy policy)
            {
                return AddException(policy);
            }

            /// <summary>
            /// Adds the exception.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            private Policy AddException(Policy policy)
            {
                if (policy == null) return Policy.Handle<TException>();
                else return policy.Or<TException>();
            }
        }

        /// <summary>
        /// Class ThenExceptionPolicyWrapper.
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        internal class ThenExceptionPolicyWrapper<TException> : IPolicyWrapper
            where TException : Exception
        {
            /// <summary>
            /// Executes the specified policy.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            public Policy Handle(Policy policy)
            {
                return AddException(policy);
            }

            /// <summary>
            /// Adds the exception.
            /// </summary>
            /// <param name="policy">The policy.</param>
            /// <returns>Policy.</returns>
            private Policy AddException(Policy policy)
            {
                return policy.ThenHandle<TException>();
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
            Policy Handle(Policy policy);
        }
        
        /// <summary>
        /// Processes the Handle.
        /// </summary>
        /// <param name="exceptionTypeStr">The exception type string.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$exceptionType is missing for handle policy item</exception>
        /// <exception cref="System.TypeLoadException">Type {exceptionTypeStr} cannot be resolved</exception>
        private static Policy ProcessHandle(string exceptionTypeStr, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(exceptionTypeStr)) throw new NullReferenceException($"exceptionType is missing for handle policy {key}");
            var type = Type.GetType(exceptionTypeStr);
            if (type != null)
            {
                  return ((IPolicyWrapper)Activator.CreateInstance(typeof(ExceptionPolicyWrapper<>).MakeGenericType(type))).Handle(policy);
            }
            throw new TypeLoadException($"Type {exceptionTypeStr} cannot be resolved");
        }

        
        /// <summary>
        /// Processes the ThenHandle.
        /// </summary>
        /// <param name="exceptionTypeStr">The exception type string.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$exceptionType is missing for handle policy item {key}</exception>
        /// <exception cref="System.TypeLoadException">Type {exceptionTypeStr} cannot be resolved</exception>
        private static Policy ProcessThenHandle(string exceptionTypeStr, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(exceptionTypeStr)) throw new NullReferenceException($"exceptionType is missing for handle policy item {key}");
            var type = Type.GetType(exceptionTypeStr);
            if (type != null)
            {
                  return ((IPolicyWrapper)Activator.CreateInstance(typeof(ThenExceptionPolicyWrapper<>).MakeGenericType(type))).Handle(policy);
            }
            throw new TypeLoadException($"Type {exceptionTypeStr} cannot be resolved");
        }

        

        /// <summary>
        /// Processes the timeout.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$timeoutInMilliseconds or timeoutInSeconds are missing or invalid for timeout policy item {key}</exception>
        private static Policy ProcessTimeout(string timeoutInMillisecondsStr, string timeoutInSecondStr, Policy policy, string key)
        {
            TimeSpan timeout = default(TimeSpan);
            var timeoutFound = false;
            int timeoutInMilliseconds;
            int timeoutInSeconds;
            if (int.TryParse(timeoutInMillisecondsStr, out timeoutInMilliseconds))
            {
                timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
                timeoutFound = true;
            }
            else if (int.TryParse(timeoutInSecondStr, out timeoutInSeconds))
            {
                timeout = TimeSpan.FromSeconds(timeoutInSeconds);
                timeoutFound = true;
            }
            if (!timeoutFound)
            {
                throw new NullReferenceException($"timeoutInMilliseconds or timeoutInSeconds are missing or invalid for timeout policy item {key}");
            }
            if (policy == null) return Policy.Timeout(timeout);
            return policy.ThenTimeout(timeout);
        }

        private static Policy ProcessThrottle(string maxParallelizationStr, string maxQueuedActionsStr, Policy policy, string key)
        {
            if (!string.IsNullOrEmpty(maxParallelizationStr))
            {
                int maxParallelization;
                if (int.TryParse(maxParallelizationStr, out maxParallelization))
                {
                    if (!string.IsNullOrEmpty(maxQueuedActionsStr))
                    {
                        int maxQueuedActions;
                        if (int.TryParse(maxQueuedActionsStr, out maxQueuedActions))
                        {
                            if (policy == null) return Policy.Throttle(maxParallelization, maxQueuedActions);
                            return policy.ThenThrottle(maxParallelization, maxQueuedActions);
                        }
                    }
                    if (policy == null) return Policy.Throttle(maxParallelization);
                    return policy.ThenThrottle(maxParallelization);
                }
            }
            throw new NullReferenceException($"maxParallelization is missing for throttle policy item {key}");
        }
        
        /// <summary>
        /// Processes the retry.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.NullReferenceException">$retryCount is missing for handle policy item {key}</exception>
        private static Policy ProcessRetry(string retryCountStr, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(retryCountStr)) throw new NullReferenceException($"retryCount is missing for retry policy item {key}");
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
            throw new NullReferenceException($"retryCount is missing for retry policy item {key}");
        }


        private static Policy ProcessCaching(string cachingProviderStr, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(cachingProviderStr)) throw new NullReferenceException($"cachingProvider is missing for handle policy item {key}");
            if (cachingProviderStr.Equals("memory", StringComparison.OrdinalIgnoreCase))
            {
                if (policy == null) return Policy.Cache();
                return policy.ThenCache();
            }
            var type = Type.GetType(cachingProviderStr);
            if (type != null)
            {
                var cacheProvider = (IResultCacheProvider)Activator.CreateInstance(type);

                if (policy == null) return Policy.Cache(cacheProvider);
                return policy.ThenCache(cacheProvider);
            }
            throw new TypeLoadException($"Type {cachingProviderStr} cannot be resolved");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStr"></param>
        /// <param name="valueType"></param>
        /// <param name="valueProviderStr"></param>
        /// <param name="policy"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static Policy ProcessFallback(string valueStr, string valueType, string valueProviderStr, IDictionary<string, string> attributes, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(valueStr))
            {
                var valueProviderType = Type.GetType(valueProviderStr);
                if (valueProviderType == null) throw new NullReferenceException($"value is missing for fallback policy item {key}");
                var contructor = valueProviderType.GetTypeInfo().DeclaredConstructors.OrderByDescending(x => x.GetParameters().Count()).FirstOrDefault();
                IFallbackValueProvider provider = null;
                if (contructor == null) provider = (IFallbackValueProvider)Activator.CreateInstance(valueProviderType);
                else
                {
                    var count = contructor.GetParameters().Count();
                    provider  = count == 0 ? (IFallbackValueProvider)Activator.CreateInstance(valueProviderType) : (IFallbackValueProvider)Activator.CreateInstance(valueProviderType, new object[] { attributes });
                }
                return policy.Fallback(provider.ExecuteAsync);
            }
            else 
            {
                if (valueStr.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return policy.Fallback<object>(() => null);
                }
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
                        return policy.Fallback<float>(() => value);
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
            throw new NullReferenceException($"retryCount is missing for retry policy item {key}");
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static Policy ProcessLatency(string timeInMillisecondsStr, string numberOfBucketsStr, string bucketDataLengthStr, Policy policy, string key)
        {
            int timeInMilliseconds;
            int numberOfBuckets;
            int bucketDataLength;
            if (!int.TryParse(timeInMillisecondsStr, out timeInMilliseconds)) throw new NullReferenceException($"timeInMilliseconds is missing for latency policy item {key}");
            if (!int.TryParse(numberOfBucketsStr, out numberOfBuckets)) throw new NullReferenceException($"numberOfBuckets is missing for latency policy item {key}");
            if (!int.TryParse(bucketDataLengthStr, out bucketDataLength)) throw new NullReferenceException($"bucketDataLength is missing for latency policy item {key}");
            return policy.ThenLatency(timeInMilliseconds, numberOfBuckets, bucketDataLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static Policy ProcessCustom(string policyTypeStr, IDictionary<string, string> attributes, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(policyTypeStr)) throw new NullReferenceException($"policyType is missing for custom policy item {key}");
            var policyType = Type.GetType(policyTypeStr);
            return policy.Wrap((Policy)Activator.CreateInstance(policyType, new object[] { attributes }));
        }

#endif
    }
}
