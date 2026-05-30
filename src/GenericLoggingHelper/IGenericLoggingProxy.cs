namespace GenericLoggingHelper
{
    public interface IGenericLoggingProxy<T> where T : class
    {
        T Create(T instance);
    }
}
