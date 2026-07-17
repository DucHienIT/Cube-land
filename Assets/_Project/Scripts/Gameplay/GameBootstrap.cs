using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CubeBlaster
{
    /// <summary>
    /// Applies config assets + frame rate + camera background early. Creates nothing — persistent
    /// objects are authored in the scene, and post-processing lives in the scene "PostFX" Volume
    /// (profile asset: Assets/_Project/Art/PostFX.asset — hand-tune it there).
    /// Runs before everything else.
    /// </summary>
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
            Cfg.SetActive(gameConfig);
            Palette.SetActive(paletteConfig);
            Visuals.SetActive(visualLibrary);

            Application.targetFrameRate = Cfg.Active.targetFrameRate;
            QualitySettings.vSyncCount = 0;

            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Palette.Background;
            }
            // Post-processing itself is scene-authored (PostFX Volume); here we only make sure
            // the camera renders it and has AA on.
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = Cfg.Active.postProcessing;
                if (cameraData.antialiasing == AntialiasingMode.None)
                    cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
            RenderSettings.fog = false;

            // Trilight ambient: warm from above, cool at the sides, navy-purple from below —
            // shadowed faces keep a colored hue (red → red-purple) instead of going gray-black.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Cfg.Active.ambientSky;
            RenderSettings.ambientEquatorColor = Cfg.Active.ambientEquator;
            RenderSettings.ambientGroundColor = Cfg.Active.ambientGround;
        }
    }
}
