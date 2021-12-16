using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
    /// <summary>
    /// Identifies a pool using its composition.
    /// </summary>
    public readonly struct PoolHandle : IReadOnlyList<Type>, IEquatable<PoolHandle>, IComparable<PoolHandle>
    {
        private static PoolHandle LastAcceptedHandle; // Captures the last accepted handle compared for equality.

        private readonly IReadOnlyList<Type> _PoolComposition; // Composition of the handle.
        private readonly int _HashCode; // The cached aggregate hashcode.


        /// <summary>
        /// Normalizes the handle using the given handle source.
        /// </summary>
        /// <param name="handle">The handle to normalize.</param>
        /// <param name="source">The collection of normal handles.</param>
        /// <returns>True if the handle is normalized; false otherwise.</returns>
        public static bool TryNormalize(ref PoolHandle handle, ICollection<PoolHandle> source)
        {
            if (source.Contains(handle)) // If the source collection contains an equivalent handle
            {
				if (handle.Equals(LastAcceptedHandle)) // If the handle is captured successfully
				{
                    handle = LastAcceptedHandle; // Normalize the handle.
                    return true;
                }
                else
				{
					foreach (PoolHandle normalHandle in source) // For all the handles in the source collection
					{
						if (handle.Equals(normalHandle)) // If the handles are equivalent
						{
                            handle = normalHandle; // Normalize the handle.
                            return true;
						}
					}
				}
            }

            return false;
        }

        /// <inheritdoc cref="TryNormalize(ref PoolHandle, ICollection{PoolHandle})"/>
        public static void Normalize(ref PoolHandle handle, ICollection<PoolHandle> source)
		{
			if (!TryNormalize(ref handle, source))
			{
                throw new InvalidOperationException();
            }
		}

        /// <returns>The retrieved handle.</returns>
        /// <inheritdoc cref="TryGetHandleFrom(IReadOnlyList{Type}, ICollection{PoolHandle}, out PoolHandle)"/>
        public static PoolHandle GetHandleFrom(IReadOnlyList<Type> composition, ICollection<PoolHandle> source) => TryGetHandleFrom(composition, source, out PoolHandle handle) ? handle : throw new InvalidOperationException();

		/// <summary>
		/// Retrieves a handle with the given composition from the handle source.
		/// </summary>
		/// <param name="source">The collection of handles.</param>
		/// <param name="handle">The retrieved handle.</param>
		/// <returns>True if the handle is retrieved; false otherwise.</returns>
		/// <inheritdoc cref="PoolHandle(IReadOnlyList{Type})"/>
		public static bool TryGetHandleFrom(IReadOnlyList<Type> composition, ICollection<PoolHandle> source, out PoolHandle handle)
        {
            handle = new PoolHandle(composition);
            return TryNormalize(ref handle, source);
        }


        /// <param name="composition">Composition of the handle.</param>
        public PoolHandle(IReadOnlyList<Type> composition)
        {
            _PoolComposition = composition;

            int aggregateHashCode = 17;

            for (int i = 0, len = composition.Count; i < len; i++)
            {
                aggregateHashCode = aggregateHashCode * 37 + composition[i].GetHashCode();
            }
            
            _HashCode = aggregateHashCode;
        }


        public override int GetHashCode() => _HashCode;
        public override bool Equals(object obj) => obj == null || GetType() != obj.GetType() ? false : Equals((PoolHandle)obj);


        public int CompareTo(PoolHandle other) => _HashCode.CompareTo(other._HashCode);
        public bool Equals(PoolHandle other)
        {
            if (_PoolComposition != other._PoolComposition)
            {
                if (_HashCode != other._HashCode || _PoolComposition.Count != other._PoolComposition.Count)
                {
                    return false;
                }

                for (int i = 0, len = other._PoolComposition.Count; i < len; i++)
                {
                    if (_PoolComposition[i] != other._PoolComposition[i])
                    {
                        return false;
                    }
                }
            }

            LastAcceptedHandle = other; // Capture this handle.
            return true;
        }


        public static bool operator ==(PoolHandle x, PoolHandle y) => x.Equals(y);
        public static bool operator !=(PoolHandle x, PoolHandle y) => !x.Equals(y);


        public int Count => _PoolComposition.Count;
        public Type this[int index] => _PoolComposition[index];


        public IEnumerator<Type> GetEnumerator() => _PoolComposition.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Holds a pool's defining properties.
    /// </summary>
    public readonly struct PoolInfo
    {
        /// <summary>
        /// Instances of the pool.
        /// </summary>
        public readonly Queue<Transform> Instances;

        /// <summary>
        /// Maximum number of instances the pool can hold.
        /// </summary>
        public readonly int Capacity;


        /// <summary>
        /// Size of the pool.
        /// </summary>
        public PoolSize Size => new PoolSize(Instances.Count, Capacity);


        public PoolInfo(Queue<Transform> pool, int capacity)
        {
            Instances = pool;
            Capacity = capacity;
        }
    }


    /// <summary>
    /// Represents the size of a pool.
    /// </summary>
    public readonly struct PoolSize
    {
        /// <summary>
        /// Number of pool instances.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Maximum number of pool instances.
        /// </summary>
        public readonly int Capacity;


        public PoolSize(int count, int capacity)
        {
			if (count < 0 || capacity < 0)
			{
                throw new ArgumentOutOfRangeException(count < 0 ? nameof(count) : nameof(capacity));
			}

            Count = count;
            Capacity = capacity;
        }
        

        public static PoolSize operator +(PoolSize x, PoolSize y) => new PoolSize(x.Count + y.Count, x.Capacity > int.MaxValue - y.Capacity ? int.MaxValue : x.Capacity + y.Capacity);
        public static PoolSize operator -(PoolSize x, PoolSize y) => new PoolSize(x.Count - y.Count, x.Capacity - y.Capacity);
        public static bool operator ==(PoolSize x, PoolSize y) => x.Count == y.Count && x.Capacity == y.Capacity;
        public static bool operator !=(PoolSize x, PoolSize y) => !(x == y);


        public override bool Equals(object obj) => obj == null || GetType() != obj.GetType() ? false : this == (PoolSize)obj;
        public override int GetHashCode() => Count ^ Capacity;


		/// <summary>
		/// Performs a component-wise maximization of the pool sizes.
		/// </summary>
		/// <param name="x">The first pool size.</param>
		/// <param name="y">The second pool size.</param>
		/// <returns>The maximized size.</returns>
		public static PoolSize Maximize(PoolSize x, PoolSize y) => new PoolSize(Mathf.Max(x.Count, y.Count), Mathf.Max(x.Capacity, y.Capacity));
    }

    /// <summary>
    /// Holds the parameters required for hierarchy-based mapping operations.
    /// </summary>
    public readonly ref struct MappingContext
    {
        /// <summary>
        /// The source hierarchy root of the source object.
        /// </summary>
        public readonly Transform SourceRoot;

        /// <summary>
        /// The target hierarchy root.
        /// </summary>
        public readonly Transform TargetRoot;

        /// <summary>
        /// A temporary buffer to use for holding hierarchy paths.
        /// </summary>
        public readonly Stack<int> PathBuffer;

        /// <summary>
        /// A temporary buffer to use for holding components.
        /// </summary>
        public readonly List<Component> ComponentBuffer;


        public MappingContext(Transform sourceRoot, Transform targetRoot, Stack<int> pathBuffer, List<Component> componentBuffer)
        {
            SourceRoot = sourceRoot;
            TargetRoot = targetRoot;
            PathBuffer = pathBuffer;
            ComponentBuffer = componentBuffer;
        }
    }

    /// <summary>
    /// Captures a <see cref="PostMapper"/> delegate and its primary arguments.
    /// </summary>
    public readonly struct PostMapperUnit
    {
        /// <summary>
        /// The mapper delegate.
        /// </summary>
        public readonly PostMapper Mapper;

        /// <summary>
        /// The component to map from.
        /// </summary>
        public readonly Component Source;

        /// <summary>
        /// The component to map to.
        /// </summary>
        public readonly Component Target;


        /// <param name="mapper">The mapper delegate</param>
        /// <param name="source">The component to map from.</param>
        /// <param name="target">The component to map to.</param>
        public PostMapperUnit(PostMapper mapper, Component source, Component target)
        {
            Mapper = mapper;
            Source = source;
            Target = target;
        }


        /// <summary>
        /// Invokes the unit.
        /// </summary>
        /// <param name="context">The hierarchy context of the unit.</param>
        public void Invoke(ref MappingContext context) => Mapper.Invoke(Source, Target, ref context);
    }
}