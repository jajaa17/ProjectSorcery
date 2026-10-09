## What does this change?

## Checklist
- [ ] Opened and played it in Unity 6 (no console errors)
- [ ] `dotnet run -c Release --project Tools/Simulator -- all` passes
- [ ] Gameplay code stays deterministic (no `UnityEngine.Random`, `Time.time` or dictionary-order iteration inside the simulation; use `Match.Rng` and fixed `dt`)
- [ ] New characters/techniques use original names
- [ ] Balance changes: before/after numbers from `-- balance 40` (if relevant)
