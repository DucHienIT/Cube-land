using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CubeBlaster.EditorTools
{

    public static class VisualAssetBaker
    {
        const string ArtDir = "Assets/_Project/Art";
        const string MeshDir = ArtDir + "/Meshes";
        const string TexDir = ArtDir + "/Textures";
        const string MatDir = ArtDir + "/Materials";
        const string VoxelMatDir = MatDir + "/Voxels";
        const string VoxelJitterMatDir = VoxelMatDir + "/Jitter";
        const string PrefabDir = "Assets/_Project/Prefabs";
        const string FxPrefabDir = PrefabDir + "/Fx";
        const string LibraryPath = "Assets/_Project/Resources/Config/VisualLibrary.asset";
        const string PostProfilePath = ArtDir + "/PostFX.asset";
        const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // The number is always warm white (ColorTools.LabelInk) and a heavy dark outline is what
        // separates it from the block, on every palette colour. Face dilate fattens the glyph
        // itself, so it stays modest — a TMP outline grows partly INWARD, and without some
        // dilate a thick one thins the white face to nothing. Dilate is what closes the
        // counters in 8/9/0, which is why the ratio here is deliberately outline-heavy
        // (a 2026-07-31 pass had to walk back dilate 0.08 for exactly that reason).
        const float LabelFaceDilate = 0.06f;
        const float LabelOutlineWidth = 0.30f;
        static readonly Color LabelOutlineColor = new Color(0.055f, 0.072f, 0.145f, 1f);
        static readonly Color LabelShadowColor = new Color(0.04f, 0.055f, 0.11f, 0.7f);

        // The rect sizes the multi-digit case (auto-size shrinks to fit it); the max sizes the
        // single-digit case (auto-size never grows past it). Both rects are well inside their
        // face because the OUTLINE RENDERS OUTSIDE THE LAYOUT BOUNDS — at 0.30 it adds roughly
        // a fifth again on each side, so a rect matched to the block face overhangs it. Measured
        // against the block: 0.52 in a 0.92 face lands the drawn number at ~0.65 of it.
        static readonly Vector2 BankLabelFit = new Vector2(0.52f, 0.52f);
        const float BankLabelSize = 0.115f;
        static readonly Vector2 GunLabelFit = new Vector2(0.50f, 0.40f);
        const float GunLabelSize = 0.080f;
        const float LabelAutoSizeMin = 0.5f;

        static bool _force;

        [MenuItem("Tools/Cube Blaster/Bake Visual Assets")]
        public static void Bake()
        {
            _force = false;
            Run();
        }

        [MenuItem("Tools/Cube Blaster/Force Rebake All (overwrites hand edits)")]
        public static void ForceRebake()
        {
            if (!EditorUtility.DisplayDialog("Force Rebake",
                "This overwrites ALL baked visual assets (materials, textures, mesh, FX prefabs) " +
                "with procedural defaults. Hand edits will be lost. Continue?", "Rebake", "Cancel"))
                return;
            _force = true;
            Run();
        }

        [MenuItem("Tools/Cube Blaster/Rebake Voxel Surface")]
        public static void RebakeVoxelSurface()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[VisualAssetBaker] Cannot rebake voxel surfaces during play mode.");
                return;
            }

            EnsureFolders();
            bool previousForce = _force;
            try
            {
                _force = true;
                Texture2D edgeTex = BakeTexture(TexDir + "/EdgeSheen.png", 128, EdgePixel, mipmaps: true);
                int materialCount = 0;
                for (int set = 0; set < 4; set++)
                {
                    var built = BakeVoxelMaterialSet(set, edgeTex);
                    materialCount += built.colors.Length + built.jitter.Length;
                }

                AssetDatabase.SaveAssets();
                Debug.Log(string.Format("[VisualAssetBaker] Rebuilt soft voxel surface: EdgeSheen + {0} materials.", materialCount));
            }
            finally
            {
                _force = previousForce;
            }
        }

        [MenuItem("Tools/Cube Blaster/Sync SSAO From Config")]
        public static void SyncSsao()
        {
            const string rendererPath = "Assets/Settings/UniversalRenderer.asset";
            var cfg = GameConfig.Active;
            int touched = 0;
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(rendererPath))
            {
                if (sub == null || sub.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                var so = new SerializedObject(sub);
                var s = so.FindProperty("m_Settings");
                if (s == null) continue;
                var pi = s.FindPropertyRelative("Intensity"); if (pi != null) pi.floatValue = cfg.aoIntensity;
                var pr = s.FindPropertyRelative("Radius"); if (pr != null) pr.floatValue = cfg.aoRadius;
                var pd = s.FindPropertyRelative("DirectLightingStrength"); if (pd != null) pd.floatValue = cfg.aoDirectStrength;
                var pa = s.FindPropertyRelative("AfterOpaque"); if (pa != null) pa.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(sub);
                touched++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log(touched > 0
                ? string.Format("[VisualAssetBaker] SSAO synced: intensity={0} radius={1} direct={2}",
                    cfg.aoIntensity, cfg.aoRadius, cfg.aoDirectStrength)
                : "[VisualAssetBaker] No ScreenSpaceAmbientOcclusion feature found on " + rendererPath);
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[VisualAssetBaker] Cannot bake during play mode — exit play mode first.");
                return;
            }
            EnsureFolders();

            Mesh rounded = BakeRoundedCubeMesh();
            Mesh gunBody = BakeGunBodyMesh();
            Mesh gunPuck = BakePuckMesh();
            Texture2D edgeTex = BakeTexture(TexDir + "/EdgeSheen.png", 128, EdgePixel, mipmaps: true);

            _bgBase = PaletteConfig.Active.background;
            _bgStrength = GameConfig.Active.bgGradientStrength;
            _bgRadius = GameConfig.Active.bgGradientRadius;
            _bgTint = GameConfig.Active.bgGradientTint;
            Texture2D bgTex = BakeTexture(TexDir + "/BgGradient.png", 256, BgPixel, mipmaps: false);
            if (bgTex != null)
            {

                var bgImp = (TextureImporter)AssetImporter.GetAtPath(TexDir + "/BgGradient.png");
                if (bgImp != null && bgImp.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    bgImp.textureCompression = TextureImporterCompression.Uncompressed;
                    bgImp.wrapMode = TextureWrapMode.Clamp;
                    bgImp.SaveAndReimport();
                    bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/BgGradient.png");
                }
            }
            Texture2D ringTex = BakeTexture(TexDir + "/FxRing.png", 64, RingPixel, mipmaps: true,
                alwaysRebuild: true);
            Texture2D dotTex = BakeTexture(TexDir + "/DartDot.png", 64, DotPixel, mipmaps: true,
                alwaysRebuild: true);

            var voxelSets = new List<VisualLibrary.MaterialSet>();
            for (int set = 0; set < 4; set++) voxelSets.Add(BakeVoxelMaterialSet(set, edgeTex));

            Material gunPart = BakeMaterial(MatDir + "/GunPart.mat", m => SetupToon(m, Color.white, null));
            Material gunHole = BakeMaterial(MatDir + "/GunHole.mat", m =>
            {
                SetupToon(m, new Color(0.05f, 0.06f, 0.10f), null);
                SetupCoolDarkToon(m,
                    new Color(0.05f, 0.08f, 0.16f),
                    new Color(0.02f, 0.03f, 0.06f),
                    new Color(0.04f, 0.06f, 0.10f));
            });
            Material slotPad = BakeMaterial(MatDir + "/SlotPad.mat", m =>
            {
                SetupToon(m, new Color(0.122f, 0.18f, 0.294f), edgeTex);
                SetupCoolDarkToon(m,
                    new Color(0.22f, 0.32f, 0.52f),
                    new Color(0.08f, 0.12f, 0.22f),
                    new Color(0.16f, 0.22f, 0.38f));
            });

            Material backdrop = BakeMaterial(MatDir + "/Backdrop.mat", m =>
            {
                m.shader = Shader.Find("Universal Render Pipeline/Unlit");
                SetBaseColor(m, Color.white);
                if (bgTex != null) { m.mainTexture = bgTex; m.SetTexture("_BaseMap", bgTex); }
                m.SetFloat("_ZWrite", 0f);
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;
            });
            // The bullet is a camera-facing DOT, not a sphere. It was the built-in sphere at 768
            // triangles, on an unlit material — so the geometry bought literally no shading — and
            // four guns keep ~90 darts in the air, which is 69k triangles for balls that render a
            // handful of pixels wide, nearly 3x the whole sculpture. Always re-applied because
            // the shader has to match the mesh the prefab uses.
            Material dartBullet = BakeMaterial(MatDir + "/DartBullet.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
                m.mainTexture = dotTex;
                SetBaseColor(m, Color.white);
            }, alwaysApply: true);
            Material dartTrail = BakeMaterial(MatDir + "/DartTrail.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
            });
            Material fxRing = BakeMaterial(MatDir + "/FxRing.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
                m.mainTexture = ringTex;
            });
            Material labelMat = BakeLabelMaterial();

            Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            // Voxels render on Unity's built-in cube — 12 triangles against RoundedCube's ~700.
            // A level is 1000-2000 cubes, so the bevel that sells a hero prop up close costs
            // over a million triangles on the sculpture while spanning a couple of pixels.
            // The props (slot rims, bank blocks) are a handful of objects seen large and keep
            // the rounded mesh.
            Mesh unitCube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            Shockwave shockwave = BakeShockwavePrefab(quad, fxRing);

            BakeVoxelCubePrefab(unitCube, voxelSets[0].colors[0]);
            BakeDartPrefab(quad, dartBullet, dartTrail);
            BakeGunPrefab(rounded, gunBody, gunPuck, gunPart, gunHole, font, labelMat);
            BakeGunSlotPrefab(rounded, slotPad, gunHole);
            BakeBankBlockPrefab(rounded, voxelSets[0].colors[0], font, labelMat);

            var lib = AssetDatabase.LoadAssetAtPath<VisualLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VisualLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.voxelSets = voxelSets.ToArray();
            lib.backdrop = backdrop;
            lib.slotPad = slotPad;
            lib.dartBullet = dartBullet;
            lib.dartTrail = dartTrail;
            lib.shockwavePrefab = shockwave;
            EditorUtility.SetDirty(lib);

            BakePostFx(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VisualAssetBaker] Bake complete. All visuals are now assets/prefabs — tweak them in " +
                      ArtDir + ", " + PrefabDir + " and " + LibraryPath);
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project", "Art");
            EnsureFolder(ArtDir, "Meshes");
            EnsureFolder(ArtDir, "Textures");
            EnsureFolder(ArtDir, "Materials");
            EnsureFolder(MatDir, "Voxels");
            EnsureFolder(VoxelMatDir, "Jitter");
            EnsureFolder(PrefabDir, "Fx");
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        static Mesh BakeRoundedCubeMesh()
        {

            const int maxSegmentsUnderBudget = 3;
            int segments = Mathf.Clamp(GameConfig.Active.voxelRoundSegments, 2, maxSegmentsUnderBudget);
            Mesh mesh = BakeRoundedMesh(MeshDir + "/RoundedCube.asset",
                GameConfig.Active.voxelCornerRadius, segments);
            int triangleCount = (int)(mesh.GetIndexCount(0) / 3);
            if (triangleCount >= 750)
                Debug.LogError("[VisualAssetBaker] RoundedCube exceeded its strict <750 triangle budget: " + triangleCount);
            return mesh;
        }

        static Mesh BakeGunBodyMesh()
        {
            return BakeRoundedMesh(MeshDir + "/GunBody.asset",
                GameConfig.Active.gunBodyRadius, GameConfig.Active.gunBodySegments);
        }

        static Mesh BakePuckMesh()
        {
            string path = MeshDir + "/GunPuck.asset";
            Mesh built = BuildPuck(GameConfig.Active.gunPuckRim, GameConfig.Active.gunPuckSides);
            return StoreMesh(path, built);
        }

        static Mesh BuildPuck(float rim, int sides)
        {
            rim = Mathf.Clamp(rim, 0.02f, 0.49f);
            sides = Mathf.Max(6, sides);
            int arc = Mathf.Max(2, Mathf.RoundToInt(sides * 0.2f));
            float flat = 0.5f - rim;

            var prof = new List<Vector4>();
            prof.Add(new Vector4(0f, -0.5f, 0f, -1f));
            prof.Add(new Vector4(flat, -0.5f, 0f, -1f));
            for (int i = 1; i <= arc; i++)
            {
                float a = Mathf.PI * 0.5f * (1f - (float)i / arc);
                prof.Add(new Vector4(flat + rim * Mathf.Cos(a), -flat - rim * Mathf.Sin(a),
                                     Mathf.Cos(a), -Mathf.Sin(a)));
            }
            prof.Add(new Vector4(0.5f, flat, 1f, 0f));
            for (int i = 1; i <= arc; i++)
            {
                float a = Mathf.PI * 0.5f * ((float)i / arc);
                prof.Add(new Vector4(flat + rim * Mathf.Cos(a), flat + rim * Mathf.Sin(a),
                                     Mathf.Cos(a), Mathf.Sin(a)));
            }
            prof.Add(new Vector4(0f, 0.5f, 0f, 1f));

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int j = 0; j < prof.Count; j++)
            {
                Vector4 p = prof[j];
                float v = (float)j / (prof.Count - 1);
                for (int i = 0; i <= sides; i++)
                {
                    float t = (float)i / sides;
                    float th = t * Mathf.PI * 2f;
                    float c = Mathf.Cos(th), s = Mathf.Sin(th);
                    verts.Add(new Vector3(p.x * c, p.y, p.x * s));
                    norms.Add(new Vector3(p.z * c, p.w, p.z * s).normalized);
                    uvs.Add(new Vector2(t, v));
                }
            }

            int stride = sides + 1;
            for (int j = 0; j < prof.Count - 1; j++)
            {
                bool lowDegenerate = prof[j].x <= 1e-5f;
                bool highDegenerate = prof[j + 1].x <= 1e-5f;
                for (int i = 0; i < sides; i++)
                {
                    int a = j * stride + i, b = a + 1;
                    int c = a + stride, d = c + 1;

                    if (!lowDegenerate) { tris.Add(a); tris.Add(c); tris.Add(b); }
                    if (!highDegenerate) { tris.Add(b); tris.Add(c); tris.Add(d); }
                }
            }

            var mesh = new Mesh { name = "GunPuck" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BakeRoundedMesh(string path, float radius, int segments)
        {

            return StoreMesh(path, BuildRoundedCube(
                Mathf.Clamp(radius, 0.02f, 0.49f),
                Mathf.Max(2, segments)));
        }

        static Mesh StoreMesh(string path, Mesh built)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.indexFormat = built.indexFormat;
                existing.vertices = built.vertices;
                existing.normals = built.normals;
                existing.uv = built.uv;
                existing.triangles = built.triangles;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(built);
                return existing;
            }
            AssetDatabase.CreateAsset(built, path);
            return built;
        }

        static Mesh BuildRoundedCube(float r, int band)
        {
            float flat = 0.5f - r;

            var raw = new List<float>();
            for (int i = 0; i <= band; i++) raw.Add(-0.5f + r * ((float)i / band));

            for (int i = 0; i <= band; i++) raw.Add(flat + r * ((float)i / band));
            raw.Sort();
            var s = new List<float>();
            foreach (var v in raw) if (s.Count == 0 || Mathf.Abs(v - s[s.Count - 1]) > 1e-4f) s.Add(v);
            int n = s.Count;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3[][] faces =
            {
                new[]{ Vector3.right,   Vector3.up,      Vector3.forward },
                new[]{ Vector3.left,    Vector3.forward, Vector3.up      },
                new[]{ Vector3.up,      Vector3.forward, Vector3.right   },
                new[]{ Vector3.down,    Vector3.right,   Vector3.forward },
                new[]{ Vector3.forward, Vector3.right,   Vector3.up      },
                new[]{ Vector3.back,    Vector3.up,      Vector3.right   },
            };

            foreach (var f in faces)
            {
                Vector3 nrm = f[0], uA = f[1], vA = f[2];
                int baseIdx = verts.Count;
                for (int iy = 0; iy < n; iy++)
                    for (int ix = 0; ix < n; ix++)
                    {
                        Vector3 p = uA * s[ix] + vA * s[iy] + nrm * 0.5f;
                        Vector3 inner = new Vector3(
                            Mathf.Clamp(p.x, -flat, flat),
                            Mathf.Clamp(p.y, -flat, flat),
                            Mathf.Clamp(p.z, -flat, flat));
                        Vector3 d = p - inner;
                        Vector3 nn = d.sqrMagnitude > 1e-8f ? d.normalized : nrm;
                        verts.Add(inner + nn * r);
                        norms.Add(nn);
                        uvs.Add(new Vector2(s[ix] + 0.5f, s[iy] + 0.5f));
                    }
                for (int iy = 0; iy < n - 1; iy++)
                    for (int ix = 0; ix < n - 1; ix++)
                    {
                        int i0 = baseIdx + iy * n + ix;
                        int i1 = i0 + 1, i2 = i0 + n, i3 = i2 + 1;

                        tris.Add(i0); tris.Add(i1); tris.Add(i2);
                        tris.Add(i2); tris.Add(i1); tris.Add(i3);
                    }
            }

            var m = new Mesh { name = "RoundedCube" };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        delegate Color32 PixelFn(float u, float v);

        static Texture2D BakeTexture(string path, int size, PixelFn fn, bool mipmaps,
            bool alwaysRebuild = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing == null || _force || alwaysRebuild)
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        px[y * size + x] = fn((x + 0.5f) / size, (y + 0.5f) / size);
                tex.SetPixels32(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            bool uncompressed = path == TexDir + "/EdgeSheen.png";
            if (imp.wrapMode == TextureWrapMode.Clamp
                && imp.filterMode == FilterMode.Bilinear
                && imp.mipmapEnabled == mipmaps
                && imp.alphaIsTransparency
                && (!uncompressed || imp.textureCompression == TextureImporterCompression.Uncompressed))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = mipmaps;
            imp.alphaIsTransparency = true;
            if (uncompressed) imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color32 EdgePixel(float u, float v)
        {
            const float plateLow = 0.90f, plateHigh = 0.98f, sigma = 0.42f;
            float du = u - 0.5f, dv = v - 0.5f;
            float lift = Mathf.Exp(-(du * du + dv * dv) / (2f * sigma * sigma));
            float value = Mathf.Lerp(plateLow, plateHigh, lift);
            byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
            return new Color32(g, g, g, 255);
        }

        static Color32 BgPixel(float u, float v)
        {
            float du = (u - 0.5f), dv = (v - 0.5f);
            float dist = Mathf.Sqrt(du * du + dv * dv) / Mathf.Max(0.05f, _bgRadius);

            float t = Mathf.Clamp01(1f - dist);
            t = t * t * t * (t * (t * 6f - 15f) + 10f);

            Color baseCol = _bgBase;
            float lift = 1f + _bgStrength * t;
            Color c = new Color(baseCol.r * lift, baseCol.g * lift, baseCol.b * lift, 1f);

            if (_bgTint > 0f)
            {
                Color rich = new Color(baseCol.r * 0.72f, baseCol.g * 0.94f, Mathf.Min(1f, baseCol.b * 1.22f), 1f);
                c = Color.Lerp(c, rich, _bgTint * t);
            }
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), 255);
        }

        static float _bgStrength, _bgRadius, _bgTint;
        static Color _bgBase = new Color(0.149f, 0.231f, 0.396f);

        /// Solid white core with a soft rim — the dart bullet, drawn on a camera-facing quad.
        static Color32 DotPixel(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.70f, 1f, d));
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }

        static Color32 RingPixel(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.70f) / 0.26f);
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(band * 1.35f));
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }

        /// `alwaysApply` is for materials whose settings are DERIVED, not hand-tuned art — the
        /// same reasoning as RoundedCube.asset and NumberLabel.mat. Without it a setup change
        /// only lands on a Force Rebake All, which resets every other material to defaults.
        static Material BakeMaterial(string path, System.Action<Material> setup, bool alwaysApply = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && !_force && !alwaysApply) return existing;
            if (existing != null)
            {
                setup(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var mat = new Material(Shader.Find("Toony Colors Pro 2/Hybrid Shader"));
            setup(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void SetupToon(Material mat, Color color, Texture2D tex)
        {
            var shader = Shader.Find("Toony Colors Pro 2/Hybrid Shader");
            if (shader != null) mat.shader = shader;
            SetBaseColor(mat, color);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }

            var cfg = GameConfig.Active;
            if (mat.HasProperty("_HColor")) mat.SetColor("_HColor", cfg.toonHighlight);
            if (mat.HasProperty("_SColor")) mat.SetColor("_SColor", cfg.toonShadow);
            if (mat.HasProperty("_RampThreshold")) mat.SetFloat("_RampThreshold", cfg.toonRampThreshold);
            if (mat.HasProperty("_RampSmoothing")) mat.SetFloat("_RampSmoothing", cfg.toonRampSmoothing);
            mat.EnableKeyword("TCP2_SHADOW_LIGHT_COLOR");

            mat.EnableKeyword("TCP2_SPECULAR");
            mat.EnableKeyword("TCP2_SPECULAR_STYLIZED");
            if (mat.HasProperty("_UseSpecular")) mat.SetFloat("_UseSpecular", 1f);
            if (mat.HasProperty("_SpecularType")) mat.SetFloat("_SpecularType", 1f);
            if (mat.HasProperty("_SpecularColor")) mat.SetColor("_SpecularColor", cfg.toonSpecColor);
            if (mat.HasProperty("_SpecularToonSize")) mat.SetFloat("_SpecularToonSize", cfg.toonSpecSize);
            if (mat.HasProperty("_SpecularToonSmoothness")) mat.SetFloat("_SpecularToonSmoothness", cfg.toonSpecSmoothing);
        }

        /// Bakes one palette set's voxel materials: the per-slot base .mat plus the per-block
        /// colour-jitter variants of each.
        ///
        /// The jitter used to be a MaterialPropertyBlock written onto every cube. A property
        /// block evicts its renderer from the SRP Batcher, which was free at 450 cubes and is
        /// not at 2000 — it means a full material bind per cube, every frame. Separate .mat
        /// assets sharing one shader stay in a single batch, so the variation is baked instead.
        static VisualLibrary.MaterialSet BakeVoxelMaterialSet(int set, Texture2D edgeTex)
        {
            var cfg = GameConfig.Active;
            var palette = PaletteConfig.Active.GetVoxelSet(set);
            var colors = new Material[palette.Length];
            var jitter = new Material[palette.Length * ColorTools.JitterVariants];

            for (int slot = 0; slot < palette.Length; slot++)
            {
                Color baseColor = palette[slot];
                colors[slot] = BakeMaterial(string.Format("{0}/Voxel_S{1}_C{2}.mat", VoxelMatDir, set, slot),
                    m => SetupVoxelToon(m, baseColor, edgeTex));

                for (int variant = 0; variant < ColorTools.JitterVariants; variant++)
                {
                    int index = slot * ColorTools.JitterVariants + variant;
                    float offset = ColorTools.GetJitterOffset(variant);
                    if (Mathf.Approximately(offset, 0f))
                    {
                        jitter[index] = colors[slot];
                        continue;
                    }
                    jitter[index] = BakeVoxelJitterMaterial(
                        string.Format("{0}/Voxel_S{1}_C{2}_J{3}.mat", VoxelJitterMatDir, set, slot, variant),
                        colors[slot],
                        ColorTools.Jitter(baseColor, variant, cfg.voxelHueJitter, cfg.voxelValueJitter));
                }
            }
            return new VisualLibrary.MaterialSet { colors = colors, jitter = jitter };
        }

        /// A jitter variant has nothing hand-editable of its own — it is "the slot material,
        /// one quantised shade off" — so it is re-derived from its source on EVERY bake rather
        /// than skipped when it already exists. That is what lets the hand-held white voxel
        /// materials (see the art passes in CLAUDE.md) carry their edits into their variants
        /// without a Force Rebake All resetting every other material.
        static Material BakeVoxelJitterMaterial(string path, Material source, Color shade)
        {
            if (source == null) return null;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }

            SetBaseColor(mat, shade);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void SetupVoxelToon(Material mat, Color color, Texture2D tex)
        {
            SetupToon(mat, color, tex);

            var cfg = GameConfig.Active;
            Color voxelHighlight = Color.Lerp(cfg.toonShadow, cfg.toonHighlight,
                cfg.voxelHighlightStrength);
            voxelHighlight.a = 1f;
            if (mat.HasProperty("_HColor")) mat.SetColor("_HColor", voxelHighlight);
            if (mat.HasProperty("_RampSmoothing"))
                mat.SetFloat("_RampSmoothing", cfg.voxelLightRampSmoothing);

            if (mat.HasProperty("_UseSpecular")) mat.SetFloat("_UseSpecular", 0f);
            if (mat.HasProperty("_SpecularType")) mat.SetFloat("_SpecularType", 0f);
            if (mat.HasProperty("_UseRim")) mat.SetFloat("_UseRim", 0f);
            if (mat.HasProperty("_UseRimLightMask")) mat.SetFloat("_UseRimLightMask", 0f);
            if (mat.HasProperty("_UseReflections")) mat.SetFloat("_UseReflections", 0f);
            if (mat.HasProperty("_UseFresnelReflections")) mat.SetFloat("_UseFresnelReflections", 0f);

            string[] hardHighlightKeywords =
            {
                "TCP2_SPECULAR", "TCP2_SPECULAR_STYLIZED", "TCP2_SPECULAR_CRISP",
                "TCP2_RIM_LIGHTING", "TCP2_RIM_LIGHTING_LIGHTMASK",
                "TCP2_REFLECTIONS", "TCP2_REFLECTIONS_FRESNEL"
            };
            foreach (string keyword in hardHighlightKeywords) mat.DisableKeyword(keyword);
            mat.enableInstancing = true;
        }

        static void SetupCoolDarkToon(Material mat, Color highlight, Color shadow, Color specular)
        {
            if (mat.HasProperty("_HColor")) mat.SetColor("_HColor", highlight);
            if (mat.HasProperty("_SColor")) mat.SetColor("_SColor", shadow);
            if (mat.HasProperty("_SpecularColor")) mat.SetColor("_SpecularColor", specular);
        }

        static void SetBaseColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
        }

        static Material BakeLabelMaterial()
        {
            string path = MatDir + "/NumberLabel.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogWarning("[VisualAssetBaker] TMP font not found at " + FontPath + " — label preset skipped.");
                return existing;
            }
            Material mat = existing != null ? existing : new Material(font.material);
            mat.shader = font.material.shader;
            mat.CopyPropertiesFromMaterial(font.material);

            mat.SetFloat(ShaderUtilities.ID_FaceDilate, LabelFaceDilate);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, LabelOutlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, LabelOutlineColor);

            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, LabelShadowColor);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.55f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.05f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);

            if (existing == null) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        static Shockwave BakeShockwavePrefab(Mesh quad, Material ringMat)
        {
            string path = FxPrefabDir + "/Shockwave.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var seed = new GameObject("Shockwave");
                PrefabUtility.SaveAsPrefabAsset(seed, path);
                Object.DestroyImmediate(seed);
            }

            EditPrefab(path, root =>
            {
                var mf = root.GetComponent<MeshFilter>();
                if (mf == null) mf = root.AddComponent<MeshFilter>();
                mf.sharedMesh = quad;

                var mr = root.GetComponent<MeshRenderer>();
                if (mr == null) mr = root.AddComponent<MeshRenderer>();
                mr.sharedMaterial = ringMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                var billboard = root.GetComponent<Billboard>();
                if (billboard == null) billboard = root.AddComponent<Billboard>();
                billboard.towardCamera = 0f;

                var shockwave = root.GetComponent<Shockwave>();
                if (shockwave == null) shockwave = root.AddComponent<Shockwave>();
                SetRef(shockwave, "quadRenderer", mr);
            });
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Shockwave>();
        }

        static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ClearChildren(GameObject root)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        static GameObject AddMeshChild(Transform parent, string name, Mesh mesh, Material mat,
            Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static TextMeshPro AddLabel(Transform parent, string text, float size, TMP_FontAsset font,
            Material labelMat, float towardCamera, Vector3 localPos, Vector2 fitSize = default)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.color = Color.white;
            tm.fontSize = size * 64f;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.enableWordWrapping = false;
            tm.margin = Vector4.zero;

            bool fits = fitSize.x > 0f && fitSize.y > 0f;
            tm.rectTransform.sizeDelta = fits ? fitSize : new Vector2(2f, 2f);
            if (fits)
            {
                tm.enableAutoSizing = true;
                tm.fontSizeMax = size * 64f;
                tm.fontSizeMin = size * 64f * LabelAutoSizeMin;
            }

            if (font != null) tm.font = font;
            if (labelMat != null) tm.fontSharedMaterial = labelMat;
            var b = go.AddComponent<Billboard>();
            b.towardCamera = towardCamera;
            return tm;
        }

        static void SetRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning("[VisualAssetBaker] field '" + fieldName + "' not found on " + target);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray(Object target, string fieldName, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning("[VisualAssetBaker] field '" + fieldName + "' not found on " + target);
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BakeVoxelCubePrefab(Mesh rounded, Material defaultMat)
        {
            EditPrefab(PrefabDir + "/VoxelCube.prefab", root =>
            {
                var mf = root.GetComponent<MeshFilter>();
                if (mf == null) mf = root.AddComponent<MeshFilter>();
                mf.sharedMesh = rounded;
                var mr = root.GetComponent<MeshRenderer>();
                if (mr == null) mr = root.AddComponent<MeshRenderer>();
                mr.sharedMaterial = defaultMat;

                var rb = root.GetComponent<Rigidbody>();
                if (rb != null) Object.DestroyImmediate(rb, true);
                var col = root.GetComponent<BoxCollider>();
                if (col != null) Object.DestroyImmediate(col, true);

                var cube = root.GetComponent<VoxelCube>();
                SetRef(cube, "meshRenderer", mr);
            });
        }

        static void BakeDartPrefab(Mesh quad, Material bulletMat, Material trailMat)
        {
            EditPrefab(PrefabDir + "/Dart.prefab", root =>
            {
                ClearChildren(root);

                var trail = root.GetComponent<TrailRenderer>();
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = GameConfig.Active.dartTrailTime;
                trail.startWidth = GameConfig.Active.dartTrailWidth;
                trail.endWidth = 0f;
                trail.numCapVertices = 4;
                trail.numCornerVertices = 2;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;

                var bullet = AddMeshChild(root.transform, "Bullet", quad, bulletMat,
                    Vector3.zero, Quaternion.identity, Vector3.one * 0.22f);
                var mr = bullet.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                // Dart.Initialize aims the ROOT along the muzzle so the trail streaks correctly;
                // the bullet is billboarded independently so the dot always faces the camera.
                var billboard = bullet.GetComponent<Billboard>();
                if (billboard == null) billboard = bullet.AddComponent<Billboard>();
                billboard.towardCamera = 0f;

                var dart = root.GetComponent<Dart>();
                SetRef(dart, "trail", trail);
                SetRef(dart, "bulletRenderer", mr);
            });
        }

        static void BakeGunPrefab(Mesh roundedCube, Mesh gunBody, Mesh puck,
            Material gunPart, Material gunHole, TMP_FontAsset font, Material labelMat)
        {
            EditPrefab(PrefabDir + "/Gun.prefab", root =>
            {
                ClearChildren(root);

                var cfg = GameConfig.Active;
                var size = cfg.gunBodySize;
                float bl = cfg.gunBarrelLength, br = cfg.gunBarrelRadius;

                const float groundY = -0.52f;
                var rig = new GameObject("Rig").transform;
                rig.SetParent(root.transform, false);
                rig.localPosition = new Vector3(0f, groundY, 0f);

                float bodyY = size.y * 0.5f;
                var body = AddMeshChild(rig, "Body", roundedCube, gunPart,
                    new Vector3(0f, bodyY, 0f), Quaternion.identity, size);

                var barrel = new GameObject("Barrel").transform;
                barrel.SetParent(rig, false);
                barrel.localPosition = new Vector3(0f, bodyY + size.y * 0.06f, size.z * 0.42f);
                barrel.localRotation = Quaternion.Euler(-cfg.gunBarrelElevation, 0f, 0f);

                var lie = Quaternion.Euler(90f, 0f, 0f);
                Vector3 Round(float dia, float thick) { return new Vector3(dia, thick, dia); }

                var collar = AddMeshChild(barrel, "Collar", puck, gunPart,
                    new Vector3(0f, 0f, bl * 0.22f), lie, Round(br * 2.90f, bl * 0.24f));
                var tube = AddMeshChild(barrel, "Tube", puck, gunPart,
                    new Vector3(0f, 0f, bl * 0.56f), lie, Round(br * 2f, bl * 0.64f));
                var band = AddMeshChild(barrel, "Band", puck, gunPart,
                    new Vector3(0f, 0f, bl * 0.79f), lie, Round(br * 2.50f, bl * 0.15f));
                var rim = AddMeshChild(barrel, "Rim", puck, gunPart,
                    new Vector3(0f, 0f, bl * 0.93f), lie, Round(br * 2.76f, bl * 0.18f));

                AddMeshChild(barrel, "Bore", puck, gunHole,
                    new Vector3(0f, 0f, bl * 0.95f), lie, Round(br * 1.45f, bl * 0.26f));

                var tip = new GameObject("BarrelTip").transform;
                tip.SetParent(barrel, false);
                tip.localPosition = new Vector3(0f, 0f, bl * 1.10f);

                // Cheap mesh, not gunBody: the nub is a ~0.2-unit tab that renders around 20px,
                // and gunBody is the 1452-triangle high-bevel mesh — a third of the whole cannon's
                // geometry for something whose bevel is sub-pixel. (The BODY is the piece that
                // uses roundedCube, which is the reverse of what the mesh names suggest; left
                // alone because changing it changes the cannon's silhouette.)
                var nub = AddMeshChild(rig, "Nub", roundedCube, gunPart,
                    new Vector3(0f, bodyY - size.y * 0.26f, -size.z * 0.5f - 0.04f), Quaternion.identity,
                    new Vector3(size.x * 0.24f, size.y * 0.20f, 0.12f));

                var label = AddLabel(root.transform, "0", GunLabelSize, font, labelMat, 0.80f,
                    new Vector3(0f, groundY + bodyY, 0f), GunLabelFit);

                var gun = root.GetComponent<Gun>();
                SetRef(gun, "bodyPivot", rig);
                SetRef(gun, "barrelTip", tip);
                SetRef(gun, "label", label);
                SetRef(gun, "bodyRenderer", body.GetComponent<MeshRenderer>());

                SetRefArray(gun, "partRenderers",
                    collar.GetComponent<MeshRenderer>(), tube.GetComponent<MeshRenderer>(),
                    band.GetComponent<MeshRenderer>(), rim.GetComponent<MeshRenderer>(),
                    nub.GetComponent<MeshRenderer>());
            });
        }

        static void BakeGunSlotPrefab(Mesh rounded, Material slotPad, Material slotWell)
        {
            EditPrefab(PrefabDir + "/GunSlot.prefab", root =>
            {
                ClearChildren(root);

                const float half = 0.56f;
                const float bar = 0.16f;
                const float outer = half * 2f + bar;
                const float rimY = -0.54f, rimH = 0.30f;

                var rimN = AddMeshChild(root.transform, "RimN", rounded, slotPad,
                    new Vector3(0f, rimY, half), Quaternion.identity, new Vector3(outer, rimH, bar));
                var rimS = AddMeshChild(root.transform, "RimS", rounded, slotPad,
                    new Vector3(0f, rimY, -half), Quaternion.identity, new Vector3(outer, rimH, bar));
                var rimE = AddMeshChild(root.transform, "RimE", rounded, slotPad,
                    new Vector3(half, rimY, 0f), Quaternion.identity, new Vector3(bar, rimH, outer));
                var rimW = AddMeshChild(root.transform, "RimW", rounded, slotPad,
                    new Vector3(-half, rimY, 0f), Quaternion.identity, new Vector3(bar, rimH, outer));

                AddMeshChild(root.transform, "Floor", rounded, slotWell,
                    new Vector3(0f, -0.70f, 0f), Quaternion.identity, new Vector3(1.10f, 0.26f, 1.10f));

                var slotSo = new SerializedObject(root.GetComponent<GunSlot>());
                var pads = slotSo.FindProperty("padRenderers");
                pads.arraySize = 4;
                pads.GetArrayElementAtIndex(0).objectReferenceValue = rimN.GetComponent<MeshRenderer>();
                pads.GetArrayElementAtIndex(1).objectReferenceValue = rimS.GetComponent<MeshRenderer>();
                pads.GetArrayElementAtIndex(2).objectReferenceValue = rimE.GetComponent<MeshRenderer>();
                pads.GetArrayElementAtIndex(3).objectReferenceValue = rimW.GetComponent<MeshRenderer>();
                slotSo.ApplyModifiedPropertiesWithoutUndo();

                var box = root.GetComponent<BoxCollider>();
                if (box == null) box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1.1f, 1.2f, 1.1f);
            });
        }

        static void BakeBankBlockPrefab(Mesh rounded, Material defaultMat, TMP_FontAsset font, Material labelMat)
        {
            EditPrefab(PrefabDir + "/BankBlock.prefab", root =>
            {
                ClearChildren(root);

                var box = root.GetComponent<BoxCollider>();
                if (box == null) box = root.AddComponent<BoxCollider>();
                box.isTrigger = false;
                box.size = new Vector3(0.95f, 0.95f, 0.95f);

                var cube = AddMeshChild(root.transform, "Cube", rounded, defaultMat,
                    Vector3.zero, Quaternion.identity, Vector3.one * 0.92f);

                var label = AddLabel(root.transform, "0", BankLabelSize, font, labelMat, 0.9f,
                    Vector3.zero, BankLabelFit);

                var block = root.GetComponent<BankBlock>();
                SetRef(block, "cubeRenderer", cube.GetComponent<MeshRenderer>());
                SetRef(block, "label", label);
                SetRef(block, "boxCollider", box);
            });
        }

        static void BakePostFx(VisualLibrary lib)
        {
            var cfg = GameConfig.Active;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile == null || _force)
            {
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, PostProfilePath);
                }

                var bloom = GetOrAddOverride<Bloom>(profile);
                bloom.intensity.Override(cfg.postBloomIntensity);
                bloom.threshold.Override(1.1f);

                var vignette = GetOrAddOverride<Vignette>(profile);
                vignette.intensity.Override(cfg.postVignette);
                vignette.color.Override(new Color(0.05f, 0.08f, 0.16f));

                var adjust = GetOrAddOverride<ColorAdjustments>(profile);
                adjust.saturation.Override(cfg.postSaturation);
                adjust.contrast.Override(cfg.postContrast);
                adjust.postExposure.Override(cfg.postExposure);

                EditorUtility.SetDirty(profile);
            }

            var scene = EditorSceneManager.GetActiveScene();
            bool openedHere = false;
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            GameObject postGo = null;
            GameBootstrap bootstrap = null;
            Camera cam = null;
            CameraRig rig = null;
            BoardInput input = null;
            UIController ui = null;
            AudioManager audio = null;
            foreach (var rootGo in scene.GetRootGameObjects())
            {
                if (rootGo.name == "PostFX") postGo = rootGo;
                if (bootstrap == null) bootstrap = rootGo.GetComponentInChildren<GameBootstrap>(true);
                if (cam == null) cam = rootGo.GetComponentInChildren<Camera>(true);
                if (rig == null) rig = rootGo.GetComponentInChildren<CameraRig>(true);
                if (input == null) input = rootGo.GetComponentInChildren<BoardInput>(true);
                if (ui == null) ui = rootGo.GetComponentInChildren<UIController>(true);
                if (audio == null) audio = rootGo.GetComponentInChildren<AudioManager>(true);
            }
            if (postGo == null)
            {
                postGo = new GameObject("PostFX");
                EditorSceneManager.MoveGameObjectToScene(postGo, scene);
            }
            var volume = postGo.GetComponent<Volume>();
            if (volume == null) volume = postGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            if (bootstrap != null) SetRef(bootstrap, "visualLibrary", lib);
            else Debug.LogWarning("[VisualAssetBaker] GameBootstrap not found in scene — visualLibrary not wired (Resources fallback still works).");

            if (cam != null)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                if (bootstrap != null)
                {
                    SetRef(bootstrap, "mainCamera", cam);
                    SetRef(bootstrap, "cameraData", camData);
                }
                if (rig != null) SetRef(rig, "sceneCamera", cam);
                if (input != null) SetRef(input, "sceneCamera", cam);

                var oldBackdrop = cam.transform.Find("Backdrop");
                if (oldBackdrop != null) Object.DestroyImmediate(oldBackdrop.gameObject);
                if (lib.backdrop != null)
                {
                    var bgGo = new GameObject("Backdrop");
                    bgGo.transform.SetParent(cam.transform, false);
                    var mf = bgGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                    var mr = bgGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = lib.backdrop;

                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    var bd = bgGo.AddComponent<Backdrop>();
                    SetRef(bd, "sceneCamera", cam);
                }
            }
            else Debug.LogWarning("[VisualAssetBaker] No Camera found in scene — camera refs not wired.");

            if (ui != null) SetRef(ui, "root", ui.GetComponent<RectTransform>());

            if (audio != null)
            {
                var sources = audio.GetComponents<AudioSource>();
                AudioSource sfx = sources.Length > 0 ? sources[0] : audio.gameObject.AddComponent<AudioSource>();
                AudioSource music = sources.Length > 1 ? sources[1] : audio.gameObject.AddComponent<AudioSource>();
                sfx.playOnAwake = false;
                music.playOnAwake = false; music.loop = true; music.volume = 0.28f;
                SetRef(audio, "sfxSource", sfx);
                SetRef(audio, "musicSource", music);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T comp;
            if (profile.TryGet(out comp)) return comp;
            comp = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(comp, profile);
            return comp;
        }
    }
}
