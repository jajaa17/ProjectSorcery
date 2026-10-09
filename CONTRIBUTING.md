# Contributing to Project Sorcery

Thanks for wanting to help. This is an open community project: bug fixes, new fighters, balance passes, VFX and audio improvements, UI polish, translations, docs and wild experiments in your own fork are all welcome.

## Ground rules

- **Original names only.** The game is *inspired by* Jujutsu Kaisen but never uses official character, technique or domain names, art or audio. Name things after what they do.
- **Code only, no imported assets.** Everything (sprites, textures, sounds, music, UI) is generated at runtime. If you really need an asset, open an issue first so we can discuss it.
- **Keep the simulation deterministic.** Online play is lockstep, so every machine must compute exactly the same match. See [Determinism](#determinism).
- **Be kind.** See the [Code of Conduct](CODE_OF_CONDUCT.md).

## Setup

1. Unity 6 (`6000.0.58f2` or any 6000.0.x) and the [.NET 8 SDK](https://dotnet.microsoft.com/download) for the simulator.
2. Fork, clone, open in Unity Hub. The project configures itself on first load (**Project Sorcery > Setup Project** re-runs it).
3. Open `Assets/ProjectSorcery/Scenes/Main.unity` and press Play.

## Workflow

1. Create a branch: `git checkout -b feature/my-change`.
2. Make your change and play it in the editor.
3. Run the simulator from the repo root:
   ```bash
   dotnet run -c Release --project Tools/Simulator -- all
   ```
   It must report `failures: 0`. CI runs the same thing on every pull request.
4. Commit the `.meta` files of any new scripts or folders (Unity creates them).
5. Open a pull request and fill in the template.

## Adding a fighter

Each faction lives in its own file under `Assets/ProjectSorcery/Scripts/Data/` (`Roster.Tokyo.cs`, `Roster.Culling.cs`, …). A fighter is one block:

```csharp
d = C("lantern", "Akari Tomoshibi", "Lantern Keeper", "Spirit Lanterns", Era.Manga, Stance.Caster,
      Hair.Bob, FaceMark.None, Weapon.None, "#ffb347", "#2a1a08");     // id, name, title, technique, era, movement style, look, colors
d.Hp = 1000; d.Ce = 120; d.CeRegen = 8f; d.Style = AIStyle.Zoner; d.PreferredRange = 5f;
d.Bio = "Lights lanterns that remember every blow that passed them.";
d.Kit = () => new Ability[]
{
    new ProjectileAb { Name = "Ember Lantern", Desc = "A slow lantern that bursts.", Cost = 14, Cooldown = 3f, Pose = FPose.Point, Color = H("#ffb347"),
        Proj = new ProjDef { Vis = ProjVis.Fire, Color = H("#ffb347"), Damage = 40, Speed = 14f, Size = 0.5f, Type = DamageType.Fire, ExplodeRadius = 1.2f, ExplodeDamage = 20f } },
    // S2, S3 ...
    // Ultimate: a big technique, or DomainUlt(SomeDomain, "description", "#hex") for a domain user
};
```

- **Building blocks** live in `Scripts/Abilities/Ability.cs`: `ProjectileAb`, `BeamAb`, `StrikeAb`, `BurstAb`, `SummonAb`, `ZoneAb`, `BuffAb`, `CounterAb`, `CommandAb`, `DomainAb` and `CustomAb` (any lambda you like).
- **Passives** (`Passive.RCT`, `Passive.Infinity`, `Passive.HeavenlyRestriction`, `Passive.Flight`, …) are in `Scripts/Fighter/FighterTypes.cs`. `Restricted(d)` sets up a heavenly-restriction body.
- **Domains** are `DomainDef`s built with `Dom(name, theme, sureHitEffect, colors, refinement, chant lines)`. Sure-hit effects are handled in `Scripts/Domain/DomainSystem.cs`.
- **AI** uses each ability's `Use` hint (`AIUse.Finisher`, `Zone`, `Escape`, `Heal`, …) plus `Style` and `PreferredRange`, so most fighters need no AI code.
- The character select, gallery, portraits, HUD and online lobby pick the new fighter up automatically.

Then balance it:

```bash
dotnet run -c Release --project Tools/Simulator -- balance 40
```

Aim for 40–60% in Hard-vs-Hard duels by tuning the kit itself first. The per-fighter multipliers in `Scripts/Core/Balance.cs` are a last resort; `-- tune 10 80` can regenerate them.

## Determinism

Code that runs inside `Match.SimTick` (fighters, abilities, projectiles, minions, domains, AI, modes) must give identical results on every machine:

- Use `M.Rng` (or `f.M.Rng`), **never** `UnityEngine.Random` or `System.Random`, for anything that affects gameplay.
- Use the fixed `dt` passed in, never `Time.deltaTime` / `Time.time`.
- Don't iterate `Dictionary`/`HashSet` in an order that affects gameplay.
- Visual-only code (VFX, camera, audio, UI) can use `UnityEngine.Random` freely. Keep it out of state that feeds back into the simulation.

`-- determinism` in the simulator runs the same match twice and compares checksums.

## Code style

- C# 9, 4-space indentation, braces on new lines for types and methods.
- Prefer small, direct code over abstraction layers; match what's around you.
- Avoid per-frame allocations in hot paths (VFX, rig, HUD). Pool instead.
- Comments explain *why*, not *what*.

## Reporting bugs and ideas

Use the issue templates: **Bug report**, **Balance feedback**, **Feature or character idea**. For online bugs, say whether the game reported a desync and attach `Player.log` if you can.

## License

By contributing you agree that your contributions are licensed under the [MIT License](LICENSE).
