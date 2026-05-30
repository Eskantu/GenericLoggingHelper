using Microsoft.Extensions.Logging;

using System.Reflection;

namespace GenericLoggingHelper
{
    public class GenericLoggingProxy<T> : DispatchProxy where T : class
    {
        private T? _decorated;
        private ILogger<T>? _logger;
        private Action<Exception, MethodInfo, object?[]>? _onException;

        public void Configure(T decorated, ILogger<T> logger, Action<Exception, MethodInfo, object?[]> onException)
        {
            _decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onException = onException;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            if (_decorated == null || _logger == null)
                throw new InvalidOperationException("Proxy is not configured. Call Configure before invoking methods.");

            var invocationArgs = args ?? Array.Empty<object?>();

            try
            {
                _logger.LogInformation("Ejecutando {Method} en {Type}", targetMethod.Name, typeof(T).Name);
                return targetMethod.Invoke(_decorated, invocationArgs);
            }
            catch (TargetInvocationException ex)
            {
                _logger.LogError(ex.InnerException, "Error en {Method} de {Type}", targetMethod.Name, typeof(T).Name);
                _onException?.Invoke(ex.InnerException ?? ex, targetMethod, invocationArgs);
                throw ex.InnerException ?? ex;
            }
        }
    }
}
