namespace Implicit.Weights
{
    public abstract class Weight
    {
        public void Normalize(Dictionary<string, Dictionary<string, float>> userItems)
        {
            if (userItems is null)
            {
                throw new ArgumentNullException(nameof(userItems));
            }

            foreach (var user in userItems)
            {
                var items = user.Value;

                this.Normalize(items);
            }
        }

        public abstract void Normalize(Dictionary<string, float> items);

        public abstract void Save(Stream stream);
    }
}
