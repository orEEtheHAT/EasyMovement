using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFootsteps.Editor
{
    [CustomEditor(typeof(SelectableFootstepMotionSource), true)]
    public sealed class SelectableFootstepMotionSourceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "stateId", "mode");
            var source = (SelectableFootstepMotionSource)target;
            var controller = source.GetComponent<CharacterStepController>();
            var id = serializedObject.FindProperty("stateId");
            if (controller == null)
            {
                EditorGUILayout.PropertyField(id, new GUIContent("State ID"));
            }
            else
            {
                using var config = new SerializedObject(controller);
                var states = config.FindProperty("_states");
                var ids = new List<string>();
                for (int i = 0; i < states.arraySize; i++)
                {
                    string name = states.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                    if (!string.IsNullOrWhiteSpace(name)) ids.Add(name);
                }
                string current = source.StateId ?? "";
                int index = ids.FindIndex(name => string.Equals(name, current, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    index = ids.Count;
                    ids.Add(current + " (unconfigured)");
                }
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUILayout.Popup("Movement State", index, ids.ToArray());
                if (EditorGUI.EndChangeCheck())
                    id.stringValue = ids[selected];
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
