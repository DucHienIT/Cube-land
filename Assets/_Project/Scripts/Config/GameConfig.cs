using UnityEngine;

namespace CubeBlaster
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CubeBlaster/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        const string ResourcePath = "Config/GameConfig";

        static readonly ConfigProvider<GameConfig> Provider = new ConfigProvider<GameConfig>(ResourcePath);

        public static GameConfig Active => Provider.Active;

        public static void Use(GameConfig asset) => Provider.Use(asset);

        [Header("Application")]
        public int targetFrameRate = 60;

        [Header("Sculpture")]
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
        public float dartTrailTime = 0.26f;
        public float dartHitScatter = 0.0f;
        public float dartApproachOffset = 2.4f;

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

        [Header("Rotation (drag only — the structure never spins by itself)")]
        public float rotateSensitivity = 0.3f;

        [Header("Camera")]
        public bool cameraOrthographic = false;
        public float cameraPitch = 75f;
        public float cameraFov = 32f;
        public float cameraFitBottomY = -5.2f;
        [Tooltip("World units of clear space reserved ABOVE the sculpture. The HUD's level title " +
                 "and progress bar sit there and the framing solver cannot see them, so without " +
                 "a real margin a tall sculpture renders straight through the text — which every " +
                 "level does now that they are all 1000-2000 cubes. This used to borrow " +
                 "cameraFitPadding, which is also a multiplier and so could not be raised alone.")]
        public float cameraTopMargin = 3.2f;

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
    }
}
