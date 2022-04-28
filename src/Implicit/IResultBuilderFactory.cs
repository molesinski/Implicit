namespace Implicit
{
    public interface IResultBuilderFactory<TResult>
    {
        TResult CreateEmpty();

        IResultBuilder<TResult> CreateBuilder(int maximumCapacity);
    }
}
