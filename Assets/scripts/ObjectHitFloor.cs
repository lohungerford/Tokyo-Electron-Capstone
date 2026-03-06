using UnityEngine;

public class ObjectHitFloor : MonoBehaviour
{
    private Rigidbody rb;
    private bool isPickedUp = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("floor"))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    // Call this when you pick the object up
    public void PickUp()
    {
        isPickedUp = true;
        rb.isKinematic = false;
    }

    // Call this when you release the object
    public void Release()
    {
        isPickedUp = false;
    }


}
