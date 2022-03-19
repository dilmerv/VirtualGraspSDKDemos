// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;

/**
 * This is an external controller class that supports a Mouse controller as an external controller.
 * Please refer to https://docs.virtualgrasp.com/controllers.html for the definition of an external controller for VG.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vg_ec_generichand." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_EC_GenericHand : VG_ExternalController
{
    [Serializable]
    public class HandMapping : VG_ExternalControllerMapping
    {
        public override void Initialize(int avatarID, VG_HandSide side)
        {
            base.Initialize(avatarID, side);
            m_BoneToTransform = new Dictionary<int, Transform>()
            {
                { 0, Hand_WristRoot },
                { 1, Hand_Thumb1 },
                { 2, Hand_Thumb2 },
                { 3, Hand_Thumb3 },
                { 4, Hand_Index1 },
                { 5, Hand_Index2 },
                { 6, Hand_Index3 },
                { 7, Hand_Middle1 },
                { 8, Hand_Middle2 },
                { 9, Hand_Middle3 },
                { 10, Hand_Ring1 },
                { 11, Hand_Ring2 },
                { 12, Hand_Ring3 },
                { 13, Hand_Pinky1 },
                { 14, Hand_Pinky2 },
                { 15, Hand_Pinky3 }
            };
        }
    }

    public VG_EC_GenericHand(int avatarID, VG_HandSide side)
    {
        m_avatarID = avatarID;
        m_handType = side;
        Initialize();
    }

    public new void Initialize()
    {
        m_mapping = new HandMapping();
        base.Initialize();
    }

    public override bool Compute()
    {
        for (int bone = 0; bone < m_mapping.GetNumBones(); bone++)
        {
            if (!m_mapping.GetTransform(bone, out Transform pose)) continue;
            SetPose(bone, Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one));
        }
        return true;
    }

    public override float GetGrabStrength()
    {
        return 0.0f;
    }

    public override Color GetConfidence()
    {
        return Color.yellow;
    }

    public override void HapticPulse(VG_HandStatus hand, float amplitude = 0.5F, float duration = 0.01F, int finger = 5)
    {
    }
}
