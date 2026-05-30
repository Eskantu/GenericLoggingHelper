namespace GenericLoggingHelper.Test
{
    public interface ITestService
    {
        void DoWork();
        void FailWork();
        void FailWithArgs(string orderId, int retries);
        Task FailWorkAsync();
    }
}
