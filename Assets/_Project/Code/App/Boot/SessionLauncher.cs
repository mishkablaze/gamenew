using System;
using FishNet.Managing;
using FishNet.Transporting;
using Raidline.Infrastructure.Logging;

namespace Raidline.App.Boot
{
    /// <summary>
    /// Starts and stops network sessions. Single player is host mode: server and client in one
    /// process over the transport bound to the loopback address (D-017).
    /// </summary>
    public sealed class SessionLauncher
    {
        /// <summary>Loopback address used for the offline (host) session.</summary>
        public const string LoopbackIPv4 = "127.0.0.1";

        private const string LoopbackIPv6 = "::1";

        private readonly NetworkManager _networkManager;

        public SessionLauncher(NetworkManager networkManager)
        {
            _networkManager = networkManager != null ? networkManager : throw new ArgumentNullException(nameof(networkManager));
        }

        /// <summary>True while the server or the client runs.</summary>
        public bool IsRunning => _networkManager.IsServerStarted || _networkManager.IsClientStarted;

        /// <summary>True when both server and client are started and the client is authenticated.</summary>
        public bool IsHostReady => _networkManager.IsHostStarted;

        /// <summary>
        /// Starts a host session on the loopback interface (nothing listens on external interfaces).
        /// Returns false if the server or the client failed to start (e.g. the port is busy).
        /// </summary>
        public bool StartOffline(ushort port)
        {
            if (IsRunning)
            {
                Log.Warn(LogCategory.Net, "StartOffline ignored: a session is already running.");
                return false;
            }

            Transport transport = _networkManager.TransportManager.Transport;
            if (transport == null)
            {
                Log.Error(LogCategory.Net, "No transport on the NetworkManager.");
                return false;
            }

            transport.SetServerBindAddress(LoopbackIPv4, IPAddressType.IPv4);
            transport.SetServerBindAddress(LoopbackIPv6, IPAddressType.IPv6);
            transport.SetClientAddress(LoopbackIPv4);
            transport.SetPort(port);

            bool serverStarted = _networkManager.ServerManager.StartConnection();
            if (!serverStarted)
            {
                Log.Error(LogCategory.Net, "Offline session: server failed to start on port " + port + ".");
                return false;
            }

            bool clientStarted = _networkManager.ClientManager.StartConnection();
            if (!clientStarted)
            {
                Log.Error(LogCategory.Net, "Offline session: client failed to start.");
                _networkManager.ServerManager.StopConnection(true);
                return false;
            }

            Log.Dev(LogCategory.Net, "Offline session starting (host on " + LoopbackIPv4 + ":" + port + ").");
            return true;
        }

        /// <summary>Stops the client and the server, if running.</summary>
        public void Stop()
        {
            if (_networkManager.IsClientStarted || _networkManager.ClientManager.Started)
            {
                _networkManager.ClientManager.StopConnection();
            }

            if (_networkManager.IsServerStarted)
            {
                _networkManager.ServerManager.StopConnection(true);
            }
        }
    }
}
