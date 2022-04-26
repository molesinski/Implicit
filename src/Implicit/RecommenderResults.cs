using System;
using System.Collections;
using System.Collections.Generic;

namespace Implicit
{
    public class RecommenderResults
    {
        public RecommenderResults(List<KeyValuePair<string, double>> results)
        {
            this.IsEmpty = results.Count == 0;
            this.Results = new ResultsCollection(results);
        }

        public static RecommenderResults Empty { get; } = new RecommenderResults(new List<KeyValuePair<string, double>>(capacity: 0));

        public bool IsEmpty { get; }

        public ResultsCollection Results { get; }

        public sealed class ResultsCollection : IEnumerable<string>
        {
            private readonly List<KeyValuePair<string, double>> results;

            internal ResultsCollection(List<KeyValuePair<string, double>> results)
            {
                this.results = results;
            }

            public int Count
            {
                get
                {
                    return this.results.Count;
                }
            }

            public ResultsCollectionEnumerator GetEnumerator()
            {
                return new ResultsCollectionEnumerator(this.results);
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
                private readonly List<KeyValuePair<string, double>> results;
                private readonly int count;
                private int index;
                private string? current;

                internal ResultsCollectionEnumerator(List<KeyValuePair<string, double>> results)
                {
                    this.results = results;
                    this.count = results.Count;
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
                        this.current = this.results[this.index].Key;
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
