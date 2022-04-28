namespace Implicit
{
    public readonly struct DataRow
    {
        public DataRow(string userId, string itemId, double confidence)
        {
            this.UserId = userId;
            this.ItemId = itemId;
            this.Confidence = confidence;
        }

        public string UserId { get; }

        public string ItemId { get; }

        public double Confidence { get; }
    }
}
