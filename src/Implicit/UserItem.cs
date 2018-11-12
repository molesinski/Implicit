namespace Implicit
{
    public struct UserItem
    {
        public UserItem(string userId, string itemId, float confidence)
        {
            this.UserId = userId;
            this.ItemId = itemId;
            this.Confidence = confidence;
        }

        public string UserId { get; }

        public string ItemId { get; }

        public float Confidence { get; }
    }
}
