using UnityEditor;
using UdonSharpEditor;

namespace JLChnToZ.VRC.AGE.Editors {
    [CustomEditor(typeof(AntiGravityEngine))]
    public class AntiGravityEngineEditor : Editor {
        public override void OnInspectorGUI() {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, true, false);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This script handles each users' position and orientation in the instance.\nDo not remove or move this game object away from the prefab.", MessageType.Info);
        }
    }
}