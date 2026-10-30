using FishNet.Managing;
using Raidline.Infrastructure.Diagnostics;
using Raidline.Infrastructure.Ticking;

namespace Raidline.App.Boot
{
    /// <summary>
    /// Services that live for the whole session (ARCHITECTURE §4). Created by <see cref="AppRoot"/>
    /// and passed explicitly to whoever needs them — never looked up through statics.
    /// </summary>
    public sealed class AppContext
    {
        public AppContext(
            NetworkManager network,
            SessionLauncher session,
            ITickSource tickSource,
            TickRunner tickRunner,
            UpdateRunner updateRunner,
            TickRateMeter tickRateMeter)
        {
            Network = network;
            Session = session;
            TickSource = tickSource;
            TickRunner = tickRunner;
            UpdateRunner = updateRunner;
            TickRateMeter = tickRateMeter;
        }

        /// <summary>Fish-Net network manager (TimeManager, ServerManager, ClientManager, transport).</summary>
        public NetworkManager Network { get; }

        /// <summary>Starts/stops sessions (offline = host).</summary>
        public SessionLauncher Session { get; }

        /// <summary>Source of simulation ticks (Fish-Net TimeManager).</summary>
        public ITickSource TickSource { get; }

        /// <summary>Runs gameplay systems by phase on every tick.</summary>
        public TickRunner TickRunner { get; }

        /// <summary>Per-frame updates for presentation, UI and camera.</summary>
        public UpdateRunner UpdateRunner { get; }

        /// <summary>Measured ticks per second (diagnostics).</summary>
        public TickRateMeter TickRateMeter { get; }
    }
}
