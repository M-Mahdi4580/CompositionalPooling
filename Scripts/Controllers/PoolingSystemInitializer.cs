using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CompositionalPooling.Utility;


namespace CompositionalPooling.Controllers
{
    /// <summary>
    /// Initializes the system's pools.
    /// </summary>
    public sealed class PoolingSystemInitializer : MonoBehaviour
    {
        /// <inheritdoc cref="AsyncInitializer.CleanupUndeclaredPools"/>
        public bool cleanupUndeclaredPools = false;

        /// <summary>
        /// Whether to destroy empty pools after the initialization.
        /// </summary>
        public bool destroyEmptyPools = false;

        /// <summary>
        /// Whether to destroy this object and its gameObject once initialization is finished.
        /// </summary>
        public bool selfDestructOnFinish = true;

        /// <summary>
        /// Time delay between initialization steps.
        /// </summary>
        public float interStepDelay;

        /// <summary>
        /// Maximum time each initialization step is allowed to take.
        /// </summary>
        public double stepThreshold;

        /// <summary>
        /// Initialization units processing mode.
        /// </summary>
        public InitializationMode initializationMode = InitializationMode.SemiDeclarative;

        /// <summary>
        /// The units to use for initialization.
        /// </summary>
        public PrototypeBasedInitializationUnit[] initializationUnits;

        /// <summary>
        /// Initialization's progress.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// Invoked on each initialization step.
        /// </summary>
        public EventHandler<float> OnStep;

        private AsyncInitializer _initializer;
        private float _delayTimer;

        
        void OnEnable()
        {
            if (_initializer == null)
            {
                _initializer = new AsyncInitializer(new PrototypeBasedInitializationUnit.Converter(initializationUnits, initializationMode), initializationMode, cleanupUndeclaredPools);
                _delayTimer = 0f;
                Progress = 0f;
            }
        }

        void Update()
        {
            if ((_delayTimer -= Time.deltaTime) < 0f)
            {
                double startTime = Time.realtimeSinceStartupAsDouble;

                do
                {
                    if (_initializer.MoveNext())
                    {
                        Progress = Mathf.Min(Progress + (1f - Progress) / _initializer.Current, 1f);
                    }
                    else
                    {
                        _initializer = null;
                        enabled = false;

                        if (destroyEmptyPools) DestroyEmptyPools();
                        if (selfDestructOnFinish) Destroy(gameObject);

                        break;
                    }
                }
                while (Time.realtimeSinceStartupAsDouble - startTime < stepThreshold);

                _delayTimer = interStepDelay;
                OnStep?.Invoke(this, Progress);
            }
        }

        /// <summary>
        /// Destroys system's empty pools.
        /// </summary>
        public static void DestroyEmptyPools()
        {
            List<PoolHandle> emptyPools = null;

            foreach (PoolHandle handle in PoolingSystem.PoolManager.Handles)
            {
                if (PoolingSystem.PoolManager.GetSize(in handle).Count == 0)
                {
                    if (emptyPools == null)
                    {
                        emptyPools = new List<PoolHandle>();
                    }

                    emptyPools.Add(handle);
                }
            }

            for (int i = 0, len = emptyPools.Count; i < len; i++)
            {
                PoolingSystem.PoolManager.Destroy(emptyPools[i]);
            }
        }


        /// <summary>
        /// Represents a serializable initialization unit defining the pool's composition using a game object.
        /// </summary>
        [Serializable]
        public struct PrototypeBasedInitializationUnit
        {
            /// <summary>
            /// An object with the same composition as the pool.
            /// </summary>
            public GameObject prototype;

            /// <summary>
            /// Number of instances to preload the pool with.
            /// </summary>
            public int loadCount;

            /// <summary>
            /// The number of instances to add to the pool in each step.
            /// </summary>
            public int loadRate;

            /// <summary>
            /// Capacity of the pool.
            /// </summary>
            public int capacity;

            /// <summary>
            /// Converts prototype-based initialization units to normal initialization units.
            /// </summary>
            public class Converter : IReadOnlyCollection<InitializationUnit>
            {
                private readonly PrototypeBasedInitializationUnit[] _Units;

                private readonly List<Component> _ComponentBuffer = new List<Component>();
                private readonly List<Type> _TypeBuffer = new List<Type>();

                /// <inheritdoc cref="initializationMode"/>
                public readonly InitializationMode Mode;

                public int Count
                {
                    get
                    {
                        int count = 0;

                        for (int i = 0; i < _Units.Length; i++)
                        {
                            count += Mathf.CeilToInt((float)_Units[i].loadCount / _Units[i].loadRate);
                        }

                        return count;
                    }
                }

                public Converter(PrototypeBasedInitializationUnit[] units, InitializationMode mode)
                {
                    _Units = units;
                    Mode = mode;
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                public IEnumerator<InitializationUnit> GetEnumerator()
                {
                    for (int i = 0; i < _Units.Length; i++)
                    {
                        _Units[i].prototype.transform.GetComposition(_ComponentBuffer, _TypeBuffer);

                        if (_Units[i].loadRate <= 0 || _Units[i].loadRate > _Units[i].loadCount || _Units[i].capacity < _Units[i].loadCount)
                        {
                            throw new ArgumentOutOfRangeException(_Units[i].loadRate <= 0 ? nameof(loadRate) : _Units[i].loadRate > _Units[i].loadCount ? nameof(loadCount) : nameof(capacity));
                        }

                        for (int count = 0; count < _Units[i].loadCount; count += _Units[i].loadRate)
                        {
                            int capacityIncreaseRate = _Units[i].capacity * _Units[i].loadRate / _Units[i].loadCount;
                            PoolSize unitSize = new PoolSize(Mathf.Min(_Units[i].loadRate, _Units[i].loadCount - count), Mathf.Min(capacityIncreaseRate, _Units[i].capacity - capacityIncreaseRate * count / _Units[i].loadRate));

                            if (Mode != InitializationMode.Additive)
                            {
                                unitSize += new PoolSize(count, _Units[i].capacity * count / _Units[i].loadCount);

                                if (Mode == InitializationMode.Declarative && PoolingSystem.PoolManager.Exists(_TypeBuffer, out PoolHandle handle))
                                {
                                    unitSize = PoolSize.Maximize(PoolingSystem.PoolManager.GetSize(in handle), unitSize);
                                }
                            }

                            yield return new InitializationUnit(_TypeBuffer, in unitSize);
                        }

                        if (Mode == InitializationMode.Declarative)
                        {
                            AsyncInitializer.Initialize(new InitializationUnit(_TypeBuffer, new PoolSize(_Units[i].loadCount, _Units[i].capacity)), Mode);
                        }
                    }
                }
            }
        }
    }
}