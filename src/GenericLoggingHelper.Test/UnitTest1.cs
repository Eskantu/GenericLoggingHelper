

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Eskantu.Logging;
using System.Reflection;
namespace GenericLoggingHelper.Test
{
    public class TestService : ITestService
    {
        public void DoWork()
        {
            Console.WriteLine("Doing work!");
        }

        public void FailWork()
        {
            throw new InvalidOperationException("Something went wrong");
        }

        public void FailWithArgs(string orderId, int retries)
        {
            throw new InvalidOperationException($"Order {orderId} failed after {retries} retries");
        }

        public async Task FailWorkAsync()
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async failure");
        }
    }

    public class LoggingProxyTests
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }

        private sealed class TestLogger : ILogger
        {
            public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception), exception));
            }
        }

        [Fact]
        public void Should_Call_Method_Through_Proxy()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            services.AddGenericLoggingProxy(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    Console.WriteLine($"[Global] {method.Name} failed: {ex.Message}");
                };
            });

            //services.AddGenericLoggingProxy(); //Opcional

            services.AddWithLogging<ITestService, TestService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act & Assert
            var exception = Record.Exception(() => service.DoWork());
            Assert.Null(exception); // No debe fallar
        }

        [Fact]
        public void Should_Use_Global_OnException_When_No_Local_Config()
        {
            // Arrange
            bool globalCalled = false;
            IServiceCollection services = new ServiceCollection();

            services.AddGenericLoggingProxy(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    globalCalled = true;
                };
            });

            services.AddWithLogging<ITestService, TestService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act
            var exception = Record.Exception(() => service.FailWork());

            // Assert
            Assert.NotNull(exception);
            Assert.True(globalCalled);
        }

        [Fact]
        public void Should_Use_Local_OnException_If_Configured()
        {
            // Arrange
            bool localCalled = false;
            bool globalCalled = false;

            IServiceCollection services = new ServiceCollection();

            services.AddGenericLoggingProxy(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    globalCalled = true;
                };
            });

            services.AddWithLogging<ITestService, TestService>(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    localCalled = true;
                };
            });

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act
            var exception = Record.Exception(() => service.FailWork());

            // Assert
            Assert.NotNull(exception);
            Assert.True(localCalled);
            Assert.False(globalCalled); // No debe llamarse el global
        }

        [Fact]
        public void Should_Throw_When_Global_Options_Are_Not_Registered()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            services.AddWithLogging<ITestService, TestService>();
            var provider = services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ITestService>());
        }

        [Fact]
        public void Should_Pass_Method_And_Arguments_To_OnException_Callback()
        {
            // Arrange
            string? capturedMethod = null;
            object?[]? capturedArgs = null;

            IServiceCollection services = new ServiceCollection();
            services.AddGenericLoggingProxy(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    capturedMethod = method.Name;
                    capturedArgs = args;
                };
            });
            services.AddWithLogging<ITestService, TestService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act
            Assert.Throws<InvalidOperationException>(() => service.FailWithArgs("ORD-100", 3));

            // Assert
            Assert.Equal(nameof(ITestService.FailWithArgs), capturedMethod);
            Assert.NotNull(capturedArgs);
            Assert.Equal("ORD-100", capturedArgs![0]);
            Assert.Equal(3, capturedArgs[1]);
        }

        [Fact]
        public void Should_Rethrow_Original_Exception_Type_And_Message()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            services.AddGenericLoggingProxy();
            services.AddWithLogging<ITestService, TestService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => service.FailWork());

            // Assert
            Assert.Equal("Something went wrong", ex.Message);
        }

        [Fact]
        public async Task Should_Not_Invoke_OnException_For_Async_Failure_In_Current_Interceptor()
        {
            // Arrange
            bool onExceptionCalled = false;
            IServiceCollection services = new ServiceCollection();
            services.AddGenericLoggingProxy(opt =>
            {
                opt.OnException = (ex, method, args) =>
                {
                    onExceptionCalled = true;
                };
            });
            services.AddWithLogging<ITestService, TestService>();

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestService>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.FailWorkAsync());
            Assert.False(onExceptionCalled);
        }

        [Fact]
        public void LoggingOptions_Default_OnException_Uses_Logger_When_Available()
        {
            // Arrange
            var logger = new TestLogger();
            var options = new LoggingOptions
            {
                Logger = logger
            };
            var method = typeof(ITestService).GetMethod(nameof(ITestService.FailWork));

            // Act
            options.OnException(new InvalidOperationException("Boom"), method!, Array.Empty<object?>());

            // Assert
            Assert.Contains(logger.Entries, entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains("FailWork") &&
                entry.Exception?.Message == "Boom");
        }

        [Fact]
        public void LoggingOptions_Default_OnException_Falls_Back_To_Console_When_Logger_Is_Null()
        {
            // Arrange
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                var options = new LoggingOptions
                {
                    Logger = null
                };
                var method = typeof(ITestService).GetMethod(nameof(ITestService.FailWork));

                // Act
                options.OnException(new InvalidOperationException("Console boom"), method!, Array.Empty<object?>());

                // Assert
                var output = writer.ToString();
                Assert.Contains("FailWork", output);
                Assert.Contains("Console boom", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void DispatchProxy_Should_Throw_If_Invoked_Before_Configure()
        {
            // Arrange
            var proxyAsService = DispatchProxy.Create<ITestService, GenericLoggingProxy<ITestService>>();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => proxyAsService.DoWork());
            Assert.Contains("Proxy is not configured", ex.Message);
        }

        [Fact]
        public void DispatchProxy_Configure_Should_Validate_Null_Arguments()
        {
            // Arrange
            var proxyAsService = DispatchProxy.Create<ITestService, GenericLoggingProxy<ITestService>>();
            var proxy = (GenericLoggingProxy<ITestService>)proxyAsService;
            var logger = NullLogger<ITestService>.Instance;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => proxy.Configure(null!, logger, (_, _, _) => { }));
            Assert.Throws<ArgumentNullException>(() => proxy.Configure(new TestService(), null!, (_, _, _) => { }));
        }

        [Fact]
        public void DispatchProxy_Should_Invoke_OnException_And_Rethrow_Inner_Exception()
        {
            // Arrange
            bool callbackCalled = false;
            var proxyAsService = DispatchProxy.Create<ITestService, GenericLoggingProxy<ITestService>>();
            var proxy = (GenericLoggingProxy<ITestService>)proxyAsService;

            var logger = NullLogger<ITestService>.Instance;

            proxy.Configure(new TestService(), logger, (ex, method, args) =>
            {
                callbackCalled = true;
            });

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => proxyAsService.FailWork());

            // Assert
            Assert.True(callbackCalled);
            Assert.Equal("Something went wrong", ex.Message);
        }
    }
}