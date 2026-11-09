using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyMovement.FirstPerson.Editor
{
    [CustomEditor(typeof(FirstPersonCharacterController), true)]
    public sealed class FirstPersonCharacterControllerEditor : UnityEditor.Editor
    {
        private bool showMotor;
        private static readonly string[] ShownFields =
        {
            "m_Script", "_maxWalkSpeed", "_maxWalkSpeedCrouched", "_canEverCrouch", "_camera",
            "cameraTarget", "defaultCamera", "crouchedCamera", "sprintCamera", "lockCursorOnStart",
            "allowSprint", "_maxSprintSpeed", "_minSprintInput", "walkState", "sprintState",
            "crouchState", "customStates", "groundProbeDepth"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Field("cameraTarget", "Look Target");
            Field("_camera", "Output Camera");
            Field("lockCursorOnStart", "Lock Cursor on Start");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement States", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Walk", EditorStyles.boldLabel);
            GaitFields("_maxWalkSpeed", "defaultCamera", "walkState");
            if (Toggle("allowSprint", "Sprint"))
            {
                GaitFields("_maxSprintSpeed", "sprintCamera", "sprintState");
                Field("_minSprintInput", "Minimum Move Input");
            }
            if (Toggle("_canEverCrouch", "Crouch"))
                GaitFields("_maxWalkSpeedCrouched", "crouchedCamera", "crouchState");

            EditorGUILayout.Space();
            var custom = serializedObject.FindProperty("customStates");
            EditorGUILayout.PropertyField(custom, new GUIContent("Custom States", "Activate these states from gameplay using TrySetCustomState."), true);
            if (custom.isExpanded)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < custom.arraySize; i++)
                {
                    string id = custom.GetArrayElementAtIndex(i).FindPropertyRelative("stateId").stringValue;
                    if (string.IsNullOrWhiteSpace(id) || !names.Add(id))
                    {
                        EditorGUILayout.HelpBox("Choose a unique step state for each custom movement state.", MessageType.Warning);
                        break;
                    }
                }
            }

            if (Application.isPlaying)
            {
                var player = (FirstPersonCharacterController)target;
                EditorGUILayout.LabelField("Active State", player.ActiveStateId);
                if (custom.arraySize > 0 && GUILayout.Button("Test State in Play Mode"))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Automatic"), false, player.ClearCustomState);
                    for (int i = 0; i < custom.arraySize; i++)
                    {
                        string id = custom.GetArrayElementAtIndex(i).FindPropertyRelative("stateId").stringValue;
                        if (!string.IsNullOrWhiteSpace(id))
                            menu.AddItem(new GUIContent(id), player.ActiveStateId == id, () => player.SetCustomState(id));
                    }
                    menu.ShowAsContext();
                }
            }
            EditorGUILayout.Space();
            showMotor = EditorGUILayout.Foldout(showMotor, "ECM2 Motor and Ground Settings", true);
            if (showMotor)
            {
                Field("groundProbeDepth", "Footstep Ground Probe Depth");
                DrawPropertiesExcluding(serializedObject, ShownFields);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void GaitFields(string speed, string view, string steps)
        {
            EditorGUI.indentLevel++;
            Field(speed, "Speed (m/s)");
            Field(view, "Camera");
            Field(steps, "Step State");
            EditorGUI.indentLevel--;
        }

        private bool Toggle(string path, string label)
        {
            Field(path, label);
            return serializedObject.FindProperty(path).boolValue;
        }

        private void Field(string path, string label)
        {
            var property = serializedObject.FindProperty(path);
            EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip), true);
        }
    }
}
