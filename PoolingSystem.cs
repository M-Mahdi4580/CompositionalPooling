using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CompositionalPooling.Utility;


namespace CompositionalPooling
{
    /// <summary>
    /// Handles object requests and releases using the object pool pattern.
    /// </summary>
    public static partial class PoolingSystem
    {
        private static readonly Dictionary<PoolHandle, PoolInfo> _PoolMap = new Dictionary<PoolHandle, PoolInfo>(); // Associates each pool handle with its pool info.

        private static readonly Stack<int> _HierarchyPathBuffer = new Stack<int>(8); // A buffer for holding hierarchy paths.
        private static readonly List<PostMapperUnit> _PostMapUnits = new List<PostMapperUnit>(16); // A buffer for holding post mapper units.

        private static readonly List<Component> _ComponentBuffer1 = new List<Component>(8); // A buffer for holding components.
        private static readonly List<Component> _ComponentBuffer2 = new List<Component>(8); // A buffer for holding components.

        private static readonly List<Type> _CompositionBuffer = new List<Type>(8); // A buffer for holding compositions (ordered lists of component types).


#if UNITY_EDITOR && DEBUG
        private const HideFlags PoolInstanceHideFlags = HideFlags.NotEditable | HideFlags.DontSave; // The pool instance shall not be user-editable and must not be destroyed on scene load.
#else
        private const HideFlags PoolInstanceHideFlags = HideFlags.HideAndDontSave; // The pool instance must be completely hidden.
#endif


        /// <summary>
        /// Invoked after an object is requested.
        /// </summary>
        public static event EventHandler<Transform> OnRequested;

        /// <summary>
        /// Invoked after an object is released.
        /// </summary>
        public static event EventHandler<Transform> OnReleased;


        /// <summary>
        /// Releases the object into the system.
        /// </summary>
        /// <param name="instance">The object to release.</param>
        /// <remarks>This may create the required pools if they do not exist already.</remarks>
        public static void ReleaseImmediate(Transform instance)
        {
            if (!Contains(instance, out _)) // Abort if the object is pooled already.
            {
                Pool(instance);
                OnReleased?.Invoke(null, instance);
            }


            static void Pool(Transform instance)
            {
                for (int i = 0, len = instance.childCount; i < len; i++) // Pool the children recursively. This ensures the pooling logic is performed on the leaf nodes of the hierarchy first.
                {
                    Pool(instance);
                }

                instance.GetComposition(_ComponentBuffer1, _CompositionBuffer);

                if (!_PoolMap.TryGetValue(new PoolHandle(_CompositionBuffer), out PoolInfo pool)) // Retreive the pool info. If the pool doesn't exist
                {
                    Type unregisteredType = DelegateManager.FindUnregistered(_CompositionBuffer);

					if (unregisteredType is null) // If there are no problematic component types
					{
                        _PoolMap.Add(new PoolHandle(_CompositionBuffer.ToArray()), pool = new PoolInfo(new Queue<Transform>(16), int.MaxValue)); // Create and register the pool.
                    }
                    else
					{
                        Debug.LogError(string.Format(PoolCreationFailureErrorFormat, unregisteredType.FullName));
						UnityEngine.Object.DestroyImmediate(instance.gameObject); // Destroy the object.
                        return;
                    }
                }

#if PoolingSystem_AllowDisposer

                for (int i = 0, len = _CompositionBuffer.Count; i < len; i++) // Dispose of all disposable components.
                {
                    if (DelegateManager.TryGetDisposer(_CompositionBuffer[i], out var disposer))
                    {
                        disposer.Invoke(_ComponentBuffer1[i]);
                    }
                }

#endif

                if (pool.Instances.Count < pool.Capacity) // If not at the size limit
                {
                    pool.Instances.Enqueue(instance); // Add the object to the pool.

                    instance.gameObject.SetActive(false); // Deactivate the object to minimize its effect on gameplay.
                    instance.gameObject.hideFlags = PoolInstanceHideFlags; // Hide the object.

#if UNITY_EDITOR && DEBUG
                    instance.parent = _DelayedReleaser.transform; // Move the object under the common root.
#else
                    instance.parent = null; // Detach the object from its parent.
#endif
                }
                else // If at the size limit
                {
                    UnityEngine.Object.DestroyImmediate(instance.gameObject); // Destroy the object.
                }
            }
        }

        /// <summary>
        /// Requests an object from the system.
        /// </summary>
        /// <param name="prototype">An existing object that you want to make a copy of.</param>
        /// <returns>The requested clone.</returns>
        public static Transform Request(Transform prototype)
        {
            _PostMapUnits.Clear();

            Transform clone = Unpool(prototype, null);
            MappingContext context = new MappingContext(prototype, clone, _HierarchyPathBuffer, _ComponentBuffer1);

            for (int i = 0, len = _PostMapUnits.Count; i < len; i++) // Invoke all registered post map units.
            {
                _PostMapUnits[i].Invoke(ref context);
            }

            SceneManager.MoveGameObjectToScene(clone.gameObject, SceneManager.GetActiveScene()); // Move the clone to the active scene.
            clone.gameObject.SetActive(prototype.gameObject.activeSelf); // Map the active status of the root objects. At this point the clone and its entire hierarchy is in a valid state and it may be activated.

            OnRequested?.Invoke(null, prototype);
            return clone;


            static Transform Unpool(Transform prototype, Transform parentClone)
            {
                Transform clone;
                prototype.GetComposition(_ComponentBuffer1, _CompositionBuffer);

                if (_PoolMap.TryGetValue(new PoolHandle(_CompositionBuffer), out PoolInfo pool) && pool.Instances.Count > 0) // If the pool exists and it's not empty
                {
                    clone = pool.Instances.Dequeue();
                    clone.SetParent(parentClone, false);

                    #region Object-mapping

                    GameObject prototype_obj = prototype.gameObject;
                    GameObject clone_obj = clone.gameObject;

                    // Map game objects.
                    clone_obj.tag = prototype_obj.tag;
                    clone_obj.name = prototype_obj.name;
                    clone_obj.layer = prototype_obj.layer;
                    clone_obj.isStatic = prototype_obj.isStatic;
                    clone_obj.hideFlags = prototype_obj.hideFlags;

                    clone_obj.GetComponents(_ComponentBuffer2);

                    for (int i = 0, len = _ComponentBuffer1.Count; i < len; i++) // Match and Map corresponding components.
                    {
                        DelegateManager.GetMapper(_CompositionBuffer[i]).Invoke(_ComponentBuffer1[i], _ComponentBuffer2[i], _PostMapUnits);
                    }

                    if (parentClone is null) // If this is the root transform
                    {
						if (clone.hierarchyCapacity < prototype.hierarchyCapacity)
						{
                            clone.hierarchyCapacity = prototype.hierarchyCapacity; // Map the hierarchy capacity of the objects. This optimizes hierarchy assembly.
                        }
                    }
					else
					{
                        clone_obj.SetActive(prototype_obj.activeSelf); // Map the active status of the objects.
					}

                    #endregion

                    for (int i = 0, len = prototype.childCount; i < len; i++) // Assemble the children recursively.
                    {
                        Unpool(prototype.GetChild(i), clone);
                    }
                }
				else
				{
                    clone = UnityEngine.Object.Instantiate(prototype);
                    clone.SetParent(parentClone, false);
                }

                return clone;
            }
        }
    }
}