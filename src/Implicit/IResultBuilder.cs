namespace Implicit
{
    public interface IResultBuilder<TResult>
    {
        void Append(string key, double score);

        TResult ToResult();
    }
}
