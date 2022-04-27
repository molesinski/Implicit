namespace Implicit
{
    public interface IResultsBuilderFactory<TResults>
    {
        TResults CreateEmpty();

        IResultsBuilder<TResults> CreateBuilder(int maximumCapacity);
    }
}
