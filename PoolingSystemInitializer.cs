using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling.Controllers
{
    /// <summary>
    /// Performs pooling system's initializations.
    /// </summary>
    public sealed class PoolingSystemInitializer : MonoBehaviour
    {
        /// <summary>
        /// Maximum time each initialization step can take.
        /// </summary>
        public float stepThreshold = 0f;

        /// <summary>
        /// Time delay between initialization steps.
        /// </summary>
        public float interstepDelay = 0f;

        /// <summary>
        /// Whether to remove the pools not declared in the initialization units.
        /// </summary>
        public bool deleteUndeclaredPools = false;

        /// <summary>
        /// Whether to destroy the game object once finished.
        /// </summary>
        public bool destroyOnFinish = true;

        /// <summary>
        /// Pool initialization units.
        /// </summary>
        public InitializationUnit[] units;


        /// <summary>
        /// Invoked after each initialization step.
        /// </summary>
        public EventHandler<float> OnProgress;


        private int _index = -1; // Index of the current initialization unit.
        private float _delayTimer; // Timer for interstep delay.
        private List<PoolHandle> _undeclaredPools; // Keeps track of undeclared pools.


		void OnEnable()
		{
			if (_index == -1) // If not in the middle of an initialization
			{
                _index = 0;
                _delayTimer = 0f;

                if (_undeclaredPools is null)
                {
                    _undeclaredPools = new List<PoolHandle>(PoolingSystem.Handles.Count);
                }
				else
				{
                    _undeclaredPools.Clear();

                    if (_undeclaredPools.Capacity < PoolingSystem.Handles.Count)
                    {
                        _undeclaredPools.Capacity = PoolingSystem.Handles.Count;
                    }
                }

                foreach (PoolHandle handle in PoolingSystem.Handles) // Capture the current pools as undeclared pools.
                {
                    _undeclaredPools.Add(handle);
                }

                _undeclaredPools.Sort(); // Sort the list to allow for binary searching.
            }
		}

		void Update()
        {
            if ((_delayTimer -= Time.deltaTime) <= 0f) // Update the inter-step delay timer and if it hits threshold
            {
                _delayTimer = interstepDelay; // Reset the inter-step delay timer.

                const float InitializationProgressWeight = .9f; // The statistical weight given to the progress of initialization stage.
                const float FullProgressWeight = 1f; // The statistical weight given to the completed progress.

                float startTime = Time.realtimeSinceStartup; // Store current time as the reference point for time measurements.


				while (_index < units.Length) // As long as we are in the initialization stage
                {
                    Transform prototype = units[_index].prototype.transform;

                    int rate = 2; // Rate of change of the number of pool instances per initialization cycle.
                    int count = 0; // Current number of pool instances.

                    PoolSize targetSize = new PoolSize(units[_index].count, units[_index].capacity); // The pool size aimed to achieve.

                    if (PoolingSystem.Exists(prototype, out PoolHandle handle)) // If the target pool exists
                    {
                        PoolSize size = PoolingSystem.GetSize(handle);
                        count = size.Count; // Update the count.

                        if (!units[_index].isDeclarative) // If the initialization is non-declarative
                        {
                            targetSize = PoolSize.Maximize(targetSize, size); // Update the target size.
                        }
                    }
					else
					{
                        handle = PoolingSystem.Initialize(prototype, new PoolSize(0, targetSize.Capacity)); // Create the pool and update the handle.
					}

                    int index = _undeclaredPools.BinarySearch(handle);

                    if (index >= 0)
                    {
                        _undeclaredPools.RemoveAt(index); // Remove current pool from undeclared pools.
                    }

                    float time = startTime;

                    while (count != targetSize.Count)
                    {
                        PoolingSystem.Initialize(prototype, new PoolSize(count = Mathf.Clamp(targetSize.Count, count - rate, count + rate), targetSize.Capacity));

                        float lastTime = time;
                        time = Time.realtimeSinceStartup;

                        if (time - startTime < stepThreshold)
                        {
                            rate = Mathf.CeilToInt(rate * (time - startTime) / (time - lastTime)); // Adjust the rate with regards to how long the last initialization cycle took and the amount of time still left.
                        }
                        else // Abort immediately if hit time threshold.
                        {
                            OnProgress?.Invoke(this, InitializationProgressWeight * ((float)_index / units.Length + .1f * Mathf.Min(count, targetSize.Count) / Mathf.Max(count, targetSize.Count)));
                            return;
                        }
                    }

                    _index++;
                }


				if (deleteUndeclaredPools) // If we are in the deletion stage
				{
					for (int i = _undeclaredPools.Count - 1; i >= 0; i--) // For all undeclared pools
					{
                        PoolingSystem.Delete(_undeclaredPools[i]); // Delete the undeclared pool.

                        _undeclaredPools.RemoveAt(i); // Update undeclared pools.
                        _index++; // Increase the index as a way to keep track of the number of deleted pools (This is required for calculating the progress).

                        if (Time.realtimeSinceStartup - startTime >= stepThreshold) // Abort immediately if hit time threshold.
                        {
                            OnProgress?.Invoke(this, InitializationProgressWeight + (FullProgressWeight - InitializationProgressWeight) * (_index - units.Length) / (_index - units.Length + i));
                            return;
                        }
                    }
                }


                if (destroyOnFinish)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Reset();
                }

                OnProgress?.Invoke(this, FullProgressWeight);
            }
        }


        /// <summary>
        /// Resets the initialization state.
        /// </summary>
		public void Reset()
		{
            _index = -1;
            enabled = false;
		}


		/// <summary>
		/// Holds pool initialization parameters.
		/// </summary>
		[Serializable]
        public struct InitializationUnit
        {
            /// <summary>
            /// An object representing the same composition as the pool.
            /// </summary>
            public GameObject prototype;

            /// <summary>
            /// The number of instances the pool should have.
            /// </summary>
            public int count;

            /// <summary>
            /// The number of instances the pool can hold.
            /// </summary>
            public int capacity;

            /// <summary>
            /// Whether the pool will be initialized to the declared size or to the maximum of its current size and the declared size.
            /// </summary>
            public bool isDeclarative;
        }
    }
}