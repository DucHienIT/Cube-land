# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Originally a starter template; now contains **Cube Blaster**, a first-party 3D voxel-demolition
shooter game (see below). Of the third-party asset packs, **Toony Colors Pro 2 is now used for all
3D game materials** (baked .mat assets, see Visual Asset Baker); DOTween/DOTweenPro and Layer Lab
GUI Pro are present but unused. 3D visuals are baked assets/prefabs; UI sprites + audio are still
procedural at runtime.

## Cube Blaster (the game)

A casual "ammo-shooter demolition" clone: numbered guns auto-fire darts at a procedural voxel
sculpture; the number on a gun is its ammo; you drag numbered blocks from the bottom bank into
empty gun slots to deploy shooters; demolish the whole sculpture to win. **A level is a SOLID
sculpture of 3000-8000 cubes, of which only the ~1000-2900 currently exposed ones exist as
GameObjects** (see the Density and Solid passes) — that scale is a hard constraint on everything
below, not a tuning preference. **Guns are color-locked**:
each bank block carries a voxel color, the deployed gun is tinted with it and only shoots/destroys
cubes of that color (`GameManager.RequestTarget(color)` filters by `cell.ColorIndex`); a gun retires and
frees its slot once no cube of its color remains, even with leftover ammo. Clones the *mechanic +
feel + mood* of the reference genre — no copied art/logo/layout.

- **Play scene**: `Assets/_Project/Scenes/Game.unity` (index 0 in the build list; SampleScene removed
  from build). Open it and press Play — zero manual setup required.
- **Namespace**: `CubeBlaster`. All first-party code lives under `Assets/_Project/Scripts/`.
- **No asmdef yet** — game code compiles into the predefined `Assembly-CSharp` (which auto-references
  Input System / UGUI / URP). Adding a dedicated asmdef later means adding those references explicitly.

### Code map (`Assets/_Project/Scripts/`)
**Architecture rules (SOLID refactor, 2026-07-29).** The codebase is comment-free by request — all
"why" lives in this file. MonoBehaviours are thin: they own serialized refs + the Unity lifecycle and
delegate the actual work to plain, injectable classes. Collaborators are reached through interfaces
(`IShooterContext`, `IDartContext`, `IBoardContext`, `IGameFlow`, `IUIHost`, `ITargetSelector`,
`IVoxelGrid`, `IVoxelVisibility`, `ILevelSource`, `IStarRule`, `ISaveRepository`, `IAudioService`,
`IFxService`, `IPointerGesture`) rather than concrete types, so `Gun`/`Dart`/
`BoardInput`/UI screens no longer name `GameManager`. Missing services resolve to Null-Object
implementations (`NullAudioService`, `NullFxService`, `AlwaysVisible`) instead of `!= null` guards at
every call site. **Serialized field names, MonoBehaviour class names and file names are load-bearing**
(scene/prefab YAML + `VisualAssetBaker.SetRef`) — never rename them; split logic into new plain
classes instead. *Moving* a script between folders is safe as long as its `.cs.meta` moves with it
(the GUID is what scene/prefab YAML points at); renaming or regenerating the .meta is what breaks
references.

**Folder layout** — top level is by *layer*, second level by *feature*. A file's folder is the
answer to "which part of the game is this?", so a new class goes next to the feature it serves, not
into a catch-all. There is no `Utils/` or `Systems/` junk drawer any more:

```
Scripts/
├── Config/                 config ScriptableObjects + the Cfg/Palette/Visuals facades
├── Core/                   pure logic, zero MonoBehaviour, unit-testable as-is
│   ├── Level/              LevelData, ILevelSource, LevelLibrary, Resource/ProceduralLevelSource, BankColorAssigner
│   ├── Voxel/              VoxelCell, IVoxelGrid, VoxelModel
│   ├── Targeting/          ITargetSelector, ExposedTargetSelector
│   └── Scoring/            IStarRule, TimeStarRule
├── Gameplay/               MonoBehaviours + the plain classes they compose
│   ├── Flow/               GameBootstrap, GameManager, GameState, GameplayContracts
│   ├── Sculpture/          SculptureView, SculptureLayout, VoxelCubeField, VoxelGroupTable, VoxelPopPool, VoxelStyle, Turntable, CameraVoxelVisibility
│   ├── Shooting/           Gun, GunSlot, RecoilSpring, DartField, DartArc
│   ├── Bank/               BankArea, BankBlock
│   ├── Destruction/        Shockwave (the one pooled FX object a destroy spawns)
│   ├── Input/              BoardInput + the IPointerGesture strategies
│   └── View/               CameraRig, CameraFraming, Backdrop, Billboard
├── Services/               cross-cutting, interface-first, Null-Object-backed
│   ├── Audio/  ├── Save/  └── Fx/
├── UI/                     UIController, IUIHost, IUIScreen, UIScreen
│   ├── Screens/  ├── Effects/  └── Toolkit/   (UIFactory, SpriteFactory)
├── Shared/                 tiny reusable primitives only: ColorTools, Ease, RendererTinter, ColliderRegistry, MeshInstanceBatcher
└── Editor/                 VisualAssetBaker
```

**Naming conventions (enforced in the 2026-07-30 pass — follow them for new code).**
- **One public type per file, file name == type name.** Only nested types share a file
  (`VisualLibrary.MaterialSet`). Interfaces, null-objects, settings structs and solvers each get
  their own file — that is why `GameplayContracts.cs`, `ConfigFacade.cs` and `UIEffects.cs` are gone.
- **No abbreviations in type or member names.** `Cfg`→`GameConfig.Active`, `Fx`→`FxService`,
  `Audio`→`AudioService` (also a real clash: `UnityEngine.Audio` is a namespace), `pos`→`Position`,
  `Init`→`Initialize`. `Fx`/`fx*` survives only as *domain vocabulary* inside names like
  `IFxService` / `FxService.Current` / `FxRing.mat`, never as a type name on its own.
- **Acronyms of 2 letters stay upper-case**: `IUIHost`, `IUIScreen`, `UIController` — not `IUiHost`.
- **Members that return a value are `Get*`/`Find*`/`Count*`**; plain verbs are reserved for methods
  that *do* something. `ColorOf`→`GetColor`, `AliveOfColor`→`CountAliveOfColor`,
  `FirstEmptySlot`→`FindFirstEmptySlot`, `FromCollider`→`FindByCollider`, `Stars(lv)`→`GetStars(lv)`.
