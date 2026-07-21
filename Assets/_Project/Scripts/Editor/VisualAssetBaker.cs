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
    /// <summary>
    /// One-shot baker that turns every formerly-procedural visual (meshes, textures, materials,
    /// particle systems, gun/dart/bank visual subtrees, post-processing profile) into real assets
    /// and fully-authored prefabs, wired into the VisualLibrary.
    ///
    /// "Bake Visual Assets" is non-destructive for hand-edits: existing .mat/.png/mesh assets are
    /// kept as-is (only missing ones are created); prefab subtrees ARE rebuilt each run.
    /// "Force Rebake All" recreates everything from the procedural defaults.
    /// </summary>
    public static class VisualAssetBaker
    {
        const string ArtDir = "Assets/_Project/Art";
        const string MeshDir = ArtDir + "/Meshes";
        const string TexDir = ArtDir + "/Textures";
        const string MatDir = ArtDir + "/Materials";
        const string VoxelMatDir = MatDir + "/Voxels";
        const string PrefabDir = "Assets/_Project/Prefabs";
        const string FxPrefabDir = PrefabDir + "/Fx";
        const string LibraryPath = "Assets/_Project/Resources/Config/VisualLibrary.asset";
        const string PostProfilePath = ArtDir + "/PostFX.asset";
        const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

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

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[VisualAssetBaker] Cannot bake during play mode — exit play mode first.");
                return;
            }
            EnsureFolders();

            // ---- 1. mesh + textures ----
            Mesh rounded = BakeRoundedCubeMesh();
            Texture2D edgeTex = BakeTexture(TexDir + "/EdgeSheen.png", 128, EdgePixel, mipmaps: true);
            Texture2D squareTex = BakeTexture(TexDir + "/FxSquare.png", 32, SquarePixel, mipmaps: false);
            Texture2D discTex = BakeTexture(TexDir + "/FxDisc.png", 32, DiscPixel, mipmaps: false);
            Texture2D ringTex = BakeTexture(TexDir + "/FxRing.png", 64, RingPixel, mipmaps: false);

            // ---- 2. materials ----
            var voxelSets = new List<VisualLibrary.MaterialSet>();
            for (int set = 0; set < 4; set++)
            {
                var pal = Palette.Active.VoxelSet(set);
                var mats = new Material[pal.Length];
                for (int slot = 0; slot < pal.Length; slot++)
                    mats[slot] = BakeMaterial(string.Format("{0}/Voxel_S{1}_C{2}.mat", VoxelMatDir, set, slot),
                        m => SetupToon(m, pal[slot], edgeTex));
                voxelSets.Add(new VisualLibrary.MaterialSet { colors = mats });
            }

            // Base stays white: every gun renderer is tinted per-gun through a MaterialPropertyBlock,
            // and an MPB SetColor REPLACES _BaseColor rather than multiplying it — so this value has
            // no effect on the tinted parts and cannot be used to hold brightness headroom.
            // That clamp lives in Gun.ApplyTint (see gunTintMaxValue).
            Material gunPart = BakeMaterial(MatDir + "/GunPart.mat", m => SetupToon(m, Color.white, null));
            Material gunHole = BakeMaterial(MatDir + "/GunHole.mat", m => SetupToon(m, new Color(0.10f, 0.10f, 0.14f), null));
            Material slotPad = BakeMaterial(MatDir + "/SlotPad.mat", m => SetupToon(m, new Color(0.122f, 0.18f, 0.294f), edgeTex));
            Material dartBullet = BakeMaterial(MatDir + "/DartBullet.mat", m =>
            {
                m.shader = Shader.Find("Universal Render Pipeline/Unlit");
                SetBaseColor(m, Color.white);
            });
            Material dartTrail = BakeMaterial(MatDir + "/DartTrail.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default"); // honors the trail's vertex colors
            });
            Material fxSquare = BakeMaterial(MatDir + "/FxSquare.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
                m.mainTexture = squareTex;
            });
            Material fxDisc = BakeMaterial(MatDir + "/FxDisc.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
                m.mainTexture = discTex;
            });
            Material fxRing = BakeMaterial(MatDir + "/FxRing.mat", m =>
            {
                m.shader = Shader.Find("Sprites/Default");
                m.mainTexture = ringTex;
            });
            Material labelMat = BakeLabelMaterial();

            // ---- 3. FX + debris prefabs ----
            ParticleSystem fxBurstP = BakeFxPrefab(FxPrefabDir + "/Fx_Burst.prefab", fxSquare, ps =>
            {
                var main = ps.main;
                main.startLifetime = 0.45f;
                main.gravityModifier = 1.6f;
                main.maxParticles = 1024;
                SetFade(ps, 1f, 0.55f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));
            });
            ParticleSystem fxShardsP = BakeFxPrefab(FxPrefabDir + "/Fx_Shards.prefab", fxSquare, ps =>
            {
                var main = ps.main;
                main.startLifetime = 0.28f;
                main.gravityModifier = 0.7f;
                main.maxParticles = 512;
                SetFade(ps, 0.6f, 0f);
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.lengthScale = 5f;      // thin straight streaks along the velocity
                r.velocityScale = 0.06f;
            });
            ParticleSystem fxFlashP = BakeFxPrefab(FxPrefabDir + "/Fx_Flash.prefab", fxDisc, ps =>
            {
                var main = ps.main;
                main.startLifetime = 0.1f;
                main.gravityModifier = 0f;
                main.maxParticles = 128;
                SetFade(ps, 0.9f, 0f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.25f));
            });
            ParticleSystem fxRingP = BakeFxPrefab(FxPrefabDir + "/Fx_Ring.prefab", fxRing, ps =>
            {
                var main = ps.main;
                main.startLifetime = 0.2f;
                main.gravityModifier = 0f;
                main.maxParticles = 64;
                SetFade(ps, 0.65f, 0f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f));
            });

            Debris debris = BakeDebrisPrefab(rounded, voxelSets[0].colors[0]);

            // ---- 4. gameplay prefabs (visual subtrees authored in) ----
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Mesh cylinder = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            BakeVoxelCubePrefab(rounded, voxelSets[0].colors[0]);
            BakeDartPrefab(sphere, dartBullet, dartTrail);
            BakeGunPrefab(rounded, sphere, cylinder, gunPart, gunHole, font, labelMat);
            BakeGunSlotPrefab(rounded, slotPad);
            BakeBankBlockPrefab(rounded, voxelSets[0].colors[0], font, labelMat);

            // ---- 5. VisualLibrary ----
            var lib = AssetDatabase.LoadAssetAtPath<VisualLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VisualLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.voxelSets = voxelSets.ToArray();
            lib.slotPad = slotPad;
            lib.dartBullet = dartBullet;
            lib.dartTrail = dartTrail;
            lib.fxBurst = fxBurstP;
            lib.fxShards = fxShardsP;
            lib.fxFlash = fxFlashP;
            lib.fxRing = fxRingP;
            lib.debrisPrefab = debris;
            EditorUtility.SetDirty(lib);

            // ---- 6. post-processing profile + scene volume + bootstrap wiring ----
            BakePostFx(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VisualAssetBaker] Bake complete. All visuals are now assets/prefabs — tweak them in " +
                      ArtDir + ", " + PrefabDir + " and " + LibraryPath);
        }

        // ================= folders =================

        static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project", "Art");
            EnsureFolder(ArtDir, "Meshes");
            EnsureFolder(ArtDir, "Textures");
            EnsureFolder(ArtDir, "Materials");
            EnsureFolder(MatDir, "Voxels");
            EnsureFolder(PrefabDir, "Fx");
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        // ================= mesh =================

        static Mesh BakeRoundedCubeMesh()
        {
            string path = MeshDir + "/RoundedCube.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            // Always rebuilt, even on a default (non-force) bake. Unlike the materials and the
            // PostFX profile there is nothing hand-editable about this mesh — it is pure geometry
            // derived from voxelCornerRadius/voxelRoundSegments. Skipping it when the asset already
            // existed meant those two config values silently did nothing on a default bake, and it
            // would have kept serving the old inside-out mesh after the winding fix below.
            // The existing asset is updated IN PLACE so its GUID (and every prefab reference) survives.
            Mesh built = BuildRoundedCube(
                Mathf.Clamp(Cfg.Active.voxelCornerRadius, 0.02f, 0.49f),
                Mathf.Max(2, Cfg.Active.voxelRoundSegments));
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

        /// <summary>
        /// A unit cube (1×1×1, centered) with rounded/beveled edges + corners. Clamp-and-push
        /// mapping: each cube-surface point is clamped to the inner flat box and pushed out by the
        /// corner radius, so flat faces stay flat and edges/corners round off. Per-face planar UVs
        /// so the EdgeSheen rim lands exactly on the bevel band.
        /// </summary>
        static Mesh BuildRoundedCube(float r, int band)
        {
            float flat = 0.5f - r;

            var raw = new List<float>();
            for (int i = 0; i <= band; i++) raw.Add(-0.5f + r * ((float)i / band));
            raw.Add(0f);
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
                        // Winding must put the FRONT face outward. Unity's rule (see the quad
                        // example in the Mesh docs): the outward normal of a triangle (v0,v1,v2)
                        // is Cross(v1-v0, v2-v0). Here i1 is +uA from i0 and i2 is +vA, and every
                        // face below is built right-handed (uA x vA == nrm), so i0->i1->i2 gives
                        // Cross(uA, vA) == nrm — outward. The old order (i0,i2,i1) produced
                        // Cross(vA, uA) == -nrm, i.e. every face front-facing INWARD: with Cull
                        // Back the near wall was culled and you saw the inside of the far walls,
                        // so a close-up cube looked hollow/scooped.
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

        // ================= textures =================

        delegate Color32 PixelFn(float u, float v);

        static Texture2D BakeTexture(string path, int size, PixelFn fn, bool mipmaps)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && !_force) return existing;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = fn((x + 0.5f) / size, (y + 0.5f) / size);
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = mipmaps;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Plastic face tile: soft off-center gaussian sheen blob over a darker plate +
        /// a very light same-tone rim on the bevel band (NOT a dark outline).
        /// The plate range is what stops a face reading as a flat slab: a toon ramp gives a flat
        /// face a constant normal and therefore ONE flat tone, so without an in-face gradient the
        /// three visible faces read as three planes taped together rather than one solid block.
        /// plateLow was 0.90 (only 10% falloff — far too weak to fake light dropping off across a
        /// face); 0.74 gives visible volume while staying smooth enough not to band.
        /// Keep `rim` mild — a strong dark rim reads as a black pixel-grid, rejected twice.</summary>
        static Color32 EdgePixel(float u, float v)
        {
            const float band = 0.10f, rim = 0.88f, plateLow = 0.74f;
            const float hiU = 0.32f, hiV = 0.72f, hiSigma = 0.40f;
            float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            float du = u - hiU, dv = v - hiV;
            float hi = Mathf.Exp(-(du * du + dv * dv) / (2f * hiSigma * hiSigma));
            float plate = Mathf.Lerp(plateLow, 1f, hi);
            float edge = Mathf.SmoothStep(rim, 1f, Mathf.Clamp01(d / band));
            byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(plate * edge) * 255f);
            return new Color32(g, g, g, 255);
        }

        /// <summary>Crisp rounded-square sprite — debris dust stays on-language with the voxels.</summary>
        static Color32 SquarePixel(float u, float v)
        {
            float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            float a = Mathf.SmoothStep(0.04f, 0.16f, d);
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }

        static Color32 DiscPixel(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.7f, 1f, d));
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }

        /// <summary>Thin ring sprite for the impact shockwave.</summary>
        static Color32 RingPixel(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f);
            a *= a;
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }

        // ================= materials =================

        static Material BakeMaterial(string path, System.Action<Material> setup)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && !_force) return existing;
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

        /// <summary>
        /// Shared TCP2 Hybrid setup (all knobs seeded from GameConfig): hue-tinted shadow color
        /// (never black), soft wide ramp (no hard cel bands), broad soft stylized specular — the
        /// "premium plastic toy" response the art reviews asked for.
        /// </summary>
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

            var cfg = Cfg.Active;
            if (mat.HasProperty("_HColor")) mat.SetColor("_HColor", cfg.toonHighlight);
            if (mat.HasProperty("_SColor")) mat.SetColor("_SColor", cfg.toonShadow);
            if (mat.HasProperty("_RampThreshold")) mat.SetFloat("_RampThreshold", cfg.toonRampThreshold);
            if (mat.HasProperty("_RampSmoothing")) mat.SetFloat("_RampSmoothing", cfg.toonRampSmoothing);
            mat.EnableKeyword("TCP2_SHADOW_LIGHT_COLOR");

            // Stylized specular: broad + soft (toy gloss, not a tight glint)
            mat.EnableKeyword("TCP2_SPECULAR");
            mat.EnableKeyword("TCP2_SPECULAR_STYLIZED");
            if (mat.HasProperty("_UseSpecular")) mat.SetFloat("_UseSpecular", 1f);
            if (mat.HasProperty("_SpecularType")) mat.SetFloat("_SpecularType", 1f);
            if (mat.HasProperty("_SpecularColor")) mat.SetColor("_SpecularColor", cfg.toonSpecColor);
            if (mat.HasProperty("_SpecularToonSize")) mat.SetFloat("_SpecularToonSize", cfg.toonSpecSize);
            if (mat.HasProperty("_SpecularToonSmoothness")) mat.SetFloat("_SpecularToonSmoothness", cfg.toonSpecSmoothing);
        }

        static void SetBaseColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
        }

        /// <summary>TMP preset for the world-space ammo numbers: bold white face + soft dark outline.</summary>
        static Material BakeLabelMaterial()
        {
            string path = MatDir + "/NumberLabel.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && !_force) return existing;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogWarning("[VisualAssetBaker] TMP font not found at " + FontPath + " — label preset skipped.");
                return existing;
            }
            Material mat = existing != null ? existing : new Material(font.material);
            mat.shader = font.material.shader;
            mat.CopyPropertiesFromMaterial(font.material);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.08f);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.10f, 0.12f, 0.22f, 1f));
            if (existing == null) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================= FX prefabs =================

        static void SetFade(ParticleSystem ps, float a0, float hold)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            var alphas = hold > 0f
                ? new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a0, hold), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(0f, 1f) };
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphas);
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        static ParticleSystem BakeFxPrefab(string path, Material mat, System.Action<ParticleSystem> setup)
        {
            var existingGo = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingGo != null && !_force) return existingGo.GetComponent<ParticleSystem>();

            var go = new GameObject(Path.GetFileNameWithoutExtension(path));
            try
            {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startSpeed = 0f;

                var em = ps.emission; em.enabled = false;
                var sh = ps.shape; sh.enabled = false;

                var r = go.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.sharedMaterial = mat;

                setup(ps);

                var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
                return saved.GetComponent<ParticleSystem>();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static Debris BakeDebrisPrefab(Mesh rounded, Material defaultMat)
        {
            string path = PrefabDir + "/Debris.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var seed = new GameObject("Debris");
                PrefabUtility.SaveAsPrefabAsset(seed, path);
                Object.DestroyImmediate(seed);
            }

            // Always ensure structure + serialized refs (idempotent; keeps the asset GUID).
            EditPrefab(path, root =>
            {
                var mf = root.GetComponent<MeshFilter>();
                if (mf == null) mf = root.AddComponent<MeshFilter>();
                mf.sharedMesh = rounded;
                var mr = root.GetComponent<MeshRenderer>();
                if (mr == null) mr = root.AddComponent<MeshRenderer>();
                if (mr.sharedMaterial == null || _force) mr.sharedMaterial = defaultMat;
                var col = root.GetComponent<BoxCollider>();
                if (col == null) col = root.AddComponent<BoxCollider>();
                col.size = Vector3.one;
                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.08f;
                var d = root.GetComponent<Debris>();
                if (d == null) d = root.AddComponent<Debris>();
                SetRef(d, "meshRenderer", mr);
                SetRef(d, "box", col);
                SetRef(d, "body", rb);
            });
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Debris>();
        }

        // ================= gameplay prefabs =================

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
            Material labelMat, float towardCamera, Vector3 localPos)
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
            tm.rectTransform.sizeDelta = new Vector2(2f, 2f);
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

                // Explosion physics is authored here (disabled/kinematic) so runtime never AddComponents.
                var col = root.GetComponent<BoxCollider>();
                if (col == null) col = root.AddComponent<BoxCollider>();
                col.size = Vector3.one;
                col.enabled = false;
                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.isKinematic = true;

                var cube = root.GetComponent<VoxelCube>();
                SetRef(cube, "meshRenderer", mr);
                SetRef(cube, "box", col);
                SetRef(cube, "body", rb);
            });
        }

        static void BakeDartPrefab(Mesh sphere, Material bulletMat, Material trailMat)
        {
            EditPrefab(PrefabDir + "/Dart.prefab", root =>
            {
                ClearChildren(root);

                var trail = root.GetComponent<TrailRenderer>();
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = Cfg.Active.dartTrailTime;
                trail.startWidth = 0.2f;
                trail.endWidth = 0f;
                trail.numCapVertices = 4;
                trail.numCornerVertices = 2;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;

                var bullet = AddMeshChild(root.transform, "Bullet", sphere, bulletMat,
                    Vector3.zero, Quaternion.identity, Vector3.one * 0.22f);
                var mr = bullet.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var dart = root.GetComponent<Dart>();
                SetRef(dart, "trail", trail);
                SetRef(dart, "bulletRenderer", mr);
            });
        }

        static void BakeGunPrefab(Mesh rounded, Mesh sphere, Mesh cylinder,
            Material gunPart, Material gunHole, TMP_FontAsset font, Material labelMat)
        {
            EditPrefab(PrefabDir + "/Gun.prefab", root =>
            {
                ClearChildren(root);

                // Reference look: a squat rounded "canister" tinted with the gun's color, dome
                // shoulders, a short stubby muzzle poking up-forward, and a BIG white ammo number.
                var body = AddMeshChild(root.transform, "Body", rounded, gunPart,
                    new Vector3(0f, -0.10f, 0f), Quaternion.identity, new Vector3(0.88f, 0.84f, 0.68f));
                var dome = AddMeshChild(root.transform, "Dome", sphere, gunPart,
                    new Vector3(0f, 0.28f, 0f), Quaternion.identity, new Vector3(0.68f, 0.4f, 0.54f));

                // Muzzle: short chunky stub tilted up-forward (+Z toward the sculpture).
                Vector3 dir = new Vector3(0f, 0.3f, 1f).normalized;
                var mount = new GameObject("BarrelMount").transform;
                mount.SetParent(root.transform, false);
                mount.localPosition = new Vector3(0f, 0.38f, 0.02f);
                mount.localRotation = Quaternion.FromToRotation(Vector3.up, dir);

                const float len = 0.42f;
                var barrelBase = AddMeshChild(mount, "Base", sphere, gunPart,
                    Vector3.zero, Quaternion.identity, new Vector3(0.3f, 0.3f, 0.3f));
                var tube = AddMeshChild(mount, "Tube", cylinder, gunPart,
                    new Vector3(0, len * 0.5f, 0), Quaternion.identity, new Vector3(0.26f, len * 0.5f, 0.26f));
                var rim = AddMeshChild(mount, "Rim", cylinder, gunPart,
                    new Vector3(0, len - 0.03f, 0), Quaternion.identity, new Vector3(0.32f, 0.06f, 0.32f));
                AddMeshChild(mount, "Hole", cylinder, gunHole,
                    new Vector3(0, len + 0.015f, 0), Quaternion.identity, new Vector3(0.18f, 0.03f, 0.18f));

                // Fire point exactly at the muzzle tip.
                var tip = new GameObject("BarrelTip").transform;
                tip.SetParent(mount, false);
                tip.localPosition = new Vector3(0, len + 0.06f, 0);

                var label = AddLabel(root.transform, "0", 0.1f, font, labelMat, 0.75f, new Vector3(0f, 0.05f, 0f));

                var gun = root.GetComponent<Gun>();
                SetRef(gun, "body", body.transform);
                SetRef(gun, "barrelTip", tip);
                SetRef(gun, "label", label);
                SetRef(gun, "bodyRenderer", body.GetComponent<MeshRenderer>());
                SetRef(gun, "domeRenderer", dome.GetComponent<MeshRenderer>());
                SetRef(gun, "tubeRenderer", tube.GetComponent<MeshRenderer>());

                var so = new SerializedObject(gun);
                var arr = so.FindProperty("rimRenderers");
                arr.arraySize = 2;
                arr.GetArrayElementAtIndex(0).objectReferenceValue = barrelBase.GetComponent<MeshRenderer>();
                arr.GetArrayElementAtIndex(1).objectReferenceValue = rim.GetComponent<MeshRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        static void BakeGunSlotPrefab(Mesh rounded, Material slotPad)
        {
            EditPrefab(PrefabDir + "/GunSlot.prefab", root =>
            {
                ClearChildren(root);

                // Chunky toy-tray socket: tall enough to read as a 3D box from the 3/4 camera.
                AddMeshChild(root.transform, "Pad", rounded, slotPad,
                    new Vector3(0, -0.56f, 0), Quaternion.identity, new Vector3(1.06f, 0.34f, 1.06f));

                // Trigger collider on the slot root for drop hit-testing.
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

                // Centered on the cube, pulled toward the camera so the number reads dead-center
                // in the cell at any pitch.
                var label = AddLabel(root.transform, "0", 0.11f, font, labelMat, 0.9f, Vector3.zero);

                var block = root.GetComponent<BankBlock>();
                SetRef(block, "cubeRenderer", cube.GetComponent<MeshRenderer>());
                SetRef(block, "label", label);
                SetRef(block, "box", box);
            });
        }

        // ================= post-processing =================

        static void BakePostFx(VisualLibrary lib)
        {
            var cfg = Cfg.Active;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile == null || _force)
            {
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, PostProfilePath);
                }

                // Subtle, controlled stack (art doc: slight saturation/contrast, very light bloom,
                // light vignette — no heavy cinematic grading). Values seeded from GameConfig.
                var bloom = GetOrAddOverride<Bloom>(profile);
                bloom.intensity.Override(cfg.postBloomIntensity);
                bloom.threshold.Override(1.1f);

                var vignette = GetOrAddOverride<Vignette>(profile);
                vignette.intensity.Override(cfg.postVignette);
                vignette.color.Override(new Color(0.05f, 0.08f, 0.16f)); // navy, not pure black

                var adjust = GetOrAddOverride<ColorAdjustments>(profile);
                adjust.saturation.Override(cfg.postSaturation);
                adjust.contrast.Override(cfg.postContrast);
                adjust.postExposure.Override(cfg.postExposure);

                EditorUtility.SetDirty(profile);
            }

            // Scene: global "PostFX" Volume + wire GameBootstrap.visualLibrary.
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

            // ---- scene ref wiring (runtime code never GetComponents — refs are authored here) ----
            if (cam != null)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                if (bootstrap != null)
                {
                    SetRef(bootstrap, "mainCamera", cam);
                    SetRef(bootstrap, "cameraData", camData);
                }
                if (rig != null) SetRef(rig, "cam", cam);
                if (input != null) SetRef(input, "cam", cam);
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
                SetRef(audio, "_sfx", sfx);
                SetRef(audio, "_music", music);
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
