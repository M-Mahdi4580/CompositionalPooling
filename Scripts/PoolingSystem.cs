using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CompositionalPooling.Utility;


namespace CompositionalPooling
{
    public static partial class PoolingSystem
    {
        #region Initialization Constants

        public const int DefaultPoolCount = 2; // The number of instances each pool starts with if created by default
        public const int DefaultPoolCapacity = int.MaxValue; // The maximum capacity each pool starts with if created by default

        private const int MaxHierarchyCount = 32; // The maximum number of children a root Transform is assumed to have
        private const int MaxHierarchyDepth = 16; // The maximum assumed depth of a child in its transform hierarchy
        private const int MaxComponentsCountPerGameObject = 16; // The maximum number of components a GameObject is assumed to have

        #endregion

#if UNITY_EDITOR && DEBUG
        private const HideFlags PoolInstanceHideFlags = HideFlags.NotEditable | HideFlags.DontSave; // The pool instance should not be user-editable and must not be destroyed on scene load.
#else
        private const HideFlags PoolInstanceHideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector; // The pool instance must be completely hidden.
#endif

        private static readonly Dictionary<PoolHandle, PoolInfo> _PoolMap = new Dictionary<PoolHandle, PoolInfo>(); // The data structure associating each composition with its pool

        private static readonly Stack<int> _HierarchyPathBuffer = new Stack<int>(MaxHierarchyDepth); // A buffer used to hold the hierarchy path of a transform
        private static readonly List<PostMappingUnit> _PostMapUnits = new List<PostMappingUnit>(MaxHierarchyCount); // A buffer for holding
        
        private static readonly List<Type> _CompositionBuffer = new List<Type>(MaxComponentsCountPerGameObject); // A buffer used for holding compositions (types of components)

        // Buffers used for holding components
        private static readonly List<Component> _ComponentBuffer1 = new List<Component>(MaxComponentsCountPerGameObject);
        private static readonly List<Component> _ComponentBuffer2 = new List<Component>(MaxComponentsCountPerGameObject);

        // Buffers used for holding transform hierarchies (root objects and all of their children)
        private static readonly List<Transform> _HierarchyBuffer1 = new List<Transform>(MaxHierarchyCount);
        private static readonly List<Transform> _HierarchyBuffer2 = new List<Transform>(MaxHierarchyCount);


        /// <summary>
        /// Creates an inactive hidden gameObject with the given composition.
        /// </summary>
        /// <param name="composition">A list of component types denoting a unique composition</param>
        /// <returns>The created object</returns>
        private static GameObject CreatePoolInstance<TList>(TList composition) where TList : IReadOnlyList<Type>
        {
            GameObject obj = new GameObject();
            
            obj.SetActive(false); // Deactivate the object to minimize its effect on gameplay. Deactivating the object prior to adding its components, also prevents the added components to be awakened. This is important since the components are likely to throw exceptions if awakened as part of a degenerate gameObject.
            obj.hideFlags = PoolInstanceHideFlags; // Hide the object to prevent its automatic destruction on scene loads and also to minimize user interactions with it.

#if UNITY_EDITOR && DEBUG
            obj.transform.parent = DelayedPoolingControl.Instance.transform; // Organize the object under a common transform root to prevent editor's cluttering.
#endif

            // Add the given components to the object.
            for (int i = 0, len = composition.Count; i < len; i++)
            {
                obj.AddComponent(composition[i]);
            }

            return obj;
        }


