

using Microsoft.Extensions.DependencyInjection;

using Eskantu.Logging;
namespace GenericLoggingHelper.Test
{
    public interface ITestService
    {
        void DoWork();
        void FailWork();
    }

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
    }

    public class LoggingProxyTests
    {
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
    }
}