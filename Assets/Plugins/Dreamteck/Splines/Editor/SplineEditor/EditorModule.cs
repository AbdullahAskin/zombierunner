﻿namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class EditorModule
    {
        protected string prefPrefix = "";

        public virtual void Select()
        {
            LoadState();
        }

        public virtual void Deselect()
        {
            SaveState();
        }

        public virtual void BeforeSceneDraw(SceneView current)
        {
        }

        public virtual void DrawScene()
        {

        }

        public virtual void OnSceneDraw()
        {
        }

        public virtual void DrawInspector()
        {
        }

        public virtual GUIContent GetIconOff()
        {
            return new GUIContent("OFF", "Point Module Off");
        }

        public virtual GUIContent GetIconOn()
        {
            return new GUIContent("ON", "Point Module On");
        }

        protected virtual void RecordUndo(string title)
        {
        }

        protected virtual void Repaint()
        {
        }

        protected void SaveBool(string variableName, bool value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetBool(prefPrefix + "." + variableName, value);
        }

        protected void SaveInt(string variableName, int value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetInt(prefPrefix + "." + variableName, value);
        }

        protected void SaveFloat(string variableName, float value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetFloat(prefPrefix + "." + variableName, value);
        }

        protected void SaveString(string variableName, string value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetString(prefPrefix + "." + variableName, value);
        }

        protected bool LoadBool(string variableName)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetBool(prefPrefix + "." + variableName, false);
        }

        protected int LoadInt(string variableName, int defaultValue = 0)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetInt(prefPrefix + "." + variableName, defaultValue);
        }

        protected float LoadFloat(string variableName, float d = 0f)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetFloat(prefPrefix + "." + variableName, d);
        }

        protected string LoadString(string variableName)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetString(prefPrefix + "." + variableName, "");
        }

        public virtual void SaveState()
        {

        }

        public virtual void LoadState()
        {

        }

        internal static GUIContent IconContent(string title, string iconName, string description)
        {
            GUIContent content = new GUIContent(title, description);
            string path = "Splines/Editor/Icons";
            if (EditorGUIUtility.isProSkin) iconName += "_dark";
            Texture2D tex = ImageDB.GetImage(iconName + ".png", path);
            if (tex != null)
            {
                content.image = tex;
                content.text = "";
            }
            return content;
        }
    }
}
