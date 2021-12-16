using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
    /// <summary>
    /// Maps the base state of the components and may register a <see cref="PostMapperUnit"/>.
    /// </summary>
    /// <param name="source">The component to map from.</param>
    /// <param name="target">The component to map to.</param>
    /// <param name="registeredUnits">List of registered <see cref="PostMapperUnit"/>s.</param>
    /// <remarks>These delegates may be called when the components are in a stale state. Therefore, any reference to the current state of the objects should be avoided and only context-free mapping operations may be performed. A <see cref="PostMapperUnit"/> may be registered to perform the context-dependent mapping operations.</remarks>
    public delegate void BaseMapper(Component source, Component target, List<PostMapperUnit> registeredUnits);

    /// <summary>
    /// Maps context-dependent state of the components.
    /// </summary>
    /// <param name="context">The components' hierarchy information</param>
    /// <remarks>These delegates may only be called if the components are in a valid base state with established hierarchies. In other words, they may only be called after all <see cref="BaseMapper"/> delegates are called.</remarks>
    /// <inheritdoc cref="BaseMapper"/>
    public delegate void PostMapper(Component source, Component target, ref MappingContext context);

#if PoolingSystem_AllowDisposer

    /// <summary>
    /// Performs cleanup operations before a component is disposed of.
    /// </summary>
    /// <param name="target">The component being disposed of.</param>
    public delegate void Disposer(Component target);

#endif

    /// <summary>
    /// Manages the association of types and delegates.
    /// </summary>
    public static partial class DelegateManager
    {
        private static readonly Dictionary<Type, BaseMapper> _Mappers = new Dictionary<Type, BaseMapper>(); // Associates each type with its mapper delegate.

        /// <summary>
        /// Types with registered mappers.
        /// </summary>
        public static Dictionary<Type, BaseMapper>.KeyCollection MappableTypes => _Mappers.Keys;

        /// <summary>
        /// Associates the mapper delegate with the given type.
        /// </summary>
        /// <param name="type">The type to associate the mapper with.</param>
        /// <param name="mapper">The mapper delegate.</param>
        public static void Register(Type type, BaseMapper mapper) => _Mappers.Add(type, mapper ?? throw new ArgumentNullException(nameof(mapper)));

        /// <summary>
        /// Determines whether the given type is associated with any mapper delegate.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>True if a mapper delegate is registered for the type; false otherwise.</returns>
        public static bool IsRegistered(Type type) => _Mappers.ContainsKey(type);

        /// <summary>
        /// Finds the first type not associated with any mapper delegates in the given list.
        /// </summary>
        /// <param name="types">List of types.</param>
        /// <returns>The unregistered type if any; null otherwise.</returns>
        public static Type FindUnregistered(IReadOnlyList<Type> types)
		{
			for (int i = 0; i < types.Count; i++)
			{
				if (!IsRegistered(types[i]))
				{
                    return types[i];
				}
			}

            return null;
		}

        /// <summary>
        /// Retrieves the mapper delegate associated with the given type.
        /// </summary>
        /// <param name="type">The type to get its associated mapper.</param>
        /// <returns>The type's mapper delegate.</returns>
        public static BaseMapper GetMapper(Type type) => _Mappers[type];


#if PoolingSystem_AllowDisposer

        private static readonly Dictionary<Type, Disposer> _Disposers = new Dictionary<Type, Disposer>(); // Associates each type with its disposer delegate.

        /// <summary>
        /// Associates the disposer delegate with the given type.
        /// </summary>
        /// <param name="type">The type to associate the disposer with.</param>
        /// <param name="disposer">The disposer delegate.</param>
        public static void Register(Type type, Disposer disposer) => _Disposers.Add(type, disposer ?? throw new ArgumentNullException(nameof(disposer)));

        /// <summary>
        /// Retrieves the disposer delegate associated with the given type.
        /// </summary>
        /// <param name="type">The type to get its associated disposer.</param>
        /// <returns>The type's disposer delegate.</returns>
        public static bool TryGetDisposer(Type type, out Disposer disposer) => _Disposers.TryGetValue(type, out disposer);

#endif
    }
}