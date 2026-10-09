using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Drives the simulation from confirmed lockstep frames instead of a local accumulator.</summary>
    public sealed class NetDriver : ITickDriver
    {
        readonly NetSession s;
        readonly LocalDevice dev;
        float acc, resend, stall;
        int tick;
        public bool Stalled => stall > 0.35f;
        public float Alpha => Mathf.Clamp01(acc / Match.TickDt);

        public NetDriver(NetSession session)
        {
            s = session;
            dev = InputHub.Get(InputHub.Merged);
        }

        public void Update(Match m, float realDt)
        {
            if (m.Ended) return;
            if (s.EndReason != null || !s.Connected)
            {
                GameRoot.I.NetFailed(s.EndReason ?? s.Status);
                return;
            }
            acc += Mathf.Min(realDt, 0.1f);
            int maxSteps = 4;
            if (!s.IsHost && s.ConfirmedUpTo - tick > s.Delay + 3) maxSteps = 8;   // catch up when behind
            int steps = 0;
            bool progressed = false;
            while (acc >= Match.TickDt && steps < maxSteps)
            {
                if (s.NeedsLocalInput(tick)) s.PushLocalInput(tick, GameRoot.I.Paused ? (ushort)0 : dev.TakeTick());
                if (!s.TryGetFrame(tick, out var bits)) break;
                m.SimTick(bits);
                s.AfterTick(tick, m.Checksum());
                tick++;
                acc -= Match.TickDt;
                steps++;
                progressed = true;
                if (m.Ended) break;
            }
            if (acc > Match.TickDt * 6f) acc = Match.TickDt * 6f;
            stall = progressed ? 0f : stall + realDt;
            resend -= realDt;
            if (resend <= 0f) { resend = 0.05f; s.ResendFrames(); }
        }

        public void Dispose() { s.EndMatch(); }
    }
}
