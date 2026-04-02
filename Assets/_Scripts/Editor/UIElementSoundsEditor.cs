#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace My.Scripts.UI.Editor
{
    [CustomEditor(typeof(UIElementSounds))]
    public class UIElementSoundsEditor : UnityEditor.Editor
    {
        private SerializedProperty _elementType;
        private SerializedProperty _size;
        private SerializedProperty _clickVariant;
        private SerializedProperty _toggleEnableVariant;
        private SerializedProperty _toggleDisableVariant;

        private void OnEnable()
        {
            _elementType = serializedObject.FindProperty("_elementType");
            _size = serializedObject.FindProperty("_size");
            _clickVariant = serializedObject.FindProperty("_clickVariant");
            _toggleEnableVariant = serializedObject.FindProperty("_toggleEnableVariant");
            _toggleDisableVariant = serializedObject.FindProperty("_toggleDisableVariant");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_elementType);

            EditorGUILayout.Space();

            var type = (UIElementType)_elementType.enumValueIndex;

            switch (type)
            {
                case UIElementType.Button:
                    EditorGUILayout.PropertyField(_size);
                    EditorGUILayout.PropertyField(_clickVariant);
                    break;

                case UIElementType.Toggle:
                    EditorGUILayout.PropertyField(_toggleEnableVariant, new GUIContent("Enable Sound"));
                    EditorGUILayout.PropertyField(_toggleDisableVariant, new GUIContent("Disable Sound"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif