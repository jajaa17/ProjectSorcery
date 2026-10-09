using System;
using UnityEngine;

namespace ProjectSorcery
{
    /// <summary>Static description of a playable sorcerer / curse. Kits are created per fighter.</summary>
    public sealed class CharacterDef
    {
        public int Index;
        public string Id, Name, Title, Technique, Bio;
        public Era Era = Era.Anime;
        public AIStyle Style = AIStyle.Balanced;
        public Look Look;
        public Stance Stance = Stance.Brawler;

        public float Hp = 1000f, Ce = 100f, CeRegen = 6f;
        public float Speed = 1f, Jump = 1f, Power = 1f, Defense = 1f, Size = 1f, Weight = 1f;
        public float RctRate;                 // HP/s while channeling (RCT) or passively (AutoRCT, at 30%)
        public Passive Passives;
        public float BlackFlashBonus;         // extra window seconds
        public float PreferredRange = 1.6f;   // AI spacing
        public Func<Ability[]> Kit;           // [S1, S2, S3, ULT]
        public DomainDef Domain;              // shown in UI; the ULT holds the actual DomainAb
        public bool BossOnly;
        public int Tier = 2;                  // 1 = top tier, 3 = underdog (used for Survival waves / AI picks)
        public float DamageMul = 1f;          // per-fighter outgoing-damage tuning (see Balance.Tuning)
        public float ToughMul = 1f;           // per-fighter incoming-damage divisor (see Balance.Tuning)

        public bool Has(Passive p) => (Passives & p) != 0;
        public bool NoCE => Has(Passive.HeavenlyRestriction);
        public bool Bladed => Look.Weapon == Weapon.Katana || Look.Weapon == Weapon.Sword || Look.Weapon == Weapon.Cleaver ||
                              Look.Weapon == Weapon.Spear || Look.Weapon == Weapon.Polearm || Look.Weapon == Weapon.Axe || Look.Weapon == Weapon.Claws;
        public bool Reach => Look.Weapon == Weapon.Spear || Look.Weapon == Weapon.Polearm || Look.Weapon == Weapon.Cloud ||
                             Look.Weapon == Weapon.Staff || Look.Weapon == Weapon.Chain;
    }
}
