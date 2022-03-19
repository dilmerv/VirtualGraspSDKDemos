// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

//#define USE_UNITYXR_CONTROLLER 

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VirtualGrasp;

/**
 * This is an external controller class that supports the UnityXR controller (such as provided by Pico or Oculus integrations) as an external controller.
 * Please refer to https://docs.virtualgrasp.com/controllers.html for the definition of an external controller for VG.
 * 
 * The following requirements have to be met to be able to enable the #define USE_UNITYXR_CONTROLLER above and use the controller:
 * - You have the Unity XR Management package installed into your Unity project.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vg_ec_unityxrhand." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_EC_UnityXRHand : VG_ExternalController
{
    private InputDevice m_device;

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

    public VG_EC_UnityXRHand(int avatarID, VG_HandSide side)
    {
        m_avatarID = avatarID;
        m_handType = side;
        Initialize();
    }

    public new void Initialize()
    {
        m_mapping = new HandMapping();
        base.Initialize();
        m_initialized = true;
    }

    public override bool Compute()
    {
        if (!m_initialized) { Initialize(); return false; }

        if (!m_device.isValid)
        {
            m_device = InputDevices.GetDeviceAtXRNode(m_handType == VG_HandSide.LEFT ? XRNode.LeftHand : XRNode.RightHand);
            return false;
        }

        if (m_device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p) &&
            m_device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q))
        {
            SetPose(0, Matrix4x4.TRS(p, q, Vector3.one));
            return true;
        }

        return false;
    }

    public override float GetGrabStrength()
    {
        if (!m_initialized || !m_device.isValid) return 0.0f;
        float trigger = 0.0f;
        switch (VG_Controller.GetTriggerButton())
        {
            case VG_VrButton.TRIGGER:
                m_device.TryGetFeatureValue(CommonUsages.trigger, out trigger); break;
            case VG_VrButton.GRIP:
                m_device.TryGetFeatureValue(CommonUsages.grip, out trigger); break;
            case VG_VrButton.GRIP_OR_TRIGGER:
                m_device.TryGetFeatureValue(CommonUsages.trigger, out trigger);
                m_device.TryGetFeatureValue(CommonUsages.grip, out float trigger2);
                trigger = Mathf.Max(trigger, trigger2);
                break;
        }
        return trigger;
    }

    public override Color GetConfidence()
    {
        return Color.yellow;
    }

    public override void HapticPulse(VG_HandStatus hand, float amplitude = 0.5F, float duration = 0.01F, int finger = 5)
    {
        if (!m_initialized || !m_device.isValid) return;
        HapticCapabilities capabilities;
        if (m_device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            m_device.SendHapticImpulse(0, amplitude, duration);
    }
}
