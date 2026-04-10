using UnityEngine;

public class CameraRigGrounder : MonoBehaviour
{
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float moveSpeed = 3f;

    private CharacterController cc;
    private float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Gravity
        if (cc.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        // Joystick locomotion (left thumbstick)
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector3 move = transform.forward * stick.y + transform.right * stick.x;
        move.y = verticalVelocity * Time.deltaTime;

        cc.Move(move * moveSpeed * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime);
    }
}