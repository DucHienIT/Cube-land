using UnityEngine;

namespace CubeBlaster
{
    public sealed class RendererTinter
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        MaterialPropertyBlock _block;

        MaterialPropertyBlock Block => _block ?? (_block = new MaterialPropertyBlock());

        public void Apply(Renderer target, Color color)
        {
            if (target == null) return;
            var block = Block;
            target.GetPropertyBlock(block);
            WriteColor(block, color);
            target.SetPropertyBlock(block);
        }

        public void Apply(Renderer[] targets, Color color)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++) Apply(targets[i], color);
        }

        public static void WriteColor(MaterialPropertyBlock block, Color color)
        {
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
        }
    }
}
