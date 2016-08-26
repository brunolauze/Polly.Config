using Microsoft.Extensions.Configuration;
using Polly.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Config.Demo
{
    public class Program
    {
        public static void Main(string[] args)
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
            IServiceProvider serviceProvider = null;

            var actual = PolicyRegistry.Resolve(name, configuration, serviceProvider);
            var typeName = actual.GetType().Name;

            var actualValueTask = actual.ExecuteAsync<string>(() => { throw new InvalidOperationException(); });
            actualValueTask.Wait();
            var actualValue = actualValueTask.Result;
            //actualValue.Should().Be("Fallback");
        }
    }

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

}
