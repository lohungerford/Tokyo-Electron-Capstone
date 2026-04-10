using UnityEngine;

public class BarrierInteractable : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
}