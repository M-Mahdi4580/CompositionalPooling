using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling.Utility
{
    /// <summary>
    /// Provides utility extensions for system's common operations.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Finds the index of an object in the list using reference equality check.
        /// </summary>
        /// <inheritdoc cref="Array.IndexOf{T}(T[], T, int, int)"/>
        public static int IndexOf<T, TList>(this TList list, T item, int startIndex, int length) where T : class where TList : IReadOnlyList<T>
        {
            length += startIndex;

            for (int i = startIndex; i < length; i++)
            {
                if (ReferenceEquals(list[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the object's composition.
        /// </summary>
        /// <param name="transform">Target object</param>
        /// <param name="componentBuffer">A buffer to fill with object's components</param>
        /// <param name="compositionBuffer">A buffer to fill with object's composition</param>
        public static void GetComposition(this Transform transform, List<Component> componentBuffer, List<Type> compositionBuffer)
        {
            transform.GetComponents(componentBuffer);
            compositionBuffer.Clear();

            for (int i = 1, len = componentBuffer.Count; i < len; i++) // Starting from index '1' skips the transform component which due to being common across all gameObjects (Ignoring RectTransform) can be safely omitted without hurting the composition comparisons.
            {
                compositionBuffer.Add(componentBuffer[i].GetType());
            }
        }



        /// <summary>
        /// Retrieves object's immediate children.
        /// </summary>
        /// <param name="transform">Target object</param>
        /// <param name="results">The list to append target's children to</param>
        /// <returns>Number of target's children</returns>
        public static int GetChildren(this Transform transform, List<Transform> results)
        {
            int childCount = transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                results.Add(transform.GetChild(i));
            }

            return childCount;
        }

        /// <summary>
        /// Retrieves all of the object's children (in breadth-first order).
        /// </summary>
        /// <inheritdoc cref="GetChildren(Transform, List{Transform})"/>
        public static void GetChildrenAll(this Transform transform, List<Transform> results)
        {
            int i = results.Count;
            transform.GetChildren(results);

            while (i < results.Count)
            {
                results[i++].GetChildren(results);
            }
        }



        /// <summary>
        /// Serializes the object's hierarchy path.
        /// </summary>
        /// <param name="transform">Target object</param>
        /// <param name="startRoot">The object to start the path from</param>
        /// <param name="result">The buffer to fill with the path data</param>
        /// <returns>True if the given start root is a valid root, false otherwise</returns>
        public static bool TryGetPath(this Transform transform, Transform startRoot, Stack<int> result)
        {
            Transform current = transform, next = transform.parent;

            while (!ReferenceEquals(next, startRoot) || !ReferenceEquals(next, null))
            {
                result.Push(current.GetSiblingIndex());
                current = next;
                next = next.parent;
            }

            return ReferenceEquals(next, startRoot);
        }

        /// <summary>
        /// Deserializes the given hierarchy path.
        /// </summary>
        /// <param name="root">The object to start the path from</param>
        /// <param name="hierarchyPath">The buffer that holds the path data</param>
        /// <returns>The object denoted by the given path</returns>
        public static Transform ApplyPath(this Transform root, Stack<int> hierarchyPath)
        {
            Transform current = root;

            while (hierarchyPath.Count > 0)
            {
                current = current.GetChild(hierarchyPath.Pop());
            }

            return current;
        }



        /// <summary>
        /// Deserializes the object's path onto the target hierarchy.
        /// </summary>
        /// <param name="obj">The object to deserialize its path</param>
        /// <param name="contextInfo">Source and target hierarchies data</param>
        /// <returns>Corresponding object in the target hierarchy</returns>
        public static Transform GetCorresponding(this Transform obj, ref HierarchyContextInfo contextInfo) // The functionality of this, is the same as unity's serialization and deserialization of Object's internal links.
        {
            return obj == null || !obj.TryGetPath(contextInfo.SourceRoot, contextInfo.HierarchyPathBuffer) ? // Encode the link and apply it to find the corresponding child if we have an internal reference.
                obj : contextInfo.TargetRoot.ApplyPath(contextInfo.HierarchyPathBuffer); // We have an external reference, since there is no correspondence, just return the original child.
        }

        /// <inheritdoc cref="GetCorresponding(Transform, ref HierarchyContextInfo)"/>
        public static T GetCorresponding<T>(this T obj, ref HierarchyContextInfo contextInfo) where T : Component
        {
            Transform sourceTransform, targetTransform;

            if (obj == null || ReferenceEquals(sourceTransform = obj.transform, targetTransform = GetCorresponding(sourceTransform, ref contextInfo))) return obj; // If no correspondence exists, 'b_targetChild' will refer to the same value as 'a_targetChild'. If there is no correspondence, then, we have an external reference and we will return the original component.

            // Otherwise, we have an internal reference then. Find the target component.
            Type componentType = obj.GetType(); // Get the type of the component.
            contextInfo.SourceRoot.GetComponents(componentType, contextInfo.ComponentBuffer); // Store all the components of this type on the object in the buffer. The order the components are stored is fixed. This will be used to get the correct component on the second object in the case of multiple components of the same type.
            int count = contextInfo.ComponentBuffer.Count;

            if (count == 1) // If there aren't components of the same type on the object, avoid the more expensive GetComponents call.
            {
                return targetTransform.GetComponent(componentType) as T;
            }
            else
            {
                targetTransform.GetComponents(componentType, contextInfo.ComponentBuffer);
                return contextInfo.ComponentBuffer[IndexOf<Component, List<Component>>(contextInfo.ComponentBuffer, obj, 0, count)] as T;
            }
        }



        /// <summary>
        /// Deserializes the objects' paths onto the target hierarchy.
        /// </summary>
        /// <param name="objs">Target objects</param>
        /// <param name="results">The buffer to store the results in</param>
        /// <inheritdoc cref="GetCorresponding(Transform, ref HierarchyContextInfo)"/>
        /// <returns>Corresponding objects in the target hierarchy</returns>
        public static Transform[] GetCorresponding(this Transform[] objs, Transform[] results, ref HierarchyContextInfo contextInfo)
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
                results[i] = objs[i].GetCorresponding(ref contextInfo);
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref HierarchyContextInfo)"/>
        public static List<Transform> GetCorresponding(this List<Transform> objs, List<Transform> results, ref HierarchyContextInfo contextInfo)
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
                results.Add(objs[i].GetCorresponding(ref contextInfo));
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref HierarchyContextInfo)"/>
        public static T[] GetCorresponding<T>(this T[] objs, T[] results, ref HierarchyContextInfo contextInfo) where T : Component
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
                results[i] = objs[i].GetCorresponding(ref contextInfo);
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref HierarchyContextInfo)"/>
        public static List<T> GetCorresponding<T>(this List<T> objs, List<T> results, ref HierarchyContextInfo contextInfo) where T : Component
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
                results.Add(objs[i].GetCorresponding(ref contextInfo));
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref HierarchyContextInfo)"/>
        public static GameObject[] GetCorresponding(this GameObject[] objs, GameObject[] results, ref HierarchyContextInfo contextInfo)
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
                results[i] = objs[i].transform.GetCorresponding(ref contextInfo).gameObject;
            }

            return results;
        }

        /// <inheritdoc cref="GetCorresponding(Transform[], Transform[], ref HierarchyContextInfo)"/>
        public static List<GameObject> GetCorresponding(this List<GameObject> objs, List<GameObject> results, ref HierarchyContextInfo contextInfo)
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
                results.Add(objs[i].transform.GetCorresponding(ref contextInfo).gameObject);
            }

            return results;
        }

        /// <summary>
        /// Copies the items to the given buffer.
        /// </summary>
        /// <typeparam name="T">Type of the items</typeparam>
        /// <param name="items">The items to copy</param>
        /// <param name="results">The buffer to copy to</param>
        /// <returns>The copied items</returns>
        public static T[] GetCorresponding<T>(this T[] items, T[] results)
        {
            if (items == null)
            {
                return null;
            }

            int length = items.Length;

            if (results == null || results.Length != length)
            {
                results = new T[length];
            }

            for (int i = 0; i < length; i++)
            {
                results[i] = items[i];
            }

            return results;
        }
        
        /// <inheritdoc cref="GetCorresponding{T}(T[], T[])"/>
        public static List<T> GetCorresponding<T>(this List<T> objs, List<T> results)
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
                results.Add(objs[i]);
            }

            return results;
        }
    }
}