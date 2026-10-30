using System;
using Raidline.Core.Common;
using Unity.Profiling;
using UnityEngine;

namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// The single MonoBehaviour that drives per-frame updates of presentation, UI and camera
    /// (ARCHITECTURE §5). Components register in OnEnable and unregister in OnDisable instead of
    /// having their own Update(). Gameplay logic does not belong here — use <see cref="ITickable"/>.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class UpdateRunner : MonoBehaviour
    {
        private static readonly ProfilerMarker s_updateMarker = new ProfilerMarker("Raidline.FrameUpdate");
        private static readonly ProfilerMarker s_lateMarker = new ProfilerMarker("Raidline.LateFrameUpdate");

        private readonly SafeIterationList<IFrameUpdatable> _updates = new SafeIterationList<IFrameUpdatable>(32);
        private readonly SafeIterationList<ILateFrameUpdatable> _lateUpdates = new SafeIterationList<ILateFrameUpdatable>(16);

        /// <summary>Number of registered per-frame updates.</summary>
        public int UpdateCount => _updates.Count;

        /// <summary>Number of registered late updates.</summary>
        public int LateUpdateCount => _lateUpdates.Count;

        public bool Register(IFrameUpdatable updatable)
        {
            if (updatable == null)
            {
                throw new ArgumentNullException(nameof(updatable));
            }

            return _updates.Add(updatable);
        }

        public bool Unregister(IFrameUpdatable updatable) => _updates.Remove(updatable);

        public bool Register(ILateFrameUpdatable updatable)
        {
            if (updatable == null)
            {
                throw new ArgumentNullException(nameof(updatable));
            }

            return _lateUpdates.Add(updatable);
        }

        public bool Unregister(ILateFrameUpdatable updatable) => _lateUpdates.Remove(updatable);

        /// <summary>Runs all per-frame updates once (called by Unity; public for tests).</summary>
        public void RunUpdate(float deltaTime)
        {
            s_updateMarker.Begin();
            int count = _updates.BeginIteration();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    IFrameUpdatable updatable = _updates[i];
                    if (updatable != null)
                    {
                        updatable.FrameUpdate(deltaTime);
                    }
                }
            }
            finally
            {
                _updates.EndIteration();
                s_updateMarker.End();
            }
        }

        /// <summary>Runs all late updates once (called by Unity; public for tests).</summary>
        public void RunLateUpdate(float deltaTime)
        {
            s_lateMarker.Begin();
            int count = _lateUpdates.BeginIteration();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    ILateFrameUpdatable updatable = _lateUpdates[i];
                    if (updatable != null)
                    {
                        updatable.LateFrameUpdate(deltaTime);
                    }
                }
            }
            finally
            {
                _lateUpdates.EndIteration();
                s_lateMarker.End();
            }
        }

        private void Update()
        {
            RunUpdate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            RunLateUpdate(Time.deltaTime);
        }
    }
}
