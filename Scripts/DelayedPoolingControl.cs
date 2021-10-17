using UnityEngine;
using CompositionalPooling.Utility;


namespace CompositionalPooling
{
    public static partial class PoolingSystem
    {
        /// <summary>
        /// Creates and initializes the <see cref="DelayedPoolingControl"/>'s singleton.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateDelayControlSingleton()
        {
            DelayedPoolingControl.Instance = new GameObject(nameof(PoolingSystem)).AddComponent<DelayedPoolingControl>();
            DelayedPoolingControl.Instance.gameObject.hideFlags = PoolInstanceHideFlags;
        }

        /// <summary>
        /// Controls delayed (non-immediate) releases.
        /// </summary>
        private sealed class DelayedPoolingControl : MonoBehaviour
        {
            private readonly DynamicArray<PoolingTimerInfo> _TimerInfos = new DynamicArray<PoolingTimerInfo>();

            void LateUpdate()
            {
                float deltaTime = Time.deltaTime;

                for (int i = _TimerInfos.Count - 1; i >= 0; i--)
                {
                    if ((_TimerInfos[i].timer -= deltaTime) <= 0f)
                    {
                        Transform instance = _TimerInfos[i].Instance;

                        _TimerInfos.RemoveAt(i);

                        if (instance)
                        {
                            ReleaseImmediate(instance);
                        }
                    }
                }

                if (_TimerInfos.Count == 0)
                {
                    enabled = false;
                }
            }

            /// <summary>
            /// Pools the instance after the delay.
            /// </summary>
            /// <param name="instance">The instance to pool</param>
            /// <param name="delay">The delay time before pooling</param>
            public void Pool(Transform instance, float delay)
            {
                _TimerInfos.Add(new PoolingTimerInfo(instance, delay));
                enabled = true;
            }


            private struct PoolingTimerInfo
            {
                public readonly Transform Instance;
                public float timer;

                public PoolingTimerInfo(Transform instance, float delay)
                {
                    Instance = instance;
                    timer = delay;
                }
            }


#if UNITY_EDITOR && DEBUG

            private static DelayedPoolingControl _instance;
            public static DelayedPoolingControl Instance
            {
                get
                {
                    if (!_instance) // If the instance doesn't exist (possibly destroyed by the user)
                    {
                        CreateDelayControlSingleton(); // Recreate the instance.
                    }

                    return _instance;
                }
                set => _instance = value;
            }


            private bool _isExitingPlayMode = false; // A flag that indicates whether the the editor is exiting playmode.

            void Awake()
            {
                // This object and its childed pool instances are hidden objects. They won't be auto-destroyed when exiting play mode and should be manually destroyed to prevent memory leaks.
                UnityEditor.EditorApplication.playModeStateChanged += e =>
                {
                    if (e == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                    {
                        _isExitingPlayMode = true;
                        DestroyImmediate(gameObject);
                    }
                };
            }

            void OnDestroy()
            {
                // This instance is the editor root of the pool instances. When this instance is destroyed, all the pool instances childed underneath it, are also destroyed which causes the pools' references to those instances to become invalid. To resolve this issue, we will manually clear and destroy the pools. This will also invoke the destroy event for the pools, broadcasting the event to all listeners.
                PoolHandle[] handles = new PoolHandle[_PoolMap.Keys.Count];
                _PoolMap.Keys.CopyTo(handles, 0);

                for (int i = 0; i < handles.Length; i++)
                {
                    PoolHandle handle = handles[i];

                    _PoolMap[handle].Pool.Clear();
                    PoolManager.Destroy(in handle);
                }

                if (!_isExitingPlayMode) // If the object is destroyed as a result of exiting play mode, no user warning is needed.
                {
                    Debug.LogWarning(nameof(PoolingSystem) + "'s editor root is destroyed! This causes all the pools to be destroyed!");
                }
            }
#else
            public static DelayedPoolingControl Instance;
#endif
        }
    }
}