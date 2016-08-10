using System;
using System.Threading;
#if SUPPORTS_ASYNC
using System.Threading.Tasks;
#endif

namespace Polly.Configuration
{
    
    public interface IFallbackValueProvider
    {
#if SUPPORTS_ASYNC
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<object> ExecuteAsync(CancellationToken cancellationToken, Context context);
#else
        object Execute();
#endif

    }
}
