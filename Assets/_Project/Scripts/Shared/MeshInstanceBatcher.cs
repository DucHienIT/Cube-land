using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeBlaster
{
    /// Collects per-frame instance matrices into one bucket per material and submits each bucket
    /// as a single Graphics.DrawMeshInstanced call.
    ///
    /// This is what replaced one GameObject + MeshRenderer per voxel, and then per dart. A
    /// level's sculpture is 1000-2900 exposed cubes and a barrage is ~90 darts, each of which
    /// was a draw call of its own; grouped by material the same scene is a handful of draws,
    /// because a level only ever uses two to four palette slots. Nothing here allocates after
    /// the first few frames — the buckets grow to their high-water mark and are then reused.
    public sealed class MeshInstanceBatcher
    {
        /// Unity's hard per-call limit.
        const int MaxInstancesPerDraw = 1023;
        const int InitialGroupCapacity = 64;

        static readonly Matrix4x4[] Chunk = new Matrix4x4[MaxInstancesPerDraw];
        static readonly bool SupportsInstancing = SystemInfo.supportsInstancing;

        Material[] _materials = Array.Empty<Material>();
        Matrix4x4[][] _matrices = Array.Empty<Matrix4x4[]>();
        int[] _counts = Array.Empty<int>();

        public void Configure(Material[] materials)
        {
            _materials = materials ?? Array.Empty<Material>();
            _matrices = new Matrix4x4[_materials.Length][];
            _counts = new int[_materials.Length];
            for (int group = 0; group < _matrices.Length; group++)
                _matrices[group] = new Matrix4x4[InitialGroupCapacity];
        }

        public void Begin()
        {
            for (int group = 0; group < _counts.Length; group++) _counts[group] = 0;
        }

        public void Add(int group, Matrix4x4 matrix)
        {
            if (group < 0 || group >= _counts.Length) return;

            var buffer = _matrices[group];
            int count = _counts[group];
            if (count == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
                _matrices[group] = buffer;
            }
            buffer[count] = matrix;
            _counts[group] = count + 1;
        }

        public void Draw(Mesh mesh, int layer, ShadowCastingMode shadows, bool receiveShadows)
        {
            if (mesh == null) return;

            for (int group = 0; group < _materials.Length; group++)
            {
                int count = _counts[group];
                var material = _materials[group];
                if (count <= 0 || material == null) continue;

                var buffer = _matrices[group];
                if (count <= MaxInstancesPerDraw)
                {
                    Submit(mesh, material, buffer, count, layer, shadows, receiveShadows);
                    continue;
                }

                // DrawMeshInstanced only reads from the START of the array, so a bucket past the
                // limit has to be copied out in slices. A single palette slot rarely gets there
                // (the jitter variants split it three ways) — this is the safety net, not the path.
                for (int offset = 0; offset < count; offset += MaxInstancesPerDraw)
                {
                    int slice = Mathf.Min(MaxInstancesPerDraw, count - offset);
                    Array.Copy(buffer, offset, Chunk, 0, slice);
                    Submit(mesh, material, Chunk, slice, layer, shadows, receiveShadows);
                }
            }
        }

        static void Submit(Mesh mesh, Material material, Matrix4x4[] matrices, int count,
            int layer, ShadowCastingMode shadows, bool receiveShadows)
        {
            if (SupportsInstancing)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count, null,
                    shadows, receiveShadows, layer, null, LightProbeUsage.Off);
                return;
            }

            for (int i = 0; i < count; i++)
                Graphics.DrawMesh(mesh, matrices[i], material, layer, null, 0, null,
                    shadows, receiveShadows, null, LightProbeUsage.Off, null);
        }
    }
}
