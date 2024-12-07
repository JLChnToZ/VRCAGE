using System;
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
using UnityEngine;

namespace JLChnToZ.VRC.AGE.Editors {
    [CustomEditor(typeof(AntiGravityManager))]
    public class AntiGravityManagerEditor : Editor {
        static AntiGravityManager singletion;
        SerializedProperty handlersProperty, initialSelectedHandlerProperty, autoReattachProperty, autoUseOnLoginProperty, detachOnRespawnProperty;
        ReorderableList handlersList;
        GUIContent tempContent = new GUIContent();

        public static AntiGravityManager SingletonInstance {
            get {
                if (!singletion) singletion = FindObjectOfType<AntiGravityManager>();
                return singletion;
            }
        }

        void OnEnable() {
            handlersProperty = serializedObject.FindProperty("handlers");
            initialSelectedHandlerProperty = serializedObject.FindProperty("initialSelectedHandler");
            autoReattachProperty = serializedObject.FindProperty("autoReattach");
            autoUseOnLoginProperty = serializedObject.FindProperty("autoUseOnLogin");
            detachOnRespawnProperty = serializedObject.FindProperty("detachOnRespawn");
            handlersList = new ReorderableList(serializedObject, handlersProperty, true, true, true, true) {
                drawHeaderCallback = DrawHandlersHeader,
                drawElementCallback = DrawHandlersElement,
                onAddCallback = OnAddHandler,
                elementHeight = EditorGUIUtility.singleLineHeight + 1,
            };
        }

        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, false, false)) return;
            serializedObject.Update();
            handlersList.DoLayoutList();
            EditorGUILayout.HelpBox("Reordering or removing handlers will may affect all AntiGravityObjectSync instances.", MessageType.Info);
            EditorGUILayout.PropertyField(autoReattachProperty);
            EditorGUILayout.PropertyField(autoUseOnLoginProperty);
            EditorGUILayout.PropertyField(detachOnRespawnProperty);
            serializedObject.ApplyModifiedProperties();
        }

        void DrawHandlersHeader(Rect rect) {
            EditorGUI.LabelField(rect, handlersProperty.displayName, EditorStyles.boldLabel);
            tempContent.text = "Default?";
            tempContent.tooltip = "";
            tempContent.image = null;
            var size = EditorStyles.label.CalcSize(tempContent);
            rect.xMin = rect.xMax - size.x;
            EditorGUI.LabelField(rect, tempContent, EditorStyles.label);
        }

        void DrawHandlersElement(Rect rect, int index, bool isActive, bool isFocused) {
            rect.height = EditorGUIUtility.singleLineHeight;
            var handler = handlersProperty.GetArrayElementAtIndex(index);
            var objectFieldRect = rect;
            objectFieldRect.xMax -= EditorGUIUtility.singleLineHeight - 2;
            EditorGUI.PropertyField(objectFieldRect, handler, GUIContent.none);
            var radioButtonRect = rect;
            radioButtonRect.xMin = objectFieldRect.xMax + 2;
            using (var property = new EditorGUI.PropertyScope(radioButtonRect, GUIContent.none, initialSelectedHandlerProperty))
            using (var changed = new EditorGUI.ChangeCheckScope()) {
                bool selected = initialSelectedHandlerProperty.intValue == index;
                selected = GUI.Toggle(radioButtonRect, selected, property.content, EditorStyles.radioButton);
                if (changed.changed && selected) initialSelectedHandlerProperty.intValue = index;
            }
        }

        void OnAddHandler(ReorderableList list) {
            list.serializedProperty.arraySize++;
            list.index = list.serializedProperty.arraySize - 1;
            var handler = list.serializedProperty.GetArrayElementAtIndex(list.index);
            handler.objectReferenceValue = null;
        }

        void AutoDetectHandlers() {

        }
    }
}