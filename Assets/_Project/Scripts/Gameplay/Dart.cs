using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// A single dart. Flies from a gun barrel to a reserved voxel's world position; on arrival it
    /// asks the GameManager to resolve the hit (destroy that voxel). The bullet sphere and the
    /// TrailRenderer (mesh, materials, widths) are authored in the Dart prefab; Init only tints
    /// them per shot (trail via vertex start/endColor, bullet via a property block).
    /// </summary>
    public class Dart : MonoBehaviour
    {
        [Header("Prefab-authored visual refs")]
        [SerializeField] TrailRenderer trail;
        [SerializeField] MeshRenderer bulletRenderer;

        GameManager _gm;
        int _voxelIndex;
        Vector3 _target;
        float _speed;
        float _life;
        bool _done;

        public void Init(GameManager gm, int voxelIndex, Vector3 startPos, Vector3 target, Color tint)
        {
            _gm = gm;
            _voxelIndex = voxelIndex;
            _target = target;
            transform.position = startPos;
            _speed = Cfg.Active.dartSpeed;
            _life = Cfg.Active.dartLife;
            transform.up = (target - startPos).normalized;

            // Reference look: near-white bullets with long bright white streaks (the color hint
            // stays faint so trails read as clean light beams against the navy backdrop).
            Color bullet = Color.Lerp(tint, Color.white, 0.75f);
            Color trailCol = new Color(bullet.r, bullet.g, bullet.b, Palette.Active.dartTrail.a);

            if (trail != null)
            {
                trail.Clear();
                trail.time = Cfg.Active.dartTrailTime;
                trail.startColor = trailCol;
                trail.endColor = new Color(trailCol.r, trailCol.g, trailCol.b, 0f);
            }
            if (bulletRenderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                bulletRenderer.GetPropertyBlock(mpb);
                VisualLibrary.Tint(mpb, bullet);
                bulletRenderer.SetPropertyBlock(mpb);
            }
        }

        void Update()
        {
            if (_done) return;
            _life -= Time.deltaTime;
            // Track the voxel's live position so darts still connect while the sculpture spins.
            if (_gm != null) _target = _gm.VoxelWorldPos(_voxelIndex);
            Vector3 to = _target - transform.position;
            float step = _speed * Time.deltaTime;
            if (to.magnitude <= step || _life <= 0f)
            {
                Arrive();
                return;
            }
            transform.position += to.normalized * step;
            transform.up = to.normalized;
        }

        void Arrive()
        {
            if (_done) return;
            _done = true;
            if (_gm != null) _gm.ResolveDartHit(_voxelIndex, transform.position);
            Destroy(gameObject);
        }
    }
}
