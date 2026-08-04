using System.Collections;
using UnityEngine;

namespace CubeBlaster
{
    public class Shockwave : MonoBehaviour
    {
        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer quadRenderer;

        readonly RendererTinter _tinter = new RendererTinter();

        Coroutine _expand;

        public void Play(Vector3 position, Color color, GameConfig config)
        {
            if (_expand != null) StopCoroutine(_expand);

            transform.position = position + LiftTowardCamera(config.shockwaveCameraLift);
            transform.localScale = Vector3.one * config.shockwaveStartSize;
            gameObject.SetActive(true);
            _expand = StartCoroutine(Expand(color, config));
        }

        static Vector3 LiftTowardCamera(float lift)
        {
            if (lift <= 0f) return Vector3.zero;
            var camera = CameraRig.Main;
            return camera == null ? Vector3.zero : -camera.transform.forward * lift;
        }

        IEnumerator Expand(Color color, GameConfig config)
        {
            Color tint = Color.Lerp(color, Color.white, config.shockwaveWhiten);
            float duration = Mathf.Max(0.01f, config.shockwaveTime);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float spread = 1f - (1f - t) * (1f - t);

                transform.localScale = Vector3.one *
                    Mathf.Lerp(config.shockwaveStartSize, config.shockwaveEndSize, spread);
                tint.a = (1f - t) * config.shockwaveAlpha;
                _tinter.Apply(quadRenderer, tint);
                yield return null;
            }

            _expand = null;
            gameObject.SetActive(false);
        }
    }
}
