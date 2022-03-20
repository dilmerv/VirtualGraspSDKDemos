using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VirtualGrasp;

public class ArticulationStats : MonoBehaviour
{
    void Start()
    {
        VG_Controller.OnObjectGrasped.AddListener((handStatus) =>
        {
            var overlayStats = handStatus.m_selectedObject.GetComponentInChildren<TextMeshPro>();
            if (overlayStats != null)
            {
                overlayStats.text = $"Grab Strength:\n{handStatus.m_grabStrength}";
            }
        });
    }
}
