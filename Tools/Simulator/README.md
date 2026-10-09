# Headless simulator

Runs Project Sorcery's real gameplay code (`Assets/ProjectSorcery/Scripts`) outside Unity, against small stand-ins for the parts of the Unity API it touches (`UnityStubs.cs`, `InputSystemStubs.cs`). Presentation (VFX, audio, UI) becomes a no-op; the deterministic 60 Hz simulation runs exactly as in the game, only much faster.

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). From the repo root:

```bash
dotnet run -c Release --project Tools/Simulator -- <mode>
```

| Mode | What it does |
|---|---|
| `all` | `determinism` + `smoke` + `modes` + `domains`. Exit code 1 on any failure. |
| `determinism` | Plays seeded 2v2 matches twice each and checks that the final state checksum and tick count match. |
| `smoke` | Every fighter plays a full Hard-vs-Hard match (no crashes, no NaNs, the match ends), then casts each of its four abilities in Training. |
| `modes` | 1v1v1, 2v2 and 2v1 matches, Survival wave progression, and 12 Calamity Raids with three Hard CPUs. |
| `domains` | Every domain user expands; 2-way and 3-way clashes resolve. |
| `balance N` | N random Hard-vs-Hard duels per fighter; prints win rates, damage and match length. |
| `anim DIR ids` | Plays a scripted move showcase (idle, walk, light string, heavy, charged heavy, launcher, sweep, dash strike, aerials, a skill) for each comma-separated fighter id against a dummy and records the solved skeleton every tick. Render it with `python3 preview.py DIR --sheet` (needs Pillow + NumPy). |
| `art DIR` | Exports every character portrait and domain interior the game paints at runtime as PNG files. |
| `tune ITER PERCHAR` | Iteratively adjusts each fighter's damage and toughness multipliers toward a 50% win rate and prints a `TABLE` to paste into `Balance.Tuning`. Set `TUNE_STEP=0.5` to take smaller steps when refining an existing table. |

If you use a new Unity API in gameplay code and the simulator stops compiling, add a minimal stand-in for it to `UnityStubs.cs`. Anything visual can be an empty method.
