﻿using System.Linq;
#if UNITY_EDITOR
using migrationtool.models;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace migrationtool.windows
{
    public class MergingWizard : ScriptableWizard
    {
        public Action<List<FoundScript>> onComplete;


        private List<FoundScript> foundScripts;

        private FoundScriptWrapper[] foundScriptWrappers;

        GUIStyle richtextStyle;
        GUIStyle classNameStyle;
        GUIStyle paddingStyle;
        GUIStyle horizontalLineStyle;
        GUIStyle verticalMarginStyle;

        Vector2 scrollPosition = Vector2.zero;

        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            var wizard = DisplayWizard<MergingWizard>("Merge fieldNames", "Merge");
            wizard.foundScripts = scriptsToMerge;
            wizard.foundScriptWrappers = new FoundScriptWrapper[scriptsToMerge.Count];

            for (int i = 0; i < scriptsToMerge.Count; i++)
            {
                wizard.foundScriptWrappers[i] = new FoundScriptWrapper(scriptsToMerge[i]);
            }

            return wizard;
        }


        private void OnEnable()
        {
            richtextStyle = new GUIStyle() {richText = true, wordWrap = true};
            classNameStyle = new GUIStyle() {fontSize = 14};
            paddingStyle = new GUIStyle() {padding = new RectOffset(15, 15, 15, 15)};
            horizontalLineStyle = new GUIStyle() {margin = new RectOffset(0, 0, 10, 8), fixedHeight = 1};
            horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
            verticalMarginStyle = new GUIStyle() {margin = new RectOffset(0, 0, 0, 6)};
        }

        private class FoundScriptWrapper
        {
            public FoundScript FoundScript;
            public bool[] FieldSelectionStates;
            public int[] OptionSelections;

            public FoundScriptWrapper(FoundScript _foundScript)
            {
                FoundScript = _foundScript;
                FieldSelectionStates = new bool[_foundScript.MergeNodes.Count];
                for (int i = 0; i < FieldSelectionStates.Length; i++)
                {
                    FieldSelectionStates[i] = true;
                }

                OptionSelections = new int[_foundScript.MergeNodes.Count];
                for (int i = 0; i < OptionSelections.Length; i++)
                {
                    OptionSelections[i] = 0;
                }
            }
        }

        private GUILayoutOption GetColumnWidth(int _columns)
        {
            float singleColumn = (Screen.width - 50) / 12;
            return GUILayout.Width(singleColumn * _columns);
        }

        protected override bool DrawWizardGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            EditorGUILayout.BeginVertical(paddingStyle);

            EditorGUILayout.LabelField(
                "The following class fields differ between the original and current project. Proposals for substitute fields are shown. Please select the correct field manually.",
                richtextStyle);
            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            for (int i = 0; i < foundScripts.Count; i++)
            {
                ClassModel classModel = foundScripts[i].newClassModel;
                FoundScriptWrapper wrapper = foundScriptWrappers[i];
                List<MergeNode> fieldsToMerge = foundScripts[i].MergeNodes;

                EditorGUILayout.LabelField(classModel.FullName, classNameStyle);
                GUILayout.Box(GUIContent.none, verticalMarginStyle);

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("<b>Use</b>", richtextStyle, GetColumnWidth(1));
                EditorGUILayout.LabelField("<b>Fields</b>", richtextStyle, GetColumnWidth(5));
                EditorGUILayout.LabelField("<b>Type</b>", richtextStyle, GetColumnWidth(6));

                GUILayout.EndHorizontal();

                GUILayout.Box(GUIContent.none, verticalMarginStyle);


                for (int j = 0; j < fieldsToMerge.Count; j++)
                {
                    MergeNode fieldToMerge = fieldsToMerge[j];

                    string originalName = fieldToMerge.OriginalValue;

                    GUILayout.BeginHorizontal();

                    wrapper.FieldSelectionStates[j] =
                        EditorGUILayout.Toggle(wrapper.FieldSelectionStates[j], GetColumnWidth(1));
                    GUI.enabled = wrapper.FieldSelectionStates[j];
                    EditorGUILayout.LabelField(originalName, richtextStyle, GetColumnWidth(5));
                    EditorGUILayout.LabelField(fieldToMerge.Type + (fieldToMerge.IsIterable ? "[]" : ""), richtextStyle,
                        GetColumnWidth(6));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("", GetColumnWidth(1));

                    EditorGUILayout.BeginVertical();

                    wrapper.OptionSelections[j] = EditorGUILayout.Popup(wrapper.OptionSelections[j],
                        fieldToMerge.Options, GetColumnWidth(5));

                    int optionsIndex = wrapper.OptionSelections[j];
                    if (fieldToMerge.Options != null && optionsIndex < fieldToMerge.Options.Length)
                    {
                        fieldToMerge.NameToExportTo = fieldToMerge.Options[optionsIndex];
                    }
                    else
                    {
                        wrapper.FieldSelectionStates[j] = false;
                    }

                    EditorGUILayout.EndVertical();

                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.Box(GUIContent.none, verticalMarginStyle);

                    foundScripts[i].MergeNodes[j] = fieldToMerge;
                }

                foundScriptWrappers[i] = wrapper;

                GUILayout.Box(GUIContent.none, horizontalLineStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            return base.DrawWizardGUI();
        }

        void OnWizardCreate()
        {
            foreach (FoundScript foundScript in foundScripts)
            {
                foundScript.HasBeenMapped = FoundScript.MappedState.Approved;
            }
            onComplete(foundScripts);
            Debug.Log("Create button clicked");
        }
    }
}
#endif