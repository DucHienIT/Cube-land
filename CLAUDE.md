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
empty gun slots to deploy shooters; demolish the whole sculpture to win. **Guns are color-locked**:
each bank block carries a voxel color, the deployed gun is tinted with it and only shoots/destroys
cubes of that color (`GameManager.RequestTarget(color)` filters by `cell.c`); a gun retires and
frees its slot once no cube of its color remains, even with leftover ammo. Clones the *mechanic +
feel + mood* of the reference genre — no copied art/logo/layout.

- **Play scene**: `Assets/_Project/Scenes/Game.unity` (index 0 in the build list; SampleScene removed
  from build). Open it and press Play — zero manual setup required.
- **Namespace**: `CubeBlaster`. All first-party code lives under `Assets/_Project/Scripts/`.
- **No asmdef yet** — game code compiles into the predefined `Assembly-CSharp` (which auto-references
  Input System / UGUI / URP). Adding a dedicated asmdef later means adding those references explicitly.

### Code map (`Assets/_Project/Scripts/`)
- `Core/` — pure logic, no MonoBehaviour. `LevelData` (JSON schema: voxels[] + bank[] + bankColors[]
  — color slot per bank block, parallel to bank), `VoxelModel` (runtime sculpture state +
  per-color alive counts + `VoxelDestroyed`/`AllCleared` events), `LevelLibrary` (loads
  `Resources/Levels/level_NNN.json`, derives `bankColors` greedily for legacy JSON without them,
  has a built-in fallback so it runs with no content).
- `Config/` — `GameConfig` + `PaletteConfig` + `VisualLibrary` ScriptableObjects. Accessed via
  always-non-null facades `Cfg.Active` / `Palette.Active` / `Visuals.Active` (serialized override →
  `Resources/Config` → CreateInstance defaults). **Never hardcode tunables — add them here.**
  `VisualLibrary` is the hub of ALL baked visual assets: per-palette voxel materials
  (`voxelSets[set].colors[slot]`), fixed materials (slotPad/dartBullet/dartTrail), the four FX
  particle prefabs and the debris prefab. It also owns `Vary` (quantized per-block jitter) and
  `Tint` (MaterialPropertyBlock helper — renderers are tinted via MPB so shared .mat assets are
  never instanced/modified).
- `Gameplay/` — `GameBootstrap` (applies config, `[DefaultExecutionOrder(-100)]`), `GameManager`
  (orchestrator: level flow, **target reservation** so no two darts hit the same voxel and none are
  wasted, **camera-exposure targeting** — `IsExposed` grid-marches the voxel→camera ray in
  sculpture-local space, so guns only shoot voxels the player can see; covered voxels wait until
  uncovered/rotated into view, win/stars/save), `SculptureView` (builds cubes as LOCAL-space children, **turntable
  rotation** — auto-spins while idle, spun by drag, `GetWorldPos` is live so darts track turning
  voxels — explodes cubes into physics debris; spawns Debris-prefab fragments), `VoxelCube`,
  `Gun` (auto-fires; visual subtree authored in the prefab, `Init` only tints via MPB), `GunSlot`,
  `Dart` (re-targets the voxel's live position each frame; flies a cubic-bezier arc bulged toward
  the player (world −Z) then dives in along the validated camera ray — never pierces other cubes;
  `dartApproachOffset` tunes the arc), `BankBlock` (draggable; only stack row 0 is playable — `Row` property, back rows dimmed),
  `BankArea` (reference-style vertical stacks: queued rows sit flush below the playable row via
  `bankRowSpacing`), `BoardInput` (drag a row-0 block = deploy; drag on the structure = spin it;
  new Input System), `CameraRig` (steep near-top-down board view — `cameraOrthographic` off,
  `cameraPitch` 75° (user preference; pairs with `sculptureTilt` 55° so the figure's front stays
  visible), narrow `cameraFov` ~32° for near-isometric low distortion, fit via
  `cameraFitBottomY`/`cameraFitPadding`; orthographic still supported when the flag is on),
  `Billboard`.
- `UI/` — `UIController` + plain-class screens (`MainMenu`/`LevelSelect`/`Hud`/`Win`/`Settings`),
  code-first uGUI via `UIFactory` (legacy `Text` + LegacyRuntime.ttf, no TMP). `UIBob` idle pulse.
- `Systems/` — `AudioManager` (all SFX/music synthesized at runtime), `SaveSystem` (PlayerPrefs,
  `cubeblaster.` prefix: unlocked level, coins, per-level stars, mute).
