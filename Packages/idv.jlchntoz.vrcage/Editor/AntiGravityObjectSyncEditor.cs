using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using System;

namespace JLChnToZ.VRC.AGE.Editors {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AntiGravityObjectSync))]
    public class AntiGravityObjectSyncEditor : Editor {
        SerializedProperty localOnlyProperty, selectedHandlerProperty, pickupableProperty;

        void OnEnable() {
            localOnlyProperty = serializedObject.FindProperty("localOnly");
            selectedHandlerProperty = serializedObject.FindProperty("initialSelectedHandler");
            pickupableProperty = serializedObject.FindProperty("pickupable");
        }

        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, false, false)) return;
            serializedObject.Update();
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.objectField, Array.Empty<GUILayoutOption>());
            using (var property = new EditorGUI.PropertyScope(rect, null, selectedHandlerProperty)) {
                var instance = AntiGravityManagerEditor.SingletonInstance;
                if (instance)
                    using (var changed = new EditorGUI.ChangeCheckScope()) {
                        int selectedHandler = selectedHandlerProperty.intValue;
                        var target = instance.handlers != null && selectedHandler >= 0 && selectedHandler < instance.handlers.Length ? instance.handlers[selectedHandler] : null;
                        target = EditorGUI.ObjectField(rect, property.content, target, typeof(AntiGravityHandler), true) as AntiGravityHandler;
                        if (changed.changed) {
                            selectedHandler = instance.handlers != null ? Array.IndexOf(instance.handlers, target) : -1;
                            if (selectedHandler < 0 && target) {
                                Undo.RecordObject(instance, "Add Handler");
                                if (instance.handlers != null && instance.handlers.Length > 0) {
                                    selectedHandler = instance.handlers.Length;
                                    Array.Resize(ref instance.handlers, selectedHandler + 1);
                                    instance.handlers[selectedHandler] = target;
                                } else {
                                    selectedHandler = 0;
                                    instance.handlers = new[] { target };
                                }
                            }
                            selectedHandlerProperty.intValue = selectedHandler;
                        }
                    }
                else {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUI.ObjectField(rect, property.content, null, typeof(AntiGravityHandler), true);
                    EditorGUILayout.HelpBox("No AntiGravityManager found in scene.", MessageType.Warning);
                }
            }
            EditorGUILayout.PropertyField(localOnlyProperty);
            EditorGUILayout.PropertyField(pickupableProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}