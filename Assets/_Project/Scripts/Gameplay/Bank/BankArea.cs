using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public class BankArea : MonoBehaviour
    {
        [SerializeField] BankBlock blockPrefab;

        readonly List<BankBlock> _blocks = new List<BankBlock>();
        readonly Queue<BankBlock> _pending = new Queue<BankBlock>();
        readonly List<List<BankBlock>> _lanes = new List<List<BankBlock>>();
        int _columns = 1;
        bool _landscape;

        public IReadOnlyList<BankBlock> Blocks => _blocks;
        public int Remaining => _blocks.Count;

        static int VisibleRows => GameConfig.Active.GetBankVisibleRows(ScreenLayout.IsLandscape);

        /// `columns` is the level asset's own value and is deliberately IGNORED: the bank has one
        /// lane per shooter slot (`GameConfig.GetBankColumns`), so a level cannot lay out a bank
        /// that does not line up with the slot row. The parameter stays on the signature because
        /// LevelData still carries the field and dropping it would silently rewrite 60 assets.
        public void Initialize(int[] values, int[] colors, int paletteIndex, int columns)
        {
            ClearBlocks();
            _landscape = ScreenLayout.IsLandscape;
            _columns = GameConfig.Active.GetBankColumns();
            BuildLanes();
            if (values != null) Spawn(values, colors, paletteIndex);
            DealRowMajor();
            Layout(snap: true);
        }

        /// A WebGL canvas is resized (or a phone rotated) mid-level. The lane COUNT no longer
        /// changes with orientation, but the visible row count still does, so the window has to
        /// be re-dealt or landscape keeps portrait's third row parked off the bottom of the
        /// frame. One bool compare a frame; the re-deal itself only runs on the flip.
        void Update()
        {
            bool landscape = ScreenLayout.IsLandscape;
            if (landscape == _landscape || _blocks.Count == 0) return;
            _landscape = landscape;
            _columns = GameConfig.Active.GetBankColumns();
            Reflow();
        }

        /// Re-deals every remaining block from scratch. `_blocks` is the queue in its original
        /// order minus what has been played, which is exactly what the first deal consumed, so
        /// re-dealing it reproduces the same distribution on the new lane count.
        void Reflow()
        {
            _lanes.Clear();
            _pending.Clear();
            BuildLanes();

            foreach (var block in _blocks)
            {
                block.gameObject.SetActive(false);
                _pending.Enqueue(block);
            }

            DealRowMajor();
            Layout(snap: true);
        }

        /// Takes a played block out of the queue WITHOUT destroying it: it is still flying to its
        /// slot, and the lane behind it closes up now rather than when it lands, so the queue
        /// answers the tap on the same frame. Whoever detached it owns destroying it.
        public void Detach(BankBlock block)
        {
            if (!_blocks.Remove(block)) return;

            int lane = FindLaneOf(block);
            if (lane >= 0)
            {
                _lanes[lane].Remove(block);
                FillLane(lane);
            }

            block.Detach();
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
                block.gameObject.SetActive(false);
                _blocks.Add(block);
                _pending.Enqueue(block);
            }
        }

        void ClearBlocks()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _blocks.Clear();
            _pending.Clear();
            _lanes.Clear();
        }

        void BuildLanes()
        {
            for (int i = 0; i < _columns; i++) _lanes.Add(new List<BankBlock>());
        }

        /// The first blocks of the queue must land on the FRONT row across every column, so the
        /// initial deal walks row by row. Topping a single lane up column-first would put blocks
        /// 0..visibleRows-1 all in column 0.
        void DealRowMajor()
        {
            int rows = VisibleRows;
            for (int row = 0; row < rows; row++)
                for (int lane = 0; lane < _lanes.Count; lane++)
                    PushToLane(lane);
        }

        void FillLane(int lane)
        {
            int rows = VisibleRows;
            while (_lanes[lane].Count < rows && PushToLane(lane)) { }
        }

        bool PushToLane(int lane)
        {
            if (_pending.Count == 0) return false;
            var block = _pending.Dequeue();
            block.SetHome(HomeFor(lane, VisibleRows, GameConfig.Active), snap: true);
            _lanes[lane].Add(block);
            return true;
        }

        int FindLaneOf(BankBlock block)
        {
            for (int i = 0; i < _lanes.Count; i++)
                if (_lanes[i].Contains(block)) return i;
            return -1;
        }

        /// The bank is a WINDOW onto the queue, and each COLUMN is its own vertical stack — not
        /// one flat sequence re-flowed across the grid. Taking the front block of a column must
        /// only pull that column forward; the other columns hold still. (Laying out by a flat
        /// index instead made every block after the taken one shift one place, so a block slid
        /// sideways and wrapped to the previous row — the whole bank behaved like a single row.)
        /// Blocks still queued behind the window are inactive and are snapped one row behind it
        /// when they enter a lane, so they slide into the window instead of popping in.
        void Layout(bool snap)
        {
            var config = GameConfig.Active;

            for (int lane = 0; lane < _lanes.Count; lane++)
            {
                var stack = _lanes[lane];
                for (int row = 0; row < stack.Count; row++)
                {
                    var block = stack[row];
                    block.gameObject.SetActive(true);
                    block.SetHome(HomeFor(lane, row, config), snap);
                    block.SetRow(row);
                }
            }
        }

        Vector3 HomeFor(int column, int row, GameConfig config) => new Vector3(
            (column - (_columns - 1) * 0.5f) * config.bankSlotSpacing,
            config.bankY,
            config.bankZ - row * config.bankRowSpacing);
    }
}