- `Utils/` — `SpriteFactory` (SDF rounded-rect/circle/star UI sprites), `Fx` (one-shot particle
  effects — instantiates the four shared world-space ParticleSystem prefabs from `VisualLibrary`
  and drives them via Emit; no procedural fallback: missing prefab = no FX), `UIFactory`.
  (The old `Look`/`WorldLabel` runtime-procedural material/mesh/label builders were REMOVED — all
  of that is baked into assets by the Visual Asset Baker, see below.)

### Cube / lighting look
Art direction follows `docs/game_graphic_style_analysis.md` (stylized plastic toy voxels, hot
red/orange vs green contrast on navy, layered destruction), refined through several user art
reviews toward "premium plastic toy, not voxel prototype". **Every surface material is Toony
Colors Pro 2's Hybrid Shader** — there is no other surface shader in the game. Sanctioned
exceptions (not surfaces): dart bullet (URP/Unlit, per baker), dart trail + FX particles
(Sprites/Default — vertex-color VFX), TMP text.

**Everything visual is a baked asset, nothing procedural at runtime.** `Tools ▸ Cube Blaster ▸
Bake Visual Assets` (`Scripts/Editor/VisualAssetBaker.cs`) generates: the rounded-cube mesh
(`Art/Meshes`), the EdgeSheen/FX textures (`Art/Textures`), all TCP2 materials seeded from
GameConfig/PaletteConfig (`Art/Materials`, voxels in `Materials/Voxels/Voxel_S{set}_C{slot}.mat`),
the four FX ParticleSystem prefabs (`Prefabs/Fx`), the Debris prefab, the visual subtrees +
serialized refs of the gameplay prefabs, the PostFX VolumeProfile (`Art/PostFX.asset`), the scene
"PostFX" Volume, and the `VisualLibrary.asset` wiring. Default bake keeps hand-edited asset values
(creates only what's missing; prefab subtrees are rebuilt); "Force Rebake All" resets everything
to procedural defaults. **Exception: `RoundedCube.asset` is now rebuilt on EVERY bake** (updated in
place so its GUID survives) — it is pure procedural geometry with nothing hand-editable, and the
old "skip if it exists" path meant `voxelCornerRadius`/`voxelRoundSegments` silently did nothing
unless you force-rebaked.

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

All cubes (voxels, debris, bank blocks) share the baked RoundedCube mesh; bevel is
`voxelCornerRadius` 0.18 (fat brick read) with `voxelRoundSegments` 3 (chamfer-soft), gap is
`voxelGap` **0** — user preference: cubes sit flush with no visible hole between them, so the only
separation is the bevel groove (which SSAO darkens). History: 0.07 gave SSAO a real cavity but read
as an airy/sparse silhouette; 0.028 made the seam a *lit bevel*, which read as panelling (see the
solid-cube pass below). If the stack ever reads as flat panels again, deepen `voxelCornerRadius` or
raise SSAO rather than re-opening the gap. Same-color cubes get a *light* quantized
per-block jitter (`VisualLibrary.Vary`, hue 0.02 / value 0.04 — lighting, not random color,
carries the shading) applied via MaterialPropertyBlock, plus a ±2% size wobble
(`voxelScaleJitter`); `VoxelCube.Color` keeps the exact base color for gameplay/FX tints. The mesh
has per-face planar UVs; voxel materials sample the EdgeSheen texture — a plastic tile with a soft
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
are applied through a `MaterialPropertyBlock` (`Gun.ApplyTint` → `VisualLibrary.Tint`), and an MPB
`SetColor` **replaces** `_BaseColor` instead of multiplying it — so lowering `GunPart.mat`'s base
color does nothing for the tinted parts (measured: dropping it 0.90→0.76 moved clipping only
89%→86%). The white slot arrives at ~0.97 and the *lighter* dome/tube lerps push it to pure white.
Fixed with `GameConfig.gunTintMaxValue` (0.80), a hue-preserving cap applied in `Gun.ApplyTint`:
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
**SSAO renderer feature** on `UniversalRenderer.asset` (intensity 1.35, radius 0.35, direct 0.1 —
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
`#FFF5E8` slot 4, dark ink slot 5). Cannons are a plump rounded "canister" tinted with the gun's
color — dome shoulders + a short stubby muzzle tilted up-forward (+Z) and a big white ammo number
on the front. Darts are near-white spheres with long bright `TrailRenderer` streaks; destruction is
layered (art doc §8): white contact flash + small expanding shockwave ring (`Fx.Flash`) + colored
particle burst + stretched shard streaks (`Fx.Burst`/`Fx.Shards` — four shared world-space
ParticleSystems total; burst/shard sprites are **rounded squares, not soft discs**, to stay on the
voxel shape language) + the cube itself as a large physics chunk + extra medium/small rounded-cube
fragments (`SculptureView.SpawnFragments`, counts in `debrisMediumCount`/`debrisSmallCount`).
Bank queue rows behind row 0 render
grayed-out with numbers hidden (`BankBlock.SetRow`).

**Destruction juice** (all knobs under GameConfig ▸ "Destruction juice"): on every voxel destroy the
view fires flash + ring, a colored burst (`fxBurstCount`), shard streaks (`fxShardCount`), soft dust
puffs (`fxPuffCount` — `Fx.Puff` reuses the burst ParticleSystem with big faded chalky particles, so
no fifth prefab), medium+small debris (`debrisMediumCount` 3 / `debrisSmallCount` 5), a scale-punch
ripple through nearby cubes (`SculptureView.PunchNeighbors` → `VoxelCube.Punch`, tuned by
`hitPunchScale`/`hitPunchTime`/`hitPunchRadius`), and a subtle camera shake (`shakeOnHit` 0.045 —
set 0 to disable). `CameraRig.Rig` is the static scene-wired handle the view shakes through, so no
serialized ref or `GetComponent` is needed.

### Prefabs (`Assets/_Project/Prefabs/`)
`VoxelCube`, `Dart`, `Gun`, `GunSlot` (references `Gun`), `BankBlock`, `Debris`, plus the four
`Fx/Fx_*.prefab` particle systems. Prefabs are now **fully authored**: the whole visual subtree
(meshes, materials, TMP labels, colliders, trail) lives in the prefab with serialized renderer
refs; `Init` only swaps baked materials / tints via MaterialPropertyBlock. The Visual Asset Baker
rebuilds these subtrees on each bake — hand edits to prefab *visuals* belong in the baker or will
be overwritten. **MonoBehaviour class name must match its `.cs` file name** or the prefab silently
stores `m_Script: {fileID: 0}`.

**No runtime `GetComponent`/`AddComponent`/`Camera.main` in gameplay/system code** (user rule) —
every component reference is a `[SerializeField]` wired in the prefab or scene (the baker wires
them all: prefab refs + scene refs — camera on GameBootstrap/CameraRig/BoardInput, the two
AudioManager AudioSources, UIController root). Patterns used instead: VoxelCube explosion physics
is authored disabled/kinematic and switched live; `Debris` fragments carry their own component
with refs; raycast hits resolve via the `BankBlock.FromCollider` static registry; the camera is
exposed via `CameraRig.Main`. Exception: the code-first UI layer (`UIFactory` + `UI/*Screen`)
still builds uGUI with AddComponent by design — converting it means prefab-izing the whole UI.

### Level content
- `Resources/Levels/level_001.json` … `level_060.json`, generated by **`Tools/gen_levels.py`**
  (`python Tools/gen_levels.py` — regenerates all 60 in place, deterministic). Keep this file IN
  the repo: the previous generator lived only in a scratchpad and was lost, which left the level
  content unregenerable.
- Sculptures are recognisable **objects** — strawberry, apple, watermelon, ice cream, donut,
  cupcake, rocket, cactus, soccer ball, gem, present, dice… (28 shapes, every one used at least
  twice). Each is authored as a 2D pixel map of *semantic colour letters* (`R O Y G P W K`), not
  raw slot indices, because the four `PaletteConfig` voxel sets differ (set A has no yellow, set C
  no purple); the generator renders each shape with a set that can express all its letters. There
  is no pedestal any more — the object is the whole sculpture.
- **Round shapes are true solids of revolution, then hollowed.** A row N cells wide is also made
  ~N cells deep (per-column depth follows the row's circular profile). Do NOT go back to a fixed
  shallow extrude: the sculpture sits on a turntable, so a fixed-depth "ball" is a coin and
  spinning it side-on exposes that instantly. The solid is then hollowed (any voxel with all six
  neighbours present is dropped) — a full ball is ~650 voxels of which the interior can never be
  seen or shot, the shell is ~270 and plays identically since targeting only picks camera-exposed
  voxels. Hollowing can strand a shell voxel whose neighbours were all interior, so the generator
  re-adds the minimum interior voxels needed to keep everything face-connected; the validator
  asserts zero orphans.
- Difficulty ramps via `bulk` (0.5 → 1.0, how fully the shape is revolved), 52 → 450 voxels across
  the 60 levels. The plan **selects the roster first, then sorts it by real voxel count** — an
  earlier version walked a size-ordered list with an advancing cursor and ran out of large combos,
  repeating one shape for the last dozen levels.
- **Solvable by construction, per color** (guns are color-locked): for every color c,
  `sum(bank values with bankColors == c) >= voxel count of color c`, with a surplus tapering
  ~32% → ~12% as levels progress. Bank blocks are split near a target value that grows with level
  and shuffled deterministically (seed = level) so colours interleave.

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