        /// <summary>
        /// Pools the instance and all of its children.
        /// </summary>
        /// <param name="instance">Target object</param>
        private static void Pool(Transform instance)
        {
            // Safety measure checks to ensure that an already pooled object, isn't pooled for a second time, corrupting the pool. These checks are not guaranteed to work for objects that were pooled but are reactivated and reused externally.
            if (instance.gameObject.hideFlags == PoolInstanceHideFlags) // Primary check: A fast check to see if the object has the same signature (e.g. HideFlags) as a pool instance. This is fail-fast check that should evaluate to false most of the time, preventing the evaluation of the slower main check. Note: This check assumes all pool instances have retained their given pool signature. So, if the pool instances were modified externally such that their signature no longer corresponds to a true pool instance's signature, this check would be invalid.
            {
                instance.GetComposition(_ComponentBuffer1, _CompositionBuffer);

                if (_PoolMap.TryGetValue(new PoolHandle(_CompositionBuffer), out var poolInfo) && poolInfo.Pool.Contains(instance)) // Main check: An slow but complete check that directly searches the corresponding pool to determine whether the object already exists in the pool.
                {
                    throw new InvalidOperationException("Pooling operation failed! Attempted pooling an object that has already been pooled!");
                }
            }

            OnRelease?.Invoke(null, instance);

            // Filling the first buffer with the hierarchy of the object.
            _HierarchyBuffer1.Clear();
            _HierarchyBuffer1.Add(instance);
            instance.GetChildrenAll(_HierarchyBuffer1);
            
            for (int i = _HierarchyBuffer1.Count - 1; i >= 0; i--) // Only degenerate objects (i.e. objects with no children and possibly no parents) can be pooled. Therefore, we will have to start from the leaf nodes of the hierarchy; thus, the iteration is done backwards.
            {
                PoolDegenerateChild(_HierarchyBuffer1[i]);
            }

            _HierarchyBuffer1.Clear();


            static void PoolDegenerateChild(Transform child)
            {
                child.GetComposition(_ComponentBuffer1, _CompositionBuffer); // Get the composition of the object.

#if PoolingSystem_AllowDisposer

                for (int i = 0, len = _TypeBuffer.Count; i < len; i++)
                {
                    if (StateMapper.TryGetDisposer(_TypeBuffer[i], out var disposer))
                    {
                        disposer.Invoke(_ComponentBuffer1[i + 1]);
                    }
                }
                
#endif
                if (!PoolHandle.TryGetHandleFrom(_PoolMap.Keys, _CompositionBuffer, out var poolHandle) || !_PoolMap.TryGetValue(poolHandle, out var poolInfo)) // Retreive the pool info. If the corresponding pool didn't exist
                {
                    if (!PoolManager.TryCreate(new InitializationUnit(_CompositionBuffer, new PoolSize(0, DefaultPoolCapacity)), out poolHandle)) // Try to create a new pool and if pool creation failed
                    {
                        UnityEngine.Object.Destroy(child.gameObject); // Destroy the object.
                        return;
                    }

                    poolInfo = _PoolMap[poolHandle]; // Retrieve the created pool's info.
                }

                OnPool?.Invoke(null, poolHandle);

                GameObject childGameObj = child.gameObject;

                if (poolInfo.Pool.Count < poolInfo.MaxCapacity) // If we haven't reached the size limit yet
                {
                    poolInfo.Pool.Enqueue(child); // Add the object to the pool.

                    childGameObj.SetActive(false); // Disable the object to minimize its effect on gameplay and render it as if destroyed.
                    childGameObj.hideFlags = PoolInstanceHideFlags; // Hide the object.

#if UNITY_EDITOR && DEBUG
                    child.parent = DelayedPoolingControl.Instance.transform; // Organize the objects under a common root to prevent editor's cluttering.
#else
                    child.parent = null; // Detach the object from its parent.
#endif
                }
                else // If we are at maximum size limit
                {
                    UnityEngine.Object.Destroy(childGameObj); // Destroy the object.
                }
            }
        }

