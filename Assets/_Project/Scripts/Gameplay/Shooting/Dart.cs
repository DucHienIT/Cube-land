using UnityEngine;

namespace CubeBlaster
{
    public class Dart : MonoBehaviour
    {
        const float BulletWhitening = 0.75f;
        const float MinArcLength = 0.1f;
        const float MinSquaredMagnitude = 1e-8f;

        [Header("Prefab-authored visual refs")]
        [SerializeField] TrailRenderer trail;
        [SerializeField] MeshRenderer bulletRenderer;

        readonly RendererTinter _tinter = new RendererTinter();

        IDartContext _context;
        int _voxelIndex;
        Vector3 _target;
        Vector3 _start;
        Vector3 _muzzleDirection;
        float _speed;
        float _life;
        float _progress;
        bool _diving;
        bool _done;

        public void Initialize(IDartContext context, int voxelIndex, Vector3 startPosition,
            Vector3 muzzleDirection, Vector3 target, Color tint)
        {
            _context = context;
            _voxelIndex = voxelIndex;
            _target = target;
            _start = startPosition;
            _muzzleDirection = muzzleDirection.sqrMagnitude > 1e-6f
                ? muzzleDirection.normalized
                : Vector3.forward;
            _progress = 0f;
            _diving = false;
            _done = false;

            var config = GameConfig.Active;
            _speed = config.dartSpeed;
            _life = config.dartLife;
            transform.position = startPosition;
            transform.up = _muzzleDirection;

            ApplyTint(tint, config.dartTrailTime);
        }

        void Update()
        {
            if (_done) return;

            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Arrive();
                return;
            }

            if (_context != null) _target = _context.GetVoxelWorldPosition(_voxelIndex);

            float step = _speed * Time.deltaTime;
            if (_diving) Dive(step);
            else FollowArc(step);
        }

        void Dive(float step)
        {
            Vector3 toTarget = _target - transform.position;
            if (toTarget.magnitude <= step)
            {
                Arrive();
                return;
            }
            toTarget.Normalize();
            transform.position += toTarget * step;
            transform.up = toTarget;
        }

        void FollowArc(float step)
        {
            var arc = DartArc.Create(_start, _muzzleDirection, _target, ApproachDirection(),
                GameConfig.Active.dartApproachOffset);

            _progress += step / Mathf.Max(MinArcLength, arc.Length);
            if (_progress >= 1f)
            {
                _diving = true;
                transform.position = arc.Approach;
                return;
            }

            Vector3 next = arc.Sample(_progress);
            Vector3 velocity = next - transform.position;
            transform.position = next;
            if (velocity.sqrMagnitude > MinSquaredMagnitude) transform.up = velocity.normalized;
        }

        Vector3 ApproachDirection()
        {
            var camera = CameraRig.Main;
            return camera != null
                ? (camera.transform.position - _target).normalized
                : (_start - _target).normalized;
        }

        void ApplyTint(Color tint, float trailTime)
        {
            Color bullet = Color.Lerp(tint, Color.white, BulletWhitening);
            if (trail != null)
            {
                Color head = new Color(bullet.r, bullet.g, bullet.b, PaletteConfig.Active.dartTrail.a);
                trail.Clear();
                trail.time = trailTime;
                trail.startColor = head;
                trail.endColor = new Color(head.r, head.g, head.b, 0f);
            }
            _tinter.Apply(bulletRenderer, bullet);
        }

        void Arrive()
        {
            if (_done) return;
            _done = true;
            if (_context != null) _context.ResolveDartHit(_voxelIndex, transform.position);
            Destroy(gameObject);
        }
    }
}
