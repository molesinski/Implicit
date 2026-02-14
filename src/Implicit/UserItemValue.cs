namespace Implicit
{
    public readonly record struct UserItemValue
    {
        public UserItemValue(string userId, string itemId, float value)
        {
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (itemId is null)
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            this.UserId = userId;
            this.ItemId = itemId;
            this.Value = value;
        }

        public string UserId { get; }

        public string ItemId { get; }

        public float Value { get; }
    }
}
