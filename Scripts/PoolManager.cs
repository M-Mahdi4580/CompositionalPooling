using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
    public static partial class PoolingSystem
    {
        /// <summary>
        /// Exposes basic pool operations.
        /// </summary>
        public static class PoolManager
        {
            /// <summary>
            /// The minimum allowed capacity for the pools.
            /// </summary>
            public const int MinCapacity = 4;

            /// <summary>
            /// Handles to all of the system's pools.
            /// </summary>
            public static Dictionary<PoolHandle, PoolInfo>.KeyCollection Handles => _PoolMap.Keys;

            /// <summary>
            /// Invoked after a new pool is created.
            /// </summary>
            public static event EventHandler<PoolHandle> OnCreate;

            /// <summary>
            /// Invoked after a pool is updated to a new size.
            /// </summary>
            public static event EventHandler<PoolHandle> OnUpdate;

            /// <summary>
            /// Invoked after a pool is destroyed.
            /// </summary>
            public static event EventHandler<PoolHandle> OnDestroy;


            /// <summary>
            /// Creates a new pool with the given composition.
            /// </summary>
            /// <param name="unit">The unit of initializaiton defining the pool's configuration</param>
            /// <param name="poolHandle">Created pool's handle</param>
            /// <returns>True if the composition is supported and the pool can be created, false otherwise</returns>
            public static bool TryCreate(in InitializationUnit unit, out PoolHandle poolHandle)
            {
                Type[] poolComposition = new Type[unit.Composition.Count]; // Declare an array for holding the composition of the pool.

                // Initialize the composition array.
                for (int i = 0, len = unit.Composition.Count; i < len; i++)
                {
                    Type componentType = unit.Composition[i];

                    if (!StateMapper.IsRegistered(componentType)) // Log error and abort in case of invalid composition.
                    {
                        Debug.LogError($"No mapper for component type {componentType.FullName} exists! Pool creation failed!");

                        poolHandle = default;
                        return false;
                    }

                    poolComposition[i] = componentType;
                }

                Queue<Transform> pool = new Queue<Transform>(MinCapacity + unit.Size.Count * 3 / 2); // Create the pool's structure with an optimal initial capacity.

                poolHandle = new PoolHandle(poolComposition); // Create the handle representing the pool type.
                _PoolMap.Add(poolHandle, new PoolInfo(pool, unit.Size.MaxCapacity)); // Register the pool in the pool dictionary.

                // Fill the pool with some initial instances.
                while (pool.Count < unit.Size.Count)
                {
                    pool.Enqueue(CreatePoolInstance(poolComposition).transform); // MAYDO: Manual creation might be slow. Replace with instantiation.
                }

                OnCreate?.Invoke(null, poolHandle);
                return true;
            }

            /// <summary>
            /// Determines if a pool with the given composition exists.
            /// </summary>
            /// <param name="composition">Target pool's composition</param>
            /// <param name="handle">Target pool's handle</param>
            /// <returns>True if the specified pool exists, false otherwise</returns>
            public static bool Exists(IReadOnlyList<Type> composition, out PoolHandle handle) => PoolHandle.TryGetHandleFrom(Handles, composition, out handle);

            /// <summary>
            /// Determines whether the pool represented by the given handle exists.
            /// </summary>
            /// <param name="handle">Target pool's handle</param>
            /// <returns>True if the specified pool exists, false otherwise</returns>
            public static bool Exists(in PoolHandle handle) => _PoolMap.ContainsKey(handle);

            /// <summary>
            /// Updates the pool to a new size.
            /// </summary>
            /// <param name="handle">Target pool's handle</param>
            /// <param name="size">The size to update the pool to</param>
            public static void Update(in PoolHandle handle, in PoolSize size)
            {
                var newHandle = PoolHandle.GetHandleFrom(Handles, in handle); // Retreive an stable handle to optimize the frequent pool access.
                var poolInfo = _PoolMap[newHandle];

                bool unequalCounts = poolInfo.Pool.Count != size.Count;
                bool unequalCapacities = poolInfo.MaxCapacity != size.MaxCapacity;

                if (unequalCounts)
                {
                    while (poolInfo.Pool.Count < size.Count)
                    {
                        poolInfo.Pool.Enqueue(CreatePoolInstance(newHandle).transform); // MAYDO: Manual creation might be slow. Replace with instantiation.
                    }

                    while (poolInfo.Pool.Count > size.Count)
                    {
                        UnityEngine.Object.Destroy(poolInfo.Pool.Dequeue().gameObject);
                    }
                }

                if (unequalCapacities)
                {
                    _PoolMap[newHandle] = new PoolInfo(poolInfo.Pool, size.MaxCapacity);
                }

                if (OnUpdate != null && (unequalCounts || unequalCapacities))
                {
                    OnUpdate.Invoke(null, newHandle);
                }
            }

            /// <summary>
            /// Destroys the pool.
            /// </summary>
            /// <param name="handle">Target pool's handle</param>
            public static void Destroy(in PoolHandle handle)
            {
                var newHandle = PoolHandle.GetHandleFrom(Handles, in handle); // Retreive an stable handle to optimize the frequent pool access.
                Update(in newHandle, new PoolSize(0, 0));
                _PoolMap.Remove(newHandle); // Destroy the pool's structure.
                OnDestroy?.Invoke(null, newHandle);
            }

            /// <summary>
            /// Retrieves the size of the pool.
            /// </summary>
            /// <param name="handle">Target pool's handle</param>
            /// <returns>Target pool's size</returns>
            public static PoolSize GetSize(in PoolHandle handle)
            {
                var poolInfo = _PoolMap[handle];
                return new PoolSize(poolInfo.Pool.Count, poolInfo.MaxCapacity);
            }
        }
    }
}