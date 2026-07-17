using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// One destruction fragment (medium/small chunk). All components (mesh, renderer, collider,
    /// rigidbody) are authored in the Debris prefab and referenced here — Launch only swaps the
    /// baked material, toggles the collider and throws the body.
    /// </summary>
    public class Debris : MonoBehaviour
    {
        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] BoxCollider box;
        [SerializeField] Rigidbody body;

        public void Launch(Material mat, Vector3 velocity, Vector3 angularVelocity, bool withCollider, float life)
        {
            if (meshRenderer != null && mat != null) meshRenderer.sharedMaterial = mat;
            if (box != null) box.enabled = withCollider;
            if (body != null)
            {
                body.velocity = velocity;
                body.angularVelocity = angularVelocity;
            }
            Destroy(gameObject, life);
        }
    }
}
