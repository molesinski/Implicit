namespace Implicit
{
    public class ItemResult
    {
        public ItemResult(string itemId, double score)
        {
            this.ItemId = itemId;
            this.Score = score;
        }

        public string ItemId { get; }

        public double Score { get; }
    }
}
