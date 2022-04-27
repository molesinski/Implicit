using System;
using System.Collections;
using System.Collections.Generic;

namespace Implicit
{
    public class RecommenderResults
    {
        internal RecommenderResults(RecommenderResultsItem[] storage, int count)
        {
            this.IsEmpty = count == 0;
            this.Results = new ResultsCollection(storage, count);
        }

        public static RecommenderResults Empty { get; } = new RecommenderResults(Array.Empty<RecommenderResultsItem>(), count: 0);

        public bool IsEmpty { get; }

        public ResultsCollection Results { get; }

        public sealed class ResultsCollection : IEnumerable<string>
        {
            private readonly RecommenderResultsItem[] storage;
            private readonly int count;

            internal ResultsCollection(RecommenderResultsItem[] storage, int count)
            {
                this.storage = storage;
                this.count = count;
            }

            public string this[int index]
            {
                get
                {
                    if (index < 0 || index >= this.count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return this.storage[index].Key;
                }
            }

            public int Count
            {
                get
                {
                    return this.count;
                }
            }

            public ResultsCollectionEnumerator GetEnumerator()
            {
                return new ResultsCollectionEnumerator(this.storage, this.count);
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public struct ResultsCollectionEnumerator : IEnumerator<string>
            {
                private readonly RecommenderResultsItem[] storage;
                private readonly int count;
                private int index;
                private string? current;

                internal ResultsCollectionEnumerator(RecommenderResultsItem[] storage, int count)
                {
                    this.storage = storage;
                    this.count = count;
                    this.index = 0;
                    this.current = default;
                }

                public string Current
                {
                    get
                    {
                        if (this.index == 0 || this.index == this.count + 1)
                        {
                            throw new InvalidOperationException("Enumerator is currently positioned before first element or after last element.");
                        }

                        return this.current!;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return this.Current;
                    }
                }

                public bool MoveNext()
                {
                    if (this.index < this.count)
                    {
                        this.current = this.storage[this.index].Key;
                        this.index++;

                        return true;
                    }

                    this.index = this.count + 1;
                    this.current = default;

                    return false;
                }

                public void Reset()
                {
                    this.index = 0;
                    this.current = default;
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
