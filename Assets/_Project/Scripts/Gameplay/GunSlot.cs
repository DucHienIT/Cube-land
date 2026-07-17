using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// A fixed shooter slot. Holds at most one Gun. The pad visual and the trigger collider used
    /// for drop-detection are authored in the GunSlot prefab. References the Gun prefab
    /// (wired in the inspector).
    /// </summary>
    public class GunSlot : MonoBehaviour
    {
        [SerializeField] Gun gunPrefab;

        GameManager _gm;
        Gun _gun;
        int _index;

        public bool IsEmpty => _gun == null;
        public int Index => _index;

        public void Init(GameManager gm, int index)
        {
            _gm = gm;
            _index = index;
        }

        /// <summary>
        /// Deploy a color-locked gun into this slot with the given ammo. The gun will only
        /// shoot voxels of colorIndex. Returns false if occupied.
        /// </summary>
        public bool Deploy(int ammo, int colorIndex)
        {
            if (!IsEmpty) return false;
            _gun = Instantiate(gunPrefab, transform);
            _gun.transform.localPosition = Vector3.zero;
            _gun.Init(_gm, this, ammo, colorIndex);
            return true;
        }

        public void OnGunEmpty(Gun g)
        {
            if (_gun == g) _gun = null;
        }
    }
}
