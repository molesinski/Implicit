namespace Implicit
{
    public interface IResultsBuilder<TResults>
    {
        void Add(string key, double score);

        TResults ToResults();
    }
}
