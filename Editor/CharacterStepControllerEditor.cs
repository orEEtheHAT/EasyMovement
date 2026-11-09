using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EasyFootsteps.Editor
{
    [CustomEditor(typeof(CharacterStepController))]
    public sealed class CharacterStepControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty states;
        private ReorderableList list;
        private static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        private void OnEnable()
        {
            states = serializedObject.FindProperty("_states");
            list = new ReorderableList(serializedObject, states, true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Movement States");
            list.elementHeightCallback = i => Line * (states.GetArrayElementAtIndex(i).isExpanded ? 6 : 1) + 4;
            list.drawElementCallback = DrawState;
            list.onAddCallback = AddState;
        }

        private void AddState(ReorderableList rows)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < states.arraySize; i++)
                names.Add(states.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);

            int n = 1;
            while (names.Contains("State " + n)) n++;
            int index = states.arraySize++;
            var row = states.GetArrayElementAtIndex(index);
            row.FindPropertyRelative("id").stringValue = "State " + n;
            row.FindPropertyRelative("stepDistance").floatValue = 1.95f;
            row.FindPropertyRelative("feedbackVolume").floatValue = 0.75f;
            row.FindPropertyRelative("headBobMultiplier").floatValue = 1f;
            row.FindPropertyRelative("externalMovementAmount").floatValue = 0.75f;
            row.FindPropertyRelative("legacySlot").intValue = 0;
            row.isExpanded = true;
            rows.index = index;
        }

        private void DrawState(Rect rect, int index, bool active, bool focused)
        {
            var row = states.GetArrayElementAtIndex(index);
            var id = row.FindPropertyRelative("id");
            rect.xMin += 12;
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            row.isExpanded = EditorGUI.Foldout(rect, row.isExpanded,
                string.IsNullOrWhiteSpace(id.stringValue) ? "(unnamed)" : id.stringValue, true);
            if (!row.isExpanded) return;

            int slot = row.FindPropertyRelative("legacySlot").intValue;
            string distance = slot switch { 1 => "_walkStepDistance", 2 => "_sprintStepDistance", 3 => "_crouchStepDistance", _ => null };
            string volume = slot switch { 1 => "_walkVolume", 2 => "_sprintVolume", 3 => "_crouchVolume", _ => null };
            string bob = slot switch { 2 => "_headBobSprintAmplitudeMultiplier", 3 => "_headBobCrouchMultiplier", _ => null };
            void Field(string path, string label, string oldPath = null)
            {
                rect.y += Line;
                var prop = oldPath == null ? row.FindPropertyRelative(path) : serializedObject.FindProperty(oldPath);
                EditorGUI.PropertyField(rect, prop, new GUIContent(label));
            }
            Field("id", "State ID");
            Field("stepDistance", "Step Distance", distance);
            Field("feedbackVolume", "Feedback Volume", volume);
            Field("headBobMultiplier", "Head Bob Multiplier", bob);
            Field("externalMovementAmount", "External Step Amount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            void Field(string path) => EditorGUILayout.PropertyField(serializedObject.FindProperty(path), true);
            Field("_motionSource");
            Field("_stepSourceMode");
            Field("_validateExternalSteps");
            Field("_minMoveSpeed");
            Field("_startStepPhase");
            list.DoLayoutList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < states.arraySize; i++)
            {
                string id = states.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id) || id != id.Trim())
                {
                    EditorGUILayout.HelpBox("Give each state a unique ID without leading or trailing spaces.", MessageType.Warning);
                    break;
                }
            }
            if (states.arraySize == 0)
                EditorGUILayout.HelpBox("Add a state to enable footsteps.", MessageType.Info);

            Field("_enableHeadBob");
            if (serializedObject.FindProperty("_enableHeadBob").boolValue)
            {
                EditorGUI.indentLevel++;
                Field("_headBobTarget");
                Field("_headBobHorizontalCurve");
                Field("_headBobVerticalCurve");
                Field("_headBobHorizontalAmplitude");
                Field("_headBobVerticalAmplitude");
                Field("_headBobStepImpactAmplitude");
                Field("_headBobStepImpactDecaySpeed");
                Field("_headBobBlendInSpeed");
                Field("_headBobBlendOutSpeed");
                Field("_headBobPositionSmoothTime");
                EditorGUI.indentLevel--;
            }
            Field("_enableStepFeedback");
            if (serializedObject.FindProperty("_enableStepFeedback").boolValue)
            {
                EditorGUI.indentLevel++;
                Field("_defaultFeedback");
                Field("_surfaces");
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
