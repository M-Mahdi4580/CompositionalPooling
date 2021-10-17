using System;
using System.Collections;
using System.Collections.Generic;


namespace CompositionalPooling.Controllers
{
    /// <summary>
    /// Describes the initialization unit's processing mode.
    /// </summary>
    public enum InitializationMode
    {
        /// <summary>
        /// The pool will be resized to fit the maximum of the current size and the given size.
        /// </summary>
        SemiDeclarative,

        /// <summary>
        /// The pool will be resized to fit the given size.
        /// </summary>
        Declarative,

        /// <summary>
        /// The pool's size will be increased by the given size.
        /// </summary>
        Additive
    }

    /// <summary>
    /// Allows for asynchronous pool initialization.
    /// </summary>
    public sealed class AsyncInitializer : IEnumerator<int>
    {
        private readonly IReadOnlyCollection<InitializationUnit> _Units;
        private readonly HashSet<PoolHandle> _UndeclaredPools = null;
        private readonly IEnumerator<int> _SemiCoroutine;


        /// <summary>
        /// Whether the existing undeclared pools will be cleaned up.
        /// </summary>
        public bool CleanupUndeclaredPools => _UndeclaredPools != null;

        /// <summary>
        /// Initialization unit's processing mode
        /// </summary>
        public readonly InitializationMode Mode;

        /// <summary>
        /// Approximate number of steps left.
        /// </summary>
        public int Current => _SemiCoroutine.Current;

        /// <summary>
        /// Executes the next step.
        /// </summary>
        /// <returns>True if more steps are left, false otherwise</returns>
        public bool MoveNext() => _SemiCoroutine.MoveNext();

        /// <param name="units">The initialization units to use</param>
        /// <param name="mode">The unit's processing mode</param>
        /// <param name="cleanupUndeclaredPools">Whether to clean up the existing undeclared pools</param>
        public AsyncInitializer(IReadOnlyCollection<InitializationUnit> units, InitializationMode mode, bool cleanupUndeclaredPools)
        {
            _Units = units;
            Mode = mode;
            _SemiCoroutine = GetSemiCoroutine();

            if (cleanupUndeclaredPools)
            {
                _UndeclaredPools = new HashSet<PoolHandle>(PoolingSystem.PoolManager.Handles);
            }
        }


        private IEnumerator<int> GetSemiCoroutine()
        {
            int counter = 0;

            if (CleanupUndeclaredPools)
            {
                foreach (InitializationUnit unit in _Units)
                {
                    _UndeclaredPools.Remove(Initialize(unit, Mode));
                    yield return _UndeclaredPools.Count + _Units.Count - ++counter;
                }

                foreach (PoolHandle handle in _UndeclaredPools)
                {
                    if (PoolingSystem.PoolManager.Exists(in handle))
                    {
                        PoolingSystem.PoolManager.Update(in handle, new PoolSize(0, PoolingSystem.PoolManager.GetSize(in handle).MaxCapacity));
                    }
                }
            }
            else
            {
                foreach (InitializationUnit unit in _Units)
                {
                    Initialize(in unit, Mode);
                    yield return _Units.Count - ++counter;
                }
            }
        }

        /// <summary>
        /// Performs the unit's expressed initialization.
        /// </summary>
        /// <param name="unit">Initialization unit</param>
        /// <param name="mode">Initialization mode</param>
        /// <returns>The initialized pool's handle</returns>
        public static PoolHandle Initialize(in InitializationUnit unit, InitializationMode mode)
        {
            if (PoolingSystem.PoolManager.Exists(unit.Composition, out PoolHandle handle))
            {
                PoolingSystem.PoolManager.Update(in handle, mode == InitializationMode.SemiDeclarative ? PoolSize.Maximize(unit.Size, PoolingSystem.PoolManager.GetSize(in handle)) : mode == InitializationMode.Declarative ? unit.Size : PoolingSystem.PoolManager.GetSize(in handle) + unit.Size);
            }
            else
            {
                PoolingSystem.PoolManager.TryCreate(in unit, out handle);
            }

            return handle;
        }


        object IEnumerator.Current => Current;
        void IEnumerator.Reset() => _SemiCoroutine.Reset();
        void IDisposable.Dispose() => _SemiCoroutine.Dispose();
    }
}