        /// <summary>
        /// Unpools an instance state-equivalent to the requested object.
        /// </summary>
        /// <param name="original">The object to unpool an instance for</param>
        /// <returns>A clone instance with the same state as the requested object</returns>
        private static Transform Unpool(Transform original)
        {
            OnRequest?.Invoke(null, original);

            // Gets an instance equivalent to the original object from the pool. This instance will become the root of the rest of the unpooled instances.
            if (!TryUnpool(original, null, out Transform instance)) // If the unpooling procedure failed
            {
                return UnityEngine.Object.Instantiate(original); // Abort by reverting back to normal instantiation procedure.
            }

            // Filling the first buffer with the hierarchy of the source object
            _HierarchyBuffer1.Clear();
            _HierarchyBuffer1.Add(original); // Add the root of the original object.
            original.GetChildrenAll(_HierarchyBuffer1); // Append the children and deep children.

            // Adding the root of the clone hierarchy to the second buffer
            _HierarchyBuffer2.Clear();
            _HierarchyBuffer2.Add(instance); // Add the root of the pool instance.

            int hierarchyCount = _HierarchyBuffer1.Count; // Get the total number of transforms in the original object's hierarchy. This will become the number of instances that are unpooled and assembled to form a clone instance hierarchy.

            if (instance.hierarchyCapacity < hierarchyCount) instance.hierarchyCapacity = hierarchyCount; // Resize the hierarchy capacity of the root instance manually if needed. This prevents repetitive resizing when more children are added and optimizes performance.

            _PostMapUnits.Clear(); // This buffer will be filled up when mapping the state of the objects.

            // Filling the second buffer with unpooled children and deep children and setting up the hierarchical relation-ships.
            for (int i = 1, searchIndex = 0, lastParentIndex = 0; i < hierarchyCount; i++) // The children in the first buffer are breadth-first ordered which means that only the objects preceding the current object in the list can potentially be the object's parent. This fact is used to optimize the parent lookup search.
            {
                Transform originalChild = _HierarchyBuffer1[i]; // The child of the original object
                int parentIndex = Extensions.IndexOf(_HierarchyBuffer1, originalChild.parent, searchIndex, hierarchyCount - searchIndex); // Retrieve the index of the corresponding parent.

                if (!TryUnpool(originalChild, _HierarchyBuffer2[parentIndex], out Transform instanceChild)) // If the unpool procedure failed
                {
                    Pool(instance); // Repool the instance and its current hierarchy children.
                    return UnityEngine.Object.Instantiate(original); // Abort by reverting back to normal instantiation procedure.
                }

                instanceChild.gameObject.SetActive(originalChild.gameObject.activeSelf); // Map the child's active status.
                _HierarchyBuffer2.Add(instanceChild); // Add the instance child to the hierarchy list.

                // Take advantage of the breadth-first order in hierarchy lists to optimize the search range for the parent.
                if (lastParentIndex != parentIndex) // If the the last parent is different from the current parent
                {
                    searchIndex++; // Decrease the search range to improve the parent lookup performance.
                }

                lastParentIndex = parentIndex; // Update the last parent index.
            }

            HierarchyContextInfo hierarchyInfo = new HierarchyContextInfo(original, instance, _HierarchyPathBuffer, _ComponentBuffer1); // The data needed to resolve internal links in the gameObject (e.g. children referencing other children or their components).

            for (int i = 0, len = _PostMapUnits.Count; i < len; i++)
            {
                _HierarchyPathBuffer.Clear();
                _PostMapUnits[i].Invoke(ref hierarchyInfo);
            }

            instance.gameObject.SetActive(original.gameObject.activeSelf); // Map the objects' active status.
            SceneManager.MoveGameObjectToScene(instance.gameObject, SceneManager.GetActiveScene()); // Move the instance to the active scene.

            return instance;


            static bool TryUnpool(Transform original, Transform parent, out Transform instance)
            {
                original.GetComposition(_ComponentBuffer1, _CompositionBuffer); // Get the source object's components as well as composition.

                if (!PoolHandle.TryGetHandleFrom(_PoolMap.Keys, _CompositionBuffer, out var poolHandle) || !_PoolMap.TryGetValue(poolHandle, out var poolInfo))  // Retreive the pool info. If the corresponding pool didn't exist
                {
                    if (!PoolManager.TryCreate(new InitializationUnit(_CompositionBuffer, new PoolSize(DefaultPoolCount, DefaultPoolCapacity)), out poolHandle)) // Try to create a new pool and if pool creation failed
                    {
                        instance = null;
                        return false; // Abort.
                    }

                    poolInfo = _PoolMap[poolHandle]; // Retrieve the created pool's info.
                }

                OnUnpool?.Invoke(null, poolHandle);

                instance = poolInfo.Pool.Count > 0 ? poolInfo.Pool.Dequeue() : CreatePoolInstance(_CompositionBuffer).transform; // Unpool an instance or create a new instance if the pool is empty.

                #region ObjectMapping

                GameObject originalGameObject = original.gameObject;
                GameObject instanceGameObject = instance.gameObject;

                // Map gameObjects.
                instanceGameObject.tag = originalGameObject.tag;
                instanceGameObject.name = originalGameObject.name;
                instanceGameObject.layer = originalGameObject.layer;
                instanceGameObject.isStatic = originalGameObject.isStatic;
                instanceGameObject.hideFlags = originalGameObject.hideFlags;

                // Map transforms.
                instance.parent = parent;
                instance.localPosition = original.localPosition;
                instance.localRotation = original.localRotation;
                instance.localScale = original.localScale;

                instanceGameObject.GetComponents(_ComponentBuffer2); // Get target's components.

                // Match and Map the corresponding components from the source to the clone.
                for (int i = 1, len = _ComponentBuffer1.Count; i < len; i++) // Index must start from '1' in order to skip the transform component which is mapped already.
                {
                    StateMapper.Retrieve(_CompositionBuffer[i - 1]).Invoke(_ComponentBuffer1[i], _ComponentBuffer2[i], _PostMapUnits);
                }

                #endregion

                return true;
            }
        }
    }
}