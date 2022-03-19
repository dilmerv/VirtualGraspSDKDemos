// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using VirtualGrasp;

/** 
 * ToggleLight shows as a tutorial on a non-physical two-stage button setup 
 * through VG_Articulation and how to use VG_Controller.GetObjectJointState to toggle light on and off. 
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vgonboarding_task1.html")]
public class ToggleLight : MonoBehaviour
{
    public Light m_light = null;
    private VG_Articulation m_articulation = null;

    void Start()
    {
        m_articulation = GetComponent<VG_Articulation>();
    }

    void Update()
    {
        float state = VG_Controller.GetObjectJointState(transform);
        if (state == m_articulation.m_min) m_light.enabled = false;
        else if (state == m_articulation.m_max) m_light.enabled = true;
    }
}
