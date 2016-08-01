#if PORTABLE
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.Primitives;

namespace Polly.Configuration.Specs
{
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