// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System.Collections.Generic;
using VirtualGrasp;
using UnityEngine;

/** 
 * VG_ExternalControllerManager exemplifies how you could provide custom controller scripts for your application.
 * The class, used in MyVirtualGrasp.cs, provides a tutorial on the VG API functions for external sensor control.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vgexternalcontrollermanager." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_ExternalControllerManager
{
    /// Map from each avatar to a sensorsetup.
    static private Dictionary<int, VG_SensorSetup> m_externalSensors = new Dictionary<int, VG_SensorSetup>() { };
    /// Map from a wrist (instance ID) to an external controller instance controlling it.
    static private Dictionary<int, VG_ExternalController> m_controllers = new Dictionary<int, VG_ExternalController>();
    /// Flag if the manager is initialized or not.
    static private bool m_initialized = false;

    static public void Initialize(VG_MainScript vg)
    {
        m_initialized = false;
        /// The controller signals should be processed before VG's update loop.
        VG_Controller.OnPreUpdate.AddListener(ExternalSensorUpdate);
        
        /// We also assign some haptic signals here.
        VG_Controller.OnObjectCollided.AddListener(TriggerHaptics);
        VG_Controller.OnObjectReleased.AddListener(TriggerHaptics);
        VG_Controller.OnObjectGrasped.AddListener(TriggerHaptics);

        // Clear out the maps and assign them (avatars and sensors + wrists and controllers)
        m_externalSensors.Clear();
        m_controllers.Clear();
        for (int i = 0; i < vg.m_sensors.Count; i++)
        {
            if (vg.m_sensors[i].m_sensor == VG_SensorType.EXTERNAL_CONTROLLER)
            {
                foreach (VG_Avatar avatar in vg.m_sensors[i].m_avatars)
                    m_externalSensors.Add(avatar.m_avatarID, vg.m_sensors[i]);
            }
        }
 
        foreach (KeyValuePair<int, VG_SensorSetup> avatar in m_externalSensors)
            RegisterExternalController(avatar.Key, avatar.Value.m_external);

        m_initialized = true;
    }

    /// If the actual Transforms are used, we can't use VG offset or origin data.
    static private void ClearOffsetOrigin(int avatarID)
    {
        m_externalSensors[avatarID].m_origin = null;
        m_externalSensors[avatarID].m_offset.position = Vector3.zero;
        m_externalSensors[avatarID].m_offset.rotation = Vector3.zero;
    }

    // Register an external controller type for an avatar.
    static public void RegisterExternalController(int avatarID, string controllerType)
    {
        foreach (VG_HandSide side in new List<VG_HandSide>() { VG_HandSide.LEFT, VG_HandSide.RIGHT })
        {
            if (VG_Controller.GetBone(avatarID, side, VG_BoneType.WRIST, out int iid, out _) == null)
            {
                VG_Debug.LogWarning("Could not initialize controller to " + side + " " + controllerType + " on avatar #" + avatarID + ". Error:  No hand found.");
                continue;
            }

            switch (controllerType)
            {
                case "QuestHand": m_controllers.Add(iid, new VG_EC_OculusHand(avatarID, side)); break;
                case "UnityXR": m_controllers.Add(iid, new VG_EC_UnityXRHand(avatarID, side)); break;
                case "MouseHand": m_controllers.Add(iid, new VG_EC_MouseHand(avatarID, side)); break;
                case "LeapHand": m_controllers.Add(iid, new VG_EC_LeapHand(avatarID, side)); break;
                default:
                    //VG_Debug.LogWarning("No VG_ExternalController found for \"" + controllerType + "\". Program it and/or add it to this list. Replacing with VG_EC_GenericHand.");
                    m_controllers.Add(iid, new VG_EC_GenericHand(avatarID, side));
                    ClearOffsetOrigin(avatarID); // Offsets and origins have to be zero for controllers directly working on Unity Transforns for now, otherwise they will spin.
                    break;
            }
        }

        VG_Controller.RegisterExternalControllers(m_controllers);
        m_externalSensors[avatarID].m_external = controllerType;
    }

    // Run the controller updates of all registered controllers.
    static public void ExternalSensorUpdate()
    {
        if (!m_initialized) return;

        foreach (int avatarID in m_externalSensors.Keys)
        {
            foreach (VG_HandSide hside in new List<VG_HandSide>() { VG_HandSide.LEFT, VG_HandSide.RIGHT })
            {
                if (VG_Controller.GetBone(avatarID, hside, VG_BoneType.WRIST, out int wristID, out _) != null &&
                    m_controllers.ContainsKey(wristID))
                {
                    VG_ExternalController controller = m_controllers[wristID];
                    controller.Compute();
                    VG_Controller.SetExternalGrabStrength(avatarID, hside, controller.GetGrabStrength());

                    // Activate if you want to draw debug skeleton.
                    //controller.DebugDraw(m_externalSensors[avatarID].m_origin);
                }
            }
        }
    }

    // Process haptics signals (see Listeners in Initialize()).
    static public void TriggerHaptics(VG_HandStatus hand)
    {
        int wristID = hand.m_hand.GetInstanceID();
        if (m_controllers.ContainsKey(wristID))
            m_controllers[wristID].HapticPulse(hand);
    }
}
