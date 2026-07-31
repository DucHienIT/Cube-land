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

        /// The bank is a WINDOW onto the queue, not the whole queue. A level holds one block
        /// per ~60 cubes — 49 early, 130+ late — and only `bankVisibleRows` of them fit under
        /// the sculpture before the rows run off the bottom of the frame, so everything past
        /// the window is deactivated. Blocks are parked exactly one row behind the window
        /// rather than at their true row, so when the queue advances they slide forward into
        /// it instead of popping in from wherever they would have been stacked.
        void Layout(bool snap)
        {
            var config = GameConfig.Active;
            int visibleRows = Mathf.Max(1, config.bankVisibleRows);

            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                int row = i / _columns;
                Vector3 home = HomeFor(i % _columns, Mathf.Min(row, visibleRows), config);

                if (row >= visibleRows)
                {
                    block.SetHome(home, snap: true);
                    block.gameObject.SetActive(false);
                    continue;
                }

                block.gameObject.SetActive(true);
                block.SetHome(home, snap);
                block.SetRow(row);
            }
        }

        Vector3 HomeFor(int column, int row, GameConfig config) => new Vector3(
            (column - (_columns - 1) * 0.5f) * config.bankSlotSpacing,
            config.bankY,
            config.bankZ - row * config.bankRowSpacing);
    }
}
