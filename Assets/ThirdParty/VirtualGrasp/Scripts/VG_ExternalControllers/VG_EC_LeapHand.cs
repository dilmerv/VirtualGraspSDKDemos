// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

//#define USE_LEAP_CONTROLLER 

using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;
#if USE_LEAP_CONTROLLER
using Leap.Unity;
#endif

/**
 * This is an external controller class that supports the LeapMotion controller as an external controller.
 * Please refer to https://docs.virtualgrasp.com/controllers.html for the definition of an external controller for VG.
 * 
 * The following requirements have to be met to be able to enable the #define USE_LEAP_CONTROLLER above and use the controller:
 * - You have a Core Assets plugin from https://developer.leapmotion.com/releases imported into your Unity project.
 * - Note that Core Assets > 4.4.0 are for LeapMotion SDK 4, older are for LeapMotion SDK 3 (lastest CA 4.3.4).
 * - You have the corresponding LeapMotion SDK (https://developer.leapmotion.com/sdk-leap-motion-controller/) installed on your computer.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vg_ec_leaphand." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_EC_LeapHand : VG_ExternalController
{
#if USE_LEAP_CONTROLLER
    static LeapProvider m_provider = null;
    Leap.Hand m_hand = null;

    static public int LeapBoneToInt(int finger, int bone)
    {
        return 4 * finger + bone + 1;
    }

    static public int LeapBoneToInt(Leap.Finger.FingerType finger, Leap.Bone.BoneType bone)
    {
        return LeapBoneToInt((int)finger, (int)bone);
    }

    static public void IntToLeapBone(int id, out int finger, out Leap.Bone.BoneType bone)
    {
        bone = (Leap.Bone.BoneType)((id - 1) % 4);
        finger = (id - (int)bone) / 4;
    }
#endif

    [Serializable]
    public class HandMapping : VG_ExternalControllerMapping
    {
        public override void Initialize(int avatarID, VG_HandSide side)
        {
            base.Initialize(avatarID, side);
            m_BoneToTransform = new Dictionary<int, Transform>()
            {
#if USE_LEAP_CONTROLLER
			{ 0, Hand_WristRoot },
            { LeapBoneToInt(0, 0), null },
            { LeapBoneToInt(0, 1), Hand_Thumb1 },
            { LeapBoneToInt(0, 2), Hand_Thumb2 },
            { LeapBoneToInt(0, 3), Hand_Thumb3 },
            { LeapBoneToInt(1, 0), null },
            { LeapBoneToInt(1, 1), Hand_Index1 },
            { LeapBoneToInt(1, 2), Hand_Index2 },
            { LeapBoneToInt(1, 3), Hand_Index3 },
            { LeapBoneToInt(2, 0), null },
            { LeapBoneToInt(2, 1), Hand_Middle1 },
            { LeapBoneToInt(2, 2), Hand_Middle2 },
            { LeapBoneToInt(2, 3), Hand_Middle3 },
            { LeapBoneToInt(3, 0), null },
            { LeapBoneToInt(3, 1), Hand_Ring1 },
            { LeapBoneToInt(3, 2), Hand_Ring2 },
            { LeapBoneToInt(3, 3), Hand_Ring3 },
            { LeapBoneToInt(4, 0), null },
            { LeapBoneToInt(4, 1), Hand_Pinky1 },
            { LeapBoneToInt(4, 2), Hand_Pinky2 },
            { LeapBoneToInt(4, 3), Hand_Pinky3 }
#endif
            };

            m_BoneToParent = new Dictionary<int, int>()
            {
            };
#if USE_LEAP_CONTROLLER
            m_BoneToParent[LeapBoneToInt(0, 0)] = 0;
            m_BoneToParent[LeapBoneToInt(0, 1)] = LeapBoneToInt(0, 0);
            m_BoneToParent[LeapBoneToInt(0, 2)] = LeapBoneToInt(0, 1);
            m_BoneToParent[LeapBoneToInt(0, 3)] = LeapBoneToInt(0, 2);
            m_BoneToParent[LeapBoneToInt(1, 0)] = 0;
            m_BoneToParent[LeapBoneToInt(1, 1)] = LeapBoneToInt(1, 0);
            m_BoneToParent[LeapBoneToInt(1, 2)] = LeapBoneToInt(1, 1);
            m_BoneToParent[LeapBoneToInt(1, 3)] = LeapBoneToInt(1, 2);
            m_BoneToParent[LeapBoneToInt(2, 0)] = 0;
            m_BoneToParent[LeapBoneToInt(2, 1)] = LeapBoneToInt(2, 0);
            m_BoneToParent[LeapBoneToInt(2, 2)] = LeapBoneToInt(2, 1);
            m_BoneToParent[LeapBoneToInt(2, 3)] = LeapBoneToInt(2, 2);
            m_BoneToParent[LeapBoneToInt(3, 0)] = 0;
            m_BoneToParent[LeapBoneToInt(3, 1)] = LeapBoneToInt(3, 0);
            m_BoneToParent[LeapBoneToInt(3, 2)] = LeapBoneToInt(3, 1);
            m_BoneToParent[LeapBoneToInt(3, 3)] = LeapBoneToInt(3, 2);
            m_BoneToParent[LeapBoneToInt(4, 0)] = 0;
            m_BoneToParent[LeapBoneToInt(4, 1)] = LeapBoneToInt(4, 0);
            m_BoneToParent[LeapBoneToInt(4, 2)] = LeapBoneToInt(4, 1);
            m_BoneToParent[LeapBoneToInt(4, 3)] = LeapBoneToInt(4, 2);
#endif
        }
    }

    public VG_EC_LeapHand(int avatarID, VG_HandSide side)
    {
        m_avatarID = avatarID;
        m_handType = side;
    }

    public new void Initialize()
    {
#if USE_LEAP_CONTROLLER
        if (m_provider == null)
        {
            m_provider = GameObject.FindObjectOfType<VG_MainScript>().gameObject.AddComponent<LeapServiceProvider>();
        }
        
        if (m_provider != null)
        {
            m_mapping = new HandMapping();
            base.Initialize();
        }
        
        m_initialized = (m_provider != null);
#endif
    }

    private Matrix4x4 modifyPose(Matrix4x4 rawPose, bool isWrist)
    {
        if (isWrist) return Matrix4x4.TRS(rawPose.GetColumn(3), Quaternion.LookRotation(-rawPose.GetColumn(1), rawPose.GetColumn(2)), Vector3.one);
        else return Matrix4x4.TRS(rawPose.GetColumn(3), Quaternion.LookRotation(m_handType == VG_HandSide.LEFT ? rawPose.GetColumn(0) : -rawPose.GetColumn(0), rawPose.GetColumn(2)), Vector3.one);
    }

    public override bool Compute()
    {
#if USE_LEAP_CONTROLLER
        if (!m_initialized || m_mapping == null) Initialize();
        if (m_mapping == null) return false;
        if (m_provider == null) return false;

        Leap.Frame frame = m_provider.CurrentFrame;
        if (frame == null) return false;

        m_hand = null;
        foreach (Leap.Hand hand in frame.Hands)
        {
            if (hand.IsLeft && m_handType == VG_HandSide.LEFT) { m_hand = hand; break; }
            if (!hand.IsLeft && m_handType == VG_HandSide.RIGHT) { m_hand = hand; break; }
        }
        if (m_hand == null) return false;

        for (int boneId = 0; boneId < GetNumBones(); boneId++)
        {
            if (boneId == 0)
            {
                SetPose(boneId, modifyPose(Matrix4x4.TRS(m_hand.WristPosition.ToVector3(), m_hand.Rotation.ToQuaternion(), Vector3.one), true));
            }
            else
            {
                IntToLeapBone(boneId, out int finger, out Leap.Bone.BoneType bone);
                Leap.Bone b = m_hand.Fingers[finger].Bone(bone);
                SetPose(boneId, modifyPose(Matrix4x4.TRS(b.NextJoint.ToVector3(), b.Rotation.ToQuaternion(), Vector3.one), false));
            }
        }

        return true;
#else
        return false;
#endif
    }

    public override float GetGrabStrength()
    {        
#if USE_LEAP_CONTROLLER
        // Get grab strength from Leap if available
        return m_hand != null ? m_hand.GrabStrength : 0.0f;
        //return -1.0f; // let VG decide from full DOF
#else
        return 0.0f;
#endif        
    }

    public override Color GetConfidence()
    {
#if USE_LEAP_CONTROLLER
        if (m_hand != null) return Color.black;
        return m_hand.Confidence > 0.5f ? Color.green : Color.red;
#else
        return Color.yellow;
#endif
    }

    public override void HapticPulse(VG_HandStatus hand, float amplitude = 0.5F, float duration = 0.01F, int finger = 5)
    {
    }
}
