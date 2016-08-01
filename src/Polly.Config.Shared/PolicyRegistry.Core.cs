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
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Resolves the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Policy.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public static Policy Resolve(string name, Microsoft.Extensions.Configuration.IConfigurationRoot configuration)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (configuration == null) throw new ArgumentNullException("configuration");
            var section = configuration.GetSection("polly");
            if (section != null)
            {
                foreach(var item in section.GetChildren())
                {
                    if (item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return CreatePolicy(item, item.Key);
                    }
                }

            }
            return null; // TODO: REVIEW: PolicyNotFoundException
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
            Policy policy = null;
            foreach(var item in policySection.GetChildren())
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
                foreach(var el in item.GetChildren())
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
                    else if (el.Key == "cacheProvider") cacheProviderStr = el.Value;
                }

                if (string.IsNullOrEmpty(type)) type = item.Key.ToLowerInvariant();
        
                switch(type)
                {
                    case "handle":
                        policy = ProcessHandle(exceptionTypeStr, policy, key);
                        break;
                    case "thenhandle":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with thenhandle type");
                        policy = ProcessThenHandle(exceptionTypeStr, policy, key);
                        break;
                    case "handleresult":
                        break;
                    case "fallback":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with fallback type");
                        policy = ProcessFallback(valueStr, valueType, policy, key);
                        break;
                    case "timeout":
                        policy = ProcessTimeout(timeoutInSecondsStr, timeoutInMillisecondsStr, policy, key);
                        break;
                    case "throttle":
                        policy = ProcessThrottle(maxParallelizationStr, maxQueuedActionsStr, policy, key);
                        break;
                    case "retry":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with retry type");
                        policy = ProcessRetry(retryCountStr, policy, key);
                        break;
                    case "circuitbreaker":
                        if (policy == null) throw new NullReferenceException("The policy items cannot start with circuitbreaker type");
                        policy = ProcessCircuitBreaker(null, policy, key);
                        break;
                    case "caching":
                        policy = ProcessCaching(cacheProviderStr, policy, key);
                        break;
                    case "custom":
                        throw new NotImplementedException();
                    default:
                        throw new InvalidOperationException(); //TODO: Invalid Policy Type Exception
                }
            }
            return policy;
        }


        private static Policy ProcessCircuitBreaker(string dummy, Policy policy, string key)
        {
            throw new NotImplementedException();
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

        private static Policy ProcessFallback(string valueStr, string valueType, Policy policy, string key)
        {
            if (string.IsNullOrEmpty(valueStr)) throw new NullReferenceException($"value is missing for fallback policy item {key}");
            if (!string.IsNullOrEmpty(valueStr))
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
            throw new NullReferenceException($"retryCount is missing for retry policy item {key}");
        }

#endif
    }
}
