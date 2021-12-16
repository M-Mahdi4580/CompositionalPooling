namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a resizable array.
    /// </summary>
    /// <typeparam name="T">Type of the array elements.</typeparam>
    public class DynamicArray<T> : IList<T>, IReadOnlyList<T>
    {
        private const int DefaultCapacity = 4;
        private const int GrowthFactor = 2;

        private T[] _array;

        /// <summary>
        /// Determines whether the array is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// The number of items contained in the array.
        /// </summary>
        public int Count { get; private set; } = 0;

        /// <summary>
        /// The number of items the array can hold without resizing.
        /// </summary>
        public int Capacity
        {
            get => _array.Length;
            set => Array.Resize(ref _array, value);
        }


        /// <summary>
        /// Initializes the array with the given capacity.
        /// </summary>
        /// <param name="capacity">Capacity of the array.</param>
        public DynamicArray(int capacity = DefaultCapacity)
        {
            _array = new T[capacity];
        }

        /// <summary>
        /// Initializes the array with the given collection.
        /// </summary>
        /// <param name="collection">The collection to add to the array.</param>
		public DynamicArray(IEnumerable<T> collection) : this(collection is IReadOnlyCollection<T> c ? c.Count : DefaultCapacity)
		{
			foreach (T item in collection)
			{
                Add(item);
			}
		}


        /// <summary>
        /// Gets the reference to the array item at the given index.
        /// </summary>
        /// <param name="index">Index of the array item.</param>
        /// <returns>Reference to the array item.</returns>
        public ref T this[int index]
        {
            get
            {
                if (index < Count)
                {
                    return ref _array[index];
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Adds the item to the array.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (Count == Capacity)
            {
                Capacity = Capacity == 0 ? DefaultCapacity : GrowthFactor * Capacity;
            }

            _array[Count++] = item;
        }

        /// <summary>
        /// Clears the array from all items.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_array, 0, Count);
            Count = 0;
        }

        /// <summary>
        /// Inserts the item at the given index.
        /// </summary>
        /// <param name="index">The index to insert the item at.</param>
        /// <param name="item">The item to insert.</param>
        public void Insert(int index, T item)
        {
            if (index > Count || index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (Count == Capacity)
            {
                T[] array = new T[Capacity == 0 ? DefaultCapacity : GrowthFactor * Capacity];

                Array.Copy(_array, array, index);
                Array.Copy(_array, index, array, index + 1, Count - index);
                array[index] = item;

                _array = array;
            }
            else
            {
                for (int i = Count; i > index; i--)
                {
                    _array[i] = _array[i - 1];
                }

                _array[index] = item;
            }

            Count++;
        }

        /// <summary>
        /// Removes the item at the given index.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            for (int i = index; i < Count - 1; i++)
            {
                _array[i] = _array[i + 1];
            }

            _array[--Count] = default;
        }

        /// <summary>
        /// Removes the given item from the array.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if item is removed; false otherwise.</returns>
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
        /// Sorts the array.
        /// </summary>
        public void Sort() => Array.Sort(_array);

        /// <param name="comparison">The delegate defining the sort order of the list items.</param>
        /// <inheritdoc cref="Sort"/>
        public void Sort(Comparison<T> comparison) => Array.Sort(_array, comparison);

        /// <param name="index">The index of the item to start sorting from.</param>
        /// <param name="length">The number of items to sort.</param>
        /// <inheritdoc cref="Sort"/>
        public void Sort(int index, int length) => Array.Sort(_array, index, length);

        /// <param name="comparer">The comparer defining the sort order of the list items.</param>
        /// <inheritdoc cref="Sort(int, int)"/>
        public void Sort(IComparer<T> comparer, int index, int length) => Array.Sort(_array, index, length, comparer);

        /// <inheritdoc cref="Array.BinarySearch(Array, object)"/>
        public int BinarySearch(T value) => Array.BinarySearch(_array, value);

        /// <inheritdoc cref="Array.BinarySearch(Array, object, IComparer)"/>
        public int BinarySearch(T value, IComparer<T> comparer) => Array.BinarySearch(_array, value, comparer);

        /// <inheritdoc cref="Array.BinarySearch{T}(T[], int, int, T)"/>
        public int BinarySearch(T value, int index, int length) => Array.BinarySearch(_array, index, length, value);

        /// <inheritdoc cref="Array.BinarySearch{T}(T[], int, int, T, IComparer{T}?)"/>
        public int BinarySearch(T value, int index, int length, IComparer<T> comparer) => Array.BinarySearch(_array, index, length, value, comparer);

        /// <summary>
        /// Determines if the array contains the given items.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item exists in the array; false otherwise.</returns>
        public bool Contains(T item) => IndexOf(item) != 0;

        /// <summary>
        /// Copies the items.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The position in the <paramref name="array"/> to start copying from.</param>
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_array, 0, array, arrayIndex, Count);

        /// <summary>
        /// Finds the index of the given item.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>Index of the item if found; -1 otherwise.</returns>
        public int IndexOf(T item) => Array.IndexOf(_array, item, 0, Count);


        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        T IReadOnlyList<T>.this[int index] => this[index];
        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }


        public struct Enumerator : IEnumerator<T>
        {
            private DynamicArray<T> _array;
            private int _index;

            public Enumerator(DynamicArray<T> array)
            {
                _array = array;
                _index = -1;
            }

            public ref T Current => ref _array[_index];
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _array.Count;
            public void Dispose() => _array = null;
            public void Reset() => _index = -1;
        }
    }
}