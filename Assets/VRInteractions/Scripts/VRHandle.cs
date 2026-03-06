using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;

/// <summary>
/// VR adds a grippable handle to the lever. Makes the lever attachable to the VR controller.
/// </summary>
public class VRHandle : MonoBehaviour
{
    public string Button;

    private bool bAttached = false;

    /// <summary>
    /// Controller that is connected to the VRHandle
    /// </summary>
    private OVRInput.Controller AttachedController = OVRInput.Controller.None;

    /// <summary>
    /// Object that is the spawn point of the handle joint
    /// </summary>
    public Transform HandlePosition;

    /// <summary>
    /// The joint prefab that connects the controller to the lever
    /// </summary>
    public Transform HandleJointPrefab;

    /// <summary>
    /// Current joint object (null if there is no connection)
    /// </summary>
    private Transform JointObject;

    /// <summary>
    /// Break force for the joint
    /// </summary>
    public float breakForces = 10;

    /// <summary>
    /// Controllers currently inside the trigger collider
    /// </summary>
    private List<OVRInput.Controller> ActiveControllers = new List<OVRInput.Controller>();

    /// <summary>
    /// Rigidbodies of controllers inside the trigger, used for joint connection
    /// </summary>
    private Dictionary<OVRInput.Controller, Rigidbody> ControllerBodies = new Dictionary<OVRInput.Controller, Rigidbody>();

    void OnTriggerEnter(Collider _collider)
    {
        Rigidbody controllerBody = _collider.attachedRigidbody;
        if (controllerBody == null) return;

        OVRInput.Controller controller = GetControllerFromObject(_collider.gameObject);
        if (controller == OVRInput.Controller.None) return;

        if (!ActiveControllers.Contains(controller))
        {
            ActiveControllers.Add(controller);
            ControllerBodies[controller] = controllerBody;
        }
    }

    void OnTriggerExit(Collider _collider)
    {
        OVRInput.Controller controller = GetControllerFromObject(_collider.gameObject);
        if (controller == OVRInput.Controller.None) return;

        ActiveControllers.Remove(controller);
        ControllerBodies.Remove(controller);
    }

    void OnCollisionEnter(Collision _collision)
    {
        Rigidbody controllerBody = _collision.collider.attachedRigidbody;
        if (controllerBody == null) return;

        OVRInput.Controller controller = GetControllerFromObject(_collision.gameObject);
        if (controller == OVRInput.Controller.None) return;

        TryAttach(controller);
    }

    void OnCollisionExit(Collision _collision)
    {
        OVRInput.Controller controller = GetControllerFromObject(_collision.gameObject);
        if (controller == OVRInput.Controller.None) return;

        ActiveControllers.Remove(controller);
        ControllerBodies.Remove(controller);
    }

    /// <summary>
    /// Figures out which OVR controller (left/right) the object belongs to by tag
    /// </summary>
    OVRInput.Controller GetControllerFromObject(GameObject obj)
    {
        if (obj.CompareTag("LeftController"))
            return OVRInput.Controller.LTouch;
        if (obj.CompareTag("RightController"))
            return OVRInput.Controller.RTouch;
        return OVRInput.Controller.None;
    }

    void TryAttach(OVRInput.Controller controller)
    {
        if (bAttached) return;
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller))
            AttachTo(controller);
    }

    public void AttachTo(OVRInput.Controller _controller)
    {
        // Haptic feedback
        OVRInput.SetControllerVibration(0.5f, 0.5f, _controller);

        if (ControllerBodies.ContainsKey(_controller))
            AddNewJoint(_controller, ControllerBodies[_controller]);
    }

    public void AddNewJoint(OVRInput.Controller _controller, Rigidbody _controllerBody)
    {
        JointObject = (Transform)Instantiate(HandleJointPrefab, HandlePosition.position, Quaternion.identity);
        JointObject.parent = transform;

        ConfigurableJoint cj = JointObject.GetComponent<ConfigurableJoint>();
        cj.connectedBody = _controllerBody;

        FixedJoint fj = JointObject.GetComponent<FixedJoint>();
        fj.connectedBody = transform.GetComponent<Rigidbody>();

        AttachedController = _controller;
        bAttached = true;
    }

    void Update()
    {
        if (bAttached)
        {
            // Release if grip is let go
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, AttachedController))
            {
                Disconnect();
            }
        }
        else
        {
            foreach (OVRInput.Controller controller in ActiveControllers)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller))
                {
                    AttachTo(controller);
                    break;
                }
            }
        }
    }

    public void Disconnect()
    {
        // Stop haptics
        OVRInput.SetControllerVibration(0, 0, AttachedController);

        Destroy(JointObject.gameObject);
        AttachedController = OVRInput.Controller.None;
        bAttached = false;
    }
}