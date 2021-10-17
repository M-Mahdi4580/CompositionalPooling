using System;
using UnityEngine;


namespace CompositionalPooling
{
    /// <summary>
    /// Handles object requests and releases using object pool pattern.
    /// </summary>
    public static partial class PoolingSystem
    {
        /// <summary>
        /// Invoked when a new object is requested.
        /// </summary>
        public static event EventHandler<Transform> OnRequest;

        /// <summary>
        /// Invoked when an object is released.
        /// </summary>
        public static event EventHandler<Transform> OnRelease;

        /// <summary>
        /// Invoked when an instance is pooled.
        /// </summary>
        public static event EventHandler<PoolHandle> OnPool;

        /// <summary>
        /// Invoked when an instance is unpooled.
        /// </summary>
        public static event EventHandler<PoolHandle> OnUnpool;


        /// <summary>
        /// Requests an object from the system.
        /// </summary>
        /// <param name="original">The object to get clone for</param>
        /// <returns>The system's assembled clone object</returns>
        public static Transform Request(Transform original) => Unpool(original);

        /// <inheritdoc cref="Request(Transform)"/>
        /// <typeparam name="T">Type of the component of the target object</typeparam>
        public static T Request<T>(T original) where T : Component => Unpool(original.transform).GetComponent<T>();

        /// <inheritdoc cref="Request{T}(T)"/>
        /// <param name="parent">The parent of the clone</param>
        /// <param name="instantiateInWorldSpace">Whether the object preserves its world coordinate status or local coordinate status</param>
        public static T Request<T>(T original, Transform parent, bool instantiateInWorldSpace = true) where T : Component
        {
            Transform clone = Unpool(original.transform);
            clone.SetParent(parent, instantiateInWorldSpace);
            return clone.GetComponent<T>();
        }

        /// <inheritdoc cref="Request{T}(T)"/>
        /// <param name="position">Position of the clone</param>
        /// <param name="rotation">Orientation of the clone</param>
        public static T Request<T>(T original, Vector3 position, Quaternion rotation) where T : Component
        {
            Transform clone = Unpool(original.transform);
            clone.position = position;
            clone.rotation = rotation;
            return clone.GetComponent<T>();
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion)"/>
        /// <param name="parent">The parent of the clone</param>
        public static T Request<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            Transform clone = Unpool(original.transform);
            clone.position = position;
            clone.rotation = rotation;
            clone.parent = parent;
            return clone.GetComponent<T>();
        }


        /// <inheritdoc cref="Request(Transform)"/>
        public static GameObject Request(GameObject original) => Unpool(original.transform).gameObject;

        /// <inheritdoc cref="Request{T}(T, Transform, bool)"/>
        public static GameObject Request(GameObject original, Transform parent, bool instantiateInWorldSpace = true)
        {
            Transform clone = Unpool(original.transform);
            clone.SetParent(parent, instantiateInWorldSpace);
            return clone.gameObject;
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion)"/>
        public static GameObject Request(GameObject original, Vector3 position, Quaternion rotation)
        {
            Transform clone = Unpool(original.transform);
            clone.position = position;
            clone.rotation = rotation;

            return clone.gameObject;
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion, Transform)"/>
        public static GameObject Request(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
        {
            Transform clone = Unpool(original.transform);
            clone.position = position;
            clone.rotation = rotation;
            clone.parent = parent;

            return clone.gameObject;
        }


        /// <summary>
        /// Releases the object into the system.
        /// </summary>
        /// <param name="obj">The object to release into the system</param>
        public static void ReleaseImmediate(Transform obj) => Pool(obj);

        /// <inheritdoc cref="ReleaseImmediate(Transform)"/>
        public static void ReleaseImmediate(GameObject obj) => Pool(obj.transform);

        

        /// <inheritdoc cref="ReleaseImmediate(GameObject)"/>
        /// <param name="delay">Time delay before release</param>
        public static void Release(Transform obj, float delay = 0f) => DelayedPoolingControl.Instance.Pool(obj, delay);

        /// <inheritdoc cref="Release(Transform, float)"/>
        public static void Release(GameObject obj, float delay = 0f) => DelayedPoolingControl.Instance.Pool(obj.transform, delay);
    }
}