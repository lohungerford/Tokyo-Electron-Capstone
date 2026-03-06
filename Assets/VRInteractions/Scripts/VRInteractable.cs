using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for interactables. Tracks whether the object is interactable or not and contains facilities for managing colliders.
/// </summary>
public class VRInteractable : ExposableMonobehaviour
{
    public bool Interactable = true;

    private Rigidbody rb3d;
    private List<Collider> mColliders3D;

    void Awake()
    {
        UpdateColliders3D();
        mCache = Interactable;
    }

    public void UpdateColliders3D()
    {
        rb3d = gameObject.GetComponent<Rigidbody>();
        mColliders3D = new List<Collider>();

        if (rb3d != null)
        {
            mColliders3D.AddRange(rb3d.gameObject.GetComponentsInChildren<Collider>());
            mColliders3D.Add(rb3d.gameObject.GetComponent<Collider>());
        }
        else
        {
            mColliders3D.AddRange(GetComponentsInChildren<Collider>());
            mColliders3D.Add(GetComponent<Collider>());
        }
    }

    public void IgnoreColliders(Rigidbody _rigidbody)
    {
        Collider[] colliders = _rigidbody.GetComponentsInChildren<Collider>();
        IgnoreColliders3D(colliders, mColliders3D.ToArray());
    }

    public void IgnoreColliders(Transform _object)
    {
        Collider[] colliders = _object.GetComponentsInChildren<Collider>();
        IgnoreColliders3D(colliders, mColliders3D.ToArray());
    }

    public void RemoveIgnoreColliders(Rigidbody _rigidbody)
    {
        Collider[] colliders = _rigidbody.GetComponentsInChildren<Collider>();
        IgnoreColliders3D(colliders, mColliders3D.ToArray(), false);
    }

    public void RemoveIgnoreColliders(Transform _object)
    {
        Collider[] colliders = _object.GetComponentsInChildren<Collider>();
        IgnoreColliders3D(colliders, mColliders3D.ToArray(), false);
    }

    public static void IgnoreColliders3D(Collider[] _colliders, Collider[] _otherColliders, bool _ignore = true)
    {
        foreach (Collider col in _colliders)
        {
            foreach (Collider otherCol in _otherColliders)
            {
                if (otherCol != null && col != null)
                    Physics.IgnoreCollision(col, otherCol, _ignore);
            }
        }
    }

    /// <summary>
    /// Ignores colliders for all active Meta controllers
    /// </summary>
    void IgnoreAllControllerColliders()
    {
        foreach (OVRInput.Controller controller in VRGripper.GetControllers())
        {
            // Find the controller GameObject by tag and ignore its colliders
            string tag = controller == OVRInput.Controller.LTouch ? "LeftController" : "RightController";
            GameObject controllerObj = GameObject.FindGameObjectWithTag(tag);
            if (controllerObj != null)
                IgnoreColliders(controllerObj.transform);
        }
    }

    /// <summary>
    /// Removes the physics ignore for all Meta controllers
    /// </summary>
    void RemoveIgnoreAllControllerColliders()
    {
        foreach (OVRInput.Controller controller in VRGripper.GetControllers())
        {
            string tag = controller == OVRInput.Controller.LTouch ? "LeftController" : "RightController";
            GameObject controllerObj = GameObject.FindGameObjectWithTag(tag);
            if (controllerObj != null)
                RemoveIgnoreColliders(controllerObj.transform);
        }
    }

    bool mCache;

    public void Update()
    {
        if (mCache != Interactable)
        {
            if (Interactable == false)
                IgnoreAllControllerColliders();
            else
                RemoveIgnoreAllControllerColliders();

            mCache = Interactable;
        }
    }
}