using System;
using System.Collections;
using System.Collections.Generic;


namespace CompositionalPooling.Utility
{
    /// <summary>
    /// Represents a list of objects maintained in a sorted order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    public sealed class SortedList<T> : IList<T>, IReadOnlyList<T>
    {
        private const int GrowthFactor = 2;
        private T[] _array;

        /// <summary>
        /// The comparer object defining the sort order of elements in the list.
        /// </summary>
        public readonly IComparer<T> Comparer;
        public int Count { get; private set; } = 0;
        public int Capacity => _array.Length;
        public bool IsReadOnly => _array.IsReadOnly;


        public SortedList(IComparer<T> comparer, int startCapacity = 8)
        {
            Comparer = comparer;
            _array = new T[startCapacity];
        }

        public SortedList(IComparer<T> comparer, IEnumerable<T> collection)
        {
            Comparer = comparer;
            _array = new T[collection is IReadOnlyCollection<T> c ? c.Count : 8];

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public SortedList(IEnumerable<T> collection) : this(Comparer<T>.Default, collection) { }
        public SortedList() : this(Comparer<T>.Default) { }


        public T this[int index]
        {
            get => index < Count ? _array[index] : throw new ArgumentOutOfRangeException(nameof(index));
            set
            {
                if (index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                int insertionIndex = ApproximateIndexOf(value);

                if (insertionIndex > index)
                {
                    insertionIndex--; // Item at the current index will be removed which causes the shift of its following elements.

                    for (int i = index; i < insertionIndex; i++)
                    {
                        _array[i] = _array[i + 1];
                    }
                }
                else
                {
                    for (int i = insertionIndex + 1; i <= index; i++)
                    {
                        _array[i] = _array[i - 1];
                    }
                }

                _array[insertionIndex] = value;
            }
        }

        /// <inheritdoc cref="ApproximateIndexOf(T, int, int)"/>
        public int ApproximateIndexOf(T item, int index = 0) => ApproximateBinarySearch(item, _array, Comparer, index, Count);

        /// <inheritdoc cref="ApproximateBinarySearch{TList}(T, TList, IComparer{T}, int, int)"/>
        /// <param name="index">The lower bound of the search range</param>
        /// <param name="length">The length of the search range</param>
        public int ApproximateIndexOf(T item, int index, int length) => ApproximateBinarySearch(item, _array, Comparer, index, length);
        public int IndexOf(T item)
        {
            if (Count > 0)
            {
                int approxIndex = ApproximateBinarySearch(item, _array, Comparer, 0, Count - 1);
                var equalityComparer = EqualityComparer<T>.Default;

                if (equalityComparer.Equals(item, _array[approxIndex]))
                {
                    return approxIndex;
                }

                for (int i = approxIndex + 1; i < Count && Comparer.Compare(item, _array[i]) == 0; i++)
                {
                    if (equalityComparer.Equals(item, _array[i]))
                    {
                        return i;
                    }
                }

                for (int i = approxIndex - 1; i >= 0 && Comparer.Compare(item, _array[i]) == 0; i--)
                {
                    if (equalityComparer.Equals(item, _array[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public void RemoveAt(int index)
        {
            for (int i = index; i < Count - 1; i++)
            {
                _array[i] = _array[i + 1];
            }

            Count--;
        }

        public void Add(T item)
        {
            int insertIndex = ApproximateBinarySearch(item, _array, Comparer, 0, Count);
            var array = _array;

            if (Count == Capacity)
            {
                array = new T[GrowthFactor * Count];

                for (int i = 0; i < insertIndex; i++)
                {
                    array[i] = _array[i];
                }
            }

            for (int i = Count; i > insertIndex; i--)
            {
                array[i] = _array[i - 1];
            }

            array[insertIndex] = item;
            _array = array;
            Count++;
        }

        public void Clear()
        {
            Array.Clear(_array, 0, Count);
            Count = 0;
        }

        public bool Contains(T item) => IndexOf(item) != -1;
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_array, 0, array, arrayIndex, Count);
        public bool Remove(T item)
        {
            int index = IndexOf(item);

            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the index at which the element would be if it existed in the list.
        /// </summary>
        /// <typeparam name="TList">Type of the list</typeparam>
        /// <param name="item">The item to look for</param>
        /// <param name="items">The ordered list</param>
        /// <param name="comparer">The comparer object defining the list's order</param>
        /// <param name="startIndex">The lower bound of the search range</param>
        /// <param name="endIndex">The upper bound of the search range</param>
        /// <returns>Element's order index in the list</returns>
        public static int ApproximateBinarySearch<TList>(T item, TList items, IComparer<T> comparer, int startIndex, int endIndex) where TList : IReadOnlyList<T>
        {
            while (endIndex > startIndex)
            {
                int mid = (startIndex + endIndex) / 2;
                int comparison = comparer.Compare(item, items[mid]);

                if (comparison < 0)
                {
                    endIndex = mid;
                }
                else if (comparison > 0)
                {
                    startIndex = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return startIndex;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Insertion operation is not allowed in a sorted list!");

        public struct Enumerator : IEnumerator<T>
        {
            private SortedList<T> _list;
            private int _index;

            public Enumerator(SortedList<T> list)
            {
                _list = list;
                _index = -1;
            }

            public T Current => _list[_index];
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++_index < _list.Count;

            public void Dispose() => _list = null;
            public void Reset() => _index = -1;
        }
    }
}
