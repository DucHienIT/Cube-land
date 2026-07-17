using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// One cube of the sculpture. Everything is authored in the prefab — mesh, renderer, a
    /// disabled BoxCollider and a kinematic Rigidbody — and referenced via serialized fields.
    /// Init swaps in the baked per-color material asset and applies the per-block jitter via a
    /// MaterialPropertyBlock (so the shared .mat asset is never touched). On <see cref="Explode"/>
    /// the authored physics is switched live (collider on, body non-kinematic) and it becomes a
    /// debris chunk that self-culls.
    /// </summary>
    public class VoxelCube : MonoBehaviour
    {
        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] BoxCollider box;      // authored disabled; enabled on explode
        [SerializeField] Rigidbody body;       // authored kinematic; freed on explode

        bool _exploded;

        public Color Color { get; private set; }
        /// <summary>Baked material asset this cube renders with (shared with its debris).</summary>
        public Material SharedMaterial => meshRenderer != null ? meshRenderer.sharedMaterial : null;

        public void Init(Material mat, Color color, float size)
        {
            Color = color; // gameplay/fx color stays the material's base color; only rendering is varied
            Vector3 lp = transform.localPosition;
            int seed = Mathf.RoundToInt(lp.x * 16f) * 73856093
                     ^ Mathf.RoundToInt(lp.y * 16f) * 19349663
                     ^ Mathf.RoundToInt(lp.z * 16f) * 83492791;
            // ±scaleJitter size wobble (quantized like the color) so stacks read hand-assembled,
            // not machine-cloned — small enough to leave the outer silhouette intact.
            float sj = Cfg.Active.voxelScaleJitter;
            float wobble = 1f + ((((seed >> 3) % 5) + 5) % 5 - 2) * 0.5f * sj;
            transform.localScale = Vector3.one * size * wobble;

            if (meshRenderer == null) return;
            if (mat != null) meshRenderer.sharedMaterial = mat;
            var mpb = new MaterialPropertyBlock();
            VisualLibrary.Tint(mpb, VisualLibrary.Vary(color, seed));
            meshRenderer.SetPropertyBlock(mpb);
        }

        public void Explode(Vector3 dir, float force, float torque, float life)
        {
            if (_exploded) return;
            _exploded = true;
            transform.SetParent(null, true);

            if (box != null) box.enabled = true;
            if (body != null)
            {
                body.isKinematic = false;
                body.AddForce((dir.normalized + Random.insideUnitSphere * 0.4f) * force, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            }

            StartCoroutine(FadeAndDie(life));
        }

        System.Collections.IEnumerator FadeAndDie(float life)
        {
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.one * transform.localScale.x * 0.995f;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
