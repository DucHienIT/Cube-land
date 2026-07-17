using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Lays out the bank of ammo blocks in a grid and reflows it as blocks are consumed.
    /// References the BankBlock prefab (wired in the inspector).
    /// </summary>
    public class BankArea : MonoBehaviour
    {
        [SerializeField] BankBlock blockPrefab;

        readonly List<BankBlock> _blocks = new List<BankBlock>();
        int _columns;

        public IReadOnlyList<BankBlock> Blocks => _blocks;
        public int Remaining => _blocks.Count;

        /// <summary>Build the bank. colors[i] is the voxel color slot block i is locked to; the
        /// block's material + tint come from the baked VisualLibrary assets for this palette set.</summary>
        public void Init(int[] values, int[] colors, int paletteIndex, int columns)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _blocks.Clear();
            _columns = Mathf.Max(1, columns);

            if (values != null)
            {
                var lib = Visuals.Active;
                for (int i = 0; i < values.Length; i++)
                {
                    int colorIndex = (colors != null && i < colors.Length) ? colors[i] : 0;
                    var b = Instantiate(blockPrefab, transform);
                    b.Init(values[i], colorIndex,
                        lib.VoxelMaterial(paletteIndex, colorIndex), lib.VoxelColor(paletteIndex, colorIndex));
                    _blocks.Add(b);
                }
            }
            Layout(true);
        }

        void Layout(bool snap)
        {
            var cfg = Cfg.Active;
            int cols = _columns;
            for (int i = 0; i < _blocks.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = (col - (cols - 1) * 0.5f) * cfg.bankSlotSpacing;
                float y = cfg.bankY;                                // flat: whole bank shares the slot-area plane
                float z = cfg.bankZ - row * cfg.bankRowSpacing;     // queued rows step back along the ground
                Vector3 home = new Vector3(x, y, z);
                _blocks[i].SetHome(home, snap);
                _blocks[i].SetRow(row);
            }
        }

        public void Consume(BankBlock b)
        {
            _blocks.Remove(b);
            b.Consume();
            Layout(false);
        }
    }
}
