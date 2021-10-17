using System;
using UnityEngine;
using CompositionalPooling.Utility;


namespace CompositionalPooling.Controllers
{
    /// <summary>
    /// Cleans up inactive pools over time.
    /// </summary>
    public sealed class PoolingSystemAutoCleanup : MonoBehaviour
    {
        /// <summary>
        /// Start timer threshold for each pool.
        /// </summary>
        public float startThreshold;

        /// <summary>
        /// Minimum timer threshold allowed for each pool.
        /// </summary>
        public float minThreshold;

        /// <summary>
        /// Maximum timer threshold allowed for each pool.
        /// </summary>
        public float maxThreshold = float.PositiveInfinity;

        /// <summary>
        /// Threshold's increase when the pool is accessed.
        /// </summary>
        public float thresholdGrowthRate;

        /// <summary>
        /// Threshold's decrease on pool timer's reset.
        /// </summary>
        public float thresholdDecayRate;

        /// <summary>
        /// Time delay before destroying empty pools.
        /// </summary>
        public float delayBeforeDestruction = float.PositiveInfinity;

        private SortedList<PoolHandle> _handles;
        private DynamicArray<TimerInfo> _timers;


        void Awake()
        {
            _handles = new SortedList<PoolHandle>();
            _timers = new DynamicArray<TimerInfo>();
        }

        void OnEnable()
        {
            _handles.Clear();
            _timers.Clear();

            foreach (PoolHandle handle in PoolingSystem.PoolManager.Handles)
            {
                Add(in handle);
            }

            PoolingSystem.PoolManager.OnCreate += PoolCreationHandler;
            PoolingSystem.PoolManager.OnDestroy += PoolDestructionHandler;

            EventHandler<PoolHandle> accessHandler = PoolAccessHandler;
            PoolingSystem.OnPool += accessHandler;
            PoolingSystem.OnUnpool += accessHandler;
            PoolingSystem.PoolManager.OnUpdate += accessHandler;
        }

        void OnDisable()
        {
            PoolingSystem.PoolManager.OnCreate -= PoolCreationHandler;
            PoolingSystem.PoolManager.OnDestroy -= PoolDestructionHandler;

            EventHandler<PoolHandle> accessHandler = PoolAccessHandler;
            PoolingSystem.OnPool -= accessHandler;
            PoolingSystem.OnUnpool -= accessHandler;
            PoolingSystem.PoolManager.OnUpdate -= accessHandler;
        }

        void Update()
        {
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                ref TimerInfo timer = ref _timers[i];

                if ((timer.timer -= Time.deltaTime) <= 0f)
                {
                    PoolHandle handle = _handles[i];
                    PoolSize oldSize = PoolingSystem.PoolManager.GetSize(in handle);

                    if (oldSize.Count > 0)
                    {
                        PoolingSystem.PoolManager.Update(in handle, new PoolSize(oldSize.Count - 1, oldSize.MaxCapacity));
                        timer.timer = timer.threshold = Mathf.Max(timer.threshold - thresholdDecayRate - thresholdGrowthRate, minThreshold); // Updating the pool in the previous statement, results in an inevitable call to the pool access handler which increases the timer's threshold. To cancel the effects of that call, the threshold growth rate must be subtracted as well.
                    }
                    else if (timer.timer < delayBeforeDestruction)
                    {
                        PoolingSystem.PoolManager.Destroy(in handle);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the pool to the cleanup process.
        /// </summary>
        /// <param name="handle">Target pool's handle</param>
        private void Add(in PoolHandle handle)
        {
            _handles.Add(handle);
            _timers.Insert(_handles.IndexOf(handle), new TimerInfo() { threshold = startThreshold, timer = startThreshold });
        }

        /// <inheritdoc cref="Add(in PoolHandle)"/>
        private void PoolCreationHandler(object sender, PoolHandle handle) => Add(in handle);

        /// <summary>
        /// Removes the pool from the cleanup process.
        /// </summary>
        /// <param name="handle">Target pool's handle</param>
        private void PoolDestructionHandler(object sender, PoolHandle handle)
        {
            int index = _handles.IndexOf(handle);
            _timers.RemoveAt(index);
            _handles.RemoveAt(index);
        }

        /// <summary>
        /// Increases the timer threshold for the pool's cleanup.
        /// </summary>
        /// <param name="handle">Target pool's handle</param>
        private void PoolAccessHandler(object sender, PoolHandle handle)
        {
            ref TimerInfo timer = ref _timers[_handles.IndexOf(handle)];
            timer.timer = timer.threshold = Mathf.Min(timer.threshold + thresholdGrowthRate, maxThreshold);
        }


        private struct TimerInfo
        {
            public float timer;
            public float threshold;
        }
    }
}