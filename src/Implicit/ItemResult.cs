namespace Implicit
{
    public class ItemResult
    {
        public ItemResult(string itemId, float score)
        {
            this.ItemId = itemId;
            this.Score = score;
        }

        public string ItemId { get; }

        public float Score { get; }
    }
}
