using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CubeBlaster
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig gameConfig;
        [SerializeField] PaletteConfig paletteConfig;
        [SerializeField] VisualLibrary visualLibrary;

        [Header("Scene-authored refs")]
        [SerializeField] Camera mainCamera;
        [SerializeField] UniversalAdditionalCameraData cameraData;

        void Awake()
        {
            ApplyConfigs();
            ApplyScreenLock();
            ApplyApplicationSettings();
            ApplyCameraSettings();
            ApplyLighting();
        }

        void ApplyConfigs()
        {
            GameConfig.Use(gameConfig);
            PaletteConfig.Use(paletteConfig);
            VisualLibrary.Use(visualLibrary);
        }

        /// Runs before CameraRig.Awake and UIController.Initialize (execution order -100), so the
        /// camera is pillarboxed and the UI is sized against the portrait slice from the very
        /// first frame rather than snapping over on the second one.
        void ApplyScreenLock()
        {
            var config = GameConfig.Active;
            ScreenLayout.Lock(config.portraitLock ? config.portraitAspect : 0f);
        }

        void ApplyApplicationSettings()
        {
            Application.targetFrameRate = GameConfig.Active.targetFrameRate;
            QualitySettings.vSyncCount = 0;
        }

        void ApplyCameraSettings()
        {
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = PaletteConfig.Active.background;
            }
            if (cameraData == null) return;

            cameraData.renderPostProcessing = GameConfig.Active.postProcessing;
            if (cameraData.antialiasing == AntialiasingMode.None)
                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        void ApplyLighting()
        {
            var config = GameConfig.Active;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = config.ambientSky;
            RenderSettings.ambientEquatorColor = config.ambientEquator;
            RenderSettings.ambientGroundColor = config.ambientGround;
        }
    }
}
