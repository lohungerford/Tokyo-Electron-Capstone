using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class VRPunchDetector : MonoBehaviour
{
    public float minPunchVelocity = 1.5f; // m/s — raise to require harder swings

    private Vector3 lastPosition;
    private float handVelocity;

    void Start()
    {
        lastPosition = transform.position;

        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.08f;
    }

    void Update()
    {
        handVelocity = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (handVelocity < minPunchVelocity) return;

        RobotHealth health = other.GetComponentInParent<RobotHealth>();
        if (health != null)
            health.TakeHit();
    }
}
