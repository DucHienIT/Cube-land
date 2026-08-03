using UnityEngine;

namespace CubeBlaster
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CubeBlaster/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        const string ResourcePath = "Config/GameConfig";
        const float MaxCameraTopReserve = 0.45f;

        static readonly ConfigProvider<GameConfig> Provider = new ConfigProvider<GameConfig>(ResourcePath);

        public static GameConfig Active => Provider.Active;

        public static void Use(GameConfig asset) => Provider.Use(asset);

        [Header("Application")]
        public int targetFrameRate = 60;

        [Header("Sculpture")]
        [Tooltip("Off by default: the sculpture is 1000-2900 renderers, and having them cast into " +
                 "the shadow map very nearly DOUBLES the frame's draw calls (measured 4215 -> 2410 " +
                 "on level 30). That is the single biggest lever for the WebGL target, where each " +
                 "draw costs far more than it does natively. The cost is the sculpture's own " +
                 "self-shadowing — undersides of overhangs go flatter — which SSAO partly covers. " +
                 "Read per frame by VoxelCubeField's instanced draw, so this now takes effect " +
                 "immediately — no re-bake.")]
        public bool voxelCastShadows = false;
        [Tooltip("Half what it was before the density pass. A level is now 1000-2000 cubes and " +
                 "its grid spans ~16-34 cells instead of ~11, so halving the cell keeps the " +
                 "sculpture at the same WORLD size — which is what every other world-unit knob " +
                 "(shockwave sizes, popRise, dart arc, camera fit) is tuned against.")]
        public float voxelSize = 0.25f;
        public float voxelGap = 0.018f;

        [Header("Cube look (shading; corner radius applies to props, not voxels)")]
        [Tooltip("Bevel of the shared RoundedCube mesh, used by the gun slot rims and bank " +
                 "blocks. Voxels render on Unity's built-in 12-triangle cube — see the density " +
                 "pass in CLAUDE.md — so this no longer affects the sculpture.")]
        [Range(0.02f, 0.45f)] public float voxelCornerRadius = 0.16f;
        [Range(2, 3)] public int voxelRoundSegments = 3;
        [Tooltip("Width of the diffuse light transition on voxel bevels only. Higher values remove hard light bands without flattening guns/UI props.")]
        [Range(0.05f, 0.8f)] public float voxelLightRampSmoothing = 0.42f;
        [Tooltip("Blends the voxel lit tone toward the shared toon shadow tone. Lower values tame bright corner strips while preserving the palette hue.")]
        [Range(0.5f, 1f)] public float voxelHighlightStrength = 0.86f;
        [Range(0f, 0.2f)] public float voxelValueJitter = 0.04f;
        [Range(0f, 0.1f)] public float voxelHueJitter = 0.02f;
        [Range(0f, 0.1f)] public float voxelScaleJitter = 0.02f;
        public Color ambientSky = new Color(0.68f, 0.62f, 0.58f);
        public Color ambientEquator = new Color(0.44f, 0.46f, 0.56f);
        public Color ambientGround = new Color(0.30f, 0.30f, 0.40f);
        public Vector3 sculptureCenter = new Vector3(0f, 3.2f, 0f);
        public float sculptureTilt = 55f;

        [Header("Sculpture presentation (framing — no gameplay effect)")]
        [Range(0.6f, 2.5f)] public float sculptureScale = 1.55f;
        [Range(0f, 90f)] public float sculptureRestYaw = 26f;
        [Range(0.25f, 0.95f)] public float sculptureFillWidth = 0.60f;

        [Header("Destruction juice (hit feedback)")]
        public float hitPunchScale = 0.14f;
        public float hitPunchTime = 0.12f;
        [Tooltip("Radius in CELLS. Raised with the density (cells are half the size now) but " +
                 "deliberately not doubled: the same world radius over a denser shell sweeps in " +
                 "far more neighbours, and every punched cube runs a coroutine and takes a " +
                 "property block, which drops it out of the SRP batch for the duration.")]
        public float hitPunchRadius = 1.8f;
        [Tooltip("Seconds a struck cube's neighbours stay tinted toward their hit-flash colour. " +
                 "Keep short (0.05-0.10) — a long flash reads as the object changing colour.")]
        [Range(0f, 0.3f)] public float hitFlashTime = 0.08f;
        [Tooltip("How far the flash lerps a cube toward white. Deliberately partial: a full-white " +
                 "flash erases the block's colour identity and reads as a rendering glitch.")]
        [Range(0f, 1f)] public float hitFlashIntensity = 0.35f;

        [Header("Cube pop (the struck cube's own death animation — spawns nothing)")]
        [Tooltip("Total seconds from hit to gone. Short: the pop has to finish before the next " +
                 "dart lands or the sculpture reads as blurring away rather than snapping apart.")]
        [Range(0.05f, 0.6f)] public float popTime = 0.17f;
        [Tooltip("How far the cube swells before it collapses. This IS the impact — with no " +
                 "particles left, the swell is the only thing that sells the hit.")]
        [Range(0f, 1f)] public float popSwell = 0.30f;
        [Tooltip("Fraction of popTime spent swelling. The rest is the collapse to zero. Below ~0.2 " +
                 "the swell is too fast to register; above ~0.5 the cube lingers fat and looks stuck.")]
        [Range(0.05f, 0.95f)] public float popSwellPhase = 0.30f;
        [Tooltip("World units the cube drifts away from the sculpture centre while collapsing. " +
                 "Keep small — this is a nudge that reveals what was behind it, not a launch.")]
        public float popRise = 0.09f;
        [Tooltip("How far the cube flashes toward white as it swells.")]
        [Range(0f, 1f)] public float popFlash = 0.85f;
        [Tooltip("Degrees of tumble over the whole pop. Cheap extra life; 0 disables it.")]
        public float popSpin = 70f;
        [Tooltip("Size of the pop animation ring. A pop no longer owns a GameObject, so this is a " +
                 "hard ceiling on simultaneous death animations and nothing is ever allocated. " +
                 "Four guns at the current fire interval destroy well over a hundred cubes a " +
                 "second and each pop lives popTime, so anything under ~30 cuts pops short.")]
        public int popMaxActive = 96;

        [Header("Shockwave ring (the ONLY thing a destroy spawns)")]
        [Tooltip("Seconds the ring takes to expand and fade out.")]
        [Range(0.05f, 0.6f)] public float shockwaveTime = 0.22f;
        public float shockwaveStartSize = 0.3f;
        [Tooltip("Ring diameter at the end, in world units. Around 3x the start reads as a snap; " +
                 "much larger and it drifts across neighbouring cubes as a halo.")]
        public float shockwaveEndSize = 0.85f;
        [Range(0f, 1f)] public float shockwaveAlpha = 0.72f;
        [Tooltip("World units the ring is pushed along the view ray toward the camera. WITHOUT " +
                 "this the ring spawns level with the cube it replaces and the surrounding cubes " +
                 "z-cull it — measured: completely invisible on anything but an isolated block. " +
                 "Roughly half a voxel is enough to clear the surface it sits on.")]
        public float shockwaveCameraLift = 0.25f;
        [Tooltip("How far the ring is bleached toward white from the cube's colour. Fully white " +
                 "loses the link to which block was hit; fully coloured disappears on same-colour cubes.")]
        [Range(0f, 1f)] public float shockwaveWhiten = 0.55f;
        [Tooltip("Size of the pre-instantiated ring pool. Rings are reused oldest-first and never " +
                 "allocate at runtime, so this is a hard ceiling on simultaneous rings. Four guns " +
                 "at the current fire interval destroy ~60 cubes a second and each ring lives " +
                 "shockwaveTime, so anything under ~15 recycles rings mid-animation.")]
        public int shockwaveMaxActive = 40;

        [Header("Guns")]
        public int gunSlotCount = 4;
        [Tooltip("Cut with the density bumps so a level still clears in a casual 20-60s: four " +
                 "guns fire ~130 darts a second. This is THE knob that pays for the solid " +
                 "sculptures — clear time is cubes / (gunSlotCount / gunFireInterval), so it has " +
                 "to move whenever gen_levels' VOXEL_MIN/MAX move. TimeStarRule's par is derived " +
                 "from this value; retune both together.")]
        public float gunFireInterval = 0.03f;
        public float gunSlotSpacing = 1.35f;
        public float gunSlotY = 0f;
        public float gunSlotZ = -2.6f;
        [Range(0.4f, 1f)] public float gunTintMaxValue = 0.80f;

        [Header("Cannon shape (own meshes — independent of the voxel bevel)")]
        [Range(0.05f, 0.49f)] public float gunBodyRadius = 0.26f;
        [Range(2, 8)] public int gunBodySegments = 5;
        [Tooltip("Body proportions (width, height, depth) in slot units.")]
        public Vector3 gunBodySize = new Vector3(0.92f, 0.80f, 0.94f);

        [Tooltip("Barrel elevation in degrees above horizontal. 0 (level) is the current art " +
                 "direction: it costs nothing in on-screen barrel length under the 75deg camera, but " +
                 "the muzzle bore then faces away and is not visible in-game.")]
        [Range(0f, 70f)] public float gunBarrelElevation = 0f;
        [Range(0.2f, 1.2f)] public float gunBarrelLength = 0.41f;
        [Range(0.08f, 0.4f)] public float gunBarrelRadius = 0.228f;

        [Tooltip("Puck mesh (barrel tube / collar / bands): sides of the revolve and how much of " +
                 "its radius is rolled into the rim fillet. A hard 90deg rim reads as machined " +
                 "metal next to the moulded body.")]
        [Range(6, 32)] public int gunPuckSides = 20;
        [Range(0.02f, 0.49f)] public float gunPuckRim = 0.14f;

        [Header("Darts")]
        public float dartSpeed = 28f;
        public float dartLife = 2f;
        [Tooltip("Seconds of streak behind a dart. This is NOT the length you see — that is " +
                 "dartTrailTime * dartSpeed in world units, against a sculpture only ~8 units " +
                 "wide, so it has to move whenever dartSpeed does. At 0.26 with the current " +
                 "speed the streak was 7.3 units, i.e. the whole flight path, and read as a rope " +
                 "rather than a streak.")]
        public float dartTrailTime = 0.11f;
        [Tooltip("Width of the tail quad where it meets the bullet; it tapers to nothing. Keep it " +
                 "a little under dartBulletSize: matched too closely and each dart reads as a " +
                 "ribbon with a bead stuck on the end, far under and the stream of darts reads as " +
                 "a dotted chain rather than tracer fire.")]
        public float dartTrailWidth = 0.16f;
        [Tooltip("Diameter of the round camera-facing bullet. It is a SEPARATE quad from the tail " +
                 "— a single stretched quad cannot hold a round head, which is what made the darts " +
                 "read as rays. Was hardcoded 0.22 in the baker until the Instanced-dart pass.")]
        public float dartBulletSize = 0.22f;
        public float dartHitScatter = 0.0f;
        public float dartApproachOffset = 2.4f;
        [Tooltip("Slots DartField starts with. A dart may never be recycled while it is still " +
                 "flying (ammo is exact), so the array grows instead of overwriting — and a grow " +
                 "is a copy of the whole array on the frame the barrage peaks, which is exactly " +
                 "the frame that can least afford it. Four guns settle at ~90 darts; this is set " +
                 "far above that so the array is allocated once, at level load, and never again.")]
        public int dartPoolCapacity = 200;

        [Header("Bank")]
        public float bankSlotSpacing = 1.5f;
        public float bankRowSpacing = 0.96f;
        public float bankY = 0f;
        public float bankZ = -4.8f;
        [Tooltip("How many rows of the bank queue are on screen. A level holds one block per " +
                 "~60 cubes (49-130+ blocks), far more than fit under the sculpture, so " +
                 "BankArea shows this many rows and parks the rest one row behind to slide in " +
                 "as the queue advances. Raising it runs the bank off the bottom of the frame — " +
                 "cameraFitBottomY is what it has to stay inside.")]
        public int bankVisibleRows = 3;
        [Tooltip("Rows shown on a wide screen. Vertical space is what a landscape frame is short " +
                 "of and horizontal space is what it has to spare, so the bank trades a row for " +
                 "columns instead of stacking under the sculpture.")]
        public int bankVisibleRowsLandscape = 2;
        [Tooltip("Thickness of the black outline shell on the PLAYABLE (front-row) bank blocks, " +
                 "as a fraction of the block. It is an inverted hull — a copy of the block mesh " +
                 "scaled up and rendered with front faces culled — so the visible ring is only " +
                 "HALF this per side (0.16 reads as an 8% rim). 0.055 was tried first and " +
                 "measured under a pixel on a phone-sized frame. Requires a re-bake: the shell's " +
                 "scale is baked into BankBlock.prefab.")]
        [Range(0.01f, 0.2f)] public float bankOutlineWidth = 0.16f;

        [Header("Rotation (drag only — the structure never spins by itself)")]
        public float rotateSensitivity = 0.3f;

        [Header("Camera")]
        public bool cameraOrthographic = false;
        public float cameraPitch = 75f;
        public float cameraFov = 32f;
        public float cameraFitBottomY = -5.2f;
        [Tooltip("Bottom of the framed board on a wide screen. Landscape shows one bank row less " +
                 "(bankVisibleRowsLandscape), so the board ends higher and the frame does not " +
                 "have to reserve the missing row.")]
        public float cameraFitBottomYLandscape = -4.7f;
        [Tooltip("Hard MINIMUM world clearance above the sculpture. The HUD reserve below is what " +
                 "normally decides the headroom; this only catches the case where a fraction of a " +
                 "small frame would leave the sculpture touching the top edge. It used to be the " +
                 "whole mechanism at 3.2 world units, which on a wide screen is far less of the " +
                 "frame than the HUD actually occupies — see cameraTopReserve.")]
        public float cameraTopMargin = 0.8f;
        [Tooltip("Fraction of the framed height kept clear at the TOP for the HUD, which the " +
                 "solver cannot see. It has to be a fraction, not world units: the HUD is a " +
                 "screen-space band, so the same 3.2 world units was ~16% of a portrait frame " +
                 "and had to cover a 28% band in landscape — which is exactly why the level " +
                 "title and the block counter used to sit on top of the sculpture.")]
        [Range(0f, 0.45f)] public float cameraTopReserve = 0.18f;
        [Tooltip("Same reserve on a wide screen. Lower than portrait because HudScreen collapses " +
                 "to a single top bar there; raise both together if the HUD ever grows.")]
        [Range(0f, 0.45f)] public float cameraTopReserveLandscape = 0.11f;

        [Header("Feel / Timing")]
        public float winPopupDelay = 0.7f;
        public float cameraFitPadding = 1.06f;
        public float shakeOnHit = 0f;

        [Header("Toon shading (Toony Colors Pro 2 Hybrid — the game's only surface shader)")]
        public Color toonHighlight = new Color(0.90f, 0.84f, 0.78f);
        public Color toonShadow = new Color(0.44f, 0.38f, 0.42f);
        [Range(0.01f, 1f)] public float toonRampThreshold = 0.49f;
        [Range(0f, 1f)] public float toonRampSmoothing = 0.22f;
        [Range(0.001f, 1f)] public float toonSpecSize = 0.20f;
        [Range(0f, 1f)] public float toonSpecSmoothing = 0.32f;
        public Color toonSpecColor = new Color(0.20f, 0.17f, 0.15f);

        [Header("Post-processing (kept subtle per art doc)")]
        public bool postProcessing = true;
        public float postBloomIntensity = 0.055f;
        public float postVignette = 0.13f;
        public float postSaturation = 13f;
        public float postContrast = 10f;
        public float postExposure = 0f;

        [Header("Backdrop (baked radial gradient quad behind the sculpture)")]
        [Tooltip("How much brighter the centre of the backdrop is than the screen edge, as a " +
                 "fraction. The brief calls for a 5-10% lift — enough to stop the navy reading as " +
                 "flat paper, small enough that it never competes with the sculpture.")]
        [Range(0f, 0.35f)] public float bgGradientStrength = 0.09f;
        [Tooltip("Radius of the bright core as a fraction of the backdrop, 0.5 = touches the edges.")]
        [Range(0.15f, 1.2f)] public float bgGradientRadius = 0.62f;
        [Tooltip("Extra blue richness in the backdrop core vs the flat background colour. " +
                 "0 = pure luminance gradient.")]
        [Range(0f, 0.4f)] public float bgGradientTint = 0.10f;

        [Header("Ambient occlusion (seeds for the URP SSAO renderer feature)")]
        [Range(0f, 3f)] public float aoIntensity = 0.95f;
        [Range(0.05f, 1f)] public float aoRadius = 0.20f;
        [Range(0f, 1f)] public float aoDirectStrength = 0.25f;

        [Header("Scoring")]
        public int coinsPerLevel = 20;

        public int GetBankVisibleRows(bool landscape) =>
            Mathf.Max(1, landscape ? bankVisibleRowsLandscape : bankVisibleRows);

        /// One bank lane per shooter slot, in BOTH orientations — the lane a block is queued in
        /// is the slot it is heading for, and a bank wider than the slot row breaks that read.
        /// The level asset's own `bankColumns` is therefore informational: it is written by
        /// gen_levels to match gunSlotCount and nothing loads it into the layout.
        public int GetBankColumns() => Mathf.Max(1, gunSlotCount);

        public float GetCameraFitBottomY(bool landscape) =>
            landscape ? cameraFitBottomYLandscape : cameraFitBottomY;

        public float GetCameraTopReserve(bool landscape) =>
            Mathf.Clamp(landscape ? cameraTopReserveLandscape : cameraTopReserve, 0f, MaxCameraTopReserve);
    }
}
