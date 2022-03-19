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
[HelpURL("https://docs.virtualgrasp.com/unity_vg_ec_mousehand." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_EC_MouseHand : VG_ExternalController
{
    private int mouse_held = 0;
    private int filter = 15;
    private float depth = .5f;

    [Serializable]
    public class HandMapping : VG_ExternalControllerMapping
    {
        public override void Initialize(int avatarID, VG_HandSide side)
        {
            base.Initialize(avatarID, side);
            m_BoneToTransform = new Dictionary<int, Transform>()
            {
                { 0, Hand_WristRoot }
            };
        }
    }

    public VG_EC_MouseHand(int avatarID, VG_HandSide side)
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
        if (Input.GetMouseButton(m_handType == VirtualGrasp.VG_HandSide.LEFT ? 0 : 1)) mouse_held = Mathf.Min(filter, mouse_held + 1);
        else mouse_held = Mathf.Max(0, mouse_held - 1);
        depth = Mathf.Clamp(depth + Input.mouseScrollDelta.y / 5.0f, .5f, 3.0f);
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Quaternion q = Camera.main.transform.rotation;
        Vector3 p = ray.origin + depth * ray.direction + q * (m_handType == VirtualGrasp.VG_HandSide.LEFT ? Vector3.left : Vector3.right) * .1f;
        SetPose(0, Matrix4x4.TRS(p, q, Vector3.one));
        return true;
    }

    public override float GetGrabStrength()
    {
        return (float) mouse_held / filter;
    }

    public override Color GetConfidence()
    {
        return Color.yellow;
    }
    public override void HapticPulse(VG_HandStatus hand, float amplitude = 0.5F, float duration = 0.01F, int finger = 5)
    {
    }
}
