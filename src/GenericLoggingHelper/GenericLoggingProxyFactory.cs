using Castle.DynamicProxy;

using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GenericLoggingHelper
{
    public class GenericLoggingProxyFactory<T> : IGenericLoggingProxy<T> where T : class
    {
        private readonly LoggingOptions _options;
        private readonly ProxyGenerator _generator = new();

        public GenericLoggingProxyFactory(LoggingOptions options)
        {
            _options = options;
        }

        public T Create(T instance)
        {
            return (T)_generator.CreateInterfaceProxyWithTarget(
                typeof(T),
                instance,
                new Interceptor(_options)
            );
        }

        private class Interceptor : IInterceptor
        {
            private readonly LoggingOptions _options;

            public Interceptor(LoggingOptions options)
            {
                _options = options;
            }

            public void Intercept(IInvocation invocation)
            {
                try
                {
                    invocation.Proceed();
                }
                catch (Exception ex)
                {
                    _options.OnException(ex, invocation.Method, invocation.Arguments);
                    throw;
                }
            }
        }
    }
}
