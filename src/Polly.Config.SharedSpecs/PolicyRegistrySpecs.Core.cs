#if PORTABLE
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Configuration.Specs
{
    public class TestFallbackValueProvider : IFallbackValueProvider
    {
        private readonly IDictionary<string, string> _attributes;

        public TestFallbackValueProvider()
            : this(new Dictionary<string, string>())
        {

        }

        public TestFallbackValueProvider(IDictionary<string, string> attributes)
        {
            _attributes = attributes;
        }

        public async Task<object> ExecuteAsync(CancellationToken cancellationToken, Context context)
        {
            return await Task.FromResult<object>(_attributes["actionName"]);
        }
    }


    public class PolicyRegistrySpecs
    {
        [Fact]
        public void Should_throw_when_policy_name_is_null()
        {
            string name = null;
            IConfigurationRoot configuration = null;
            Action action = () => { PolicyRegistry.Resolve(name, configuration); };
            action.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("name");
        }

        [Fact]
        public void Should_throw_when_configuration_source_is_null()
        {
            string name = "name";
            IConfigurationRoot configuration = null;
            Action action = () => { PolicyRegistry.Resolve(name, configuration); };
            action.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("configuration");
        }

        [Fact]
        public void Should_resolve_fallback_with_value_provider()
        {
            string name = "SimpleFallback";

            var dic = new Dictionary<string, string>
            {
                {"Polly:SimpleFallback:Handle:exceptionType", "System.Exception"},
                {"Polly:SimpleFallback:Handle:order", "1"},
                {"Polly:SimpleFallback:Fallback:order", "2"},
                {"Polly:SimpleFallback:Fallback:valueProviderType", typeof(TestFallbackValueProvider).AssemblyQualifiedName},
                {"Polly:SimpleFallback:Fallback:actionName", "Fallback"},
            };

            /* JSON would be: */
            /*
            "polly": {
                "SimpleFallback": {
                    "Handle": {
                        "order": 1,
                        "exceptionType": "System.Exception"
                    },
                    "Fallback": {
                        "order": 2,
                        "valueProviderType": "Polly.Configuration.Specs.TestFallbackValueProvider",
                        "actionName": "Fallback"
                    }
                }
            }
            */

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            IConfigurationRoot configuration = configurationBuilder.Build();


            var actual = PolicyRegistry.Resolve(name, configuration);
            actual.GetType().Name.Should().Be("FallbackPolicy");

            var actualValueTask = actual.ExecuteAsync<string>(() => { throw new InvalidOperationException(); });
            actualValueTask.Wait();
            var actualValue = actualValueTask.Result;
            actualValue.Should().Be("Fallback");
        }



        [Fact]
        public void Should_resolve_simple_retry()
        {
            string name = "SimpleRetry";

            var dic = new Dictionary<string, string>
            {
                {"Polly:SimpleRetry:Handle:exceptionType", "System.Exception"},
                {"Polly:SimpleRetry:Retry:retryCount", "3"},
            };

            /* JSON would be: */
            /*
            "polly": {
                "SimpleRetry": {
                    "Handle": {
                        "exceptionType": "System.Exception"
                    },
                    "Retry": {
                        "retryCount": 3
                    }
                }
            }
            */

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            IConfigurationRoot configuration = configurationBuilder.Build();


            var actual = PolicyRegistry.Resolve(name, configuration);
            actual.GetType().Name.Should().Be("RetryPolicy");
        }

        [Fact]
        public void Should_resolve_simple_circuit_breaker()
        {
            string name = "SimpleCircuitBreaker";

            var dic = new Dictionary<string, string>
            {
                {"Polly:SimpleCircuitBreaker:Handle:order", "1" },
                {"Polly:SimpleCircuitBreaker:Handle:exceptionType", "System.Exception"},
                {"Polly:SimpleCircuitBreaker:CircuitBreaker:order", "2" },
                {"Polly:SimpleCircuitBreaker:CircuitBreaker:exceptionsAllowedBeforeBreaking", "3"},
                {"Polly:SimpleCircuitBreaker:CircuitBreaker:durationOfBreakInSeconds", "3"},
            };

            /* JSON would be: */
            /*
            "polly": {
                "SimpleRetry": {
                    "Handle": {
                        "order": 1,
                        "exceptionType": "System.Exception"
                    },
                    "CircuitBreaker": {
                        "order": 2,
                        "exceptionsAllowedBeforeBreaking": 3,
                        "durationOfBreakInSeconds": 3
                    }
                }
            }
            */

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            IConfigurationRoot configuration = configurationBuilder.Build();


            var actual = PolicyRegistry.Resolve(name, configuration);
            actual.GetType().Name.Should().Be("CircuitBreakerPolicy");
            actual.AsCircuitBreaker().CircuitState.Should().Be(CircuitBreaker.CircuitState.Closed);
        }

        internal class PollyConfig
        {
            public IEnumerable<PolicyItem> Policies { get; set; }
        }

        internal class PolicyItem
        {
            public IEnumerable<PolicyStep> Steps { get; set; }
        }

        internal class PolicyStep
        {
            public string Type { get; set; }
        }


    }
}
#endif