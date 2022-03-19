// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

//#define HIGHLIGHT_PLUS
//#define USE_CAKESLICE_OUTLINE // https://github.com/cakeslice/Outline-Effect

using UnityEngine;
using System.Collections.Generic;
using VirtualGrasp;
#if HIGHLIGHT_PLUS
using HighlightPlus;
#endif
#if USE_CAKESLICE_OUTLINE
using cakeslice;
#endif

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(VG_Highlighter))]
class VG_Highlighter_GUI : Editor
{
    private class State
    {
        public bool enabled = true;
        public string text = "";
        public string tooltip = "";
        public State(string _text, string _tooltip, bool _enabled = true)
        {
            text = _text;
            tooltip = _tooltip;
            enabled = _enabled;
        }
    }

    private Dictionary<VG_ReturnCode, State> m_states = new Dictionary<VG_ReturnCode, State>() {
        { VG_ReturnCode.OBJECT_NO_BAKE, new State("No bakes.", "These objects have not been baked.") },
        { VG_ReturnCode.OBJECT_NO_GRASPS, new State("Dynamic Grasps", "These objects have been baked and will be available for dynamic grasping.") },
        { VG_ReturnCode.SUCCESS, new State("+ Static Grasps", "These objects have been baked and grasps have been added in GraspStudio.") }
    };

    private List<VG_ReturnCode> state_copy = new List<VG_ReturnCode>();
    private HashSet<VG_ReturnCode> state_selected = new HashSet<VG_ReturnCode>();

    public void OnEnable()
    {
        foreach (var state in m_states)
        {
            state_copy.Add(state.Key);
            if (state.Value.enabled) state_selected.Add(state.Key);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!VG_Controller.IsEnabled()) 
        { 
            GUILayout.Label(
                "Play scene to highlight the state of objects in the scene.", 
                EditorStyles.wordWrappedLabel);
            return;
        }
        
        GUILayout.Label(
            "Select to highlight the state of objects in the scene.", 
            EditorStyles.wordWrappedLabel);

        Color color;
        bool state_changed = false;
        GUIStyle myStyle = new GUIStyle(GUI.skin.button);
        VG_Highlighter highlighter = target as VG_Highlighter;
        EditorGUILayout.BeginHorizontal();
        foreach (VG_ReturnCode code in state_copy)
        {
            color = highlighter.GetColor(code);
            myStyle.normal.textColor = color;
            myStyle.onNormal.textColor = color;
            myStyle.onHover.textColor = color;
            m_states[code].enabled = GUILayout.Toggle(m_states[code].enabled, new GUIContent(m_states[code].text, m_states[code].tooltip), myStyle);
            bool this_enabled = !m_states[code].enabled;
            if (this_enabled ^ state_selected.Contains(code)) state_changed |= true;
            if (this_enabled) state_selected.Add(code);
            else state_selected.Remove(code);
        }
        EditorGUILayout.EndHorizontal();

        if (state_changed) highlighter.HighlightObjectStatus(state_selected);
    }
}
#endif

