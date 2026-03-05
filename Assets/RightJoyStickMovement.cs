using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RightStickLocomotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform yawSource; // CenterEyeAnchor

    [Header("Tuning")]
    [SerializeField] private float speed = 2.0f;
    [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.25f;

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        Debug.Log("RightStickLocomotion running");
        // RIGHT stick on Quest controllers
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (stick.magnitude < deadzone)
            stick = Vector2.zero;

        Vector3 forward = yawSource.forward; forward.y = 0; forward.Normalize();
        Vector3 right = yawSource.right; right.y = 0; right.Normalize();

        Vector3 move = (forward * stick.y + right * stick.x) * speed;
        cc.Move(move * Time.deltaTime);
    }
}