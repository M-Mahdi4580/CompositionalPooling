using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
    /// <summary>
    /// Represents a unique pool with certain composition.
    /// </summary>
    public readonly struct PoolHandle : IReadOnlyList<Type>, IEquatable<PoolHandle>, IComparable<PoolHandle>
    {
        private static PoolHandle LastEquivalentHandle; // Captures the last handle passing the equivalence test.
        private static int PassedEquivalenceTestsCount; // Counts the number of equivalence tests performed.

        private readonly IReadOnlyList<Type> _PoolComposition; // The represented composition of the handle.
        private readonly int _HashCode; // A cached aggregate hashcode for optimizing performance.


        /// <summary>
        /// Gets a handle of the specified composition from the given handle source.
        /// </summary>
        /// <typeparam name="T">Source dictionary's value type</typeparam>
        /// <param name="source">The set of handles to get the desired handle from</param>
        /// <param name="composition">Composition of the desired handle</param>
        /// <returns>A handle with the same composition obtained from the handle source</returns>
        public static PoolHandle GetHandleFrom<T>(Dictionary<PoolHandle, T>.KeyCollection source, IReadOnlyList<Type> composition) => TryGetHandleFrom(source, composition, out PoolHandle handle) ? handle : throw new KeyNotFoundException();
        
        /// <inheritdoc cref="GetHandleFrom{T}(Dictionary{PoolHandle, T}.KeyCollection, IReadOnlyList{Type})"/>
        /// <param name="handle">The handle representing the desired composition</param>
        public static PoolHandle GetHandleFrom<T>(Dictionary<PoolHandle, T>.KeyCollection source, in PoolHandle handle)
        {
            PoolHandle sourceHandle = handle;
            return TryGetHandleFrom(source, ref sourceHandle) ? sourceHandle : throw new KeyNotFoundException();
        }

        
        /// <param name="handle">The equivalent handle found within the given handle source</param>
        /// <returns>True if the desired handle was found, false otherwise</returns>
        /// <inheritdoc cref="GetHandleFrom{T}(Dictionary{PoolHandle, T}.KeyCollection, IReadOnlyList{Type})"/>
        public static bool TryGetHandleFrom<T>(Dictionary<PoolHandle, T>.KeyCollection source, IReadOnlyList<Type> composition, out PoolHandle handle)
        {
            handle = new PoolHandle(composition);
            return TryGetHandleFrom(source, ref handle);
        }


        /// <param name="handle">The handle representing the desired composition</param>
        /// <inheritdoc cref="TryGetHandleFrom{T}(Dictionary{PoolHandle, T}.KeyCollection, IReadOnlyList{Type}, out PoolHandle)"/>
        public static bool TryGetHandleFrom<T>(Dictionary<PoolHandle, T>.KeyCollection source, ref PoolHandle handle)
        {
            PassedEquivalenceTestsCount = 0;

            if ((source as ICollection<PoolHandle>).Contains(handle) && (PassedEquivalenceTestsCount == 1 || handle.Equals(LastEquivalentHandle))) // The target handle should be captured
            {
                handle = LastEquivalentHandle;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Constructs a new pool handle.
        /// </summary>
        /// <param name="poolComposition">Composition of the target pool</param>
        public PoolHandle(IReadOnlyList<Type> poolComposition)
        {
            _PoolComposition = poolComposition;

            int aggregateHashCode = 17;

            for (int i = 0, len = _PoolComposition.Count; i < len; i++)
            {
                aggregateHashCode = aggregateHashCode * 37 + _PoolComposition[i].GetHashCode();
            }
            
            _HashCode = aggregateHashCode;
        }


        public override int GetHashCode() => _HashCode;
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((PoolHandle)obj);
        }

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

            LastEquivalentHandle = other;
            PassedEquivalenceTestsCount++;
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
    /// Holds the pool's data structure and its maximum allowed capacity.
    /// </summary>
    public readonly struct PoolInfo
    {
        /// <summary>
        /// The structure holding references to the pool instances.
        /// </summary>
        public readonly Queue<Transform> Pool;

        /// <summary>
        /// Maximum allowed number of instances in the pool.
        /// </summary>
        public readonly int MaxCapacity;

        public PoolInfo(Queue<Transform> pool, int maxCapacity)
        {
            Pool = pool;
            MaxCapacity = maxCapacity;
        }
    }


    /// <summary>
    /// Represents the size of a pool.
    /// </summary>
    public readonly struct PoolSize
    {
        /// <summary>
        /// Number of instances currently in the pool.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Maximum number of instances the pool can hold.
        /// </summary>
        public readonly int MaxCapacity;

        public PoolSize(int count, int maxCapacity)
        {
            if (count < 0 || count > maxCapacity) // A quick check to disallow invalid sizes.
            {
                throw new ArgumentOutOfRangeException(count < 0 ? nameof(count) : nameof(maxCapacity));
            }
            Count = count;
            MaxCapacity = maxCapacity;
        }
        
        public static PoolSize operator +(PoolSize x, PoolSize y) => new PoolSize(x.Count + y.Count, x.MaxCapacity > int.MaxValue - y.MaxCapacity ? int.MaxValue : x.MaxCapacity + y.MaxCapacity);
        public static PoolSize operator -(PoolSize x, PoolSize y) => new PoolSize(x.Count - y.Count, x.MaxCapacity - y.MaxCapacity);

        /// <summary>
        /// Calculates a component-wise maximization of the pool sizes.
        /// </summary>
        /// <param name="x">The first pool size</param>
        /// <param name="y">The second pool size</param>
        /// <returns>Maximization of the pool sizes</returns>
        public static PoolSize Maximize(PoolSize x, PoolSize y) => new PoolSize(Mathf.Max(x.Count, y.Count), Mathf.Max(x.MaxCapacity, y.MaxCapacity));
    }


    /// <summary>
    /// Holds the initialization data for a pool.
    /// </summary>
    public readonly struct InitializationUnit
    {
        /// <summary>
        /// Pool's composition.
        /// </summary>
        public readonly IReadOnlyList<Type> Composition;

        /// <summary>
        /// Pool's size.
        /// </summary>
        public readonly PoolSize Size;

        public InitializationUnit(IReadOnlyList<Type> composition, in PoolSize size)
        {
            Composition = composition;
            Size = size;
        }
    }

    /// <summary>
    /// Provides hierarchy information and useful buffers for hierarchy-based operations.
    /// </summary>
    public readonly ref struct HierarchyContextInfo
    {
        /// <summary>
        /// The hierarchy root of the source object.
        /// </summary>
        public readonly Transform SourceRoot;

        /// <summary>
        /// The hierarchy root of the target object.
        /// </summary>
        public readonly Transform TargetRoot;

        /// <summary>
        /// A temporary buffer for storage of components.
        /// </summary>
        public readonly List<Component> ComponentBuffer;

        /// <summary>
        /// A temporary buffer for serializing and deserializing hierarchy paths.
        /// </summary>
        public readonly Stack<int> HierarchyPathBuffer;

        public HierarchyContextInfo(Transform sourceRoot, Transform targetRoot, Stack<int> hierarchyPathBuffer, List<Component> componentBuffer)
        {
            SourceRoot = sourceRoot;
            TargetRoot = targetRoot;
            HierarchyPathBuffer = hierarchyPathBuffer;
            ComponentBuffer = componentBuffer;
        }
    }

    /// <summary>
    /// Holds the data required for a delayed mapping operation.
    /// </summary>
    public readonly struct PostMappingUnit
    {
        /// <summary>
        /// The delegate performing the mapping operations.
        /// </summary>
        public readonly PostMapper Mapper;

        /// <summary>
        /// The post mapper's first argument.
        /// </summary>
        public readonly Component Source;

        /// <summary>
        /// The post mapper's second argument.
        /// </summary>
        public readonly Component Target;

        public PostMappingUnit(PostMapper mapper, Component source, Component target)
        {
            Mapper = mapper;
            Source = source;
            Target = target;
        }

        /// <summary>
        /// Performs the mapping operation.
        /// </summary>
        /// <param name="contextInfo">The mapping context</param>
        public void Invoke(ref HierarchyContextInfo contextInfo) => Mapper.Invoke(Source, Target, ref contextInfo);
    }
}