using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling.Utility
{
    /// <summary>
    /// Provides utility extensions for object mapping operations.
    /// </summary>
    public static class MapperUtils
    {
        /// <summary>
        /// Finds index of the object in the list.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <typeparam name="TList">Type of the list.</typeparam>
        /// <param name="list">The list of objects.</param>
        /// <param name="obj">The object to find.</param>
        /// <param name="index">The index to start searching from.</param>
        /// <param name="length">The number of indices to search.</param>
        /// <returns>Index of the object if found; -1 otherwise.</returns>
        /// <remarks>The equality comparison is performed soley based on reference equality.</remarks>
        public static int IndexOf<T, TList>(this TList list, T obj, int index, int length) where T : class where TList : IReadOnlyList<T>
        {
            length += index;

            for (int i = index; i < length; i++)
            {
                if (ReferenceEquals(list[i], obj))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the object's composition.
        /// </summary>
        /// <param name="obj">The object to get its composition.</param>
        /// <param name="buffer">A buffer to use for getting the components of the object.</param>
        /// <param name="results">The list to store the results in.</param>
        public static void GetComposition(this Transform obj, List<Component> buffer, List<Type> results)
        {
            results.Clear();
            obj.GetComponents(buffer);

            for (int i = 0, len = buffer.Count; i < len; i++)
            {
                results.Add(buffer[i].GetType());
            }
        }

        /// <summary>
        /// Serializes the object's hierarchy path.
        /// </summary>
        /// <param name="obj">The object to serialize its hierarchy path.</param>
        /// <param name="result">The buffer to fill with path data.</param>
        /// <param name="root">The object the path should start from.</param>
        /// <returns>True if the given root is valid; false otherwise.</returns>
        public static bool GetPath(this Transform obj, Stack<int> result, Transform root = null)
        {
            result.Clear(); // Clear the buffer before use.

            while (!ReferenceEquals(obj, root) && !ReferenceEquals(obj, null)) // As long as the root is not reached
            {
                result.Push(obj.GetSiblingIndex()); // Store the sibling position of the object.
                obj = obj.parent; // Move up to the hierarchy.
            }

            return ReferenceEquals(obj, root); // Return whether the given root is reached.
        }

        /// <summary>
        /// Deserializes the hierarchy path onto the given object.
        /// </summary>
        /// <param name="obj">The object to deserialize the path on.</param>
        /// <param name="path">The buffer that holds the path data.</param>
        /// <returns>The object reached by deserialization of the hierarchy path</returns>
        public static Transform ApplyPath(this Transform obj, Stack<int> path)
        {
            while (path.Count > 0) // As long as there are more sibling indices in the buffer
            {
                obj = obj.GetChild(path.Pop()); // Traverse to the child denoted by the sibling index.
            }

            return obj; // Return the child reached by traversing the given path.
        }



        /// <summary>
        /// Deserializes the object's path onto the target hierarchy.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="context">Hierarchy context.</param>
        /// <returns>The object itself if it is not part of the source hierarchy; the object with the same path in the target hierarchy otherwise.</returns>
        public static Transform GetCorresponding(this Transform obj, ref MappingContext context)
        {
            return 
                obj == null ? null :
                obj.GetPath(context.PathBuffer, context.SourceRoot) ? context.TargetRoot.ApplyPath(context.PathBuffer) : // Serialize the object's path and then, deserialize it onto the target hierarchy if the object is part of the source hierarchy.
                obj;
        }

        /// <inheritdoc cref="GetCorresponding(Transform, ref MappingContext)"/>
        public static T GetCorresponding<T>(this T obj, ref MappingContext context) where T : Component
        {
			if (obj == null)
			{
                return null;
			}

            Transform source = obj.transform;
            Transform target = source.GetCorresponding(ref context);

            if (ReferenceEquals(source, target)) // If there is no correspondence
            {
                return obj; // Return the component itself.
            }

            Type type = obj.GetType(); // Get the type of this component.

            obj.GetComponents(type, context.ComponentBuffer); // Get all components of the same type on the game object.
            int index = context.ComponentBuffer.IndexOf<Component, List<Component>>(obj, 0, context.ComponentBuffer.Count); // Get the index of this component. This index indicates the position of the component on the game object.

            target.GetComponents(type, context.ComponentBuffer); // Get all components of this type on the target object.
            return context.ComponentBuffer[index] as T; // Return the component at the same position as this component on the target object.
        }

        /// <inheritdoc cref="GetCorresponding(Transform, ref MappingContext)"/>
        public static GameObject GetCorresponding(this GameObject obj, ref MappingContext context) => obj == null ? null : obj.transform.GetCorresponding(ref context).gameObject;


        /// <summary>
        /// Deserializes the objects' paths onto the target hierarchy.
        /// </summary>
        /// <param name="objs">The objects to serialize their paths.</param>
        /// <param name="results">An optional buffer to use for storing the results.</param>
        /// <param name="context">Hierarchy context.</param>
        /// <returns>Results of deserialization.</returns>
        public static Transform[] GetCorresponding(this Transform[] objs, Transform[] results, ref MappingContext context)
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Length;

            if (results == null || results.Length != length)
            {
                results = new Transform[length];
            }

            for (int i = 0; i < length; i++)
            {
                results[i] = objs[i].GetCorresponding(ref context);
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref MappingContext)"/>
        public static List<Transform> GetCorresponding(this List<Transform> objs, List<Transform> results, ref MappingContext context)
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Count;

            if (results == null)
            {
                results = new List<Transform>(length);
            }
            else
            {
                results.Clear();

                if (results.Capacity < length)
                {
                    results.Capacity = length;
                }
            }

            for (int i = 0; i < length; i++)
            {
                results.Add(objs[i].GetCorresponding(ref context));
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref MappingContext)"/>
        public static T[] GetCorresponding<T>(this T[] objs, T[] results, ref MappingContext context) where T : Component
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Length;

            if (results == null || results.Length != length)
            {
                results = new T[length];
            }

            for (int i = 0; i < length; i++)
            {
                results[i] = objs[i].GetCorresponding(ref context);
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref MappingContext)"/>
        public static List<T> GetCorresponding<T>(this List<T> objs, List<T> results, ref MappingContext context) where T : Component
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Count;

            if (results == null)
            {
                results = new List<T>(length);
            }
            else
            {
                results.Clear();

                if (results.Capacity < length)
                {
                    results.Capacity = length;
                }
            }

            for (int i = 0; i < length; i++)
            {
                results.Add(objs[i].GetCorresponding(ref context));
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref MappingContext)"/>
        public static GameObject[] GetCorresponding(this GameObject[] objs, GameObject[] results, ref MappingContext context)
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Length;

            if (results == null || results.Length != length)
            {
                results = new GameObject[length];
            }

            for (int i = 0; i < length; i++)
            {
                results[i] = objs[i].GetCorresponding(ref context);
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref MappingContext)"/>
        public static List<GameObject> GetCorresponding(this List<GameObject> objs, List<GameObject> results, ref MappingContext context)
        {
            if (objs == null)
            {
                return null;
            }

            int length = objs.Count;

            if (results == null)
            {
                results = new List<GameObject>(length);
            }
            else
            {
                results.Clear();

                if (results.Capacity < length)
                {
                    results.Capacity = length;
                }
            }

            for (int i = 0; i < length; i++)
            {
                results.Add(objs[i].GetCorresponding(ref context));
            }

            return results;
        }



        /// <summary>
        /// Clones the collection.
        /// </summary>
        /// <typeparam name="T">Type of the collection.</typeparam>
        /// <param name="collection">The collection to clone.</param>
        /// <param name="results">An optional collection to use as the clone.</param>
        /// <returns>The cloned collection.</returns>
        public static T[] GetCorresponding<T>(this T[] collection, T[] results)
        {
            if (collection == null)
            {
                return null;
            }

            int length = collection.Length;

            if (results == null || results.Length != length)
            {
                results = new T[length];
            }

            for (int i = 0; i < length; i++)
            {
                results[i] = collection[i];
            }

            return results;
        }
        
        /// <inheritdoc cref="GetCorresponding{T}(T[], T[])"/>
        public static List<T> GetCorresponding<T>(this List<T> collection, List<T> results)
        {
            if (collection == null)
            {
                return null;
            }

            int length = collection.Count;

            if (results == null)
            {
                results = new List<T>(length);
            }
            else
            {
                results.Clear();

                if (results.Capacity < length)
                {
                    results.Capacity = length;
                }
            }

            for (int i = 0; i < length; i++)
            {
                results.Add(collection[i]);
            }

            return results;
        }
    }
}