using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSorcery
{
    public struct Arena
    {
        public float Left, Right, Ceiling;
        public int Theme;
        public float Width => Right - Left;
    }

    /// <summary>
    /// Notifications from the simulation. Mode controllers (sim-side) may react; UI/audio listeners must not
    /// change simulation state.
    /// </summary>
    public sealed class MatchEvents
    {
        public event Action<Fighter, Fighter, int> OnBlackFlash;
        public event Action<Fighter, Fighter> OnKilled;
        public event Action<Fighter, Fighter, float> OnDamaged;
        public event Action<Fighter, int> OnChant;
        public event Action<Fighter> OnLowCe;
        public event Action<Fighter, StatusType> OnStatusAdded, OnStatusEnded;
        public event Action<string, string, Color, float> OnAnnounce;
        public event Action<Fighter, DomainDef> OnDomainExpanded;
        public event Action<List<Fighter>> OnClashStart;
        public event Action<Fighter> OnClashEnd;
        public event Action<Fighter, DomainDef> OnDomainEnded;
        public event Action<string> OnCustom;

        public void BlackFlash(Fighter a, Fighter v, int streak) => OnBlackFlash?.Invoke(a, v, streak);
        public void Killed(Fighter v, Fighter k) => OnKilled?.Invoke(v, k);
        public void Damaged(Fighter v, Fighter a, float d) => OnDamaged?.Invoke(v, a, d);
        public void Chant(Fighter f, int lvl) => OnChant?.Invoke(f, lvl);
        public void LowCe(Fighter f) => OnLowCe?.Invoke(f);
        public void StatusAdded(Fighter f, StatusType t) => OnStatusAdded?.Invoke(f, t);
        public void StatusEnded(Fighter f, StatusType t) => OnStatusEnded?.Invoke(f, t);
        public void Announce(string big, string small, Color c, float dur) => OnAnnounce?.Invoke(big, small, c, dur);
        public void DomainExpanded(Fighter f, DomainDef d) => OnDomainExpanded?.Invoke(f, d);
        public void ClashStart(List<Fighter> fs) => OnClashStart?.Invoke(fs);
        public void ClashEnd(Fighter winner) => OnClashEnd?.Invoke(winner);
        public void DomainEnded(Fighter f, DomainDef d) => OnDomainEnded?.Invoke(f, d);
        public void Custom(string s) => OnCustom?.Invoke(s);
    }
}
