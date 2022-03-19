// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using VirtualGrasp;

/** 
 * DissembleWithDistance shows as a tutorial on how to use the VG_Controller.ChangeObjectJoint
 * function to dissemble parts that are initially non-physical objects.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vgonboarding_task2." + VG_Version.__VG_VERSION__ + ".html")]
public class DissembleWithDistance : MonoBehaviour
{
    public float m_disassembleDistance = 0.2f;

    void Update()
    {        
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
        {
            if(hand.m_selectedObject == transform && hand.IsHolding()
                && VG_Controller.GetObjectJointType(transform) != VG_JointType.FLOATING)
            {
                VG_Controller.GetSensorPose(hand.m_avatarID, hand.m_side, out Vector3 sensor_pos, out Quaternion sensor_rot);
                if((sensor_pos - hand.m_hand.position).magnitude > m_disassembleDistance)
                {
                   VG_Controller.ChangeObjectJoint(transform, VG_JointType.FLOATING);
                    transform.SetParent(transform.parent.parent);

                    // Set this object as physical object
                    if(!transform.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
                    {
                        rb = transform.gameObject.AddComponent<Rigidbody>();
                        rb.useGravity = true;
                    }
                    if (!transform.TryGetComponent<Collider>(out _))
                        transform.gameObject.AddComponent<BoxCollider>();
                }
            }
        }
    }
}
