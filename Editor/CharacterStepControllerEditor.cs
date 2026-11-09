using UnityEditor;
using UnityEngine;

namespace EasyFootsteps.Editor
{
    [CustomEditor(typeof(CharacterStepController))]
    public sealed class CharacterStepControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _motionSource;
        private SerializedProperty _stepSourceMode;
        private SerializedProperty _validateExternalSteps;
        private SerializedProperty _minMoveSpeed;
        private SerializedProperty _walkStepDistance;
        private SerializedProperty _sprintStepDistance;
        private SerializedProperty _crouchStepDistance;
        private SerializedProperty _startStepPhase;
        private SerializedProperty _enableHeadBob;
        private SerializedProperty _headBobTarget;
        private SerializedProperty _headBobHorizontalCurve;
        private SerializedProperty _headBobVerticalCurve;
        private SerializedProperty _headBobHorizontalAmplitude;
        private SerializedProperty _headBobVerticalAmplitude;
        private SerializedProperty _headBobCrouchMultiplier;
        private SerializedProperty _headBobSprintAmplitudeMultiplier;
        private SerializedProperty _headBobStepImpactAmplitude;
        private SerializedProperty _headBobStepImpactDecaySpeed;
        private SerializedProperty _headBobBlendInSpeed;
        private SerializedProperty _headBobBlendOutSpeed;
        private SerializedProperty _headBobPositionSmoothTime;
        private SerializedProperty _enableStepFeedback;
        private SerializedProperty _defaultFeedback;
        private SerializedProperty _surfaces;
        private SerializedProperty _walkVolume;
        private SerializedProperty _sprintVolume;
        private SerializedProperty _crouchVolume;

        private void OnEnable()
        {
            _motionSource = serializedObject.FindProperty("_motionSource");
            _stepSourceMode = serializedObject.FindProperty("_stepSourceMode");
            _validateExternalSteps = serializedObject.FindProperty("_validateExternalSteps");
            _minMoveSpeed = serializedObject.FindProperty("_minMoveSpeed");
            _walkStepDistance = serializedObject.FindProperty("_walkStepDistance");
            _sprintStepDistance = serializedObject.FindProperty("_sprintStepDistance");
            _crouchStepDistance = serializedObject.FindProperty("_crouchStepDistance");
            _startStepPhase = serializedObject.FindProperty("_startStepPhase");
            _enableHeadBob = serializedObject.FindProperty("_enableHeadBob");
            _headBobTarget = serializedObject.FindProperty("_headBobTarget");
            _headBobHorizontalCurve = serializedObject.FindProperty("_headBobHorizontalCurve");
            _headBobVerticalCurve = serializedObject.FindProperty("_headBobVerticalCurve");
            _headBobHorizontalAmplitude = serializedObject.FindProperty("_headBobHorizontalAmplitude");
            _headBobVerticalAmplitude = serializedObject.FindProperty("_headBobVerticalAmplitude");
            _headBobCrouchMultiplier = serializedObject.FindProperty("_headBobCrouchMultiplier");
            _headBobSprintAmplitudeMultiplier = serializedObject.FindProperty("_headBobSprintAmplitudeMultiplier");
            _headBobStepImpactAmplitude = serializedObject.FindProperty("_headBobStepImpactAmplitude");
            _headBobStepImpactDecaySpeed = serializedObject.FindProperty("_headBobStepImpactDecaySpeed");
            _headBobBlendInSpeed = serializedObject.FindProperty("_headBobBlendInSpeed");
            _headBobBlendOutSpeed = serializedObject.FindProperty("_headBobBlendOutSpeed");
            _headBobPositionSmoothTime = serializedObject.FindProperty("_headBobPositionSmoothTime");
            _enableStepFeedback = serializedObject.FindProperty("_enableStepFeedback");
            _defaultFeedback = serializedObject.FindProperty("_defaultFeedback");
            _surfaces = serializedObject.FindProperty("_surfaces");
            _walkVolume = serializedObject.FindProperty("_walkVolume");
            _sprintVolume = serializedObject.FindProperty("_sprintVolume");
            _crouchVolume = serializedObject.FindProperty("_crouchVolume");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_motionSource);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Step Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_stepSourceMode);
            EditorGUILayout.PropertyField(_validateExternalSteps);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Step Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minMoveSpeed);
            EditorGUILayout.PropertyField(_walkStepDistance);
            EditorGUILayout.PropertyField(_sprintStepDistance);
            EditorGUILayout.PropertyField(_crouchStepDistance);
            EditorGUILayout.PropertyField(_startStepPhase);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_enableHeadBob, new GUIContent("Enable Head Bob"));
            if (_enableHeadBob.boolValue)
            {
                EditorGUILayout.LabelField("Head Bob", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_headBobTarget, new GUIContent("Camera Target"));
                EditorGUILayout.PropertyField(_headBobHorizontalCurve, new GUIContent("Horizontal Curve"));
                EditorGUILayout.PropertyField(_headBobVerticalCurve, new GUIContent("Vertical Curve"));
                EditorGUILayout.PropertyField(_headBobHorizontalAmplitude);
                EditorGUILayout.PropertyField(_headBobVerticalAmplitude);
                EditorGUILayout.PropertyField(_headBobCrouchMultiplier);
                EditorGUILayout.PropertyField(_headBobSprintAmplitudeMultiplier);
                EditorGUILayout.PropertyField(_headBobStepImpactAmplitude);
                EditorGUILayout.PropertyField(_headBobStepImpactDecaySpeed);
                EditorGUILayout.PropertyField(_headBobBlendInSpeed);
                EditorGUILayout.PropertyField(_headBobBlendOutSpeed);
                EditorGUILayout.PropertyField(_headBobPositionSmoothTime);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_enableStepFeedback, new GUIContent("Enable Step Feedback"));
            if (_enableStepFeedback.boolValue)
            {
                EditorGUILayout.LabelField("Step Feedback", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_defaultFeedback);
                EditorGUILayout.PropertyField(_surfaces, true);
                EditorGUILayout.PropertyField(_walkVolume);
                EditorGUILayout.PropertyField(_sprintVolume);
                EditorGUILayout.PropertyField(_crouchVolume);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
