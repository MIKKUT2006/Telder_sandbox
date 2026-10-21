using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Save;
using Game.World;
using Game.World.Dimensions;

namespace Game.Networking
{
    /// <summary>
    /// Lightweight LAN/VPN multiplayer transport. TCP carries reliable state commands;
    /// UDP is used only for LAN server discovery. All Unity API work stays on main thread.
    /// </summary>
    public sealed class TelderNetworkRuntime : MonoBehaviour
    {
        public const int TcpPort = 47821;
        public const int DiscoveryPort = 47822;
        public static TelderNetworkRuntime Instance { get; private set; }

        [Serializable] public class Packet
        {
            public string type, id, name, dimension, worldName, message;
            public float x, y, z;
            public int seed, blockX, blockY, port;
            public ushort blockId;
        }
        [Serializable] public class DiscoveryRecord
        {
            public string worldName, hostName, address, dimension;
            public int port, players, seed;
        }
        private sealed class Peer
        {
            public string id, name;
            public TcpClient client;
            public StreamWriter writer;
            public readonly object writeLock = new object();
        }

        private readonly ConcurrentQueue<Tuple<Packet, Peer>> incoming = new ConcurrentQueue<Tuple<Packet, Peer>>();
        private readonly ConcurrentQueue<DiscoveryRecord> discovered = new ConcurrentQueue<DiscoveryRecord>();
        private readonly Dictionary<string, Peer> peers = new Dictionary<string, Peer>();
        private readonly Dictionary<string, RemotePlayerAvatar> avatars = new Dictionary<string, RemotePlayerAvatar>();
        private TcpListener listener;
        private TcpClient client;
        private StreamWriter clientWriter;
        private readonly object clientWriteLock = new object();
        private Thread acceptThread, discoveryThread, clientReadThread;
        private volatile bool running;
        private volatile bool hosting;
        private float nextDiscovery;
        private float nextSnapshot;
        private string localId = "host";
        private string localNick = "Игрок";
        private string worldName = "Сетевая игра";
        private int worldSeed;
        private string hostDimension = "surface";
        private string hostAddress;
        private string lastStatus = "";
        private readonly List<DiscoveryRecord> servers = new List<DiscoveryRecord>();
        private readonly object serversLock = new object();

        public bool IsHosting => hosting;
        public bool IsConnected { get; private set; }
        public string Status => lastStatus;
        public string LocalNick => localNick;
        public IReadOnlyList<DiscoveryRecord> Servers { get { lock (serversLock) return servers.ToArray(); } }
        public event Action StatusChanged;
        public event Action ServersChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("TelderNetworkRuntime");
            DontDestroyOnLoad(go);
            go.AddComponent<TelderNetworkRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.runInBackground = true;
        }

        public void SetNick(string nick)
        {
            localNick = CleanNick(nick);
            PlayerPrefs.SetString("Telder.NetworkNick", localNick);
        }

        public static string CleanNick(string nick)
        {
            nick = (nick ?? "").Trim();
            if (nick.Length > 20) nick = nick.Substring(0, 20);
            return string.IsNullOrWhiteSpace(nick) ? "Игрок" : nick;
        }

