using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GenericLoggingHelper
{
    public class LoggingOptions
    {
        public Action<Exception, MethodInfo, object[]> OnException { get; set; }

        public ILogger Logger { get; set; }

        public LoggingOptions()
        {
            // Comportamiento por defecto usando ILogger si existe
            OnException = (ex, method, args) =>
            {
                if (Logger != null)
                {
                    Logger.LogError(ex, "[Default Logging] {Method} failed with args: {Args}", method.Name, args);
                }
                else
                {
                    // Fallback a Console si no hay ILogger
                    Console.WriteLine($"[Default Logging] {method.Name} falló: {ex.Message}");
                }
            };
        }
    }
}
