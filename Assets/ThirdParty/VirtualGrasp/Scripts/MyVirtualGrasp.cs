// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using VirtualGrasp;
using UnityEngine;

/**
 * MyVirtualGrasp is a customizable main tutorial component.
 *
 * MyVirtualGrasp inherits from VG_MainScript, which wraps the main communication functions of the VirtualGrasp API.
 * VG_MainScript inherits from Monobehavior so you can use this as a component to a GameObject in Unity.
 * All the API functions you want to use in your own scripts can be accessed through VG_Controller.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_myvirtualgrasp." + VG_Version.__VG_VERSION__ + ".html")]
public class MyVirtualGrasp : VG_MainScript
{
    override public void Awake()
    {
        base.Awake();
        VG_Controller.Initialize();
        VG_ExternalControllerManager.Initialize(this);
    }

    override public void Update()
    {
        base.Update();
    }

    override public void FixedUpdate()
    {
        base.FixedUpdate();
    }
}
