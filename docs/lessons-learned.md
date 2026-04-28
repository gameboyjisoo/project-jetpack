# Lessons Learned

Append-only log of hard-won knowledge. Things that aren't obvious from reading the code. Each entry has a date, category, and the fix so we don't repeat mistakes.

---

## MCP / Editor Automation

### 2026-04-28 — CreateSpawnTiles.Execute() destroys painted tilemaps
**What happened**: Running `CreateSpawnTiles.Execute()` to add a new tile type deleted and recreated ALL existing tile .asset files and rebuilt the Interactables Palette from scratch. Every SpawnTile painted in the scene lost its reference.
**Root cause**: The script uses `AssetDatabase.DeleteAsset()` + `AssetDatabase.CreateAsset()` for every tile, generating new GUIDs. The palette is also deleted and rebuilt.
**Fix**: Never call `CreateSpawnTiles.Execute()` after initial setup. To add a new tile: (1) create only the new .asset file, (2) append it to the palette via `PrefabUtility.LoadPrefabContents` → `tilemap.SetTile` → `SaveAsPrefabAsset`. See `AddTileToPalette.cs` and `SetupNewGimmicks.cs` for the safe pattern.

### 2026-04-28 — MCP execute_script injects garbage into scene YAML
**What happened**: Using Coplay MCP's `execute_script` to create prefabs and GameObjects injected inline `Sprite` objects and duplicate data into `TestRoom.unity`. The scene file grew by 1400+ lines of garbage.
**Root cause**: Editor scripts that instantiate GameObjects or create sprites at runtime leave serialized artifacts in the active scene.
**Fix**: Create assets (prefabs, tiles, sprites) via editor scripts that do NOT touch the scene. Save prefabs to `Assets/Prefabs/` directly. Let the user place them manually via the Tile Palette or Inspector. Always check `git diff --stat` on the scene file after MCP operations. If hundreds of unexpected lines appear, `git checkout` the scene immediately.
**This has happened twice**: 2026-04-26 and 2026-04-28.

### 2026-04-21 — SerializedObject changes on existing components don't persist via MCP
**What happened**: Using MCP `execute_script` with `SerializedObject` to modify fields on existing scene components appeared to work but reverted on save.
**Fix**: Use the MCP `set_property` tool for changing serialized fields on existing components. Or edit the .asset/.prefab YAML directly for ScriptableObjects.

### 2026-04-26 — save_scene needs the full path
**What happened**: `save_scene` with `scene_name: "TestRoom"` saved to `Assets/TestRoom.unity` (wrong location) instead of `Assets/Scenes/TestRoom.unity`.
**Fix**: Always use `scene_name: "Assets/Scenes/TestRoom"` with the full path prefix.

### 2026-04-26 — Runtime-created sprites don't persist
**What happened**: `Sprite.Create(new Texture2D(...))` in editor scripts produces sprites that vanish on scene save/reload (the sprite field becomes empty).
**Fix**: Always use `AssetDatabase.LoadAssetAtPath<Sprite>("path.png")` for persistent sprite references. For objects that need to fill an area, use `SpriteDrawMode.Tiled` with `sr.size` matching the collider.

### 2026-04-21 — Ghost duplicate GameObjects from editor scripts
**What happened**: Editor scripts creating GameObjects with the same name as existing ones produced invisible duplicates.
**Fix**: Always check `FindObjectsByType` count after scene modifications via MCP. Use unique names or check-before-create logic.

---

## Unity Engine

### 2026-04-21 — CompositeCollider2D must use Polygons, not Outlines
**What happened**: Ground detection broke. Raycasts starting just below the floor surface hit the bottom edge of an Outlines collider (which has a downward-facing normal), failing the `normal.y > 0.7` check.
**Fix**: Always set `CompositeCollider2D.geometryType = CompositeColliderType2D.Polygons`. This makes the collider solid rather than edge-based.

### 2026-04-19 — SerializeField Inspector values override script defaults
**What happened**: Changed default values in code, but the Inspector retained old serialized values. Behavior didn't change.
**Fix**: After changing defaults in code, right-click the component in Inspector → Reset. Or manually update the Inspector value. Unity's serialization system preserves the last-saved Inspector value over script defaults.

### 2026-04-28 — Sprite tiling requires Full Rect mesh type
**What happened**: `SpriteDrawMode.Tiled` showed a warning and rendered incorrectly because the sprite's mesh type was Tight (default).
**Fix**: Set `spriteMeshType = SpriteMeshType.FullRect` in the TextureImporter settings via `ReadTextureSettings` / `SetTextureSettings`.

---

## Game Design

### 2026-04-28 — Every gimmick must interact with the fuel system
**Design rule**: No generic platformer gimmicks that ignore fuel. This is the core identity of the game (see `design/gdd/design-direction.md`).
**Examples of correct interaction**:
- Gravity switches change which surface is "ground" → changes where fuel recharges → changes fuel economy
- Closing platforms force timing → can't hover long (only ~1s fuel) → must time approach, not brute-force hover
- Crushers force dashes → dash costs ammo (recharges on ground) → ground access becomes valuable

### 2026-04-28 — Screen shake should be Celeste-minimal, not aggressive
**What happened**: Added screen shake on landing, dash, and death. User hated it — too much visual noise.
**Fix**: Reduced to death-only (0.2s, 0.12 magnitude). Added `enableScreenShake` toggle for accessibility. Celeste only shakes on death and specific boss/environmental events — never on landing.

### 2026-04-21 — Wall nudge conflicts with gravity=0 axiom
**What happened**: Wall nudge during horizontal jetpack boost gave the player upward velocity. With gravity=0, that velocity persisted forever, causing infinite wall climbing.
**Lesson**: Any mechanic that sets velocity during jetpack (when gravity=0) will persist indefinitely. Design accordingly. This led to removing wall nudge entirely.

### 2026-04-27 — Tutorial design: safe space → gate → danger
**Design rule (Chapter 1 only)**: Each tutorial concept follows: safe area to practice → comprehension gate → danger zone that tests the skill. Later chapters can break this pattern.
**Obstacle sizing rule**: Obstacle length forces specific mechanic combos. E.g., spike gap longer than jetpack range = must jetpack + dash.

---

## Process

### 2026-04-28 — Don't try to keep all docs in sync every session
**What happened**: 14 design docs flagged stale after a single feature session. Updating all of them would take longer than the feature work itself.
**Fix**: CLAUDE.md is the live source of truth (updated every session). GDDs and architecture docs are reference docs — update in batches before milestones or when designing features that depend on them. Session Log in CLAUDE.md gives 30-second context for the next session.

### 2026-04-28 — Prototype first, GDD second
**What happened**: User wanted to develop gimmick concepts. Instead of writing formal GDDs, we prototyped ClosingPlatform, Crusher, and GravitySwitch directly. The user playtested and gave immediate feedback (hated screen shake, wanted directional arrow sprites, etc.).
**Lesson**: For this project and this developer, build → test → document is faster and produces better results than document → build → test. Write the GDD from what felt right, not from theory.
