using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
    /// <summary>
    /// Maps primary state of the given components and may register a post mapper for state-dependent mappings.
    /// </summary>
    /// <param name="objA">Source component</param>
    /// <param name="objB">Target component</param>
    /// <param name="postMapUnits">List of registered post mapper units</param>
    public delegate void BaseMapper(Component objA, Component objB, List<PostMappingUnit> postMapUnits);

    /// <summary>
    /// Maps dependent state of the given components.
    /// </summary>
    /// <param name="objA">Source component</param>
    /// <param name="objB">Target component</param>
    /// <param name="contextInfo">Context information on hierarchies of the components</param>
    public delegate void PostMapper(Component objA, Component objB, ref HierarchyContextInfo contextInfo);

#if PoolingSystem_AllowDisposer

    /// <summary>
    /// Resets the object and releases the aquired resources.
    /// </summary>
    /// <param name="obj"></param>
    public delegate void Disposer(Component obj);

#endif

    /// <summary>
    /// Manages component mapper delegates.
    /// </summary>
    public static partial class StateMapper
    {
        private static readonly Dictionary<Type, BaseMapper> _Mappers = new Dictionary<Type, BaseMapper>(); // Associates each type with its mapper delegate.

        /// <summary>
        /// Types with registered mappers.
        /// </summary>
        public static Dictionary<Type, BaseMapper>.KeyCollection RegisteredTypes => _Mappers.Keys;

        /// <summary>
        /// Registers the mapper delegate.
        /// </summary>
        /// <param name="type">Target component type</param>
        /// <param name="mapper">Mapper delegate</param>
        public static void Register(Type type, BaseMapper mapper)
        {
#if UNITY_EDITOR && DEBUG

            if (!type.IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException($"Cannot register a mapper for the non-component type {type.FullName}!", nameof(type));
            }

            if (type == typeof(Transform) || type.IsSubclassOf(typeof(Transform)))
            {
                throw new NotSupportedException($"Registering a mapper for {type.FullName} is not supported!");
            }
#endif
            _Mappers.Add(type, mapper ?? throw new ArgumentNullException(nameof(mapper)));
        }

        /// <summary>
        /// Determines whether the mapper delegate for the given type exists.
        /// </summary>
        /// <param name="type">Target component type</param>
        /// <returns>True if a mapper delegate is registered for the component type, false otherwise</returns>
        public static bool IsRegistered(Type type) => _Mappers.ContainsKey(type);

        /// <summary>
        /// Retrieves the mapper delegate associated with the given type.
        /// </summary>
        /// <param name="type">Target component type</param>
        /// <returns>Target type's mapper</returns>
        public static BaseMapper Retrieve(Type type) => _Mappers[type];


#if PoolingSystem_AllowDisposer

        private static readonly Dictionary<Type, Disposer> _Disposers = new Dictionary<Type, Disposer>(); // Associates each type with its disposer delegate.

        /// <summary>
        /// Registers the disposer delegate.
        /// </summary>
        /// <param name="type">Target component type</param>
        /// <param name="disposer">Disposer delegate</param>
        public static void Register(Type type, Disposer disposer)
        {
#if UNITY_EDITOR && DEBUG

            if (!type.IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException($"Cannot register a disposer for the non-component type {type.FullName}!", nameof(type));
            }

            if (type == typeof(Transform) || type.IsSubclassOf(typeof(Transform)))
            {
                throw new NotSupportedException($"Registering a disposer for {type.FullName} is not supported!");
            }
#endif
            _Disposers.Add(type, disposer ?? throw new ArgumentNullException(nameof(disposer)));
        }

        /// <summary>
        /// Retrieves the disposer associated with the given type.
        /// </summary>
        /// <param name="type">Target component type</param>
        /// <param name="disposer">Target type's disposer</param>
        /// <returns>True if a disposer for the target type exists, false otherwise</returns>
        public static bool TryGetDisposer(Type type, out Disposer disposer) => _Disposers.TryGetValue(type, out disposer);
#endif
    }
}