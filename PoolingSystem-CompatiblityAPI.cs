using UnityEngine;


namespace CompositionalPooling
{
    public static partial class PoolingSystem
    {
        /// <typeparam name="T">Type of the component.</typeparam>
        /// <inheritdoc cref="Request(Transform)"/>
        public static T Request<T>(T prototype) where T : Component => Request(prototype.transform).GetComponent<T>();

        /// <inheritdoc cref="Request{T}(T)"/>
        /// <inheritdoc cref="Object.Instantiate(Object, Transform, bool)"/>
        public static T Request<T>(T prototype, Transform parent, bool instantiateInWorldSpace = true) where T : Component
        {
            Transform clone = Request(prototype.transform);
            clone.SetParent(parent, instantiateInWorldSpace);
            return clone.GetComponent<T>();
        }

        /// <inheritdoc cref="Request{T}(T)"/>
        /// <inheritdoc cref="Object.Instantiate(Object, Vector3, Quaternion)"/>
        public static T Request<T>(T prototype, Vector3 position, Quaternion rotation) where T : Component
        {
            Transform clone = Request(prototype.transform);
            clone.position = position;
            clone.rotation = rotation;
            return clone.GetComponent<T>();
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion)"/>
        /// <inheritdoc cref="Request{T}(T, Transform, bool)"/>
        public static T Request<T>(T prototype, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            Transform clone = Request(prototype.transform);
            clone.position = position;
            clone.rotation = rotation;
            clone.parent = parent;
            return clone.GetComponent<T>();
        }


        /// <inheritdoc cref="Request(Transform)"/>
        public static GameObject Request(GameObject prototype) => Request(prototype.transform).gameObject;

        /// <inheritdoc cref="Request{T}(T, Transform, bool)"/>
        public static GameObject Request(GameObject prototype, Transform parent, bool instantiateInWorldSpace = true)
        {
            Transform clone = Request(prototype.transform);
            clone.SetParent(parent, instantiateInWorldSpace);
            return clone.gameObject;
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion)"/>
        public static GameObject Request(GameObject prototype, Vector3 position, Quaternion rotation)
        {
            Transform clone = Request(prototype.transform);
            clone.position = position;
            clone.rotation = rotation;

            return clone.gameObject;
        }

        /// <inheritdoc cref="Request{T}(T, Vector3, Quaternion, Transform)"/>
        public static GameObject Request(GameObject prototype, Vector3 position, Quaternion rotation, Transform parent)
        {
            Transform clone = Request(prototype.transform);
            clone.position = position;
            clone.rotation = rotation;
            clone.parent = parent;

            return clone.gameObject;
        }

        
        /// <inheritdoc cref="ReleaseImmediate(Transform)"/>
        public static void ReleaseImmediate(GameObject instance) => ReleaseImmediate(instance.transform);

        /// <inheritdoc cref="DelayedReleaseControl.Release(Transform, float)"/>
        public static void Release(Transform instance, float delay = 0f) => _DelayedReleaser.Release(instance, delay);

        /// <inheritdoc cref="DelayedReleaseControl.Release(Transform, float)"/>
        public static void Release(GameObject instance, float delay = 0f) => _DelayedReleaser.Release(instance.transform, delay);
    }
}