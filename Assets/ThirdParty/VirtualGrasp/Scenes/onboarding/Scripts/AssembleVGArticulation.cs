// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using VirtualGrasp;
using System.Collections.Generic;

/** 
 * AssembleVGArticulation shows as a tutorial on how to use the VG_Controller.ChangeObjectJoint function for
 * assemble and dissemble non-physical objects (objects without rigid body or articulation body).
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vgonboarding_task5." + VG_Version.__VG_VERSION__ + ".html")]
public class AssembleVGArticulation : MonoBehaviour
{
    public Transform m_newParent = null;
    public Transform m_desiredPose = null;
    public float m_assembleDistance = 0.05f;
    public float m_disassembleDistance = 0.5f;
    public VG_Articulation m_assembleArticulation = null;

    private float timeAtDisassemble = 0.0F;
    private float assembleDelay = 1.0F;

    void Start()
    {
        
    }

    void Update()
    {
        assembleByJointChange();
        dessembleByJointChange();
    }

    void assembleByJointChange()
    {
        if ((Time.realtimeSinceStartup - timeAtDisassemble) > assembleDelay
           && (m_desiredPose.position - this.transform.position).magnitude < m_assembleDistance
           && VG_Controller.GetObjectJointType(this.transform, false) == VG_JointType.FLOATING)
        {
            m_desiredPose.gameObject.SetActive(false);
            this.transform.SetPositionAndRotation(m_desiredPose.position, m_desiredPose.rotation);

            if (m_newParent != null)
                this.transform.SetParent(m_newParent);

            VG_Controller.ChangeObjectJoint(transform, m_assembleArticulation);
        }
    }

    void dessembleByJointChange()
    {
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
        {
            if (hand.m_selectedObject == transform && hand.IsHolding()
                && VG_Controller.GetObjectJointType(transform) != VG_JointType.FLOATING)
            {
                VG_Controller.GetSensorPose(hand.m_avatarID, hand.m_side, out Vector3 sensor_pos, out Quaternion sensor_rot);
               
                if (VG_Controller.GetObjectJointState(transform) == 0.0f
                    && (sensor_pos - hand.m_hand.position).magnitude > m_disassembleDistance)
                {
                    m_desiredPose.gameObject.SetActive(true);
                    VG_Controller.RecoverObjectJoint(transform);
                    transform.SetParent(m_newParent.parent);

                    timeAtDisassemble = Time.realtimeSinceStartup;
                }
            }
        }
    }

}
