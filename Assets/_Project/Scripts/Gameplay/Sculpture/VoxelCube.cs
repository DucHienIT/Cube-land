using System.Collections;
using UnityEngine;

namespace CubeBlaster
{
    public class VoxelCube : MonoBehaviour
    {
        const int HashX = 73856093;
        const int HashY = 19349663;
        const int HashZ = 83492791;
        const float HashQuantization = 16f;

        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer meshRenderer;

        readonly RendererTinter _tinter = new RendererTinter();

        bool _popped;
        Vector3 _baseScale;
        Coroutine _punch;
        Color _renderColor = UnityEngine.Color.white;

        public Color Color { get; private set; }

        public void Initialize(Material material, Color color, VoxelStyle style)
        {
            Color = color;

            int seed = PositionSeed(transform.localPosition);
            float wobble = 1f + ColorTools.GetQuantizedOffset(seed >> 3) * style.ScaleJitter;
            transform.localScale = Vector3.one * style.Size * wobble;
            _baseScale = transform.localScale;

            if (meshRenderer == null) return;
            if (material != null) meshRenderer.sharedMaterial = material;
            _renderColor = ColorTools.Jitter(color, seed, style.HueJitter, style.ValueJitter);
            _tinter.Apply(meshRenderer, _renderColor);
        }

        public void Punch(PunchSettings settings)
        {
            if (_popped || !settings.IsActive) return;
            if (_punch != null) StopCoroutine(_punch);
            _punch = StartCoroutine(PunchRoutine(settings));
        }

        public void Pop(Vector3 outward, PopSettings settings)
        {
            if (_popped) return;
            _popped = true;

            if (_punch != null)
            {
                StopCoroutine(_punch);
                _punch = null;
                transform.localScale = _baseScale;
            }
            transform.SetParent(null, true);

            StartCoroutine(PopRoutine(outward, settings));
        }

        IEnumerator PunchRoutine(PunchSettings settings)
        {
            bool flashing = settings.Flashes && meshRenderer != null;
            float span = Mathf.Max(settings.Duration, flashing ? settings.FlashTime : 0f);

            float elapsed = 0f;
            while (elapsed < span && !_popped)
            {
                elapsed += Time.deltaTime;
                if (elapsed < settings.Duration)
                    transform.localScale = _baseScale * (1f + settings.Amount * Ease.Pulse(elapsed / settings.Duration));
                if (flashing)
                    ApplyFlash(Mathf.Max(0f, 1f - elapsed / settings.FlashTime) * settings.FlashIntensity);
                yield return null;
            }

            if (!_popped)
            {
                transform.localScale = _baseScale;
                if (flashing) _tinter.Apply(meshRenderer, _renderColor);
            }
            _punch = null;
        }

        IEnumerator PopRoutine(Vector3 outward, PopSettings settings)
        {
            Vector3 start = transform.position;
            Vector3 drift = outward.normalized * settings.Rise;
            Quaternion spinFrom = transform.rotation;
            Vector3 spinAxis = Random.onUnitSphere;
            float duration = Mathf.Max(0.01f, settings.Duration);
            float swellPhase = Mathf.Clamp(settings.SwellPhase, 0.05f, 0.95f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = t < swellPhase
                    ? Mathf.Lerp(1f, 1f + settings.Swell, Ease.SmoothStep01(t / swellPhase))
                    : Mathf.Lerp(1f + settings.Swell, 0f, Ease.SmoothStep01((t - swellPhase) / (1f - swellPhase)));

                transform.localScale = _baseScale * scale;
                transform.position = start + drift * Ease.SmoothStep01(t);
                transform.rotation = spinFrom * Quaternion.AngleAxis(settings.Spin * t, spinAxis);
                ApplyFlash(Mathf.Max(0f, 1f - t / swellPhase) * settings.Flash);
                yield return null;
            }
            Destroy(gameObject);
        }

        void ApplyFlash(float amount)
        {
            _tinter.Apply(meshRenderer,
                amount > 0f ? UnityEngine.Color.Lerp(_renderColor, UnityEngine.Color.white, amount) : _renderColor);
        }

        static int PositionSeed(Vector3 localPosition) =>
            Mathf.RoundToInt(localPosition.x * HashQuantization) * HashX
            ^ Mathf.RoundToInt(localPosition.y * HashQuantization) * HashY
            ^ Mathf.RoundToInt(localPosition.z * HashQuantization) * HashZ;
    }
}
