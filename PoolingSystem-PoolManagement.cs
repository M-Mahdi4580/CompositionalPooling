using System;
using System.Collections.Generic;
using UnityEngine;
using CompositionalPooling.Utility;


namespace CompositionalPooling
{
    public static partial class PoolingSystem
    {
        private const string PoolCreationFailureErrorFormat = "The component type {0} is not associated with any mapper delegate! Pool creation failed!"; // The error message format used for logging pool creation failures due to problematic component types.

        /// <summary>
        /// Handles to all of the existing pools.
        /// </summary>
        public static Dictionary<PoolHandle, PoolInfo>.KeyCollection Handles => _PoolMap.Keys;


        /// <summary>
        /// Invoked after a pool is initialized to a new size.
        /// </summary>
        public static event EventHandler<PoolHandle> OnInitialized;

        /// <summary>
        /// Invoked after a pool is removed.
        /// </summary>
        public static event EventHandler<PoolHandle> OnDeleted;


        /// <summary>
        /// Determines whether the pool represented by the given handle exists.
        /// </summary>
        /// <param name="handle">The pool's handle.</param>
        /// <returns>True if the pool exists, false otherwise.</returns>
        public static bool Exists(in PoolHandle handle) => _PoolMap.ContainsKey(handle);


        /// <inheritdoc cref="Exists(Transform, out PoolHandle)"/>
        public static bool Exists(GameObject prototype, out PoolHandle handle) => Exists(prototype, out handle);

        /// <summary>
        /// Determines whether the pool represented by the composition of the given prototype exists.
        /// </summary>
        /// <param name="prototype">An object representing the same composition as the pool.</param>
        /// <inheritdoc cref="Exists(in PoolHandle)"/>
        public static bool Exists(Transform prototype, out PoolHandle handle)
        {
            prototype.GetComposition(_ComponentBuffer1, _CompositionBuffer);
            return PoolHandle.TryGetHandleFrom(_CompositionBuffer, Handles, out handle);
        }


        /// <inheritdoc cref="Contains(Transform, out PoolHandle)"/>
        public static bool Contains(GameObject instance, out PoolHandle handle) => Contains(instance.transform, out handle);

        /// <summary>
        /// Determines whether the given object is an instance of an existing pool.
        /// </summary>
        /// <param name="instance">The object to check for.</param>
        /// <param name="handle">Handle to the object's pool.</param>
        /// <returns>True if the object is a pool instance, false otherwise.</returns>
        /// <remarks>This method may not return correct results if the pool instances are altered externally.</remarks>
        public static bool Contains(Transform instance, out PoolHandle handle)
        {
            if (instance.gameObject.hideFlags == PoolInstanceHideFlags) // A fail-fast check to quickly determine if the object can be a pool instance by checking its signature (e.g. hideFlags). Note: This check assumes all pool instances have retained their given pool signature.
            {
                instance.GetComposition(_ComponentBuffer1, _CompositionBuffer);
                return PoolHandle.TryGetHandleFrom(_CompositionBuffer, Handles, out handle) && _PoolMap[handle].Instances.Contains(instance); // An slow but comprehensive check which searches the entirety of the corresponding pool for the object.
            }

            handle = default;
            return false;
        }


        /// <inheritdoc cref="Initialize(Transform, PoolSize)"/>
        public static PoolHandle Initialize(GameObject prototype, PoolSize size) => Initialize(prototype.transform, size);

        /// <summary>
        /// Initializes the pool to a new size.
        /// </summary>
        /// <param name="prototype">An object representing the same composition as the pool.</param>
        /// <param name="size">The pool size to initialize to.</param>
        /// <returns>The pool's handle.</returns>
        /// <remarks>This may create the pool if it doesn't exist already.</remarks>
        public static PoolHandle Initialize(Transform prototype, PoolSize size)
		{
            PoolInfo pool;
            bool poolStateChanged; // A flag which determines if the state of the pool is changed due to this initialization.

            if (Exists(prototype, out PoolHandle handle))
			{
                pool = _PoolMap[handle];
                poolStateChanged = pool.Size != size;
            }
			else // If the pool doesn't exist
			{
                Type unregisteredType = DelegateManager.FindUnregistered(_CompositionBuffer);

                if (unregisteredType is null) // If there are no problematic component types
                {
                    // Create the pool.
                    handle = new PoolHandle(_CompositionBuffer.ToArray());
                    pool = new PoolInfo(new Queue<Transform>(size.Capacity > 128 && size.Capacity > size.Count * 2 ? size.Count : Mathf.Max(size.Count, size.Capacity)), size.Capacity);
                    poolStateChanged = true;

                    _PoolMap.Add(handle, pool); // Register the pool.
                }
                else
                {
                    Debug.LogError(string.Format(PoolCreationFailureErrorFormat, unregisteredType.FullName));
                    return default;
                }
            }

			if (poolStateChanged)
			{
                if (pool.Instances.Count == 0 && size.Count > 0) // If the initialization involves instance replication and there are no existing instances to replicate
                {
                    ReleaseImmediate(UnityEngine.Object.Instantiate(prototype)); // Release a clone of the prototype to get more instances.
                }

                while (pool.Instances.Count < size.Count) // As long as the initialization requires more instances
                {
                    Transform instance = UnityEngine.Object.Instantiate(pool.Instances.Peek()); // Replicate an existing pool instance.
                    pool.Instances.Enqueue(instance); // Add the instance to the pool.

#if UNITY_EDITOR && DEBUG
                    instance.parent = _DelayedReleaser.transform; // Move the instance under the common root.
#endif
                }

                while (pool.Instances.Count > size.Count) // As long as the initialization requires less instances
                {
                    UnityEngine.Object.DestroyImmediate(pool.Instances.Dequeue().gameObject); // Remove and destroy an existing pool instance.
                }

                if (pool.Capacity != size.Capacity) // If the initialization requires a new pool capacity
                {
                    _PoolMap[handle] = new PoolInfo(pool.Instances, size.Capacity); // Update the pool to the new capacity.
                }

                OnInitialized?.Invoke(null, handle);
            }
            
            return handle;
		}


        /// <summary>
        /// Removes the pool.
        /// </summary>
        /// <param name="handle">The pool handle.</param>
        /// <remarks>This will immediately destroy the pool instances.</remarks>
        public static void Delete(PoolHandle handle)
        {
            PoolHandle.Normalize(ref handle, Handles);
            PoolInfo pool = _PoolMap[handle];

			while (pool.Instances.Count > 0)
			{
                UnityEngine.Object.DestroyImmediate(pool.Instances.Dequeue().gameObject);
			}
            
            _PoolMap.Remove(handle); // Destroy the pool's structure.
            OnDeleted?.Invoke(null, handle);
        }

        /// <summary>
        /// Gets the size of the pool.
        /// </summary>
        /// <param name="handle">The pool handle.</param>
        /// <returns>Size of the pool.</returns>
        public static PoolSize GetSize(in PoolHandle handle) => _PoolMap[handle].Size;
    }
}