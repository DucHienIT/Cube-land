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
        // 0 = blocks touch face-to-face (user preference: no visible hole between cubes). The only
        // separation left is the bevel groove from voxelCornerRadius, which SSAO darkens. History:
        // 0.07 was chosen so SSAO had a real cavity to find, but that reads as an airy/sparse
        // silhouette; if the stack ever looks like flat panels again, deepen the bevel groove
        // (voxelCornerRadius) or raise SSAO rather than re-opening this gap.
        public float voxelGap = 0f;             // no gap — cubes sit flush, seam comes from the bevel + AO

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
        public Color ambientSky = new Color(0.92f, 0.88f, 0.84f);     // from above — warm, bright
        public Color ambientEquator = new Color(0.60f, 0.62f, 0.74f); // sides — cool fill sits in the 60-75% band so faces separate but never sink
        public Color ambientGround = new Color(0.50f, 0.46f, 0.68f);  // from below — navy-purple tint, never dark
        public Vector3 sculptureCenter = new Vector3(0f, 3.2f, 0f);
        public float sculptureTilt = 55f;       // lean back (deg, about X) so the steep camera still sees the figure's front
        public float debrisForce = 4.5f;        // impulse applied to a destroyed cube
        public float debrisLife = 1.1f;         // seconds before a debris cube is culled
        public float debrisTorque = 6f;
        public int debrisMediumCount = 3;       // extra mid-size chunks per destroyed voxel (50-70% scale)
        public int debrisSmallCount = 5;        // extra small fragments per destroyed voxel (20-40% scale)

        [Header("Destruction juice (hit feedback)")]
        public float hitPunchScale = 0.14f;     // neighbor cubes swell by this fraction on a nearby destroy
        public float hitPunchTime = 0.16f;      // seconds for the punch in-out
        public float hitPunchRadius = 1.35f;    // in cells — which neighbors receive the punch
        public int fxBurstCount = 10;           // colored confetti squares per destroy
        public int fxShardCount = 6;            // fast shard streaks per destroy
        public int fxPuffCount = 3;             // soft dust puffs per destroy (0 = off)

        [Header("Guns")]
        public int gunSlotCount = 4;
        public float gunFireInterval = 0.16f;   // seconds between darts from one gun
        public float gunSlotSpacing = 1.35f;
        public float gunSlotY = 0f;
        public float gunSlotZ = -2.6f;
        // Ceiling on a deployed gun's tint brightness. Gun tints are applied through a
        // MaterialPropertyBlock, which REPLACES _BaseColor rather than multiplying it, so the
        // material itself cannot hold headroom — without this cap the white color slot (~0.97,
        // pushed to pure white by the lighter dome/tube lerps) clipped to a featureless blob.
        [Range(0.4f, 1f)] public float gunTintMaxValue = 0.80f;

        [Header("Darts")]
        public float dartSpeed = 22f;           // world units / second
        public float dartLife = 2f;
        public float dartTrailTime = 0.26f;     // long white streaks
        public float dartHitScatter = 0.0f;     // aim jitter
        public float dartApproachOffset = 2.4f; // dart arc control point: this far in front of the target, toward the camera

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
        public float shakeOnHit = 0.045f;   // subtle per-destroy camera shake (0 = off)

        [Header("Toon shading (Toony Colors Pro 2 Hybrid — the game's only surface shader)")]
        public Color toonHighlight = new Color(0.98f, 0.96f, 0.93f);   // _HColor: lit-side tint (sub-1 so whites never burn)
        // _SColor is near-NEUTRAL on purpose: it must darken the base color without dragging its
        // hue. A saturated (purple) shadow turns green into teal and red into magenta-brown, and a
        // hue that shifts under shadow is the #1 cue the brain reads as "light is passing through"
        // — i.e. translucent jelly, not opaque plastic.
        public Color toonShadow = new Color(0.52f, 0.49f, 0.56f);      // _SColor: barely-cool neutral — darkens, never re-hues
        [Range(0.01f, 1f)] public float toonRampThreshold = 0.44f;     // side faces fall partly into the shadow tint so form reads (white mats hand-hold their own values)
        [Range(0f, 1f)] public float toonRampSmoothing = 0.38f;        // enough gradient for the bevel to read as a rounded edge; past ~0.5 the wrap reads as subsurface wax
        [Range(0.001f, 1f)] public float toonSpecSize = 0.25f;         // stylized specular: small...
        [Range(0f, 1f)] public float toonSpecSmoothing = 0.45f;        // ...and defined — tight plastic gloss, not a broad waxy sheen
        // Must stay DIM. Voxel faces are flat, so a toon specular lobe covers an entire face at
        // once instead of making a small highlight — the term is added uniformly across the whole
        // surface. At 0.80 that pushed lit red faces to ~(1.57,0.94,0.84), i.e. clipped to white
        // (7.1% of warm pixels blown). 0.18 keeps a plastic sheen at 0.4% blown. The gloss read on
        // flat faces has to come from the EdgeSheen gradient, NOT from specular.
        public Color toonSpecColor = new Color(0.18f, 0.175f, 0.165f); // gentle sheen — never a face-wide white wash

        [Header("Post-processing (kept subtle per art doc)")]
        public bool postProcessing = true;
        public float postBloomIntensity = 0.10f;   // very light bloom (threshold 1.3 in PostFX.asset — highlights must not glow)
        public float postVignette = 0.12f;         // light vignette
        // CEILING, not a taste knob: past ~10 the grade drives a channel to 0 on the saturated
        // voxels (green hit R=0 at sat 20, and 43% of green pixels already clipped at the old 12).
        // A pixel with one channel pinned at 0 reads as glowing gel, never as opaque plastic.
        // Get richness from the palette + lighting; verify with the clip check in CLAUDE.md.
        public float postSaturation = 8f;          // keeps faces vivid without crushing a channel to zero
        public float postContrast = 5f;            // gentle — contrast also spreads channels apart
        public float postExposure = 0f;            // no lift — the toon ramp already reads bright

        [Header("Scoring")]
        public int coinsPerLevel = 20;
    }
}
