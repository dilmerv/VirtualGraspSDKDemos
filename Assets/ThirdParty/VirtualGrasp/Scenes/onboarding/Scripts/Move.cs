// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using UnityEngine.XR;

public class Move : MonoBehaviour
{
    public Transform m_character = null;
    public bool m_verticalOnly = false;
    private Vector2 m_axisL = Vector2.zero;
    private Vector2 m_axisR = Vector2.zero;
    private Camera m_camera = null;
    void Start()
    {
        if (m_character == null) m_character = transform;
        m_camera = GetComponentInChildren<Camera>();
        if (m_camera == null) m_camera = Camera.main;
    }

    void FixedUpdate()
    {
        if (!InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out m_axisL))
            m_axisL = Vector2.zero;

        if (!InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out m_axisR))
            m_axisR = Vector2.zero;

        if (m_verticalOnly)
        {
            float y = Mathf.Abs(m_axisL.y) > Mathf.Abs(m_axisR.y) ? m_axisL.y : m_axisR.y;
            if (Mathf.Abs(y) > 0.1f) m_character.Translate(0.03f * y * Vector3.up, Space.World);
        }
        else
        {
            float x = Mathf.Abs(m_axisL.x) > Mathf.Abs(m_axisR.x) ? m_axisL.x : m_axisR.x;
            float y = Mathf.Abs(m_axisL.y) > Mathf.Abs(m_axisR.y) ? m_axisL.y : m_axisR.y;
            if (Mathf.Abs(x) > 0.1f) m_character.Rotate(new Vector3(0, 2.0f * x, 0), Space.Self);
            if (Mathf.Abs(y) > 0.1f) m_character.Translate(0.03f * y * (m_camera.transform.rotation * Vector3.forward), Space.World);
        }
    }
}
