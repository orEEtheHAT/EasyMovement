using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFootsteps.Editor
{
    [CustomPropertyDrawer(typeof(FootstepStateIdAttribute))]
    public sealed class FootstepStateIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty field, GUIContent label)
        {
            var owner = field.serializedObject.targetObject as Component;
            var steps = owner ? owner.GetComponent<CharacterStepController>() : null;
            EditorGUI.BeginProperty(rect, label, field);
            try
            {
                if (!steps)
                {
                    EditorGUI.BeginChangeCheck();
                    string text = EditorGUI.DelayedTextField(rect, label, field.stringValue);
                    if (EditorGUI.EndChangeCheck()) field.stringValue = text.Trim();
                    return;
                }

                using var settings = new SerializedObject(steps);
                var states = settings.FindProperty("_states");
                var names = new List<string>(states.arraySize + 1);
                for (int i = 0; i < states.arraySize; i++)
                {
                    string id = states.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                    if (!string.IsNullOrWhiteSpace(id)) names.Add(id);
                }

                string current = field.stringValue;
                if (string.IsNullOrWhiteSpace(current) && owner is NavMeshFootstepSource agent)
                    current = agent.StateId;
                int index = names.FindIndex(id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase));
                int count = names.Count;
                if (index < 0)
                {
                    index = count;
                    names.Add(string.IsNullOrEmpty(current) ? "Select a step state" : current + " (missing)");
                }
                EditorGUI.BeginChangeCheck();
                int chosen = EditorGUI.Popup(rect, label.text, index, names.ToArray());
                if (EditorGUI.EndChangeCheck() && chosen < count) field.stringValue = names[chosen];
            }
            finally { EditorGUI.EndProperty(); }
        }
    }
}