/**
 * VG_Highlighter exemplifies how you could enable object highlighting based on the current hand status.
 * The MonoBehavior provides a tutorial on the VG API functions for some of the VG_Controller event functions, such as OnObjectSelected and OnObjectDeselected.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vghighlighter." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_Highlighter : MonoBehaviour
{
    [Tooltip("Set the shader that is used for highlighting.")]
    public Shader m_shader = null;

#if !HIGHLIGHT_PLUS && !USE_CAKESLICE_OUTLINE
    private List<Material>[] m_unhighlightedMaterials = new List<Material>[2];
    private List<Material>[] m_highlightedMaterials = new List<Material>[2];
#endif
    [Tooltip("Set the color that are used for highlighting objects selected by the left hand.")]
    public Color m_leftHandColor = Color.green;
    [Tooltip("Set the color that are used for highlighting objects selected by the right hand.")]
    public Color m_rightHandColor = Color.green;

    // Dictionary to keep track of highlighted objects.
    private Dictionary<VG_HandSide, Transform> m_highlightedObjects = new Dictionary<VG_HandSide, Transform>();

#if USE_CAKESLICE_OUTLINE
    private Outline[] m_outlines = new Outline[2] { new Outline(), new Outline() };
#endif
#if HIGHLIGHT_PLUS
    
#endif

    public Color GetColor(VG_ReturnCode code)
    {
        switch (code)
        {
            case VG_ReturnCode.OBJECT_NO_BAKE: return Color.red;
            case VG_ReturnCode.OBJECT_NO_GRASPS: return Color.green;
            case VG_ReturnCode.SUCCESS: return Color.cyan;
            default: return Color.black;
        }
    }

    void Start()
	{
        // Initialize the highlighted objects dictionary.
        m_highlightedObjects[VG_HandSide.LEFT] = null;
        m_highlightedObjects[VG_HandSide.RIGHT] = null;

        m_leftHandColor.a = 0.5f;
        m_rightHandColor.a = 0.5f;

        VG_Controller.OnObjectSelected.AddListener(Highlight);
        VG_Controller.OnObjectDeselected.AddListener(Unhighlight);
        VG_Controller.OnObjectGrasped.AddListener(Unhighlight);
    }

    public void HighlightObjectStatus(HashSet<VG_ReturnCode> states)
    {
        bool isSelected;
        int numSelected = 0;
        int numAll = 0;

        Color color;
        foreach (Transform t in VG_Controller.GetSelectableObjects())
        {
            color = Color.black;

            isSelected = false;
            foreach (VG_ReturnCode state in states)
            {
                isSelected = VG_Controller.GetUnbakedObjects(state).Contains(t);
                if (state == VG_ReturnCode.SUCCESS) isSelected = !isSelected;
                if (isSelected) { color = GetColor(state); break; }
            }            
            color.a = 0.5f;

            foreach (Material m in t.GetComponentInChildren<MeshRenderer>().materials)
            {
                m.shader = isSelected ? m_shader : Shader.Find("Legacy Shaders/Specular");
                m.SetFloat("_RimPower", 0.25f);
                m.SetColor("_RimColor", color);
            }
            if (isSelected) numSelected++;
            numAll++;
        }

        if (states.Count > 0)
            Debug.Log("Highlighting " + numSelected + " out of " + numAll + " interactable objects.");        
    }

    private void Highlight(VG_HandStatus hand)
    {
        //if (VG_Controller.IsPushable(obj) && 
        //    !VG_Controller.IsGraspable(obj))
        //return;

        // If the selected object is already highlighted
        if (hand.m_selectedObject == m_highlightedObjects[hand.m_side])
            return;

        // If the selected object is the same
        if (hand.m_selectedObject == m_highlightedObjects[hand.m_side == VG_HandSide.LEFT ? VG_HandSide.RIGHT : VG_HandSide.LEFT])
            return;

        m_highlightedObjects[hand.m_side] = hand.m_selectedObject;

        int id = hand.m_side < 0 ? 0 : 1;
        Color color = id == 0 ? m_leftHandColor : m_rightHandColor;
#if HIGHLIGHT_PLUS
        HighlightEffect highlight = hand.m_selectedObject.GetComponent<HighlightEffect>();
        if (highlight == null)
        {
            highlight = hand.m_selectedObject.gameObject.AddComponent<HighlightPlus.HighlightEffect>();
            highlight.highlighted = true;
            highlight.glow = 1.0f;
            highlight.overlayAnimationSpeed = 0.0f;
            highlight.overlay = 0.0f;
            highlight.seeThrough = SeeThroughMode.Never;
            highlight.outlineVisibility = Visibility.AlwaysOnTop;
        }
        else highlight.highlighted = true;
        for (int gp = 0; gp < highlight.glowPasses.Length; gp++)
            highlight.glowPasses[gp].color = color;
        highlight.outlineColor = color;
#else
#if USE_CAKESLICE_OUTLINE
        m_outlines[id].Renderer = hand.m_selectedObject.GetComponent<Renderer>();
        m_outlines[id].Enable();
#else
        Material[] objectMaterials = hand.m_selectedObject.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        m_unhighlightedMaterials[id] = new List<Material>(hand.m_selectedObject.GetComponentInChildren<MeshRenderer>().sharedMaterials);
        for (int i = 0, count = m_unhighlightedMaterials[id].Count; i < count; i++)
            m_unhighlightedMaterials[id][i] = new Material(m_unhighlightedMaterials[id][i]);

        m_highlightedMaterials[id] = new List<Material>();
        for (int i = 0, count = m_unhighlightedMaterials[id].Count; i < count; i++)
        {
            m_highlightedMaterials[id].Add(new Material(m_unhighlightedMaterials[id][i]));
            m_highlightedMaterials[id][i].shader = m_shader;
        }

        MeshRenderer objectRenderer = m_highlightedObjects[hand.m_side].GetComponentInChildren<MeshRenderer>();
        for (int i = 0, count = objectRenderer.materials.Length; i < count; i++)
        {
            objectMaterials[i] = m_highlightedMaterials[id][i];
            objectMaterials[i].SetColor("_RimColor", color);
        }

        objectRenderer.sharedMaterials = objectMaterials;
#endif
#endif
    }

    private void Unhighlight(VG_HandStatus hand)
    {
        int id = hand.m_side < 0 ? 0 : 1;
        if (!m_highlightedObjects.ContainsKey(hand.m_side))
            return;

        // Got no object (or the same object as before), got no unhighlight
        Transform highlightedObject = m_highlightedObjects[hand.m_side];
        if (highlightedObject == null) return;

#if HIGHLIGHT_PLUS
        if (hand.m_formerSelectedObject.GetComponent<HighlightPlus.HighlightEffect>() != null)
            hand.m_formerSelectedObject.GetComponent<HighlightPlus.HighlightEffect>().highlighted = false;
#else
#if USE_CAKESLICE_OUTLINE
        m_outlines[id].Disable();
        m_outlines[id].Renderer = null;
#else
        highlightedObject.GetComponentInChildren<MeshRenderer>().sharedMaterials = m_unhighlightedMaterials[id].ToArray();
#endif
#endif
        m_highlightedObjects[hand.m_side] = null;
    }
}