- **Never reuse a Unity magic/base-member name for unrelated behaviour.** `VoxelModel.Destroy` is now
  `DestroyVoxel` (it is not `Object.Destroy`), `Turntable.Reset` is `Configure` (it is not the
  editor's `Reset` callback).
- **Locator members are `Active` (config assets) or `Current` (services)** — pick one and never mix
  `.Active`/`.Service`/`.Instance` for the same role.
- **UI host commands read as intents**: `GoToLevelSelect`, `GoToMainMenu`, `GoToNextLevel`,
  `RestartLevel`, `StartLevel` (things that fire SFX and drive flow) vs `UIController.Show*`
  (pure screen toggling). Do not name a command after the button that triggers it (`PlayPressed`).
- **Serialized fields are descriptive camelCase, never underscore-prefixed.** `_sfx`→`sfxSource`,
  `cam`→`sceneCamera`, `box`→`boxCollider`, `body`→`physicsBody` (Rigidbody) / `bodyPivot`
  (Gun's recoil Transform — the old shared name `body` meant two different types).
  **Renaming a serialized field requires `[FormerlySerializedAs("oldName")]`** plus updating the
  matching `VisualAssetBaker.SetRef("...")` string. The prefab/scene YAML on disk still carries the
  legacy keys (`box:`, `cam:`, `_sfx:`) and Unity maps them on load, so **do not delete those
  attributes** until the assets have been re-saved. Trap hit during the pass: a blanket rename also
  rewrote the old name *inside* the attribute string, which silently nulled every reference — and an
  already-open scene keeps its stale deserialization until you reload it, so verify by reflection
  after a scene reload, not just by compiling.

Everything stays in the single `CubeBlaster` namespace (Editor code in `CubeBlaster.EditorTools`) —
folders are for navigation, not for namespacing, so moving a file never forces a `using` churn.
`Gameplay/Flow/GameplayContracts.cs` deliberately holds all six interfaces together: they are the
public surface `GameManager` implements, and reading them in one place is how you see what the
orchestrator owes each feature.

- `Core/` — pure logic, no MonoBehaviour, no `UnityEngine` beyond math/`Color`. (`Core/Level`'s
  loading chain is the one deliberate exception: `LevelAsset` is a ScriptableObject and
  `LevelAssetSource` calls `Resources.Load`, because level content IS project assets.)
  `LevelData` (runtime shape: voxels[] + bank[] + bankColors[] — color slot per bank block,
  parallel to bank), `VoxelCell` (immutable grid cell: X/Y/Z/ColorIndex/Alive), `VoxelModel`
  (runtime sculpture state + per-color alive counts + `VoxelDestroyed`/`AllCleared` events;
  implements `IVoxelGrid`), `ExposedTargetSelector` (`ITargetSelector`: reservation + top-band
  reservoir pick, filtered by an injected `IVoxelVisibility`), `TimeStarRule` (`IStarRule`), and the
  level-loading chain: `LevelLibrary` (facade, `Use(...)`-swappable) → `LevelAssetSource`
  (`Resources/Levels/level_NNN.asset`) → `ProceduralLevelSource` (fallback so it runs with no
  content).
- `Config/` — `GameConfig` + `PaletteConfig` + `VisualLibrary` ScriptableObjects. Accessed via
  a static locator ON THE ASSET TYPE ITSELF — `GameConfig.Active`, `PaletteConfig.Active`,
  `VisualLibrary.Active`, each `Use(asset)`-overridable and all three backed by one generic
  `ConfigProvider<T>` (serialized override → `Resources/Config` → CreateInstance defaults). There is
  no separate `Cfg`/`Palette`/`Visuals` facade type any more. **Never hardcode tunables — add them
  here.**
  `VisualLibrary` is the hub of ALL baked visual assets and *only* that: per-palette voxel materials
  (`voxelSets[set].colors[slot]`) and their per-block jitter variants
  (`voxelSets[set].jitter[slot * ColorTools.JitterVariants + variant]`), their hit-flash shades
  (`voxelSets[set].flash[...]`) and their darts (`voxelSets[set].dart` = bullet,
  `.dartStreak` = tail), the two
  built-in meshes the instanced draws use (`voxelMesh` = cube, `dartMesh` = quad), fixed materials
  (slotPad/dartBullet), the Shockwave prefab. The colour *policy* it used to carry moved to
  `Shared/ColorTools` (`Jitter`/`PickJitterVariant` = quantized per-block variation, `LabelInk` =
  the one ink every ammo number uses, `ClampBrightness`) and `Shared/RendererTinter` (the MaterialPropertyBlock helper — props are
  tinted via MPB so shared .mat assets are never instanced/modified; the block is created lazily
  because Unity forbids native objects in MonoBehaviour field initializers. `Clear` puts a renderer
  back in the SRP batch and is what keeps the voxels there — see the Density pass).
- `Gameplay/` — `GameBootstrap` (applies config, `[DefaultExecutionOrder(-100)]`), `GameManager`
  (orchestrator only: level flow, slot/dart spawning, win/stars/save. It implements `IGameFlow` +
  `IShooterContext` + `IDartContext` + `IBoardContext` and delegates the two hard policies —
  **target reservation** so no two darts hit the same voxel and none are wasted, and
  **camera-exposure targeting** — to `ExposedTargetSelector` + `CameraVoxelVisibility`, which
  grid-marches the voxel→camera ray in sculpture-local space so guns only shoot voxels the player
  can see; covered voxels wait until uncovered/rotated into view),
  `SculptureView` (composition root for the sculpture: `SculptureLayout` computes local positions +
  the tilt-aware camera-fit bounds, `VoxelCubeField` owns the cubes and the neighbour punch ripple,
  `Turntable` owns the **turntable rotation** — **drag only, it never spins by itself** (see the
  No-auto-spin pass), `GetWorldPos` is live so darts still track voxels while the player turns
  them — and the view fires one `IFxService.PlayImpact`
  per destroy; the view itself only wires them and forwards
  `ISculptureSpace`/`ISpinnable`), `VoxelCubeField` + `MeshInstanceBatcher` + `VoxelGroupTable` +
  `VoxelPopPool` (**cubes are not GameObjects** — jitter/punch/flash/pop are arrays of state drawn
  through `Graphics.DrawMeshInstanced`, see the Instanced-voxel pass),
  `Gun` (auto-fires; visual subtree authored in the prefab, `Initialize` only tints via MPB; recoil lives
  in `RecoilSpring`), `GunSlot`,
  `DartField` (**darts are not GameObjects either** — one struct array, one instanced draw per
  colour, ticked from `GameManager.Update`; see the Instanced-dart pass. Each dart re-targets the
  voxel's live position every frame and flies a cubic-bezier arc that **leaves
  along the barrel axis** — `Gun` passes `barrelTip.forward` through `GameManager.SpawnDart` — then
  dives in along the validated camera ray, so it never pierces other cubes; `dartApproachOffset`
  tunes the arc. Do NOT put the first control point behind the muzzle: it used to be
  `_start + back*offset`, and under the 75° camera world −Z projects DOWN-screen, so every dart shot
  backwards out of frame and crossed back over its own cannon — user-visible as "đạn không bay từ
  nòng ra". Fixed 2026-07-21. The arc math lives in the pure `DartArc` struct — change control
  points there, not in `DartField`), `BankBlock` (draggable; only stack row 0 is playable — `Row` property, back rows dimmed;
  raycast hits resolve through the generic `ColliderRegistry<T>`),
  `BankArea` (reference-style vertical stacks: queued rows sit flush below the playable row via
  `bankRowSpacing`), `BoardInput` (new Input System; it is only a *router* — the two behaviours are
  `IPointerGesture` strategies, `BlockDragGesture` (drag a row-0 block = deploy, tap = first empty
  slot) and `TurntableGesture` (drag on the structure = spin it). First gesture whose `TryBegin`
  accepts the press wins, so a new interaction means a new gesture class, not an `if` in
  `BoardInput`), `CameraRig` (applies the framing solved by the pure `CameraFramingSolver`;
  steep near-top-down board view — `cameraOrthographic` off,
  `cameraPitch` 75° (user preference; pairs with `sculptureTilt` 55° so the figure's front stays
  visible), narrow `cameraFov` ~32° for near-isometric low distortion, fit via
  `cameraFitBottomY`/`cameraFitPadding`; orthographic still supported when the flag is on),
  `Billboard`, `Backdrop` (fits the baked gradient quad to the camera frustum for any mobile
  aspect), `Shockwave` (the pooled ring quad — see Destruction juice).

**Framing: `sculptureFillWidth` is the on-screen-size control, not `sculptureScale`** (2026-07-21).
`CameraRig.FitTo` solves for the distance at which the sculpture spans `sculptureFillWidth` of the
screen width (`distFill = extents.x / (fill * tanH)`), then takes `max(distFill, distFit)` so the
bank/slot rows can never be clipped. Two traps, both hit and fixed during that pass:
- Scaling the sculpture root (`sculptureScale`) is nearly self-cancelling on its own — the fit
  derives its distance from the sculpture's own bounds, so the camera backs off by almost exactly
  as much as the object grew. It is now left at 1.0 and kept only as a sculpture-vs-bank ratio knob.
- `halfH` used to be `extents.x + 3.0`, which grew the required half-width every time the sculpture
  grew — the same lockstep problem. It is now driven by the bank row (`max(4.3, extents.x*0.62)`).
`SculptureView.Bounds` is also tilt-aware now: the old version measured the *untilted* local box and
over-reported a tall figure's on-screen height by 1/cos(tilt) (~1.7x at 55°), which is what left a
band of dead space above the object. Measured result: on a compact shape (level 12) the sculpture
went from 35.9% → 64.1% of screen width. Tall thin shapes (level 1) gain less (+15% height) because
`distFit` correctly caps them — that is the guard working, not a bug.
- `UI/` — `UIController` (implements `IUIHost`, holds the screens as `IUIScreen` and hides them
  through one list, so adding a screen touches no `HideAll` branch) + plain-class screens
  (`MainMenu`/`LevelSelect`/`Hud`/`Win`/`Settings`) that depend on `IUIHost`/`IGameFlow`, never on
  `GameManager`. Code-first uGUI via `UIFactory` (legacy `Text` + LegacyRuntime.ttf, no TMP).
  Juice components live one-per-file: `UIPressEffect`, `UIMotion`, `UIPopIn` (the old combined
  `UIEffects.cs` is gone), plus `UIBob` idle pulse.
- `Services/` — `Audio/`: `AudioManager` (MonoBehaviour playback only; it implements `IAudioService` and
  registers itself with the `Audio` facade in `Awake`. Synthesis moved to `ProceduralClipFactory` →
  `GameClips`). **Call audio as `AudioService.Current.PlayX()`** — the facade falls back to
  `NullAudioService`, so the old `if (AudioManager.Instance != null)` guards are gone; do not
  reintroduce them. `Save/`: `SaveSystem` is a static facade over a swappable `ISaveRepository`
  (`PlayerPrefsSaveRepository`, `cubeblaster.` prefix: unlocked level, coins, per-level stars, mute).
  `Fx/`: `ShockwaveFxService` (`IFxService` — pre-instantiates a fixed ring of `Shockwave`
  quads from `VisualLibrary` on first use and cycles them oldest-first; never allocates at
  runtime, and a missing prefab simply means no FX) behind the `FxService.Current` facade, which
  is settable so a test/scene can swap in `NullFxService`.
- `UI/Toolkit/` — `SpriteFactory` (SDF rounded-rect/circle/star UI sprites) + `UIFactory`.
- `Shared/` — small reusable primitives ONLY, nothing game-specific: `ColorTools`, `RendererTinter`,
  `Ease` (the punch, pop-in and settle curves all come from `Ease`), the generic
  `ColliderRegistry<T>` and `MeshInstanceBatcher` (matrices bucketed per material, one
  `Graphics.DrawMeshInstanced` per bucket — it lives here because BOTH the sculpture and the dart
  swarm submit through it; it knows nothing about either). If a class knows about guns, voxels or
  levels it does not belong here.
  (The old `Look`/`WorldLabel` runtime-procedural material/mesh/label builders were REMOVED — all
  of that is baked into assets by the Visual Asset Baker, see below.)

### Cube / lighting look
Art direction follows `docs/game_graphic_style_analysis.md` (stylized plastic toy voxels, hot
red/orange vs green contrast on navy, layered destruction), refined through several user art
reviews toward "premium plastic toy, not voxel prototype". **Every surface material is Toony
Colors Pro 2's Hybrid Shader** — there is no other surface shader in the game. Sanctioned
exceptions (not surfaces): dart bullet (URP/Unlit, per baker), dart trail + shockwave ring
(Sprites/Default — vertex-color VFX), TMP text.

**Everything visual is a baked asset, nothing procedural at runtime.** `Tools ▸ Cube Blaster ▸
Bake Visual Assets` (`Scripts/Editor/VisualAssetBaker.cs`) generates: the rounded-cube mesh
(`Art/Meshes`), the EdgeSheen/FX textures (`Art/Textures`), all TCP2 materials seeded from
GameConfig/PaletteConfig (`Art/Materials`, voxels in `Materials/Voxels/Voxel_S{set}_C{slot}.mat`
plus their jitter variants in `Materials/Voxels/Jitter/`),
the Shockwave ring prefab (`Prefabs/Fx`), the visual subtrees +
serialized refs of the gameplay prefabs, the PostFX VolumeProfile (`Art/PostFX.asset`), the scene
"PostFX" Volume, and the `VisualLibrary.asset` wiring. Default bake keeps hand-edited asset values
(creates only what's missing; prefab subtrees are rebuilt); "Force Rebake All" resets everything
to procedural defaults. **Exception: `RoundedCube.asset` is now rebuilt on EVERY bake** (updated in
place so its GUID survives) — it is pure procedural geometry with nothing hand-editable, and the
old "skip if it exists" path meant `voxelCornerRadius`/`voxelRoundSegments` silently did nothing
unless you force-rebaked. **Two more always-applied exceptions (2026-07-31):** `NumberLabel.mat`
(see the Number legibility pass) and **texture IMPORTER settings** — `BakeTexture` used to return
early when the .png existed, so `mipmaps`/`wrapMode`/`alphaIsTransparency` were only ever honoured
on the frame a texture was first created. Flipping the FX textures to mipmapped did nothing until
this was split: pixels are still only regenerated when missing or forced, but importer settings are
now compared and re-applied every bake. The `BgGradient` special case just above it in `Run()` was
a manual workaround for exactly this bug.

**Inside-out mesh bug (fixed 2026-07-20).** `BuildRoundedCube` wound its triangles `i0,i2,i1`,
which by Unity's rule (outward normal = `Cross(v1-v0, v2-v0)`, see the quad example in the Mesh
docs) makes every face front-facing *inward*. With `Cull Back` the near wall was culled and you saw
the inside of the far walls, so a cube looked hollow/scooped when inspected up close. It hid for a
long time because the normals are written explicitly outward (`norms.Add(nn)`), so the interior
walls still lit as if they faced out — at gameplay scale the stack read as solid and only a
single-cube close-up exposed it. Winding is now `i0,i1,i2` / `i2,i1,i3`. If cubes ever look scooped
again, check the winding before touching lighting. **Hand-tune look in the .mat/.asset files; re-run the baker after changing
the seed values in GameConfig/PaletteConfig.**

Toon seed knobs in GameConfig: `toonHighlight`/`toonShadow` (_HColor/_SColor — shadow is a
navy-purple hue `(0.50,0.44,0.68)`, never black; highlight sub-1 `(0.98,0.96,0.93)` so whites
don't burn), `toonRampThreshold` 0.38 / `toonRampSmoothing` 0.50 (soft wrap, no hard cel bands),
stylized specular (TCP2_SPECULAR_STYLIZED, `toonSpecSize` 0.38 / `toonSpecSmoothing` 0.78, spec
color 0.72/0.70/0.66) — wide, soft, never burning-white toy-gloss. With the toon ramp, key-light
intensity must stay ~1.0 (higher multiplies straight into the ramp and blows red into orange —
1.18 already read as "emission/neon" in review r10). All TCP2 mats once carried a latent yellow
`_EmissionColor (1,1,0)` (keyword off) — zeroed in r10; keep emission black. **TCP2-URP14
gotcha:** the pack's embedded URP support predates `GetNormalizedScreenSpaceUV`, so the
`_SCREEN_SPACE_OCCLUSION` shader variant fails to compile and everything renders flat cyan — SSAO
MUST stay in **After Opaque** mode (set on the renderer feature) so that keyword never turns on.

**Voxels render on Unity's built-in 12-triangle cube; the RoundedCube mesh now only dresses the
props (gun slot rims, bank blocks) — see the Density pass.** Everything in this paragraph about
the bevel therefore applies to the props, and the gap/jitter parts still apply to the voxels.
**Current values: `voxelCornerRadius` 0.16, `voxelGap` 0.018** (halved with `voxelSize`, same 7%
ratio), `voxelRoundSegments` 3. The gap deliberately
reverses the earlier "gap 0 / radius 0.18" setting: at 0.18 the chamfer ate ~36% of the face and the
blocks read as jelly/gumdrops rather than toy bricks, and with a tight bevel the groove alone is too
shallow for SSAO to find — so a small real gap is needed to get the briefed "clear dark gaps between
blocks". Stay in the 0.08–0.12 / 0.03–0.045 bands. History: gap 0.07 read as an airy/sparse
silhouette; 0.028 made the seam a *lit bevel*, which read as panelling (see the solid-cube pass
below); 0 removed the cavity entirely. Same-color cubes get a *light* quantized
per-block jitter (`ColorTools.Jitter`, hue 0.02 / value 0.04 — lighting, not random color,
carries the shading) served as **baked material variants, never a MaterialPropertyBlock** (see the
Density pass), plus a ±2% size wobble
(`voxelScaleJitter`); `VoxelCubeField.GetColor` keeps the exact base color for gameplay/FX tints. Both the
built-in cube and RoundedCube have per-face 0..1 UVs; voxel materials sample the EdgeSheen texture — a plastic tile with a soft
off-center gaussian sheen blob (plate 0.90 → 1.0 at the blob, sigma 0.34, rim 0.90, band 0.08)
that fakes a broad premium-gloss highlight on every face. Deliberately subtle: any strong dark rim
reads as a black pixel-grid, which the user rejected twice. **Lighting philosophy (final, per reference): "everything is lit — shadow only shapes form."**
Every face sits on a HIGH ambient floor so nothing sinks into dark; form comes from the strong
near-top-down key + soft AO, NOT from darkening sides. Lighting review r8 pulled the whole floor
down a notch ("bản cũ quá sáng cục bộ, mất chiều sâu — trắng/đỏ như tự phát sáng"): Ambient is
Trilight (sky 0.92/0.88/0.84, equator 0.66/0.68/0.82, ground 0.50/0.46/0.68 — ground keeps a
navy-purple tint so what shading remains is colored; the same values are baked into the scene's
edit-mode RenderSettings since r9 so editor preview matches runtime). Key light: 58°/+20° (steep
top-down, biased slightly front-LEFT per r8), intensity **1.0 for the TCP2 toon ramp** (r10: 1.18
read as emission/neon; 2.8 was right for URP/Lit but blows the ramp out), soft shadows at only
0.3 strength; cool **wrap fill from the camera side** (14°/8°, 0.32) lifts front faces. History:
a low-ambient/strong-contrast pass read as "tối, mặt bên chìm", an even-flat pass read as
"phẳng, thiếu key", and an over-lifted pass (r9: _SColor 0.62, ramp 0.34, sat 16) read as
"emission/neon" — the balance that works is bright-everywhere + a clearly dominant top light,
with `_SColor (0.48,0.42,0.68)` deep enough that top/front/side clearly separate. White mats
(`*_C4`) hand-hold their own values (see below) and are NOT reseeded by a default bake.
**Reference-match pass (2026-07-20)** pulled the ambient floor down one more notch and deepened the
shading chain so the object stops reading washed-out against the navy: equator 0.78→0.60,
ground 0.60→0.50, `toonRampThreshold` 0.38→0.44 (side faces now fall partly into the shadow tint
instead of staying fully lit), `toonSpecColor` →`(0.80,0.77,0.72)`. Shadowed faces still hold
~60-75% of base brightness — the "everything is lit" rule survives; it is the *floor* that moved.

**Opaque-plastic pass (2026-07-20, follow-up).** User: *"các khối đang bị cảm giác trong suốt, tôi
muốn nó đục và tạo cảm giác nhựa."* The blocks were never actually transparent — queue 2000, ZWrite
on, alpha 1, `_UseRim`/`_UseReflections` both 0. Three shading cues were faking translucency, all
now fixed:
1. **Over-saturation was the main cause.** `postSaturation` 20 drove the green voxels to
   `RGB(0.000, 0.933, 0.482)` — R pinned at exactly 0 on **99.4%** of green pixels. A channel at 0
   is an absolutely-pure hue, which reads as backlit gel; opaque plastic always retains some light
   in all three channels from ambient bounce. Note the *old shipped* value of 12 already clipped
   43%, so this predated the reference pass. Now **saturation 8 / contrast 5 → 9.6% clipped,
   avg min-channel 0.065.** Treat ~10 as a hard ceiling, not a taste knob.
2. **Hue-shifting shadow.** `_SColor` was purple `(0.48,0.42,0.68)`, which multiplied green into
   teal (G and B nearly equal). Now near-neutral `(0.52,0.49,0.56)`: darkens without re-hueing.
3. **Too-soft ramp + broad specular** = subsurface/wax read. `toonRampSmoothing` 0.50→0.30 (crisper
   terminator, still not a hard cel band) and specular tightened to size 0.25 / smoothing 0.45.

**Solid-cube pass (2026-07-20, follow-up).** User: *"cảm giác nó mang lại là nó không phải 1 khối
cube mà chỉ có 3 mặt phẳng ghép lại."* Two causes, both fixed:
1. **Blocks were separated by a BRIGHT seam, not a dark cavity.** At `voxelGap` 0.028 the blocks
   effectively touched, so the only thing between two cubes was the lit top bevel of the one below —
   a hot orange stripe. A bright separator reads as panelling; a dark cavity is what makes each
   block read as its own solid. Gap is now **0.07** (a real seam SSAO can find) and SSAO went to
   intensity 1.6 / radius 0.30. Trade-off to know: bigger gap = more airy silhouette, so this pulls
   slightly against "chunky mass" — 0.07 is the balance point, lower it if the figure reads sparse.
2. **Faces had no in-face gradient.** A toon ramp gives a flat face a constant normal → exactly one
   flat tone, so the only thing that can vary across a face is the EdgeSheen texture — and its plate
   range was 0.90→1.0, a 10% falloff far too weak to fake light dropping off. `plateLow` is now
   **0.74** (26% range, `VisualAssetBaker.EdgePixel`), plus `toonRampSmoothing` 0.30→0.38 so the
   bevel shows a rounded gradient instead of snapping to the face tone. Re-run the baker (or
   regenerate `Art/Textures/EdgeSheen.png`) after changing those constants.

**Blowout pass (2026-07-20, after the winding fix).** Once the mesh faced outward, the flat faces
caught the key light directly and lit red faces clipped to cream — `toonSpecColor` was 0.80, and
because voxel faces are FLAT a toon specular lobe covers an *entire face at once* rather than making
a small highlight, so the term is added uniformly across the whole surface: lit red became
~(1.57,0.94,0.84). Spec is now **(0.18,0.175,0.165)** (whites `_C4` hand-hold (0.10,0.105,0.12)),
taking warm pixels blown to white from **7.1% → 0.4%** while saturation stayed at 0.891 vs 0.900
with specular fully off. **On flat-faced voxels the gloss read must come from the EdgeSheen
gradient, not from specular** — turning spec up to get "more plastic" just washes whole faces out.

**Cannon blowout (2026-07-20, after the spec fix).** The spec fix above only touched
`Materials/Voxels` — `GunPart.mat`/`GunHole.mat`/`SlotPad.mat` were left on the OLD seeds
(spec 0.72, purple `_SColor` 0.54/0.47/0.72, ramp 0.38/0.5) and blew out. **When retuning shading,
sync the non-voxel TCP2 materials too**, or the guns/pads silently drift a whole art pass behind.
They now carry the same values as the voxels.

The white gun then still read as a featureless blob, and the fix was NOT where it looked: gun tints
are applied through a `MaterialPropertyBlock` (`Gun.ApplyTint` → `RendererTinter`), and an MPB
`SetColor` **replaces** `_BaseColor` instead of multiplying it — so lowering `GunPart.mat`'s base
color does nothing for the tinted parts (measured: dropping it 0.90→0.76 moved clipping only
89%→86%). The white slot arrives at ~0.97 and the *lighter* dome/tube lerps push it to pure white.
Fixed with `GameConfig.gunTintMaxValue` (0.80), a hue-preserving cap (`ColorTools.ClampBrightness`) applied in `Gun.ApplyTint`:
white-gun clipping 86% → 44%, red/green guns untouched (their max channel is already under the cap).

**Regression check for "does it read as opaque plastic":** screenshot a level in Play mode and
measure — do NOT judge by eye, and note the preview image's gamma is unreliable (see the memory on
Play-mode verification). Two metrics: warm/lit pixels desaturated toward white (`r>0.80 && g>0.72 &&
b>0.62`) should stay **under ~1%**, and avg min-channel on the sculpture should sit **around 0.2+**.
Current healthy reference (post winding fix): blown 0.0%, avg min-channel 0.254 on a green level.
Caution: the older "keep green `r<0.02` clipping under 10%" threshold was calibrated on the
INSIDE-OUT render and no longer applies — correct geometry reads ~22% on that metric while looking
right, so trust the blown/min-channel pair instead. Beware the trap in both directions: raising
saturation to fix "dull" causes the gel look, and raising specular to fix "not glossy" causes the
blowout.
**Reference-polish pass (2026-07-21).** Brief: bigger hero object, less neon green, no white
blowout, AO as cavity not outline, cubic bevel, richer background, matching slots/number blocks,
juicier feedback. What it changed and *why* (all verified by pixel measurement, not by eye):
1. **The neon green was a palette problem, not a grade problem.** The old green
   `(0.196, 0.824, 0.447)` had R = 0.196 — low enough that the saturation grade drove R to ~0 on
   **31.9%** of green pixels, and a channel pinned at 0 is what reads as backlit gel. The green is
   now a warmer, yellow-leaning `(0.315, 0.775, 0.365)`; orange lost its R = 1.0 pin and yellow's
   B went 0.094 → 0.175 for the same reason. Measured green clipping: **31.9% → 0.0%**.
2. **Consequently `postSaturation` 8 → 9 is now SAFE.** The old "≤10 ceiling" note was really a
   proxy for "no palette channel may sit near zero". With the palette fixed, richness can come back
   from the grade. Re-check the clip metric if the palette is ever re-hued.
3. **White slot**: base `(0.855,0.875,0.912)`, and the white mats hand-hold `_HColor (0.80,0.812,
   0.838)`, `_RampThreshold 0.52`, `_SpecularColor (0.07,0.075,0.09)`. `ambientSky` also dropped to
   `(0.86,0.84,0.81)`. Measured blowout on a red+green+white level: **3.99% → 1.30%**.
4. **AO was an outline because intensity 1.6 / radius 0.30 was tracing silhouettes.** Now
   intensity 0.95, radius 0.20, DirectLightingStrength 0.25 (lit faces keep their brightness).
   `Tools ▸ Cube Blaster ▸ Sync SSAO From Config` pushes the GameConfig `ao*` seeds onto the
   renderer asset — they are NOT read by URP directly. AfterOpaque is force-set to 1 (TCP2 gotcha).
5. **Background** is no longer flat (measured luminance std-dev 0.0015 ≈ perfectly flat). A baked
   `BgGradient.png` + unlit `Backdrop.mat` on a camera-child quad (`Backdrop.cs` fits it to the
   frustum) gives a ~9% centre lift; std-dev is now ~0.0070–0.0087.
6. **Shooter slots must be a FOUR-BAR FRAME, not a solid rim.** The camera looks down at ~75°, so a
   solid rim's top face completely hides anything inset beneath it and the slot goes straight back
   to reading as one flat tile — this was tried and measured before the frame version.
   `IdleRim` is also far darker than it looks like it should be `(0.085,0.125,0.215)`, because the
   toon highlight + ambient floor lift it ~2.7x on screen.
7. **`SlotPad.mat` / `GunHole.mat` hit the flat-face specular trap** (same as the voxels: a toon
   spec lobe covers a whole flat face at once and is added uniformly). Their spec is now ~0.03 and
   their `_HColor` is cool, or the sockets drift to warm grey `(0.400,0.369,0.345)`.
8. **Number text contrast**: `ColorTools.LabelInk` was a luminance switch here (navy ink above
   Rec.601 0.62, warm white below) because the white ammo block rendered white-on-white and was
   literally unreadable. Superseded by the Outlined-number pass below — it is now a constant.

**Number legibility pass (2026-07-31).** User: *"hình ảnh của các text số trên bank đang không ổn."*
The bank digits were fat, soft and low-contrast. Four causes, all fixed in
`VisualAssetBaker` + `BankBlock`:
1. **The glyphs were bloated by their own preset.** `NumberLabel.mat` carried `_FaceDilate` 0.08 with
   a 0.22 outline — at bank scale that is most of a stroke width, so the counters in 8/9/0 close up
   and two-digit numbers merge. Now dilate 0.02 / outline 0.12, plus a soft `UNDERLAY_ON` drop
   shadow (offset ±0.55, softness 0.25, navy at 55% alpha) which separates the number from the block
   without thickening it.
2. **A fixed font size cannot serve both "7" and "21".** The label is now auto-sized against a
   `0.72 × 0.72` rect (`BankLabelFit`, block face is 0.92) with max = `BankLabelSize` 0.105 and
   min 55% of that, so single digits stay hero-sized and two digits shrink to fit instead of
   spilling over the bevel. `AddLabel`'s new `fitSize` argument is opt-in — the gun label passes
   nothing and keeps its hand-tuned 0.066.
3. **`LabelInkDim` was actively harmful and is deleted.** Queued rows lerped the ink 35% toward the
   block, and a queued *white* block lands at luminance ~0.62 — exactly the `LabelInk` switch point,
   so it picked warm-white ink and then faded it toward a near-white background. Measured on screen:
   unreadable. Queued rows now use full `LabelInk(shown)`; the 42%-dimmed cube alone carries the
   "not playable" signal, which is enough.
4. **`BakeLabelMaterial` no longer early-returns when the .mat exists.** Contrast/legibility is
   derived policy, not hand-tuned art — under the old skip, none of the above would land without a
   Force Rebake All (which resets every other material to defaults). Same reasoning as
   `RoundedCube.asset`.

**Dart-geometry pass (2026-07-31).** User: *"khi đặt các bank lên slot để bắn các khối cube thì số
lượng tris tăng lên, check vì sao?"* Measured on level 30: **72k triangles at rest, 173k mid-barrage**
(2.4x). Where the extra 101k came from, and what it says about where to look next. **The dart half
of this is history — the Instanced-dart pass below removed the dart GameObject entirely, and with
it the bullet renderer, the trail and `DartDot.png`; the gun-mesh findings still stand:**
- **~69k of it was the dart bullets.** The bullet was Unity's built-in `New-Sphere.fbx` at **768
  triangles**, and four guns at `gunFireInterval` 0.03 keep **~90 darts in the air** (133/s × ~0.65s
  of flight). That is **2.6x the entire 2179-cube sculpture** (26k tris) for balls that render a
  handful of pixels wide — and the material is `URP/Unlit`, so the geometry bought no shading
  whatsoever. Now a **billboarded quad: 2 triangles**, `Sprites/Default` + a baked `DartDot.png`
  (solid core, soft rim — same trick the shockwave ring already used). 69,120 → 180 tris.
  `Dart.Initialize` still aims the ROOT along the muzzle so the trail streaks correctly; the
  `Billboard` sits on the bullet child so the dot faces the camera independently. Scale went
  0.22 → 0.26 to compensate for the dot's soft rim.
- **~16k was the four guns themselves**, spawned by the deploy. Per gun the `Nub` — a ~0.2-unit tab
  on the back that renders about 20px — was using `GunBody.asset` at **1452 triangles**, a third of
  the whole cannon, for a bevel that is sub-pixel at that size. Now `roundedCube`: gun 4046 → 3176.
  **The BODY meanwhile uses `roundedCube` while the mesh named `gunBody` went to the nub** — the
  reverse of what the names suggest. Left alone deliberately: `gunBodyRadius` 0.26 vs
  `voxelCornerRadius` 0.16 means swapping it changes the cannon's silhouette, and the current one
  has been through several user art reviews.
- Measured after: **103k tris at 100 darts**, against 173k at 90 before.
- **What did NOT cause it**, checked and ruled out: the sculpture barely moves (2071 → 2179
  renderers as craters expose buried cubes), and the shockwave rings are 2-triangle quads.
- Remaining cost is **draw calls, not triangles**: each dart is a bullet renderer with a
  MaterialPropertyBlock (so, outside the SRP Batcher) plus a TrailRenderer, i.e. ~200 extra draws at
  100 darts — that is what takes setPass from 61 to ~200. Pool the darts and tint them with baked
  materials if that ever needs to come down.

**WebGL draw-call pass (2026-07-31).** Target is a **WebGL build**, which changes what the desktop
numbers mean: WebGL is single-threaded (the render thread's work folds into main) and each draw
crosses the JS/WASM → browser-GL boundary, so per-draw cost is several times native. Triangles were
never the issue — GPU sat at ~2ms with 70k tris. **Draw calls are.** Measured on level 30, 2188 voxel
renderers:
- **Voxels no longer cast shadows** (`GameConfig.voxelCastShadows` — it was baked into
  VoxelCube.prefab at the time; the Instanced-voxel pass below made it a per-frame draw argument, so
  it no longer needs a re-bake). **batches 4215 → 2347, tris 86.6k → 65.2k, shadow casters 1106 → 63**
  (only the guns/slots/bank still cast). The sculpture loses its own self-shadowing — undersides of
  overhangs go flatter — which is why an earlier pass kept it; SSAO covers enough of the cavity read
  that at this cube size the screenshots are near-identical. Set it back to true for a
  desktop/TV-only build.
- **Probes are off on voxels too.** Not cosmetic: per-renderer probe data is one of the things that
  stops Unity merging renderers into instanced draws. The scene has no baked probes (ambient is
  Trilight, zero LightProbeGroups — checked), so nothing is lost.
- **GPU instancing works now, and it is the reason the jitter-material change matters more than it
  first appeared.** With the SRP Batcher disabled (the situation on any platform that lacks it),
  instancing collapses **2188 cubes into 87 draw calls**. That only works because voxels carry no
  MaterialPropertyBlock at rest. All 72 voxel materials now have `enableInstancing`.
- **But the punch-flash MPB shreds it.** Mid-barrage on the instancing path, setPass went **59 → 299**:
  every flashing cube takes a property block, drops out of the instanced batch, and costs a full
  material bind. On the SRP Batcher path the same scene is setPass 57. **This was called out as the
  next lever, and the Instanced-voxel pass below took it** — the flash is baked shades now.
- **Do not trust frame-time numbers taken through the MCP harness.** The auto-deploy driver used for
  these runs calls `FindObjectsOfType` every editor tick, which dominates the main thread and made
  timings swing 8-24ms independently of the rendering change. UnityStats counts (batches, setPass,
  tris) are unaffected and are what the numbers above rest on. Also note the Statistics window's
  "CPU: main" **includes the wait for `targetFrameRate`** — at 60 fps it reads ~15.7ms no matter how
  little work the frame does; uncap before reading it.

**Instanced-voxel pass (2026-07-31) — voxels have no GameObjects at all.** Offered as one of five
draw-call options; the user picked this one. The sculpture is ~93% of the frame's draw calls and it
was one renderer per exposed cube — this is the only change that touches that number. **A cube is now
an entry in a struct array**: `VoxelCubeField` holds position/scale/group/punch state,
`SculptureView.LateUpdate` ticks it, and `VoxelInstanceBatcher` submits the matrices grouped by
material through `Graphics.DrawMeshInstanced`. Deleted with it: the `VoxelCube` MonoBehaviour,
`VoxelCube.prefab`, `SculptureView.voxelPrefab`, and every runtime MaterialPropertyBlock on a voxel.
- **Grouping IS the batching, so per-cube colour is not a tuning choice — it is impossible.** Two
  cubes share a draw call exactly when they share a material. TCP2 Hybrid carries
  `#pragma multi_compile_instancing` (so transforms instance fine) but declares `_BaseColor` in the
  `UnityPerMaterial` CBUFFER, **not** in an instancing buffer — checked in the shader source — so a
  per-instance colour array would be silently ignored. Every shade a cube can take therefore has to
  be a real material. That is why the jitter was already baked (Density pass) and why the flash is
  now baked too, in `Art/Materials/Voxels/Flash/Voxel_S{set}_C{slot}_F{level}.mat`.
- **The flash is quantised to `ColorTools.FlashLevels` (3) shades, 0.3 apart.** Both callers land
  almost exactly on a step — `hitFlashIntensity` 0.35 → level 1 (0.30), `popFlash` 0.85 → level 3
  (0.90) — so the look is unchanged. It deliberately stops at 0.90 and never reaches pure white; see
  the blowout passes. Adding any new per-cube visual state means adding a material group, never a
  property block.
- **A destroy now allocates nothing whatsoever.** `VoxelPopPool` is a fixed ring of
  `GameConfig.popMaxActive` (96) animation states — the pop plays in world space so it stops
  following the turntable, exactly as the old detached GameObject did. Before this, four guns at
  `gunFireInterval` 0.03 meant well over a hundred `Destroy` calls a second feeding the GC, which
  matters far more on WebGL than natively.
- **This fixed a latent bug in the pop, so hits read punchier than before.** `VoxelCube.Pop` did
  `SetParent(null, worldPositionStays: true)`, which rewrites `localScale` to preserve world size —
  0.25 local under a root at `sculptureScale` 1.55 becomes 0.3875. But `PopRoutine` then assigned
  `_baseScale * swell`, and `_baseScale` was the *pre-detach local* 0.25. So every cube snapped to
  64% the instant it was hit and "swelled" from there to 0.83× its original size — the swell that is
  supposed to BE the impact was actually a shrink. The pool multiplies by `lossyScale` explicitly,
  so the swell now reads as a swell. If hits feel too strong, `popSwell` is the knob, not this.
- **`PunchAround` walks the grid neighbourhood, not the voxel array.** It used to scan all
  `grid.Count` entries per destroy — on a 7815-cube level at ~133 destroys/s that is a million array
  iterations a second, which would have eaten most of what this pass bought back. The radius is
  stated in CELLS, so the integer box of `ceil(hitPunchRadius)` around the epicentre provably
  contains every candidate: 125 dictionary lookups instead of 7815 compares, same result.
- **Traps, in the order they will bite:**
  - `Graphics.DrawMeshInstanced` draws for **one frame only**. If `SculptureView.LateUpdate` stops
    running, the sculpture is simply invisible with no error. (The No-auto-spin pass had deleted
    `SculptureView.Update` because nothing needed a per-frame tick — this is why there is one again.)
  - **1023 instances per call** is a hard Unity limit. `MeshInstanceBatcher` slices past it through
    a scratch buffer; a single palette slot rarely gets there because the jitter splits it three ways.
  - **This change needs a re-bake.** The flash materials and `VisualLibrary.voxelMesh` do not exist
    until `Tools ▸ Cube Blaster ▸ Bake Visual Assets` runs. Both paths fall back (unflashed material,
    built-in cube), so the symptom is "hits stopped flashing", not a crash — which is exactly the
    kind of thing that ships unnoticed.
  - `voxelCastShadows` is now a per-frame draw argument, so flipping it no longer needs a bake.
- **Measured in Play mode on 2026-07-31** (level 20, 4543 cubes, 1445 of them exposed and drawn):
  **98 batches / 44 setPass at rest, 174 / 78 during a four-gun barrage**, 1475 renderers collapsed
  into instanced draws. `UnityEditor.UnityStats` is what these come from — see the verification trap
  in the Instanced-dart pass before trusting a screenshot instead.

**Dart-streak pass (2026-07-31, follow-up).** User: *"trail của viên đạn đang hơi đậm và hơi dài."*
It was a regression from the density pass: **the streak's length is `dartTrailTime * dartSpeed`, and
raising `dartSpeed` 22 → 28 lengthened it without anyone touching the time.** At 0.26s that is 7.3
world units against a sculpture only ~8 wide — every dart's trail reached from muzzle to target, and
with ~90 darts up they merged into solid white ribbons over the object.
- `dartTrailTime` 0.26 → **0.11** (3.1 units), `dartTrail` alpha 0.85 → **0.55** (the trail material
  is `Sprites/Default`, a premultiplied blend, so near-opaque white renders close to additive).
- `dartTrailWidth` is a **new GameConfig knob** — it was hardcoded 0.2 in the baker, which is
  exactly the "never hardcode tunables" rule. It is applied per dart in `Dart.ApplyTint`, not only
  baked into the prefab, so the streak can be retuned without re-running the baker.
- **Width has a narrow band, found by A/B.** First try was 0.12 against a 0.26 bullet and the stream
  read as a *dotted chain* — the dots dominated their own tails. Now width 0.16 against a 0.22
  bullet, which reads as tracer fire. Matching them exactly goes the other way: a ribbon with a bead
  stuck on the end.

**Instanced-dart pass (2026-07-31) — darts have no GameObjects either.** The same change the
sculpture got, applied to the other half of the frame's draw calls: the Dart-geometry pass had left
"pool the darts and tint them with baked materials" as the named next lever, and this is it.
**`Dart.cs`, `Dart.prefab`, `DartDot.png` and `DartTrail.mat` are gone**; a dart is an entry in
`DartField`'s struct array, and the whole swarm is one `Graphics.DrawMeshInstanced` per colour
through the shared `MeshInstanceBatcher`. The flight itself is unchanged — the arc, the dive, the
per-frame re-target and `DartArc` are ported line for line.
- **A dart is TWO quads: a round camera-facing bullet (`DartDot.png`, `dartBulletSize` 0.22) and a
  tapering tail behind it (`DartStreak.png`, `dartTrailWidth` × `dartTrailTime * dartSpeed`).** Per
  dart that is **4 triangles and 0 extra draw calls** against a bullet renderer with a property
  block plus a trail renderer, which was ~200 extra draws at 90 darts.
- **It was first built as ONE stretched quad with the head painted into the streak, and that was
  wrong** — user: *"gun đang bắn những tia thay vì là circle, và trail có vẻ cũng đang bị hỏng"*.
  The tail quad is ~19:1, and whatever shares it is stretched with it: a head that looks round on
  screen would have to be ~1 texel tall in the tile. Decoupling the blob's width and height inside
  the texture only trades one artifact for another. **The bullet has to be its own square,
  camera-aligned quad** — which costs one extra material per colour and nothing else.
- **The tail's +Y runs along the heading, widest end at the bullet, placed half a length behind
  it**, with its normal being the view ray's component perpendicular to the heading — a plain
  billboard (copying the camera's rotation, as `Billboard` does, which IS what the bullet uses)
  would twist the tail off its own flight path. `dartTrailTime` also gates the length while the
  dart is younger than it, because the old trail grew from nothing and a full-length tail popping
  out of the muzzle reads as a flash.
- **Colour is a baked material per palette slot and per part** —
  `Art/Materials/Darts/Dart_S{set}_C{slot}.mat` (bullet, opaque) and `DartTrail_S{set}_C{slot}.mat`
  (tail, `PaletteConfig.dartTrail.a`), wired into `VisualLibrary.voxelSets[set].dart` /
  `.dartStreak`. Same reason as the voxel jitter and flash: grouping IS the batching, so a per-dart
  `MaterialPropertyBlock` would put every dart back in a draw call of its own. Two draws per colour
  on screen, whatever the dart count. Re-bake after changing the alpha — it is baked in.
- **URP 14's Unlit shader has NO premultiplied-alpha path, and assuming it did is what produced
  the white squares.** The old bullet/trail were `Sprites/Default`, whose *shader* does
  `c.rgb *= c.a`, so the hardware blend `One OneMinusSrcAlpha` is correct there. `UnlitForwardPass.hlsl`
  only calls `AlphaModulate`, a no-op unless `_ALPHAMODULATE_ON` (the MULTIPLY blend mode) is set —
  there is no `_ALPHAPREMULTIPLY_ON` in that shader at all. With the premultiplied HW blend and an
  un-premultiplied source, **every fully transparent pixel of the quad adds its full rgb**: the
  bullets rendered as solid white squares and the tails as solid bars. The dart materials use a
  straight `SrcAlpha OneMinusSrcAlpha` blend. Straight alpha is dimmer than the old near-additive
  trail, which is why `dartTrail.a` went 0.55 → **0.72** to land back on the same read.
- **The array grows, it never recycles.** `VoxelPopPool` overwrites its oldest entry when it
  overflows because a cut-short pop costs nothing; a dart that vanished before resolving its hit
  would leave a voxel with no ammo left to break it, and the levels ship with **exact** ammo. A
  barrage settles at ~92 darts against the initial 128 slots, so it never actually grows.
- **Verified end to end on both geometry modes**: levels 20 (revolved, 4543 cubes), 6 (watermelon,
  `flat`, 3372) and 16 (rocket, 4384) each auto-played to `state=Won, alive=0, bankLeft=0,
  gunAmmoLeft=0` — every one of ~12,300 darts resolved its hit, and `ExposedTargetSelector`'s
  reservation set measured empty afterwards, which is the thing to check first if a level ever
  stalls with ammo left.
- **Verification trap, cost half an hour:** `manage_camera screenshot` over MCP renders the camera
  **mid-frame**, so it misses everything submitted in `LateUpdate` — the sculpture came back
  completely invisible while the darts (submitted from `GameManager.Update`) rendered fine. Nothing
  was wrong: `UnityStats` showed 61k triangles and 98 batches that same frame. Use
  `ScreenCapture.CaptureScreenshot` from `execute_code` for anything drawn with
  `Graphics.DrawMeshInstanced`, and confirm with `UnityStats` before believing a black frame.

**Outlined-number pass (2026-07-31).** User supplied a reference screenshot of the genre's bank and
asked for its text treatment. It is the standard casual-game one and it supersedes points 1-3 above:
**one white number with a heavy dark outline, on every block colour**, rather than an ink that
flips with the block's brightness.
1. **`ColorTools.LabelInk` is a CONSTANT now, not a function of the background** (warm white
   `(1, 0.99, 0.96)`). The luminance switch kept every number readable but made the bank read as two
   different label styles depending on which palette a level happened to use — a level with white
   voxels showed navy digits next to white ones. Contrast is the outline's job. `Luminance` went
   with it; it had no other caller.
2. **`NumberLabel.mat`: outline 0.12 → 0.30, face dilate 0.02 → 0.06.** The ratio is deliberately
   outline-heavy: dilate fattens the glyph itself and is what closes the counters in 8/9/0 (point 1
   above had to walk back dilate 0.08 for exactly that), while the outline grows mostly outward. But
   a TMP outline also eats *inward*, so some dilate is needed or a thick outline thins the white
   face to nothing.
3. **Label rects had to SHRINK even though the numbers got bigger**, because the outline renders
   outside the layout bounds — at 0.30 it adds roughly a fifth again on each side, so a rect matched
   to the block face overhangs it. First attempt used the full 0.74 rect and the digits visibly hung
   over the block edges. Now `BankLabelFit` 0.52 in a 0.92 face (drawn number lands at ~0.65 of it),
   `GunLabelFit` 0.50 × 0.40. The size constants are the *single-digit* cap; the rect is what sizes
   the two-digit case, since auto-size only shrinks.

**Cannon shape pass (2026-07-21).** The cannon started as a voxel-bevelled box + a squashed sphere +
a barrel stub ("a box with a lump"), then became one smooth tapered capsule — which the user still
rejected. It is now modelled from a **reference toy field-cannon** (5 orthographic views, supplied as
an image): a chunky rounded-box body with a **banded barrel** out of its front face, a **dark bore**
and a small tab on the back. **The reference's four wheels were then dropped and the barrel levelled
at the user's request** (same day) — the wheel code and the `gunWheel*` knobs are gone, and
`gunBarrelElevation` is now 0 (still tunable). Two meshes carry it, both baked and rebuilt on every
bake: `Art/Meshes/GunBody.asset` (the body — same rounded-cube generator as the voxels but at
`gunBodyRadius` 0.26, so the cannon bevel stays independent of the block bevel) and
`Art/Meshes/GunPuck.asset` (`BuildPuck` — a revolved unit puck, radius 0.5 × height 1, both circular
edges filleted by `gunPuckRim`). **Every round part is that one puck, scaled**: collar, tube, band,
muzzle rim, bore. Config lives under "Cannon shape": `gunBodySize` (0.92, 0.80, 0.94),
`gunBarrelElevation` 0, `gunBarrelLength` 0.41, `gunBarrelRadius` 0.228, `gunPuckSides` 20,
`gunPuckRim` 0.14 — the barrel was then halved and widened 20% on user request, so it is now a short
fat stub protruding 0.34 past the body face rather than a long tube. What was learned, in order:
1. **Stacked pieces make seams — unless every piece is filleted.** The old Dome+Collar build showed a
   hard band at each overlap. A plain `New-Cylinder` has the same problem (a 90° rim next to a moulded
   body reads as machined metal), which is why the puck rolls its edges off instead.
2. **The barrel points +Z — away from the camera, toward the sculpture.** Elevation barely costs
   on-screen length (at the 75° camera the projected length peaks near 15° and is still 97% at 30°),
   so it is purely an art call. It buys one thing: at ≥~25° the muzzle tips back far enough that the
   **dark bore is visible**; at the shipped 0° the bore faces away and never renders in-game. Past
   ~50° the barrel foreshortens into a blob.
3. **A LEVEL barrel loses length behind the body's own top face**, so on-screen it always reads much
   shorter than it measures: at a 75° pitch the top face projects ~0.24 up-screen and eats the first
   part of the barrel. Two levers, both used: the **pivot depth** (`size.z*0.42` — deep enough that
   the collar still straddles the front face, no deeper, or a short barrel is half buried) and the
   **axis height** (`size.y*0.06` — as high as the fat collar can go before it bulges through the top
   face; verified by measurement, collar top 0.259 vs body top 0.280). Re-check both after any change
   to `gunBodySize`, `gunBarrelRadius` or the elevation.
4. **The muzzle rim is a solid disc, not a ring, so a recessed bore is completely hidden** — pushing
   the bore back to t 0.85 made the muzzle render as a plain red ball. It must poke a hair PAST the
   rim face (t 0.95).
5. **Barrel rings need a bare-tube run between them** or they crowd into faint ripples. Order along
   the barrel: fat collar straddling the body face (t 0.22) → bare tube → band (0.79) → rim (0.93).
6. **One continuous colour.** `Gun.partRenderers` (barrel + tab) takes the SAME tint as the body;
   only the bore stays dark, and it keeps its own material rather than a tint. Per-piece lightening —
   the old `domeRenderer`/`tubeRenderer` fields, and the darkened `rimRenderers`, all now removed —
   reads as parts glued together.
7. **The recoil squash pivots at the contact line.** `Gun.body` points at the `Rig` transform (placed
   at groundY −0.52, which seats the body just below the slot's rim bars), so the squash presses the
   cannon down onto the slot floor instead of shrinking it toward its middle. The label is parented to
   the ROOT, not the rig, so recoil never scales the text.
8. **Do NOT offset the ammo label by -Z to "put it on the front face".** Under a steep pitch a -Z
   offset projects *downward* on screen and parks the number under the cannon. The label sits over the
   body centre and `Billboard.towardCamera` (0.80) pulls it out along the view ray — the same
   mechanism the bank blocks use. The size was a hand-tuned 0.066 that only suited one digit count;
   it auto-sizes against `GunLabelFit` now — see the Outlined-number pass.
Verified in Play mode: darts leave the muzzle on their arc and 92/150 voxels fell in a normal-speed
run with four guns.

**SSAO renderer feature** on `UniversalRenderer.asset` (intensity 0.95, radius 0.20, direct 0.25 —
seams/cavity read; much stronger reads as an outline grid; keep After Opaque — see TCP2 gotcha).
SSAO quality: **Source=DepthNormals** (TCP2 Hybrid has a DepthNormals pass), Samples High,
NormalSamples High — with Source=Depth the reconstructed normals dusted every curved surface
(gun dome) in heavy black speckle at close range (scene-view/prefab-stage zoom); DepthNormals
killed it completely.
Post-processing is the scene-authored "PostFX" global Volume with `Art/PostFX.asset` (bloom 0.10
threshold 1.3 — static blocks must never bloom, navy vignette 0.12, saturation +8, gentle
contrast 5, exposure 0 — hand-tune the profile; the GameConfig `post*` fields are only bake
seeds). Do NOT push saturation past ~10 — see the opaque-plastic pass below. `GameBootstrap` just enables
rendering post + SMAA on the camera; background is navy `#263B65` (`PaletteConfig.background`). Voxel palette bases
per art review r3: red `#C92D20`, yellow `#F5C518`, purple `#6B2BE8`, green `#32D272`, blue-tinted
white `#D8E1F2` (slot 4 — r8: brighter whites burn to featureless glow under key+ambient; white
mats also get reduced spec `(0.44,0.46,0.52)` sm 0.88, `_HColor (0.885,0.89,0.90)`, `_SColor
(0.53,0.56,0.73)` and ramp 0.42 — hand-held so the white group stays brightest but every face reads), dark ink
(slot 5) — secondaries deliberately run brighter than the doc's originals because they kept
reading duller than the red. Voxel palettes are the
art-doc swatches (red `#C93620`/orange `#FF7543`/green `#32C76A`/purple/yellow + warm white
`#FFF5E8` slot 4, dark ink slot 5). Cannons are a toy field-cannon tinted with the gun's color — a
rounded-box body with a banded barrel running level toward the sculpture (+Z) and a big ammo number
over the body (see the Cannon shape pass). A dart is two quads — a round near-white bullet
(`DartDot.png`) with a tapering tail behind it (`DartStreak.png`), both tinted per colour by baked
materials — see the Instanced-dart pass; destruction is a **cube pop + one
shockwave ring** and nothing else — see the Pop pass below for what replaced the old layered
particle/debris stack.
Bank queue rows behind row 0 render with a grayed-out cube but a **full-contrast number**
(`BankBlock.SetRow`) — see the Number legibility pass.

**Destruction juice** (all knobs under GameConfig ▸ "Destruction juice" / "Cube pop" /
"Shockwave ring"): on every voxel destroy the view fires exactly three things — the struck cube's
own `Pop` (swell → white flash → collapse to zero with a small outward drift and a tumble), one
pooled `Shockwave` ring quad, and a scale-punch ripple through nearby cubes
(`VoxelCubeField.PunchAround`, tuned by
`hitPunchScale`/`hitPunchTime`/`hitPunchRadius`) — plus a subtle camera shake (`shakeOnHit` 0.045 —
set 0 to disable). `CameraRig.Rig` is the static scene-wired handle the view shakes through, so no
serialized ref or `GetComponent` is needed.

**Hit flash (2026-07-21).** Punched neighbours also flash toward white
(`hitFlashTime` 0.08 / `hitFlashIntensity` 0.55, scaled by the punch falloff so the impact keeps a
clear origin and the region does not strobe). The flash rides the EXISTING `PunchRoutine` coroutine
rather than adding an `Update` to every voxel — a big level has hundreds of cubes and per-voxel
Update callbacks would all be no-ops.

**No-auto-spin pass (2026-07-31).** User: *"remove logic tự xoay khối Sculpture."* The sculpture
used to drift on its own after `autoRotateDelay` 1.6s of no input, at `autoRotateSpeed` 16°/s. Both
config fields are gone and `Turntable` is now purely reactive: `Configure` sets the rest pose,
`ApplyYaw` (drag) is the only thing that moves it, and each of those applies the rotation itself.
Knock-on cleanups that fell out of it:
- **`Turntable.Tick` and `SculptureView.Update` are deleted.** With no idle drift there is nothing to
  advance per frame, so the sculpture root no longer runs a MonoBehaviour Update at all.
- **`ISpinnable.EndSpin` is deleted** (with `Turntable.EndSpin` and the call in
  `TurntableGesture.Cancel`). It existed only to reset the idle timer; with no timer it was a no-op,
  and `ISpinnable` is now the single method `ApplyYaw`. Re-add it if drag inertia is ever wanted.
- `sculptureRestYaw` (26°) is unchanged and is now the orientation a level actually stays at, so it
  became a real art knob rather than just a starting point the drift wandered away from.
- Side benefit for tuning: screen-size measurements are reproducible now. The old note "set
  `autoRotateSpeed = 0` before measuring width" no longer applies — just don't drag.

**Pop pass (2026-07-31) — particles and debris are GONE.** User: *"đổi particle khi cube bị bắn vỡ,
không spawn ra các hạt nữa... tôi muốn nó thật nhẹ."* Chosen from three offered options:
*pop + shockwave ring*. What a destroy costs now, measured live in a 4-gun barrage:
**0 ParticleSystems, 0 Rigidbodies, 0 runtime Instantiate** — verified with
`FindObjectsOfType<ParticleSystem>().Length == 0` and the same for `Rigidbody` while a level was
being demolished. Before: 4 ParticleSystems emitting ~20 sprites, 8 pooled rigidbody fragments, and
the struck cube turned into a 9th rigidbody, several times a second.
- **A pop replaced the old `Explode`.** The cube animates its own death (`popTime` 0.17,
  `popSwell` 0.30 over the first `popSwellPhase` 0.30 of it, then collapse to zero; `popRise` 0.18
  drift, `popSpin` 70°, `popFlash` 0.85). With nothing else spawning, **the swell IS the impact** —
  it is the first knob to raise if hits stop feeling punchy. (It was `VoxelCube.Pop` on the cube's
  own MonoBehaviour until the Instanced-voxel pass moved it into `VoxelPopPool`.)
- **The VoxelCube prefab lost its BoxCollider and Rigidbody entirely** (and later the prefab itself
  — see the Instanced-voxel pass). They existed only to be
  switched on by `Explode`; the collider was authored disabled and nothing raycasts voxels
  (`TurntableGesture.TryBegin` accepts any press, it never hits a collider). A big level is several
  hundred cubes, so this is several hundred fewer physics components per load.
- **`Shockwave` is a pooled unlit quad, not a ParticleSystem.** `ShockwaveFxService` pre-instantiates
  `shockwaveMaxActive` (16) of them on first use and cycles oldest-first, so a destroy never
  allocates. It reuses `FxRing.mat`/`FxRing.png` and tints through a MaterialPropertyBlock (`_Color`
  alpha), and it carries the existing `Billboard` so it always faces the camera.
- **Trap: the ring is invisible without `shockwaveCameraLift`.** Spawned at the destroyed cube's own
  position it is coplanar with the surrounding cubes and they z-cull it — measured: completely
  invisible on anything but an isolated block. It is now pushed 0.5 world units back along the view
  ray (`-camera.forward`), roughly half a voxel, which clears the surface it sits on. Same trick the
  ammo labels use via `Billboard.towardCamera`.
- **Ring texture had to be re-baked, which exposed a second baker bug.** `RingPixel`'s band was
  half-width 0.14 with a squared falloff — a 3px hoop that reads as a faint smudge; it is now 0.26
  with a `SmoothStep(band*1.35)` profile so the core stays solid and only the edges are soft.
  `BakeTexture` skips existing PNGs, so editing the function did nothing; `FxRing.png` now passes
  `alwaysRebuild: true` (it is pure procedural with nothing hand-editable, same reasoning as
  `RoundedCube.asset`).
- **Deleted with the old system**: `ParticleFxService`, `DestructionEffects` (a one-line forwarder
  once there was a single FX layer), `FragmentEmitter`, `Debris`, `DebrisPool`, `IDebrisPool`,
  `Debris.prefab`, the four `Fx_*.prefab` particle systems, `FxSquare`/`FxDisc` textures +
  materials, and every `fx*Count`/`fx*Size`/`debris*` GameConfig field. `IFxService` is now the
  single method `PlayImpact(position, color)`.

**Density pass (2026-07-31) — 1000-2000 cubes a level, on Unity's built-in cube.** User: *"tăng mật
độ của Sculpture lên, và thay vì dùng RoundedCube thì hãy dùng các khối cube cơ bản của unity để
giảm tris... 1 game đấu ít nhất phải 1000-2000 khối cube."* Levels went from 52-450 voxels to
**990-2006**, ramped linearly by level. (**The cube counts and `gunFireInterval` here were
superseded hours later by the Solid pass below — everything else stands.**)
Measured on level 60 (2006 cubes): the sculpture draws
**24k triangles** where the old rounded mesh would have drawn 1.18M, and **98.8% of voxel renderers
carry no MaterialPropertyBlock**, so they stay in the SRP Batcher (1212 renderers, 14 with a block,
during a live four-gun barrage). What each piece is and why:
- **Voxels use `Resources.GetBuiltinResource<Mesh>("Cube.fbx")` — 12 triangles vs
  RoundedCube's 588.** (It sat on `VoxelCube.prefab` at the time; the Instanced-voxel pass moved it
  to `VisualLibrary.voxelMesh`.) Its per-face UVs are the full 0..1 square, exactly like the rounded mesh's
  planar UVs, so EdgeSheen maps unchanged. `RoundedCube.asset` is still baked and still used by the
  **gun slot rims and bank blocks** — a handful of props seen large, where the bevel is what sells
  the toy read. `voxelCornerRadius`/`voxelRoundSegments` therefore no longer affect the sculpture.
- **`voxelSize` 0.5 → 0.25, `voxelGap` 0.035 → 0.018.** A level's grid now spans ~16-34 cells
  instead of ~11, so halving the cell keeps the sculpture at the SAME world size — which is what
  the camera fit, the dart arc and every world-unit FX knob are tuned against. Anything measured in
  world units was halved with it (`popRise`, `shockwaveStartSize`/`EndSize`/`CameraLift`);
  `hitPunchRadius` is in CELLS and went 1.35 → 1.8, deliberately less than double (see below).
- **Per-block colour jitter is now BAKED MATERIALS, not a MaterialPropertyBlock.** This is the
  single most important perf decision here. An MPB evicts its renderer from the SRP Batcher, which
  costs nothing at 450 cubes and is a full material bind per cube at 2000; distinct .mat assets
  sharing one shader stay in one batch. `ColorTools.JitterVariants` (3) quantised shades per slot
  live in `Art/Materials/Voxels/Jitter/Voxel_S{set}_C{slot}_J{v}.mat`, reached through
  `VisualLibrary.GetVoxelMaterial(set, slot, variant)`; variant 1 is the zero-offset shade and just
  points at the base .mat, so only 2 extra files per slot exist. `VoxelCubeField` picks the variant
  from the same position hash the old jitter used. **A voxel must therefore never hold a property
  block** — at the time that meant clearing it the moment a punch flash ended; since the
  Instanced-voxel pass a voxel has no renderer to hold one at all, and *every* per-cube shade
  including the flash is a baked material.
  `BakeVoxelShadeMaterial` re-derives each variant from its base with `CopyPropertiesFromMaterial`
  on EVERY bake (same always-apply reasoning as `RoundedCube.asset`/`NumberLabel.mat`) so the
  hand-held white voxel materials carry their edits into their variants.
- **`ExposedTargetSelector.Reserve` resolves one height band at a time.** Visibility is a ray march
  through the grid and everything else in the filter is a field compare; scanning all 2000 voxels
  would march 2000 times per shot, four guns × ~17 shots/s. It now finds the highest band with any
  live unreserved candidate, marches only that band, and drops to the next band down only if
  nothing there is exposed. Same semantics as the old single-pass version, a few marches instead of
  thousands.
- **`gunFireInterval` 0.16 → 0.06** (0.03 since the Solid pass) so a level still clears in 15-30s.
  At the time the bank still held ~15 blocks, which kept the player's deploy cadence at the
  450-cube version's; the bank became a queue of 50-70 ammo blocks later, see Level content.
  `TimeStarRule`'s `secondsPerVoxel` is derived from this (`gunFireInterval / gunSlotCount` is the
  floor; par is 2× it) — **retune both together** or every level hands out three stars.
- **Knock-on effects that had to be damped, all caused by ~60 destroys a second:**
  `AudioManager` throttles the shoot/break one-shots to ~20/s each (past that it is a flat buzz and
  churns voices); `hitFlashIntensity` 0.55 → 0.35 and `hitPunchTime` 0.16 → 0.12 (hundreds of
  simultaneous flashes read as strobing, and each flashing cube is out of the batch);
  `shockwaveMaxActive` 16 → 40 (60/s × 0.22s life recycles a 16-ring pool mid-animation — measured
  15 rings live at once).
- **`cameraTopMargin` (3.2) is a NEW knob and was needed.** The framing solver used to take its top
  headroom from `cameraFitPadding`, which is also the multiplier on `halfHeight`, so it could not be
  raised on its own. Every level is a big sculpture now and tall ones (the apple's stem, the heart)
  rendered straight through the HUD's progress bar and "N blocks left" label, which the solver
  cannot see. Verified at 3.2 on the tallest shapes.

**Solid pass (2026-07-31, immediately after) — sculptures are no longer hollow.** User: *"có vài
khối Sculpture tạo thành hình dạng các loại trái cây nhưng bị rỗng ruột... bỏ giới hạn trên đi, bao
nhiêu khối cube cũng được."* The Density pass hollowed every shape to a one-cell shell to keep the
cube count affordable, and that is visibly wrong the moment the player breaks through: a
half-demolished strawberry was an empty husk. **The hollowing (and its orphan-repair loop) is gone —
the model is a full solid.** The cost is paid on the Unity side instead:
- **`VoxelCubeField` only DRAWS EXPOSED cubes** (at least one of six grid neighbours
  missing) and `Reveal(index)` adds a voxel's still-living neighbours when it dies — exactly the
  event that exposes them. Drawn count therefore tracks the sculpture's *surface area*, not its
  volume, and never rises during a level. Measured on level 60: **7815 solid cubes, 1944 drawn
  (25%)**, and the count only falls from there (1944 → 1840 → 1522 → 690 → 0 over a full clear).
  (Those were 1944 GameObjects at the time; since the Instanced-voxel pass they are 1944 matrices.)
  Rendering cost is within noise of the hollow version. **Do not go back to instantiating every
  voxel** — the buried ones are pure cost with nothing on screen.
- `gen_levels`' `VOXEL_MIN`/`VOXEL_MAX` (3000 → 8000) are now a **pacing** knob, not a rendering
  budget: clear time is `cubes / (gunSlotCount / gunFireInterval)`. Raising them costs level
  duration and asset size (4.0 MB across the 60 levels, 100 KB at the largest), not frame time.
  `solve_resample` switched its first guess from `factor^2` to `factor^3` — a solid is a volume.
  The resample factors barely moved (~2.3-2.6), so the shapes carry the same detail as before; they
  are just filled in.
- **`gunFireInterval` 0.06 → 0.03** is what pays for the extra cubes: ~130 darts/s keeps a level at
  a casual 22-60s. `TimeStarRule.secondsPerVoxel` follows it to 0.015.
- **The risk this had to clear**: with exact ammo, a buried voxel that never becomes
  camera-exposed makes a level unwinnable. Verified end to end on both geometry modes — level 60
  (lemon, revolved, 7815 cubes) and level 6 (watermelon, `flat` mode, 3372) each finish
  `state=Won, alive=0, bankLeft=0, gunAmmoLeft=0`. Re-verify both after any change to
  `ExposedTargetSelector` or `CameraVoxelVisibility`.
- **KNOWN, UNFIXED — a level can DEADLOCK if every slot holds a colour that is entirely buried.**
  Observed on level 6 twice on 2026-07-31 while auto-playing it: the watermelon's white layer sits
  between the green skin and the red flesh, so mid-level all 286 remaining white cubes measured
  `exposed = 0`. A gun holds fire when `RequestTarget` returns −1 and only retires when its colour's
  ALIVE count hits 0 — which cannot happen while it cannot shoot — so four white guns sat on four
  slots forever with 1118 cubes left. Nothing is leaked (the reservation set measured 0); it is a
  rule interaction, and it predates the instanced-dart pass. A player can walk into it the same way.
  The cheap fixes if it ever needs one: retire a gun whose colour has had no exposed target for N
  seconds, or refuse a deploy whose colour has no exposed cube (`DeployBlock` already refuses a
  colour with none ALIVE — this is the same guard one step stricter).

### Prefabs (`Assets/_Project/Prefabs/`)
`Gun`, `GunSlot` (references `Gun`), `BankBlock`, plus `Fx/Shockwave.prefab`. **There is neither a
voxel nor a dart prefab** — the sculpture is drawn from `VisualLibrary.voxelMesh` + the baked voxel
materials and the darts from `dartMesh` + the baked dart materials, see the Instanced-voxel and
Instanced-dart passes. Prefabs are **fully authored**: the whole visual subtree
(meshes, materials, TMP labels, colliders) lives in the prefab with serialized renderer
refs; `Initialize` only swaps baked materials / tints via MaterialPropertyBlock. The Visual Asset Baker
rebuilds these subtrees on each bake — hand edits to prefab *visuals* belong in the baker or will
be overwritten. **MonoBehaviour class name must match its `.cs` file name** or the prefab silently
stores `m_Script: {fileID: 0}`.

**No runtime `GetComponent`/`AddComponent`/`Camera.main` in gameplay/system code** (user rule) —
every component reference is a `[SerializeField]` wired in the prefab or scene (the baker wires
them all: prefab refs + scene refs — camera on GameBootstrap/CameraRig/BoardInput, the two
AudioManager AudioSources, UIController root). Patterns used instead: voxels have no components at
all any more (see the Pop and Instanced-voxel passes); raycast hits resolve via the `BankBlock.FromCollider` static registry; the camera is
exposed via `CameraRig.Main`. Exception: the code-first UI layer (`UIFactory` + `UI/*Screen`)
still builds uGUI with AddComponent by design — converting it means prefab-izing the whole UI.

### Level content
- `Resources/Levels/level_001.asset` … `level_060.asset` — **`LevelAsset` ScriptableObjects, not
  JSON** (2026-07-31; user: *"chuyển đổi json thành scriptable object, dùng json khá khó để thao
  tác"*). Generated by **`Tools/gen_levels.py`** (`python Tools/gen_levels.py` — regenerates all 60
  in place, deterministic; 4.0 MB total vs 8.7 MB as JSON). Keep the generator IN the repo: an
  earlier one lived only in a scratchpad and was lost, which left the level content unregenerable.
- **The sculpture is stored PACKED — one int per cube** (`LevelAsset.Pack`: x, y, z, colour as four
  bytes, coordinates biased by `CoordinateOffset` 128 because two axes are centred and go negative).
  A `VoxelDef[]` of 3000-8000 entries would be ~5 YAML lines per cube — about a megabyte per asset —
  and would hand the default inspector an 8000-element array to draw. `packedVoxels` is therefore a
  private serialized field: nothing draws it, and `ToLevelData()` unpacks on load (cloning `bank`/
  `bankColors`, or gameplay would write through into the asset on disk).
- **Everything a human edits is a plain field** — `paletteIndex`, `gunSlots`, `bankColumns`,
  `bank[]`, `bankColors[]` — and `LevelAssetEditor` adds the two things the raw fields cannot show:
  cube-vs-ammo per colour, and a solvability verdict from `LevelAsset.FindBankIssues()`. **That
  check is not decoration**: guns are colour-locked with no surplus ammo, so changing one bank
  number by one makes the level impossible with no symptom until it is played to the end.
- **The generator writes the .asset YAML (and .meta) itself**, rather than emitting JSON for an
  editor import step, so one command rebuilds all content with no Unity running. It needs exactly
  one thing from the project: `LevelAsset`'s script GUID, read from the committed
  `LevelAsset.cs.meta`. Per-level asset GUIDs are an md5 of the level number so regenerating never
  renames a file. If you rename or move `LevelAsset.cs`, keep its `.cs.meta` — the generator and
  every existing level asset point at that GUID.
- Sculptures are recognisable **objects** — strawberry, apple, watermelon, ice cream, donut,
  cupcake, rocket, cactus, soccer ball, gem, present, dice… (28 shapes, every one used at least
  twice). Each is authored as a 2D pixel map of *semantic colour letters* (`R O Y G P W K`), not
  raw slot indices, because the four `PaletteConfig` voxel sets differ (set A has no yellow, set C
  no purple); the generator renders each shape with a set that can express all its letters. There
  is no pedestal any more — the object is the whole sculpture.
- **Density is stated in cubes, and the art is RESAMPLED to hit it.** Levels ramp linearly from
  `VOXEL_MIN` 3000 to `VOXEL_MAX` 8000 (actual: 2960-7974). The pixel maps stay at a readable
  ~11×12 — far too coarse for four figures — so each level nearest-neighbour-upscales its map
  (`resample`) before revolving, and `solve_resample` bisects for the factor that lands closest to
  the target. The first guess comes from count ≈ factor³ (the sculpture is a solid volume), which
  makes the search converge in a handful of evaluations. Nearest-neighbour rather than any smoothing
  is deliberate: the map is a *colour* map and interpolation would invent colours no palette set can
  express. Because the target is stated per level, `build_plan` no longer sorts the roster by
  natural voxel count — it only decides coverage and variety.
- **Round shapes are true solids of revolution, then hollowed.** A row N cells wide is also made
  ~N cells deep (per-column depth follows the row's circular profile). Do NOT go back to a fixed
  shallow extrude: the sculpture sits on a turntable, so a fixed-depth "ball" is a coin and
  spinning it side-on exposes that instantly. **Each contiguous RUN of a row is revolved
  separately** (`runs`) — a row like the heart's `..RR...RR..` or the cherry's `RRRRR.RRRRR` is two
  volumes, and measuring the profile across the whole row gives both the depth of the *combined*
  span, revolving them into one wide flat plate. At the authored resolution that was a couple of
  stray cubes; at the resampled resolution it was a very visible slab on top of the level-1 heart.
  **The result is NOT hollowed** — see the Solid pass. `count_exposed` reports how many of a level's
  cubes start visible, i.e. how many GameObjects it actually spawns; that is the number to watch
  when changing `VOXEL_MIN`/`VOXEL_MAX`, not the total.
- `bulk` (0.8 / 1.0) is now only a roundness/variety knob — the difficulty ramp is the cube target.
- **EXACT ammo, no surplus (2026-07-31).** The bank holds one dart per cube: `sum(bank) ==
  len(voxels)`, and because guns are color-locked it holds **per color** too —
  `sum(bank values with bankColors == c) == voxel count of color c`. `gen_levels.check_bank`
  asserts both plus "no zero-value block" for all 60 levels, and `ProceduralLevelSource` (the
  no-content fallback) splits exactly the same way. The earlier 32%→12% surplus taper is gone.
  This is solvable, not brutal, because **no dart can ever be wasted**: `Gun.Update` holds fire
  when `RequestTarget` returns -1 (nothing live, unreserved and camera-exposed), reservation stops
  two darts claiming one voxel, `GameManager.DeployBlock` refuses a block whose color is already
  cleared, and a gun only retires with leftover ammo once its color is gone — i.e. when it had
  nothing left to shoot anyway. Verified end-to-end in Play mode at the new density: level 60
  (2006 cubes) finished `state=Won` with `alive=0, bankLeft=0, gunAmmoLeft=0`. **If you ever add a
  way to waste a dart** (a miss, a dart destroyed in flight, deploying onto an already-cleared
  color) the levels become unwinnable — put the surplus back at the same time.
- **The bank block VALUE is capped, and the count is free** (2026-07-31; user: *"chia ra nhiều bank
  hơn, hiện tại mỗi bank đang 100-200 là khá nhiều. mỗi bank value sẽ 50 đến 70"*). `BLOCK_MIN` 50 /
  `BLOCK_TARGET` 60 / `BLOCK_MAX` 70, so a block is always a clean two-digit number and a level
  holds `cubes / 60` of them — **49 blocks early, 133 late**. This is the reverse of the previous
  rule, which capped the count at the 15 that fit on screen and let the value absorb the level's
  density (100-240 per block, three digits). `split_color` picks whichever of
  floor/ceil(ammo / 60) lands closest to the target: some totals cannot be cut into the band at all
  (90 is one block of 90 or two of 45), and landing slightly under beats landing far over, so
  `check_bank` asserts only the upper bound. Blocks are shuffled deterministically (seed = level) so
  colours interleave.
- **`BankArea` is therefore a WINDOW onto a queue, not the whole bank.** It lays out
  `GameConfig.bankVisibleRows` (3) × `bankColumns` (5) = 15 blocks and deactivates the rest,
  parking them exactly one row behind the window so they slide forward as the queue advances
  instead of popping in. `bankVisibleRows` existed in the config but was unused until this pass.
- Both the bank and the gun labels still auto-size (`BankLabelFit`, `GunLabelFit` in
  `VisualAssetBaker`) — a fixed size can only ever be right for one digit count, and the gun label
  was fixed at 0.066 until the density pass.
- **The knock-on to know: block value sets the deploy cadence.** A gun burns a block in
  `blockValue × gunFireInterval` seconds, so with 60 ammo at 0.03 a slot frees every ~1.8s and,
  across four slots, the player deploys roughly **every 0.45s** for the whole level. Level duration
  is then `cadence × blockCount` — the three are locked together, so a calmer cadence at this cube
  count means proportionally longer levels. Raise `BLOCK_TARGET` (busier numbers, calmer hands) or
  lower `VOXEL_MIN`/`VOXEL_MAX` (fewer blocks) to move it; changing `gunFireInterval` alone trades
  cadence against duration one-for-one.

### Config assets
`Assets/_Project/Resources/Config/GameConfig.asset` + `PaletteConfig.asset` + `VisualLibrary.asset`.
Wired into `GameBootstrap` in the scene; also findable via `Resources.Load` if the serialized ref
is missing.

### Building the scene/prefabs again
All scene objects and prefabs were built via Unity MCP `execute_code` (CodeDom, C# 6 — no `using`
directives, fully-qualify `UnityEditor.*`). The scene wires `GameManager` to the scene views + prefabs
via `SerializedObject`. If rebuilding, compile scripts first, then create prefabs, then the scene.

## Unity version (important)

- Editor version is **`2022.3.62f3`** (`ProjectSettings/ProjectVersion.txt`, branch `unity_2022`). The project was intentionally downgraded from Unity 6 — see commit `1f3bbff "down version"`. Open with this exact version to avoid a forced upgrade.
- If `ProjectVersion.txt` and `README.md` ever disagree on the version, `ProjectVersion.txt` is authoritative.

## Key stack

- **URP 14.0** — supports **both 2D and 3D**. The active pipeline `Assets/Settings/UniversalRP.asset` (guid `681886c5...`, referenced by every quality tier) holds two renderers:
  - index **0 = `Renderer2D.asset`** — kept for 2D scenes (opt-in per camera).
  - index **1 = `UniversalRenderer.asset`** — the standard 3D forward renderer, **the default** (`m_DefaultRendererIndex: 1`).
  - The default MUST stay on the 3D renderer: editor preview/scene-view/prefab-stage cameras always
    use the default renderer, and the TCP2 Hybrid shader has no `Universal2D` pass — with the 2D
    renderer as default every baked-material prefab renders **invisible** in the inspector/prefab
    previews (bug fixed 2026-07-17). A 2D scene's camera can still opt into `Renderer2D (0)` via
    **Camera → Rendering → Renderer**. Add renderers by appending to `m_RendererDataList` in `UniversalRP.asset`.
  - Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`.
- **New Input System 1.18.0** — actions asset wired via `ProjectSettings/EditorBuildSettings.asset` (`com.unity.input.settings.actions`). Do not use the legacy `Input.*` API.
- **2D tooling**: Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras).
- **DOTween / DOTweenPro** (`Assets/Plugins/Demigiant/`) — tweening. Settings: `Assets/Resources/DOTweenSettings.asset`.
- **Layer Lab GUI Pro-CasualGame** (`Assets/Layer Lab/`) — prefab-based casual UI kit (buttons, popups, frames, sliders). Prefer composing these prefabs for UI.
- **Toony Colors Pro 2** (`Assets/JMO Assets/`) — stylized shading; ships its own asmdefs (`ToonyColorsPro.*`).
  **In use**: every baked surface material (`Assets/_Project/Art/Materials`) uses
  `"Toony Colors Pro 2/Hybrid Shader"` (URP SubShader). Its embedded URP support is pre-URP14: the
  `_SCREEN_SPACE_OCCLUSION` shader variant does not compile (missing
  `GetNormalizedScreenSpaceUV`) → keep the SSAO renderer feature in **After Opaque** mode or every
  TCP2 material renders flat cyan.

## Unity MCP

`com.coplaydev.unity-mcp` (MCP for Unity) is a package dependency, so the editor can be driven programmatically over MCP. If `mcp__UnityMCP__*` tools are missing or on the wrong port, use the `unity-mcp-connect` skill — each open editor/project binds its own port. Note: `.mcp.json` is not committed.

## Working in this repo

- **No build/lint/test CLI is set up.** Building and playmode happen inside the Unity Editor. The Test Framework (`com.unity.test-framework`) is installed but there are no test assemblies yet.
- To run tests headlessly once test asmdefs exist:
  ```bash
  Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml
  ```
  (swap `EditMode`/`PlayMode`; use the 2022.3.62f3 editor binary).
- The `Assembly-CSharp*.csproj` and `.sln` at the repo root are Unity-generated and gitignored — never hand-edit them; they regenerate on import.
- When adding first-party code, create a dedicated assembly definition (e.g. `Assets/_Game/`) rather than dropping scripts into the third-party folders.

## Conventions for new game code

The available skills encode the intended patterns for this template — follow them when the task matches:
- **`unity-game-clone`** — scene-authored + prefab architecture, tunables/colors in ScriptableObject configs (never hardcoded), procedural sprites/audio, solver-generated levels. Use when building a game from a spec.
- **`unity-ui-refactor`** — code-first uGUI (UIFactory/SpriteFactory), procedural "candy" UI, no image assets.
- **`texture-override`** — pick compression format by target device (TV/desktop → DXT, mobile/TikTok → ASTC) when building WebGL.
- **`tiktok-minigame-sdk`** / **`tv-input-kit`** — TikTok Mini Game (WebGL) and TV-remote input integration.

## Git

- Default branch is `main`; active work is on `unity_2022`.
- Auto-generated folders (`Library/`, `Temp/`, `Logs/`, `obj/`, IDE/`.sln`/`.csproj` files) are gitignored.
