using System;
using System.Collections.Generic;
using UnityEngine;


namespace CompositionalPooling
{
	public static partial class PoolingSystem
    {
        private static DelayedReleaseControl _delayedReleaser = null; // Holds the delayed release singleton.
        private static DelayedReleaseControl _DelayedReleaser // Returns the delayed release singleton or creates one if it doesn't exist.
        {
            get
            {
                if (_delayedReleaser is null)
                {
                    _delayedReleaser = new GameObject(nameof(PoolingSystem)).AddComponent<DelayedReleaseControl>();
                    _delayedReleaser.gameObject.hideFlags = PoolInstanceHideFlags;

#if UNITY_EDITOR && DEBUG

                    Action<UnityEditor.PlayModeStateChange> playModeExitHandler = e =>
                    {
                        if (e == UnityEditor.PlayModeStateChange.ExitingPlayMode) // If we are exiting playmode
                        {
                            UnityEngine.Object.DestroyImmediate(_delayedReleaser); // Destroy the singleton and its childed hidden pool instances to prevent memory leaks.
                        }
                    };

                    UnityEditor.EditorApplication.playModeStateChanged += playModeExitHandler; // Attach the play-mode handler.
                    _delayedReleaser.OnDestroyed += (sender, e) =>
                    {
                        _delayedReleaser = null; // Nullify the field explicitly.
                        UnityEditor.EditorApplication.playModeStateChanged -= playModeExitHandler; // Detach the play-mode handler to prevent having multiple handlers after recreation.

                        PoolHandle[] handles = new PoolHandle[Handles.Count];
                        Handles.CopyTo(handles, 0);

                        for (int i = 0; i < handles.Length; i++) // For all pools
                        {
                            _PoolMap[handles[i]].Instances.Clear(); // Clear the pool from the corrupted references to the destroyed instances. This accommodates for the unsynchronized destruction of the pool instances which happens as a side-effect of destroying their common root which is this object.
                            Delete(handles[i]); // Remove the pool formally. This broadcasts the event to all listeners.
                        }
                    };

#endif
                }

                return _delayedReleaser;
            }
        }

        /// <summary>
        /// Controls delayed (non-immediate) releases.
        /// </summary>
        private sealed class DelayedReleaseControl : MonoBehaviour
        {
            private readonly DynamicArray<DelayedRelease> _PendingReleases = new DynamicArray<DelayedRelease>(); // The list of pending releases.

            void LateUpdate()
            {
                float deltaTime = Time.deltaTime;

                for (int i = _PendingReleases.Count - 1; i >= 0; i--)
                {
                    if ((_PendingReleases[i].delay -= deltaTime) <= 0f) // Update the timer and if it hits threshold
                    {
                        if (_PendingReleases[i].Instance) // If the object is not destroyed
                        {
                            ReleaseImmediate(_PendingReleases[i].Instance); // Release the object.
                        }

                        _PendingReleases.RemoveAt(i); // Update the list.
                    }
                }

                if (_PendingReleases.Count == 0) // If there are no pending releases
                {
                    enabled = false; // Deactivate. This improves the performance.
                }
            }

            /// <inheritdoc cref="DelayedRelease(Transform, float)"/>
            public void Release(Transform instance, float delay)
            {
                _PendingReleases.Add(new DelayedRelease(instance, delay));
                enabled = true;
            }


            /// <summary>
            /// Holds a delayed release's parameters.
            /// </summary>
            private struct DelayedRelease
            {
                /// <summary>
                /// The object to release.
                /// </summary>
                public readonly Transform Instance;

                /// <summary>
                /// Time delay before the release.
                /// </summary>
                public float delay;


                /// <param name="instance">The object to release.</param>
                /// <param name="delay">Time delay before the release.</param>
                public DelayedRelease(Transform instance, float delay)
                {
                    Instance = instance;
                    this.delay = delay;
                }
            }


#if UNITY_EDITOR && DEBUG

            public event EventHandler OnDestroyed;
            void OnDestroy() => OnDestroyed.Invoke(this, EventArgs.Empty);

#endif
        }
    }
}