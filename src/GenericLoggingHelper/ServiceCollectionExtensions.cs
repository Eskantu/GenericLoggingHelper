using GenericLoggingHelper;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eskantu.Logging
{
    public static class ServiceCollectionExtensions
    {
        private static LoggingOptions _globalOptions = new();

        /// <summary>
        /// Configura el logging global para todos los proxies.
        /// </summary>
        public static IServiceCollection AddGenericLoggingProxy(this IServiceCollection services, Action<LoggingOptions> configure = null)
        {
            services.AddSingleton(sp =>
            {
                var options = new LoggingOptions();

                // Inyectamos ILogger automáticamente desde DI
                var loggerType = typeof(ILogger<>).MakeGenericType(typeof(GenericLoggingProxy<>));
                var logger = sp.GetService(loggerType) as ILogger;
                options.Logger = logger;

                configure?.Invoke(options);

                return options;
            });

            return services;
        }

        ///// <summary>
        ///// Registra un servicio con logging usando la configuración global.
        ///// </summary>
        //public static IServiceCollection AddWithLogging<TInterface, TImplementation>(this IServiceCollection services)
        //    where TInterface : class
        //    where TImplementation : class, TInterface
        //{
        //    return services.AddWithLogging<TInterface, TImplementation>();
        //}

        /// <summary>
        /// Registra un servicio con logging usando configuración específica (override).
        /// </summary>
        /// Registro de servicios con logging opcional
    public static IServiceCollection AddWithLogging<TInterface, TImplementation>(
        this IServiceCollection services,
        Action<LoggingOptions> configureOverride = null)
        where TInterface : class
        where TImplementation : class, TInterface
        {
            services.AddTransient<TImplementation>();

            services.AddTransient<TInterface>(sp =>
            {
                var realService = sp.GetRequiredService<TImplementation>();
                var globalOptions = sp.GetRequiredService<LoggingOptions>();

                var localOptions = new LoggingOptions();

                // Aplica override si existe
                configureOverride?.Invoke(localOptions);

                // Si no hay OnException en local, usa el global
                if (configureOverride == null)
                    localOptions.OnException = globalOptions.OnException;

                // Logger fallback
                if (localOptions.Logger == null)
                    localOptions.Logger = globalOptions.Logger;

                var proxyFactory = new GenericLoggingProxyFactory<TInterface>(localOptions);
                return proxyFactory.Create(realService);
            });

            return services;
        }
    }
}
