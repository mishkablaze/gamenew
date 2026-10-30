using System;
using FishNet.Managing;
using FishNet.Managing.Timing;
using Raidline.Infrastructure.Ticking;

namespace Raidline.Gameplay.Networking
{
    /// <summary>
    /// Tick source backed by Fish-Net's <see cref="TimeManager"/> (D-007, D-017).
    /// TimeManager.OnTick → <see cref="PrePhysicsTick"/>; Fish-Net then steps physics
    /// (Physics Mode = TimeManager); TimeManager.OnPostTick → <see cref="PostPhysicsTick"/>.
    /// </summary>
    public sealed class NetworkTickSource : ITickSource, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly TimeManager _timeManager;
        private bool _disposed;

        /// <exception cref="ArgumentNullException">No NetworkManager or it has no TimeManager yet.</exception>
        public NetworkTickSource(NetworkManager networkManager)
        {
            _networkManager = networkManager != null ? networkManager : throw new ArgumentNullException(nameof(networkManager));
            _timeManager = networkManager.TimeManager;
            if (_timeManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager), "NetworkManager has no TimeManager (not initialized yet?).");
            }

            _timeManager.OnTick += HandleTick;
            _timeManager.OnPostTick += HandlePostTick;
        }

        public event TickCallback PrePhysicsTick;

        public event TickCallback PostPhysicsTick;

        public double TickDelta => _timeManager.TickDelta;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
                _timeManager.OnPostTick -= HandlePostTick;
            }
        }

        private void HandleTick()
        {
            TickCallback callback = PrePhysicsTick;
            if (callback == null)
            {
                return;
            }

            TickContext ctx = CreateContext();
            callback(in ctx);
        }

        private void HandlePostTick()
        {
            TickCallback callback = PostPhysicsTick;
            if (callback == null)
            {
                return;
            }

            TickContext ctx = CreateContext();
            callback(in ctx);
        }

        private TickContext CreateContext()
        {
            return new TickContext(
                _timeManager.LocalTick,
                (float)_timeManager.TickDelta,
                _networkManager.IsServerStarted,
                _networkManager.IsClientStarted,
                false);
        }
    }
}