        public void Host(string nick, string name)
        {
            StopSession();
            SetNick(nick);
            worldName = string.IsNullOrWhiteSpace(name) ? "Сетевая игра" : name.Trim();
            if (!SaveGameRuntime.CreateNewSave(worldName))
            {
                SetStatus("Не удалось создать сохранение.");
                return;
            }
            hostDimension = DimensionTravelRuntime.Current.Name;
            worldSeed = DimensionTravelRuntime.Current.Seed;
            running = true;
            hosting = true;
            IsConnected = true;
            localId = "host";
            try
            {
                listener = new TcpListener(IPAddress.Any, TcpPort);
                listener.Start(16);
                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "Telder TCP accept" };
                acceptThread.Start();
                discoveryThread = new Thread(DiscoveryLoop) { IsBackground = true, Name = "Telder UDP discovery" };
                discoveryThread.Start();
                SetStatus("Сервер запущен. Порт TCP " + TcpPort + ", UDP " + DiscoveryPort);
            }
            catch (Exception e)
            {
                StopSession();
                SetStatus("Не удалось открыть порт: " + e.Message);
                return;
            }
            LoadGameScene();
        }

        public void Join(string address, string nick)
        {
            StopSession();
            SetNick(nick);
            address = (address ?? "").Trim();
            if (string.IsNullOrWhiteSpace(address)) { SetStatus("Укажи IP-адрес сервера."); return; }
            try
            {
                client = new TcpClient();
                client.NoDelay = true;
                client.Connect(address, TcpPort);
                clientWriter = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
                IsConnected = true;
                hosting = false;
                running = true;
                hostAddress = address;
                SendClient(new Packet { type = "hello", name = localNick });
                clientReadThread = new Thread(ClientReadLoop) { IsBackground = true, Name = "Telder client reader" };
                clientReadThread.Start();
                SetStatus("Подключено к " + address + ". Ожидание данных мира…");
            }
            catch (Exception e)
            {
                StopSession();
                SetStatus("Не удалось подключиться: " + e.Message);
            }
        }

        public void RefreshServers()
        {
            var found = new List<DiscoveryRecord>();
            try
            {
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    udp.Client.ReceiveTimeout = 150;
                    byte[] bytes = Encoding.UTF8.GetBytes("TELDER_DISCOVER_V1");
                    udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                    var until = DateTime.UtcNow.AddMilliseconds(650);
                    while (DateTime.UtcNow < until)
                    {
                        try
                        {
                            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                            byte[] response = udp.Receive(ref ep);
                            var record = JsonUtility.FromJson<DiscoveryRecord>(Encoding.UTF8.GetString(response));
                            if (record != null && !string.IsNullOrWhiteSpace(record.worldName))
                            {
                                record.address = ep.Address.ToString();
                                found.Add(record);
                            }
                        }
                        catch (SocketException) { }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Telder Multiplayer] Discovery: " + e.Message); }
            lock (serversLock)
            {
                servers.Clear();
                servers.AddRange(found);
            }
        }

        private void AcceptLoop()
        {
            while (running && listener != null)
            {
                try
                {
                    TcpClient tcp = listener.AcceptTcpClient();
                    tcp.NoDelay = true;
                    var peer = new Peer { client = tcp };
                    peer.writer = new StreamWriter(tcp.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
                    var thread = new Thread(() => PeerReadLoop(peer)) { IsBackground = true };
                    thread.Start();
                }
                catch (SocketException) { if (running) Thread.Sleep(50); }
                catch (ObjectDisposedException) { break; }
            }
        }

        private void PeerReadLoop(Peer peer)
        {
            try
            {
                using (var reader = new StreamReader(peer.client.GetStream(), Encoding.UTF8))
                {
                    string line;
                    while (running && peer.client.Connected && (line = reader.ReadLine()) != null)
                    {
                        Packet p = JsonUtility.FromJson<Packet>(line);
                        if (p != null) incoming.Enqueue(Tuple.Create(p, peer));
                    }
                }
            }
            catch { }
            incoming.Enqueue(Tuple.Create(new Packet { type = "disconnect" }, peer));
        }

        private void ClientReadLoop()
        {
            try
            {
                using (var reader = new StreamReader(client.GetStream(), Encoding.UTF8))
                {
                    string line;
                    while (running && client.Connected && (line = reader.ReadLine()) != null)
                    {
                        Packet p = JsonUtility.FromJson<Packet>(line);
                        if (p != null) incoming.Enqueue(Tuple.Create(p, (Peer)null));
                    }
                }
            }
            catch { }
            if (running) incoming.Enqueue(Tuple.Create(new Packet { type = "connection_lost" }, (Peer)null));
        }

        private void DiscoveryLoop()
        {
            UdpClient udp = null;
            try
            {
                udp = new UdpClient(DiscoveryPort);
                udp.EnableBroadcast = true;
                var ep = new IPEndPoint(IPAddress.Any, 0);
                while (running && hosting)
                {
                    if (!udp.Client.Poll(200000, SelectMode.SelectRead)) continue;
                    byte[] bytes = udp.Receive(ref ep);
                    if (Encoding.UTF8.GetString(bytes) != "TELDER_DISCOVER_V1") continue;
                    var rec = new DiscoveryRecord
                    {
                        worldName = worldName, hostName = Environment.MachineName,
                        port = TcpPort, players = peers.Count + 1,
                        seed = worldSeed, dimension = hostDimension
                    };
                    byte[] response = Encoding.UTF8.GetBytes(JsonUtility.ToJson(rec));
                    udp.Send(response, response.Length, ep);
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally { if (udp != null) udp.Close(); }
        }

        private void Update()
        {
            while (incoming.TryDequeue(out var item))
            {
                if (hosting) HandleHostPacket(item.Item1, item.Item2);
                else HandleClientPacket(item.Item1);
            }
            if (IsConnected && Time.unscaledTime >= nextSnapshot)
            {
                nextSnapshot = Time.unscaledTime + 0.05f;
                Transform player = FindLocalPlayer();
                if (player != null)
                {
                    var dim = SafeDimension();
                    var packet = new Packet { type = "position", id = localId, name = localNick,
                        x = player.position.x, y = player.position.y, z = player.position.z, dimension = dim };
                    if (hosting) Broadcast(packet, null);
                    else SendClient(packet);
                }
            }
            foreach (var pair in avatars)
                if (pair.Value != null) pair.Value.SetVisibleInDimension(pair.Value.Dimension == SafeDimension());
        }

        private void HandleHostPacket(Packet p, Peer peer)
        {
            if (p.type == "hello" && peer != null)
            {
                peer.name = CleanNick(p.name);
                peer.id = Guid.NewGuid().ToString("N").Substring(0, 8);
                peers[peer.id] = peer;
                SendPeer(peer, new Packet { type = "welcome", id = peer.id, name = peer.name,
                    worldName = worldName, seed = worldSeed, dimension = hostDimension });
                foreach (var existing in peers.Values)
                    if (existing != peer)
                        SendPeer(peer, new Packet { type = "player_join", id = existing.id, name = existing.name });
                EnsureAvatar(peer.id, peer.name);
                Broadcast(new Packet { type = "player_join", id = peer.id, name = peer.name }, peer);
                SetStatus(peer.name + " подключился.");
                return;
            }
            if (p.type == "disconnect" && peer != null)
            {
                if (!string.IsNullOrEmpty(peer.id))
                {
                    peers.Remove(peer.id);
                    Broadcast(new Packet { type = "player_leave", id = peer.id }, null);
                }
                try { peer.client.Close(); } catch { }
                return;
            }
            if (peer != null && !string.IsNullOrEmpty(peer.id))
            {
                p.id = peer.id;
                p.name = peer.name;
                if (p.type == "position")
                {
                    var avatar = EnsureAvatar(p.id, p.name);
                    avatar.SetTarget(new Vector3(p.x, p.y, p.z), p.dimension);
                    Broadcast(p, peer);
                }
                else if (p.type == "block")
                {
                    // Only mutate a loaded host dimension; client-side echoes are suppressed.
                    bool accepted = false;
                    if (p.dimension == SafeDimension() && WorldManager.Instance != null)
                    {
                        TelderNetworkRuntime.SuppressBlockBroadcast = true;
                        try { accepted = WorldManager.Instance.SetBlock(p.blockX, p.blockY, p.blockId); }
                        finally { TelderNetworkRuntime.SuppressBlockBroadcast = false; }
                    }
                    if (accepted) Broadcast(p, peer);
                }
            }
        }

        private void HandleClientPacket(Packet p)
        {
            switch (p.type)
            {
                case "welcome":
                    localId = p.id;
                    worldName = p.worldName;
                    worldSeed = p.seed;
                    hostDimension = p.dimension;
                    // Make a local save shell using the authoritative dimension seed.
                    if (SaveGameRuntime.CreateNewSave(worldName))
                    {
                        DimensionTravelRuntime.SetCurrentForLoad(hostDimension, worldSeed);
                        LoadGameScene();
                    }
                    SetStatus("Подключено к миру «" + worldName + "».");
                    break;
                case "player_join":
                    if (!string.IsNullOrEmpty(p.id) && p.id != localId) EnsureAvatar(p.id, p.name);
                    break;
                case "player_leave":
                    RemoveAvatar(p.id);
                    break;
                case "position":
                    if (p.id != localId)
                    {
                        var avatar = EnsureAvatar(p.id, p.name);
                        avatar.SetTarget(new Vector3(p.x, p.y, p.z), p.dimension);
                    }
                    break;
                case "block":
                    if (p.dimension == SafeDimension() && WorldManager.Instance != null)
                    {
                        SuppressBlockBroadcast = true;
                        try { WorldManager.Instance.SetBlock(p.blockX, p.blockY, p.blockId); }
                        finally { SuppressBlockBroadcast = false; }
                    }
                    break;
                case "connection_lost":
                    SetStatus("Соединение с сервером потеряно.");
                    IsConnected = false;
                    break;
            }
        }

        public static bool SuppressBlockBroadcast { get; private set; }

        public static void NotifyBlockChanged(int x, int y, ushort id)
        {
            var n = Instance;
            if (n == null || SuppressBlockBroadcast || !n.IsConnected) return;
            var p = new Packet { type = "block", blockX = x, blockY = y, blockId = id, dimension = SafeDimension() };
            if (n.hosting) n.Broadcast(p, null); else n.SendClient(p);
        }

        private RemotePlayerAvatar EnsureAvatar(string id, string nick)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!avatars.TryGetValue(id, out var avatar) || avatar == null)
            {
                GameObject go = new GameObject("RemotePlayer_" + id);
                avatar = go.AddComponent<RemotePlayerAvatar>();
                avatar.Initialize(CleanNick(nick));
                avatars[id] = avatar;
            }
            else avatar.SetNick(CleanNick(nick));
            return avatar;
        }

        private void RemoveAvatar(string id)
        {
            if (avatars.TryGetValue(id, out var a) && a != null) Destroy(a.gameObject);
            avatars.Remove(id);
        }

        private void Broadcast(Packet p, Peer except)
        {
            string json = JsonUtility.ToJson(p);
            lock (peers)
                foreach (var peer in peers.Values)
                    if (peer != except) Write(peer, json);
        }

        private void SendPeer(Peer peer, Packet p) { Write(peer, JsonUtility.ToJson(p)); }
        private void Write(Peer peer, string line)
        {
            try { lock (peer.writeLock) peer.writer.WriteLine(line); }
            catch { }
        }

        private void SendClient(Packet p)
        {
            try { if (clientWriter != null) lock (clientWriteLock) clientWriter.WriteLine(JsonUtility.ToJson(p)); }
            catch { }
        }

        private Transform FindLocalPlayer()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                if (p.GetComponent<RemotePlayerAvatar>() == null) return p.transform;
            }
            return null;
        }

        private static string SafeDimension()
        {
            try { return DimensionTravelRuntime.Current.Name; }
            catch { return "surface"; }
        }

        private void SetStatus(string status)
        {
            lastStatus = status ?? "";
            StatusChanged?.Invoke();
            Debug.Log("[Telder Multiplayer] " + lastStatus);
        }

        private void LoadGameScene()
        {
            string sceneName = "Game";
            var selectors = FindObjectsByType<Game.UI.MainMenu.SaveSelectionController>(FindObjectsSortMode.None);
            if (selectors.Length > 0)
            {
                var field = typeof(Game.UI.MainMenu.SaveSelectionController).GetField("gameSceneName",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    var value = field.GetValue(selectors[0]) as string;
                    if (!string.IsNullOrWhiteSpace(value)) sceneName = value;
                }
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void StopSession()
        {
            running = false;
            hosting = false;
            IsConnected = false;
            try { listener?.Stop(); } catch { }
            try { client?.Close(); } catch { }
            try { clientWriter?.Dispose(); } catch { }
            listener = null; client = null; clientWriter = null;
            lock (peers)
            {
                foreach (var peer in peers.Values) { try { peer.client.Close(); } catch { } }
                peers.Clear();
            }
            foreach (var a in avatars.Values) if (a != null) Destroy(a.gameObject);
            avatars.Clear();
        }

        private void OnApplicationQuit() { StopSession(); }
        private void OnDestroy() { if (Instance == this) StopSession(); }
    }
}
