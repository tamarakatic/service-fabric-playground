using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.ServiceFabric.Data;
using System.Collections;

namespace Common
{
    /// <summary>
    /// Because the book's example didn't work for IReliableDictionary 
    /// I added this class for converting IReliableDictionary into Enumerable object. 
    /// </summary>
    public static class AsyncEnumerableExtensions
    {
        public static IEnumerable<TSource> ToEnumerable<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentException("source");
            }

            return new AsyncEnumerableWrapper<TSource>(source);
        }

        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> instance, CancellationToken cancellationToken, Action<T> doSomething)
        {
            using (IAsyncEnumerator<T> e = instance.GetAsyncEnumerator())
            {
                while (await e.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    doSomething(e.Current);
                }
            }
        }

        public static async Task<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var count = 0;
            using (var asyncEnumerator = source.GetAsyncEnumerator())
            {
                while (await asyncEnumerator.MoveNextAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    if (predicate(asyncEnumerator.Current))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        internal struct AsyncEnumerableWrapper<TSource> : IEnumerable<TSource>
        {
            private readonly IAsyncEnumerable<TSource> _source;

            public AsyncEnumerableWrapper(IAsyncEnumerable<TSource> source)
            {
                this._source = source;
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                return new AsyncEnumeratorWrapper<TSource>(_source.GetAsyncEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct AsyncEnumeratorWrapper<TSource> : IEnumerator<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private TSource _current;

            public AsyncEnumeratorWrapper(IAsyncEnumerator<TSource> source)
            {
                _source = source;
                _current = default(TSource);
            }
            public TSource Current => _current;

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose() { }

            public bool MoveNext()
            {
                if (!_source.MoveNextAsync(CancellationToken.None).GetAwaiter().GetResult())
                {
                    return false;
                }
                _current = _source.Current;
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}