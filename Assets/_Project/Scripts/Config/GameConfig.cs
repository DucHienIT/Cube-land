using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// All gameplay/feel tunables. Never hardcode these in gameplay code — designers tune here.
    /// Access via the <see cref="Cfg"/> facade, which resolves an always-non-null Active instance.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CubeBlaster/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Application")]
        public int targetFrameRate = 60;

        [Header("Sculpture")]
        public float voxelSize = 0.5f;          // world size of one cube
        public float voxelGap = 0.028f;         // tight gap — blocks read as one chunky mass, seams via AO

        [Header("Cube look (rounded corners + shading)")]
        [Range(0.02f, 0.45f)] public float voxelCornerRadius = 0.18f; // fat bevel — each block reads as its own plastic brick
        [Range(2, 8)] public int voxelRoundSegments = 3;              // chamfer-soft, not fully round
        [Range(0f, 0.2f)] public float voxelValueJitter = 0.04f;     // per-block brightness variation — light does the heavy lifting
        [Range(0f, 0.1f)] public float voxelHueJitter = 0.02f;       // per-block hue variation (quantized)
        [Range(0f, 0.1f)] public float voxelScaleJitter = 0.02f;     // ±2% size wobble so stacks feel hand-assembled
        // Trilight ambient: shadow faces shift toward a purple-navy hue instead of multiplying
        // toward black (art review: "shadow vẫn có màu, không làm asset bị xỉn").
        // "Everything is lit" (reference philosophy): HIGH ambient floor so no face ever sinks
        // into dark; form comes from the strong near-top-down key + soft AO, not from dark sides.
        public Color ambientSky = new Color(0.95f, 0.92f, 0.88f);     // from above — warm, bright
        public Color ambientEquator = new Color(0.78f, 0.79f, 0.88f); // sides — high cool fill, blocks keep base color
        public Color ambientGround = new Color(0.60f, 0.56f, 0.76f);  // from below — navy-purple tint, never dark
        public Vector3 sculptureCenter = new Vector3(0f, 3.2f, 0f);
        public float sculptureTilt = 55f;       // lean back (deg, about X) so the steep camera still sees the figure's front
        public float debrisForce = 4.5f;        // impulse applied to a destroyed cube
        public float debrisLife = 1.1f;         // seconds before a debris cube is culled
        public float debrisTorque = 6f;
        public int debrisMediumCount = 2;       // extra mid-size chunks per destroyed voxel (50-70% scale)
        public int debrisSmallCount = 3;        // extra small fragments per destroyed voxel (20-40% scale)

        [Header("Guns")]
        public int gunSlotCount = 4;
        public float gunFireInterval = 0.16f;   // seconds between darts from one gun
        public float gunSlotSpacing = 1.35f;
        public float gunSlotY = 0f;
        public float gunSlotZ = -2.6f;

        [Header("Darts")]
        public float dartSpeed = 22f;           // world units / second
        public float dartLife = 2f;
        public float dartTrailTime = 0.26f;     // long white streaks
        public float dartHitScatter = 0.0f;     // aim jitter

        [Header("Bank")]
        public float bankSlotSpacing = 1.5f;
        public float bankRowSpacing = 0.96f;  // depth step of the queue: queued rows sit flush
                                              // behind the playable row, on the same ground plane
        public float bankY = 0f;      // bank plane height — matches gunSlotY so bank & slots are coplanar
        public float bankZ = -4.8f;
        public int bankVisibleRows = 3;         // how many rows of blocks show per column

        [Header("Rotation (structure spins like a turntable)")]
        public float autoRotateSpeed = 16f;      // deg/sec drift while idle
        public float rotateSensitivity = 0.3f;   // deg per pixel of horizontal drag
        public float autoRotateDelay = 1.6f;     // idle seconds before auto-rotate resumes

        [Header("Camera")]
        public bool cameraOrthographic = false;  // art doc: 3/4 view with slight perspective
        public float cameraPitch = 75f;          // downward tilt; near top-down board view (user preference)
        public float cameraFov = 32f;            // narrow FOV = near-isometric, low distortion (art doc: 25-40°)
        public float cameraFitBottomY = -5.2f;   // world Y of the bottom of the framed area (covers bank rows projected low by the pitch)

        [Header("Feel / Timing")]
        public float winPopupDelay = 0.7f;
        public float cameraFitPadding = 1.06f;
        public float shakeOnHit = 0f;   // camera shake disabled

        [Header("Toon shading (Toony Colors Pro 2 Hybrid — the game's only surface shader)")]
        public Color toonHighlight = new Color(0.98f, 0.96f, 0.93f);   // _HColor: lit-side tint (sub-1 so whites never burn)
        public Color toonShadow = new Color(0.54f, 0.47f, 0.72f);      // _SColor: shadow keeps a navy-purple hue, never black — deep enough that top/front/side clearly separate
        [Range(0.01f, 1f)] public float toonRampThreshold = 0.38f;     // low = most of the block counts as lit (white mats hand-hold 0.42 + own _HColor/_SColor/spec to keep face separation)
        [Range(0f, 1f)] public float toonRampSmoothing = 0.50f;        // soft wrap — no hard cel bands (art doc 5.1)
        [Range(0.001f, 1f)] public float toonSpecSize = 0.38f;         // stylized specular: broad...
        [Range(0f, 1f)] public float toonSpecSmoothing = 0.78f;        // ...and soft — the toy gloss, wide + never burning white
        public Color toonSpecColor = new Color(0.72f, 0.70f, 0.66f);

        [Header("Post-processing (kept subtle per art doc)")]
        public bool postProcessing = true;
        public float postBloomIntensity = 0.10f;   // very light bloom (threshold 1.3 in PostFX.asset — highlights must not glow)
        public float postVignette = 0.12f;         // light vignette
        public float postSaturation = 12f;         // keeps the darker (hue-shifted) faces vivid without going neon
        public float postContrast = 6f;            // gentle contrast boost
        public float postExposure = 0f;            // no lift — the toon ramp already reads bright

        [Header("Scoring")]
        public int coinsPerLevel = 20;
    }
}
