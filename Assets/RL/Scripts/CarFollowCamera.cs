using UnityEngine;

/// <summary>
/// Smooth third-person follow camera for the ML-Agent car.
/// AUTO-FINDS the car by name — no manual wiring needed.
/// Just attach this script to your Camera prefab and press Play!
/// </summary>
public class CarFollowCamera : MonoBehaviour
{
    [Header("Target (leave empty to auto-find)")]
    public Transform target;

    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset       = new Vector3(0f, 4.5f, -9f);
    [SerializeField] private float   smoothSpeed  = 8f;
    [SerializeField] private float   rotationSpeed = 6f;

    [Header("Look")]
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);

    // Names to search for, in priority order
    private static readonly string[] CarNames = {
        "Player Car 1 (AI Variant) 1",
        "Player Car 1",
        "Cube"
    };

    private void Start()
    {
        FindTarget();
    }

    private void FindTarget()
    {
        if (target != null) return;

        foreach (var name in CarNames)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                target = go.transform;
                Debug.Log("[CarFollowCamera] Auto-found target: " + name);
                return;
            }
        }

        Debug.LogWarning("[CarFollowCamera] Could not find car! Assign target manually.");
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        // Use only the car's yaw (Y rotation) — ignore pitch/roll so camera stays level
        Quaternion yawOnly    = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3    desiredPos = target.position + yawOnly * offset;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                           smoothSpeed * Time.deltaTime);

        // Always look slightly above the car centre
        Vector3    lookAt     = target.position + lookAtOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookAt - transform.position);
        transform.rotation    = Quaternion.Slerp(transform.rotation, desiredRot,
                                                   rotationSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position + lookAtOffset, 0.2f);
    }
#endif
}
