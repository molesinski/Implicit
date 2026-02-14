namespace Implicit
{
    public static class UserItemValueExtensions
    {
        public static Dictionary<string, Dictionary<string, float>> ToUserItems(IEnumerable<UserItemValue> userItemValues)
        {
            if (userItemValues is null)
            {
                throw new ArgumentNullException(nameof(userItemValues));
            }

            var userItems = new Dictionary<string, Dictionary<string, float>>();

            foreach (var userItemValue in userItemValues)
            {
                if (!userItems.TryGetValue(userItemValue.UserId, out var items))
                {
                    userItems.Add(userItemValue.UserId, items = new Dictionary<string, float>());
                }

                if (items.TryGetValue(userItemValue.ItemId, out var currentValue))
                {
                    items[userItemValue.ItemId] = currentValue + userItemValue.Value;
                }
                else
                {
                    items.Add(userItemValue.ItemId, userItemValue.Value);
                }
            }

            return userItems;
        }
    }
}
