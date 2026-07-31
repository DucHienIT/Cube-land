using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public class BankArea : MonoBehaviour
    {
        [SerializeField] BankBlock blockPrefab;

        readonly List<BankBlock> _blocks = new List<BankBlock>();
        int _columns = 1;

        public IReadOnlyList<BankBlock> Blocks => _blocks;
        public int Remaining => _blocks.Count;

        public void Initialize(int[] values, int[] colors, int paletteIndex, int columns)
        {
            ClearBlocks();
            _columns = Mathf.Max(1, columns);
            if (values != null) Spawn(values, colors, paletteIndex);
            Layout(snap: true);
        }

        public void Consume(BankBlock block)
        {
            _blocks.Remove(block);
            block.Consume();
            Layout(snap: false);
        }

        void Spawn(int[] values, int[] colors, int paletteIndex)
        {
            var library = VisualLibrary.Active;
            for (int i = 0; i < values.Length; i++)
            {
                int colorIndex = colors != null && i < colors.Length ? colors[i] : 0;
                var block = Instantiate(blockPrefab, transform);
                block.Initialize(values[i], colorIndex,
                    library.GetVoxelMaterial(paletteIndex, colorIndex),
                    library.GetVoxelColor(paletteIndex, colorIndex));
                _blocks.Add(block);
            }
        }

        void ClearBlocks()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _blocks.Clear();
        }

        void Layout(bool snap)
        {
            var config = GameConfig.Active;
            for (int i = 0; i < _blocks.Count; i++)
            {
                int column = i % _columns;
                int row = i / _columns;
                var home = new Vector3(
                    (column - (_columns - 1) * 0.5f) * config.bankSlotSpacing,
                    config.bankY,
                    config.bankZ - row * config.bankRowSpacing);

                _blocks[i].SetHome(home, snap);
                _blocks[i].SetRow(row);
            }
        }
    }
}
