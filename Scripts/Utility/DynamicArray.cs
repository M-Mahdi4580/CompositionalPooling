using System;
using System.Collections;
using System.Collections.Generic;


namespace CompositionalPooling.Utility
{
    /// <summary>
    /// Represents a dynamically resized array of objects.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    public class DynamicArray<T> : IList<T>, IReadOnlyList<T>
    {
        private const int GrowthFactor = 2;
        private T[] _array;

        public int Count { get; private set; } = 0;
        public int Capacity => _array.Length;
        public bool IsReadOnly => _array.IsReadOnly;


        public DynamicArray(int startCapacity = 4)
        {
            _array = new T[startCapacity];
        }

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


        public void Add(T item)
        {
            if (Count == Capacity)
            {
                Array.Resize(ref _array, 2 * Capacity);
            }

            _array[Count++] = item;
        }

        public void Clear()
        {
            Array.Clear(_array, 0, Count);
            Count = 0;
        }

        public void Insert(int index, T item)
        {
            if (index > Count || index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (Count == Capacity)
            {
                var newArray = new T[GrowthFactor * Count];

                Array.Copy(_array, newArray, index);
                Array.Copy(_array, index, newArray, index + 1, Count - index);
                _array[index] = item;
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

        public void RemoveAt(int index)
        {
            for (int i = index; i < Count - 1; i++)
            {
                _array[i] = _array[i + 1];
            }

            Count--;
        }

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

        public void Sort() => Array.Sort(_array);
        public void Sort(Comparison<T> comparison) => Array.Sort(_array, comparison);
        public void Sort(int index, int length) => Array.Sort(_array, index, length);
        public void Sort(IComparer<T> comparer, int index, int length) => Array.Sort(_array, index, length, comparer);

        public int BinarySearch(in T value) => Array.BinarySearch(_array, value);
        public int BinarySearch(in T value, IComparer<T> comparer) => Array.BinarySearch(_array, value, comparer);
        public int BinarySearch(in T value, int index, int length) => Array.BinarySearch(_array, index, length, value);
        public int BinarySearch(in T value, int index, int length, IComparer<T> comparer) => Array.BinarySearch(_array, index, length, value, comparer);

        public bool Contains(T item) => IndexOf(item) != 0;
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_array, 0, array, arrayIndex, Count);
        public int IndexOf(T item) => Array.IndexOf(_array, item, 0, Count);
        public Enumerator GetEnumerator() => new Enumerator(this);
        T IReadOnlyList<T>.this[int index] => this[index];
        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


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
