using UnityEngine;

namespace ProjectSorcery
{
    public enum DomainTheme { Void, Shrine, Caldera, Shore, Shadow, Swords, Court, Jackpot, Palms, Womb, Moon, Hometown, Garden, Spheres }

    public enum SureHit
    {
        Paralyze,   // infinite information: victims cannot act
        Slashes,    // endless cutting
        Burn,       // scorching heat + eruptions
        Swarm,      // shikigami swarm
        Sink,       // shadows swallow footing
        Swords,     // caster empowered, blades rain
        Verdict,    // trial: confiscation or execution
        Jackpot,    // gamble for immortality
        SoulTouch,  // one-touch soul reshaping
        Gravity,    // crushing gravity
        MoveCut,    // every movement cuts you
        SoulStrike, // soul-severing strikes
        Bloom,      // roots + energy drain
        TrueSphere  // perfect sphere strike
    }

    public sealed class DomainDef
    {
        public string Name;
        public DomainTheme Theme;
        public SureHit Effect;
        public Color Primary = Color.white, Secondary = Color.gray, Sky = Color.black, Ground = Color.black;
        public float Refinement = 5f;     // clash strength 1..10
        public float Duration = 9f;
        public bool Open;                 // barrierless (huge range, wins clashes from outside the barrier)
        public string[] Chant;
        public float Power = 1f;
        public string HandSign = "";
    }
}
