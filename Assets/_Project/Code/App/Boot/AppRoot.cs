using FishNet.Managing;
using FishNet.Managing.Timing;
using Raidline.Gameplay.Networking;
using Raidline.Infrastructure.Diagnostics;
using Raidline.Infrastructure.Logging;
using Raidline.Infrastructure.Ticking;
using UnityEngine;

namespace Raidline.App.Boot
{
    /// <summary>
    /// Composition root of the Boot scene (ARCHITECTURE §4). Lives for the whole session
    /// (DontDestroyOnLoad), creates <see cref="AppContext"/>, configures Fish-Net timing
    /// (60 Hz, physics stepped by the TimeManager) and starts the offline host session.
    /// Runs after Fish-Net's NetworkManager has created its managers.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Fish-Net NetworkManager of the Boot scene.")]
        private NetworkManager _networkManager;

        [SerializeField]
        [Range(1, 240)]
        [Tooltip("Simulation ticks per second (D-007).")]
        private ushort _tickRate = 60;

        [SerializeField]
        [Tooltip("Start the offline host session automatically when the Boot scene loads.")]
        private bool _startOfflineOnBoot = true;

        [SerializeField]
        [Tooltip("Loopback port of the offline host session.")]
        private ushort _offlinePort = 7770;

        private NetworkTickSource _tickSource;
        private bool _loggedTickRate;

        /// <summary>Session-wide services; null until Awake has run.</summary>
        public AppContext Context { get; private set; }

        private void Awake()
        {
            if (_networkManager == null)
            {
                _networkManager = GetComponent<NetworkManager>();
            }

            if (_networkManager == null || _networkManager.TimeManager == null)
            {
                Log.Error(LogCategory.Boot, "AppRoot needs an initialized Fish-Net NetworkManager. Run Raidline/Setup/Create Boot Scene.");
                enabled = false;
                return;
            }

            DontDestroyOnLoad(gameObject);

            TimeManager timeManager = _networkManager.TimeManager;
            timeManager.SetTickRate(_tickRate);
            timeManager.SetPhysicsMode(PhysicsMode.TimeManager);

            UpdateRunner updateRunner = GetComponent<UpdateRunner>();
            if (updateRunner == null)
            {
                updateRunner = gameObject.AddComponent<UpdateRunner>();
            }

            var tickRunner = new TickRunner();
            _tickSource = new NetworkTickSource(_networkManager);
            tickRunner.Attach(_tickSource);

            var tickRateMeter = new TickRateMeter();
            tickRunner.Register(TickPhase.Raid, tickRateMeter);

            Context = new AppContext(
                _networkManager,
                new SessionLauncher(_networkManager),
                _tickSource,
                tickRunner,
                updateRunner,
                tickRateMeter);

            Log.Dev(LogCategory.Boot, "AppRoot ready: tick rate " + _tickRate + " Hz, physics stepped by TimeManager.");
        }

        private void Start()
        {
            if (Context != null && _startOfflineOnBoot)
            {
                Context.Session.StartOffline(_offlinePort);
            }
        }

        private void Update()
        {
            // One-time boot diagnostic; not gameplay logic.
            if (_loggedTickRate || Context == null || Context.TickRateMeter.TotalTicks < (ulong)_tickRate * 3)
            {
                return;
            }

            _loggedTickRate = true;
            Log.Dev(LogCategory.Tick, "Measured tick rate: " + Context.TickRateMeter.TicksPerSecond.ToString("0.0") + " ticks/s; host ready: " + Context.Session.IsHostReady + ".");
        }

        private void OnDestroy()
        {
            if (Context == null)
            {
                return;
            }

            Context.Session.Stop();
            Context.TickRunner.Detach();
            _tickSource?.Dispose();
            Context = null;
        }
    }
}
