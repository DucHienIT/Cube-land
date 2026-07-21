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
        Vector3 _start;
        float _speed;
        float _life;
        float _s; // 0→1 progress along the arc
        bool _diving;
        bool _done;

        public void Init(GameManager gm, int voxelIndex, Vector3 startPos, Vector3 target, Color tint)
        {
            _gm = gm;
            _voxelIndex = voxelIndex;
            _target = target;
            _start = startPos;
            _s = 0f;
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
            if (_life <= 0f) { Arrive(); return; }
            // Track the voxel's live position so darts still connect while the sculpture spins.
            if (_gm != null) _target = _gm.VoxelWorldPos(_voxelIndex);

            // Flight = cubic-bezier arc bowed toward the player, then a straight dive. With the
            // steep camera, "toward the camera" is nearly straight up, so the arc's bulge is in
            // world -Z (the fixed board axis the guns and camera sit on) — the dart pops out in
            // front of the sculpture, climbs clear of the pedestal wall, and ends at an approach
            // point held dartApproachOffset in front of the target's camera-facing side. The dive
            // then follows the same clear camera ray GameManager.IsExposed validated at pick
            // time, which is what keeps darts from punching through blocks covering their target.
            float step = _speed * Time.deltaTime;
            Vector3 n = ApproachDir(_target);
            if (_diving)
            {
                Vector3 to = _target - transform.position;
                if (to.magnitude <= step) { Arrive(); return; }
                to.Normalize();
                transform.position += to * step;
                transform.up = to;
                return;
            }

            float offset = Cfg.Active.dartApproachOffset;
            Vector3 p1 = _start + Vector3.back * offset + Vector3.up * (offset * 0.5f);
            Vector3 p2 = _target + n * (offset * 2f) + Vector3.back * offset;
            Vector3 approach = _target + n * offset;
            float len = (p1 - _start).magnitude + (p2 - p1).magnitude + (approach - p2).magnitude;
            _s += step / Mathf.Max(0.1f, len);
            if (_s >= 1f) { _diving = true; transform.position = approach; return; }

            float u = 1f - _s;
            Vector3 pos = u * u * u * _start + 3f * u * u * _s * p1
                        + 3f * u * _s * _s * p2 + _s * _s * _s * approach;
            Vector3 vel = pos - transform.position;
            transform.position = pos;
            if (vel.sqrMagnitude > 1e-8f) transform.up = vel.normalized;
        }

        Vector3 ApproachDir(Vector3 from)
        {
            var cam = CameraRig.Main;
            return cam != null
                ? (cam.transform.position - from).normalized
                : (_start - _target).normalized;
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
