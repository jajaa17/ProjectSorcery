using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>
    /// Peer-to-host UDP session with deterministic lockstep.
    /// Lobby: host owns the MatchConfig; clients send their pick/ready.
    /// Match: every peer sends its per-tick input bits to the host; the host bundles all human inputs for
    /// each tick into a FRAME and broadcasts it. Everyone simulates a tick only once its FRAME is known.
    /// </summary>
    public sealed class NetSession
    {
        public static NetSession Current;

        enum Msg : byte { Hello = 1, Welcome, Reject, Lobby, Pick, Start, StartAck, Input, Frame, Hash, Desync, Ping, Pong, Bye }
        const byte Magic = 0x53, Version = 3;
        public const int MaxPeers = 4;

        public sealed class Peer
        {
            public int Id;
            public EndPoint Ep;
            public string Name = "";
            public float LastHeard;
            public int InputAck = -1;      // highest contiguous tick we have inputs for (host side)
            public int FrameAck = -1;      // highest contiguous frame the client confirmed
            public bool StartAcked;
            public float Ping;
        }

        public readonly bool IsHost;
        public int MyId { get; private set; } = -1;
        public string Status = "";
        public bool Connected => IsHost || MyId >= 0;
        public bool InMatch;
        public string EndReason;
        public MatchConfig Lobby = new MatchConfig();
        public readonly List<Peer> Peers = new List<Peer>();
        public event Action OnLobbyChanged;
        public event Action<MatchConfig> OnStart;

        Socket sock;
        EndPoint hostEp;
        readonly byte[] rxBuf = new byte[2048];
        readonly MemoryStream txStream = new MemoryStream(2048);
        readonly BinaryWriter tx;
        float time, lastLobbySend, lastHello, lastPing;
        int startToken;
        float startSentAt;

        // lockstep buffers
        const int Ring = 1024;
        readonly ushort[][] frames = new ushort[Ring][];      // confirmed per-slot bits by tick
        readonly bool[] frameKnown = new bool[Ring];
        readonly ushort[,] peerInputs = new ushort[MaxPeers, Ring]; // host: inputs from each peer
        readonly bool[,] peerKnown = new bool[MaxPeers, Ring];
        readonly ushort[] localInputs = new ushort[Ring];
        readonly bool[] localKnown = new bool[Ring];
        readonly uint[] hashes = new uint[Ring];
        public int ConfirmedUpTo = -1;   // client: highest contiguous frame received
        int slotCount;
        int[] slotPeer;                  // slot -> peer id (or -1 for CPU)

        NetSession(bool host)
        {
            IsHost = host;
            tx = new BinaryWriter(txStream);
        }

        // ================================================================== setup
        public static NetSession Host(int port, string name)
        {
            var s = new NetSession(true);
            try
            {
                s.Open(port);
                s.MyId = 0;
                s.Peers.Add(new Peer { Id = 0, Name = name, LastHeard = 0f });
                s.Lobby = DefaultLobby(name);
                s.Status = "Hosting on port " + port + ". Share your IP with friends (LAN, port forward, or a VPN like Tailscale/ZeroTier).";
            }
            catch (Exception e)
            {
                s.Status = "Could not host: " + e.Message;
                s.Close();
                return null;
            }
            Current = s;
            return s;
        }

        public static NetSession Join(string address, int defaultPort, string name)
        {
            var s = new NetSession(false);
            try
            {
                string host = address.Trim();
                int port = defaultPort;
                int colon = host.LastIndexOf(':');
                if (colon > 0 && int.TryParse(host.Substring(colon + 1), out int p)) { port = p; host = host.Substring(0, colon); }
                IPAddress ip;
                if (!IPAddress.TryParse(host, out ip))
                {
                    var addrs = Dns.GetHostAddresses(host);
                    ip = null;
                    foreach (var a in addrs) if (a.AddressFamily == AddressFamily.InterNetwork) { ip = a; break; }
                    if (ip == null) throw new Exception("could not resolve " + host);
                }
                s.hostEp = new IPEndPoint(ip, port);
                s.Open(0);
                s.Peers.Add(new Peer { Id = 0, Ep = s.hostEp, Name = "Host" });
                s.myName = name;
                s.Status = "Connecting to " + ip + ":" + port + "...";
            }
            catch (Exception e)
            {
                s.Status = "Could not join: " + e.Message;
                s.Close();
                return null;
            }
            Current = s;
            return s;
        }

        string myName = "Sorcerer";

        void Open(int port)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Blocking = false;
            try { sock.IOControl(unchecked((IOControlCode)(-1744830452)), new byte[] { 0, 0, 0, 0 }, null); } catch { } // ignore ICMP resets (Windows)
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void Close()
        {
            if (sock != null)
            {
                try
                {
                    if (IsHost)
                    {
                        foreach (var p in Peers) if (p.Id != 0) Send(p.Ep, Msg.Bye);
                    }
                    else if (hostEp != null)
                    {
                        Send(hostEp, Msg.Bye);
                    }
                }
                catch { }
                try { sock.Close(); } catch { }
                sock = null;
            }
            if (Current == this) Current = null;
        }

        static MatchConfig DefaultLobby(string hostName)
        {
            var c = new MatchConfig { Mode = GameMode.Versus, Layout = TeamLayout.Duel, Online = true, RoundsToWin = 2, RoundTime = 99f, Arena = 0, InputDelay = Settings.InputDelay };
            for (int i = 0; i < 4; i++) c.Slots.Add(new SlotConfig { Control = SlotControl.None, Peer = -1, Character = 0, Name = "" });
            c.Slots[0] = new SlotConfig { Control = SlotControl.Remote, Peer = 0, Name = hostName, Character = 0, Team = 0 };
            c.Slots[1] = new SlotConfig { Control = SlotControl.Cpu, Diff = Difficulty.Medium, Character = 1, Team = 1 };
            NormalizeLobby(c);
            return c;
        }

        /// <summary>Fixes slot count / teams for the chosen mode and layout.</summary>
        public static void NormalizeLobby(MatchConfig c)
        {
            int n = c.Mode == GameMode.Survival ? 2 : c.Mode == GameMode.Raid ? 4 : MatchConfig.SlotCount(c.Layout);
            while (c.Slots.Count < 4) c.Slots.Add(new SlotConfig { Control = SlotControl.None });
            for (int i = 0; i < 4; i++)
            {
                var s = c.Slots[i];
                if (i >= n) { if (s.Control != SlotControl.Remote) s.Control = SlotControl.None; continue; }
                if (c.Mode == GameMode.Versus) s.Team = MatchConfig.TeamFor(c.Layout, i);
                else if (c.Mode == GameMode.Survival) s.Team = 0;
                else s.Team = i == 3 ? 1 : 0;
                if (c.Mode == GameMode.Raid && i == 3) { s.Control = SlotControl.Cpu; s.Diff = Difficulty.Nightmare; s.Peer = -1; s.Name = ""; s.Character = Roster.BossIndex; }
                if (c.Mode == GameMode.Versus && s.Control == SlotControl.None && i < n) { s.Control = SlotControl.Cpu; s.Diff = Difficulty.Medium; }
            }
            if (c.Mode == GameMode.Raid) c.Arena = Arenas.RaidArena;
            else if (c.Arena >= Arenas.SelectableCount) c.Arena = 0;
        }

        // ================================================================== send helpers
        void Begin(Msg m)
        {
            txStream.Position = 0; txStream.SetLength(0);
            tx.Write(Magic); tx.Write(Version); tx.Write((byte)m);
        }

        void Flush(EndPoint ep)
        {
            if (sock == null || ep == null) return;
            try { sock.SendTo(txStream.GetBuffer(), 0, (int)txStream.Length, SocketFlags.None, ep); }
            catch (SocketException) { }
        }

        void Send(EndPoint ep, Msg m) { Begin(m); Flush(ep); }

        void BroadcastLobby()
        {
            var data = Lobby.Serialize();
            foreach (var p in Peers)
            {
                if (p.Id == 0) continue;
                Begin(Msg.Lobby); tx.Write((short)data.Length); tx.Write(data); Flush(p.Ep);
            }
        }

        // ================================================================== frame update
        public void Update(float dt)
        {
            if (sock == null) return;
            time += dt;
            Receive();

            if (!IsHost)
            {
                if (MyId < 0 && time - lastHello > 0.5f) { lastHello = time; Begin(Msg.Hello); tx.Write(myName); Flush(hostEp); }
                if (Peers.Count > 0 && Peers[0].LastHeard > 0f && time - Peers[0].LastHeard > 8f) Fail("Lost connection to host.");
                if (MyId >= 0 && Peers.Count > 0 && Peers[0].LastHeard == 0f && time > 10f) Fail("Host did not respond.");
            }
            else
            {
                if (!InMatch && time - lastLobbySend > 0.25f) { lastLobbySend = time; BroadcastLobby(); }
                for (int i = Peers.Count - 1; i >= 0; i--)
                {
                    var p = Peers[i];
                    if (p.Id == 0) continue;
                    if (time - p.LastHeard > 8f) { DropPeer(p, "timed out"); }
                }
                if (pendingStart != null && time - startSentAt > 0.3f) SendStart();
            }
            if (time - lastPing > 1f) { lastPing = time; PingAll(); }
        }

        void PingAll()
        {
            if (IsHost) { foreach (var p in Peers) if (p.Id != 0) { Begin(Msg.Ping); tx.Write(time); Flush(p.Ep); } }
            else if (hostEp != null) { Begin(Msg.Ping); tx.Write(time); Flush(hostEp); }
        }

        void Fail(string why)
        {
            EndReason = why;
            Status = why;
            Close();
        }

        void DropPeer(Peer p, string why)
        {
            Peers.Remove(p);
            foreach (var s in Lobby.Slots) if (s.Control == SlotControl.Remote && s.Peer == p.Id) { s.Control = Lobby.Mode == GameMode.Versus ? SlotControl.Cpu : SlotControl.None; s.Peer = -1; s.Name = ""; }
            if (InMatch) EndReason = p.Name + " disconnected (" + why + ").";
            OnLobbyChanged?.Invoke();
        }

        void Receive()
        {
            int guard = 0;
            while (sock != null && guard++ < 256)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int n;
                try
                {
                    if (sock.Available <= 0) break;
                    n = sock.ReceiveFrom(rxBuf, ref from);
                }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
                if (n < 3 || rxBuf[0] != Magic) continue;
                if (rxBuf[1] != Version) { if (IsHost) { Begin(Msg.Reject); tx.Write("Version mismatch - update the game."); Flush(from); } continue; }
                try { Handle((Msg)rxBuf[2], from, n); } catch (Exception e) { Debug.LogWarning("[Net] bad packet: " + e.Message); }
            }
        }

        Peer PeerByEp(EndPoint ep)
        {
            foreach (var p in Peers) if (p.Ep != null && p.Ep.Equals(ep)) return p;
            return null;
        }

        void Handle(Msg m, EndPoint from, int len)
        {
            using (var ms = new MemoryStream(rxBuf, 3, len - 3))
            using (var r = new BinaryReader(ms))
            {
                var peer = IsHost ? PeerByEp(from) : (Peers.Count > 0 ? Peers[0] : null);
                if (peer != null) peer.LastHeard = time;
                switch (m)
                {
                    case Msg.Hello when IsHost:
                    {
                        string name = r.ReadString();
                        if (peer == null)
                        {
                            if (InMatch) { Begin(Msg.Reject); tx.Write("Match already in progress."); Flush(from); return; }
                            int slot = FreeSlot();
                            if (slot < 0 || Peers.Count >= MaxPeers) { Begin(Msg.Reject); tx.Write("Lobby is full."); Flush(from); return; }
                            int id = 1; while (Peers.Exists(p => p.Id == id)) id++;
                            peer = new Peer { Id = id, Ep = from, Name = SanitizeName(name), LastHeard = time };
                            Peers.Add(peer);
                            var s = Lobby.Slots[slot];
                            s.Control = SlotControl.Remote; s.Peer = id; s.Name = peer.Name; s.Ready = false;
                            OnLobbyChanged?.Invoke();
                        }
                        Begin(Msg.Welcome); tx.Write((byte)peer.Id); Flush(from);
                        BroadcastLobby();
                        break;
                    }
                    case Msg.Welcome when !IsHost:
                        MyId = r.ReadByte();
                        Status = "Connected. Waiting for the host to start.";
                        break;
                    case Msg.Reject when !IsHost:
                        Fail("Rejected: " + r.ReadString());
                        break;
                    case Msg.Lobby when !IsHost:
                    {
                        int l = r.ReadInt16();
                        var data = r.ReadBytes(l);
                        Lobby = MatchConfig.Deserialize(data, 0, data.Length);
                        OnLobbyChanged?.Invoke();
                        break;
                    }
                    case Msg.Pick when IsHost:
                    {
                        if (peer == null) return;
                        short ch = r.ReadInt16(); bool ready = r.ReadBoolean();
                        foreach (var s in Lobby.Slots) if (s.Control == SlotControl.Remote && s.Peer == peer.Id) { s.Character = Mathf.Clamp(ch, 0, Roster.Count - 1); s.Ready = ready; }
                        OnLobbyChanged?.Invoke();
                        break;
                    }
                    case Msg.Start when !IsHost:
                    {
                        int token = r.ReadInt32();
                        int l = r.ReadInt16();
                        var data = r.ReadBytes(l);
                        Begin(Msg.StartAck); tx.Write(token); Flush(hostEp);
                        if (InMatch && token == startToken) return;
                        startToken = token;
                        var cfg = MatchConfig.Deserialize(data, 0, data.Length);
                        BeginMatch(cfg);
                        OnStart?.Invoke(cfg);
                        break;
                    }
                    case Msg.StartAck when IsHost:
                    {
                        if (peer == null) return;
                        int token = r.ReadInt32();
                        if (token == startToken) peer.StartAcked = true;
                        if (pendingStart != null && Peers.TrueForAll(p => p.Id == 0 || p.StartAcked))
                        {
                            var cfg = pendingStart;
                            pendingStart = null;
                            BeginMatch(cfg);
                            OnStart?.Invoke(cfg);
                        }
                        break;
                    }
                    case Msg.Input when IsHost:
                    {
                        if (peer == null || !InMatch) return;
                        int first = r.ReadInt32(); int count = r.ReadByte();
                        for (int i = 0; i < count; i++)
                        {
                            ushort b = r.ReadUInt16();
                            int t = first + i;
                            if (t < 0) continue;
                            peerInputs[peer.Id, t % Ring] = b;
                            peerKnown[peer.Id, t % Ring] = true;
                        }
                        while (peerKnown[peer.Id, (peer.InputAck + 1 + Ring) % Ring] && peer.InputAck + 1 <= first + count - 1) peer.InputAck++;
                        peer.FrameAck = Math.Max(peer.FrameAck, r.ReadInt32());
                        break;
                    }
                    case Msg.Frame when !IsHost:
                    {
                        if (!InMatch) return;
                        int first = r.ReadInt32(); int count = r.ReadByte(); int slots = r.ReadByte();
                        for (int i = 0; i < count; i++)
                        {
                            int t = first + i;
                            var arr = new ushort[slots];
                            for (int s = 0; s < slots; s++) arr[s] = r.ReadUInt16();
                            if (t <= ConfirmedUpTo || t < 0) continue;
                            frames[t % Ring] = arr;
                            frameKnown[t % Ring] = true;
                        }
                        while (frameKnown[(ConfirmedUpTo + 1) % Ring] && frames[(ConfirmedUpTo + 1) % Ring] != null && ConfirmedUpTo + 1 <= first + count - 1) ConfirmedUpTo++;
                        hostInputAck = Math.Max(hostInputAck, r.ReadInt32());
                        break;
                    }
                    case Msg.Hash when IsHost:
                    {
                        int t = r.ReadInt32(); uint h = r.ReadUInt32();
                        if (hashTick[t % 64] == t && hashes[t % Ring] != h)
                        {
                            EndReason = "Desync detected (tick " + t + "). The match was stopped to keep things fair.";
                            foreach (var p in Peers) if (p.Id != 0) Send(p.Ep, Msg.Desync);
                        }
                        break;
                    }
                    case Msg.Desync when !IsHost:
                        EndReason = "Desync detected. The match was stopped to keep things fair.";
                        break;
                    case Msg.Ping:
                    {
                        float t = r.ReadSingle();
                        Begin(Msg.Pong); tx.Write(t); Flush(from);
                        break;
                    }
                    case Msg.Pong:
                        if (peer != null) peer.Ping = (time - r.ReadSingle()) * 1000f;
                        break;
                    case Msg.Bye:
                        if (IsHost && peer != null) DropPeer(peer, "left");
                        else if (!IsHost) Fail("The host closed the session.");
                        break;
                }
            }
        }

        int FreeSlot()
        {
            NormalizeLobby(Lobby);
            int n = Lobby.Mode == GameMode.Survival ? 2 : Lobby.Mode == GameMode.Raid ? 3 : MatchConfig.SlotCount(Lobby.Layout);
            for (int i = 0; i < n; i++) if (Lobby.Slots[i].Control == SlotControl.Cpu || Lobby.Slots[i].Control == SlotControl.None) return i;
            return -1;
        }

        static string SanitizeName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "Sorcerer";
            n = n.Replace("<", "").Replace(">", "");
            return n.Length > 16 ? n.Substring(0, 16) : n;
        }

        // ================================================================== lobby actions
        public void SendPick(int character, bool ready)
        {
            if (IsHost)
            {
                foreach (var s in Lobby.Slots) if (s.Control == SlotControl.Remote && s.Peer == 0) { s.Character = character; s.Ready = ready; }
                OnLobbyChanged?.Invoke();
                return;
            }
            if (hostEp == null) return;
            Begin(Msg.Pick); tx.Write((short)character); tx.Write(ready); Flush(hostEp);
        }

        MatchConfig pendingStart;

        public bool CanStart(out string why)
        {
            why = "";
            int humans = 0;
            foreach (var s in Lobby.Slots)
            {
                if (s.Control != SlotControl.Remote) continue;
                humans++;
                if (!s.Ready) { why = (string.IsNullOrEmpty(s.Name) ? "A player" : s.Name) + " is not ready."; return false; }
            }
            if (humans < 1) { why = "No players."; return false; }
            return true;
        }

        public void HostStart()
        {
            if (!IsHost || !CanStart(out _)) return;
            var cfg = Lobby.Copy();
            cfg.Online = true;
            cfg.Seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            cfg.InputDelay = Settings.InputDelay;
            if (cfg.Mode == GameMode.Raid) cfg.Arena = Arenas.RaidArena;
            for (int i = cfg.Slots.Count - 1; i >= 0; i--) if (cfg.Slots[i].Control == SlotControl.None) cfg.Slots.RemoveAt(i);
            var pick = new System.Random((int)(cfg.Seed & 0x7fffffff));
            foreach (var s in cfg.Slots)
            {
                if (s.Control == SlotControl.Remote) s.Device = -1;
                if (s.Control == SlotControl.Cpu && s.Diff != Difficulty.Nightmare) s.Character = Roster.RandomPlayableIndex(pick);
            }
            startToken = UnityEngine.Random.Range(1, int.MaxValue);
            foreach (var p in Peers) p.StartAcked = p.Id == 0;
            pendingStart = cfg;
            SendStart();
            if (Peers.Count == 1) { pendingStart = null; BeginMatch(cfg); OnStart?.Invoke(cfg); }
        }

        void SendStart()
        {
            if (pendingStart == null) return;
            startSentAt = time;
            var data = pendingStart.Serialize();
            foreach (var p in Peers)
            {
                if (p.Id == 0 || p.StartAcked) continue;
                Begin(Msg.Start); tx.Write(startToken); tx.Write((short)data.Length); tx.Write(data); Flush(p.Ep);
            }
        }

        // ================================================================== lockstep
        public int Delay { get; private set; } = 3;
        int nextLocal;            // next tick to generate local input for
        int hostInputAck = -1;    // client: host's ack of our inputs
        readonly int[] hashTick = new int[64];

        void BeginMatch(MatchConfig cfg)
        {
            InMatch = true;
            EndReason = null;
            Delay = Mathf.Clamp(cfg.InputDelay, 1, 8);
            slotCount = cfg.Slots.Count;
            slotPeer = new int[slotCount];
            for (int i = 0; i < slotCount; i++) slotPeer[i] = cfg.Slots[i].Control == SlotControl.Remote ? cfg.Slots[i].Peer : -1;
            Array.Clear(frameKnown, 0, Ring);
            Array.Clear(localKnown, 0, Ring);
            Array.Clear(peerKnown, 0, peerKnown.Length);
            for (int i = 0; i < Ring; i++) frames[i] = null;
            for (int i = 0; i < 64; i++) hashTick[i] = -1;
            ConfirmedUpTo = -1;
            hostInputAck = -1;
            foreach (var p in Peers) { p.InputAck = -1; p.FrameAck = -1; }
            // the first `Delay` ticks have empty input for everyone
            nextLocal = 0;
            for (int t = 0; t < Delay; t++) { localInputs[t] = 0; localKnown[t] = true; nextLocal = t + 1; if (IsHost) for (int p = 0; p < MaxPeers; p++) { peerInputs[p, t] = 0; peerKnown[p, t] = true; } }
            foreach (var p in Peers) p.InputAck = Delay - 1;
        }

        /// <summary>True when the local input for (simTick + Delay) hasn't been recorded yet.</summary>
        public bool NeedsLocalInput(int simTick) => nextLocal <= simTick + Delay;

        /// <summary>Records the local player's input for the next unscheduled tick.</summary>
        public void PushLocalInput(int simTick, ushort bits)
        {
            // keep local input exactly `Delay` ticks ahead of the simulation
            while (nextLocal <= simTick + Delay)
            {
                localInputs[nextLocal % Ring] = bits;
                localKnown[nextLocal % Ring] = true;
                if (IsHost) { peerInputs[0, nextLocal % Ring] = bits; peerKnown[0, nextLocal % Ring] = true; }
                nextLocal++;
            }
            if (!IsHost) SendInputs();
        }

        void SendInputs()
        {
            if (hostEp == null) return;
            int first = Math.Max(0, Math.Max(hostInputAck + 1, nextLocal - 40));
            int count = Math.Min(60, nextLocal - first);
            if (count <= 0) return;
            Begin(Msg.Input); tx.Write(first); tx.Write((byte)count);
            for (int i = 0; i < count; i++) tx.Write(localInputs[(first + i) % Ring]);
            tx.Write(ConfirmedUpTo);
            Flush(hostEp);
        }

        /// <summary>Host: assemble the frame for a tick if every human's input is known.</summary>
        bool HostAssemble(int t)
        {
            if (frameKnown[t % Ring] && frames[t % Ring] != null) return true;
            var arr = new ushort[slotCount];
            for (int s = 0; s < slotCount; s++)
            {
                int pid = slotPeer[s];
                if (pid < 0) continue;
                if (pid >= MaxPeers || !Peers.Exists(p => p.Id == pid)) continue;     // disconnected: idle
                if (!peerKnown[pid, t % Ring]) return false;
                arr[s] = peerInputs[pid, t % Ring];
            }
            frames[t % Ring] = arr;
            frameKnown[t % Ring] = true;
            return true;
        }

        void HostBroadcastFrames(int upTo)
        {
            foreach (var p in Peers)
            {
                if (p.Id == 0) continue;
                int first = Math.Max(0, Math.Max(p.FrameAck + 1, upTo - 40));
                int count = Math.Min(40, upTo - first + 1);
                if (count <= 0) continue;
                Begin(Msg.Frame); tx.Write(first); tx.Write((byte)count); tx.Write((byte)slotCount);
                for (int i = 0; i < count; i++)
                {
                    var arr = frames[(first + i) % Ring];
                    for (int s = 0; s < slotCount; s++) tx.Write(arr != null ? arr[s] : (ushort)0);
                }
                tx.Write(p.InputAck);
                Flush(p.Ep);
            }
        }

        int hostConfirmed = -1;

        /// <summary>Returns the input bits for a tick if it's ready to simulate.</summary>
        public bool TryGetFrame(int t, out ushort[] bits)
        {
            bits = null;
            if (IsHost)
            {
                if (!HostAssemble(t)) return false;
                if (t > hostConfirmed) { hostConfirmed = t; HostBroadcastFrames(t); }
                bits = frames[t % Ring];
                return true;
            }
            if (t > ConfirmedUpTo) return false;
            bits = frames[t % Ring];
            return bits != null;
        }

        public void AfterTick(int t, uint checksum)
        {
            hashes[t % Ring] = checksum;
            hashTick[t % 64] = t;
            if (!IsHost && t % 60 == 0 && hostEp != null) { Begin(Msg.Hash); tx.Write(t); tx.Write(checksum); Flush(hostEp); }
            frameKnown[(t + Ring - 200) % Ring] = false; // recycle very old slots
            if (IsHost) for (int p = 0; p < MaxPeers; p++) peerKnown[p, (t + Ring - 200) % Ring] = false;
            localKnown[(t + Ring - 200) % Ring] = false;
        }

        public void ResendFrames()
        {
            if (IsHost && hostConfirmed >= 0) HostBroadcastFrames(hostConfirmed);
            else if (!IsHost) SendInputs();
        }

        public int Lead => IsHost ? 0 : ConfirmedUpTo;

        public void EndMatch() { InMatch = false; }
    }
}
