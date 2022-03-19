// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System.Collections.Generic;
using VirtualGrasp;
using UnityEngine;

/** 
 * ManageContainerObject shows as a tutorial on how to use the VG_Controller.OnObjectFullyReleased
 * and VG_Controller.OnObjectGrasped combined with Unity's physical joints to manage a 
 * container object to hold the contained physical objects stably without falling off. 
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vgonboarding_task3." + VG_Version.__VG_VERSION__ + ".html")]
public class ManageContainerObject : MonoBehaviour
{
    /// A set off objects that are actively colliding with this one.
    private HashSet<Transform> m_collisions = new HashSet<Transform>();
    /// A map of objects with ArticulationBody to their original parents.
    private Dictionary<Transform, Transform> m_parentCache = new Dictionary<Transform, Transform>();
    /// A map of objects with Rigidbody to the fixed joints connecting to this container object
    private Dictionary<Transform, FixedJoint> m_attachJoints = new Dictionary<Transform, FixedJoint>();
    /// If dot product between velocity and down is large enough (ie. vectors are aligned).
    public float m_dropAlignment = 0.8f;

    private void Start()
    {
        // Register the some grasp event listeners
        VG_Controller.OnObjectFullyReleased.AddListener(OnObjectFullyReleased);
        VG_Controller.OnObjectGrasped.AddListener(OnObjectGrasped);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // See if the object in collision is actually held by a hand (and is not a hand itself).
        bool valid_object = true;
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
        {
            if (hand.m_hand == collision.transform)
                valid_object &= false;
            if (hand.m_selectedObject == collision.transform && hand.IsHolding())
                valid_object &= false;
        }

        if (valid_object && // If it's valid ...
            (collision.rigidbody != null || collision.gameObject.TryGetComponent(out ArticulationBody ab)) && // and has a rigid body or articulation body ...
            Vector3.Dot(collision.relativeVelocity.normalized, Vector3.down) > m_dropAlignment) // .. and if the object is dropped from somewhat above.
        {
            Attach(collision.transform);
            m_collisions.Add(collision.transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        m_collisions.Remove(collision.transform);
    }

    private void OnObjectFullyReleased(VG_HandStatus hand)
    {
        if (m_collisions.Contains(hand.m_selectedObject))
            Attach(hand.m_selectedObject);
    }

    private void OnObjectGrasped(VG_HandStatus hand)
    {
        Unattach(hand.m_selectedObject);
    }

    void Attach(Transform attachedObject)
    {
        if(attachedObject.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (!attachedObject.gameObject.TryGetComponent<FixedJoint>(out FixedJoint joint))
            {
                joint = attachedObject.gameObject.AddComponent<FixedJoint>();
                m_attachJoints[attachedObject] = joint;
                if (transform.gameObject.TryGetComponent<Rigidbody>(out Rigidbody container_rb))
                    joint.connectedBody = container_rb;
                else if (transform.gameObject.TryGetComponent<ArticulationBody>(out ArticulationBody container_ab))
                    joint.connectedArticulationBody = container_ab;
            }
        }
        else if (attachedObject.gameObject.TryGetComponent<ArticulationBody>(out ArticulationBody ab))
        {
            if (transform.gameObject.TryGetComponent<ArticulationBody>(out ArticulationBody container_ab))
            {
                m_parentCache[attachedObject] = attachedObject.parent;
                attachedObject.SetParent(transform);
                ab.jointType = ArticulationJointType.FixedJoint;
            }
            else
                Debug.LogError("Can not attach object " + attachedObject.name + " with ArticulationBody to " + transform.name + " without ArticulationBody.");
        }
    }

    void Unattach(Transform attachedObject)
    {
        if (attachedObject.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (!m_attachJoints.ContainsKey(attachedObject))
                return;
            DestroyImmediate(m_attachJoints[attachedObject]);
            m_attachJoints.Remove(attachedObject);
        }
        else if (attachedObject.gameObject.TryGetComponent<ArticulationBody>(out ArticulationBody ab))
        {
            if (!transform.gameObject.TryGetComponent<ArticulationBody>(out ArticulationBody container_ab))
                return;
            if (attachedObject.parent != transform)
                return;

            attachedObject.SetParent(m_parentCache[attachedObject]);
        }
    }
}
