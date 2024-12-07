using UnityEditor;
using UdonSharpEditor;

namespace JLChnToZ.VRC.AGE.Editors {
    [CustomEditor(typeof(AntiGravityHandler))]
    public class AntiGravityHandlerEditor : Editor {
        public override void OnInspectorGUI() {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, true, false);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This is the default handler.\nWhen users or pickupable objects attached to this handler, they will acts like they are parented to the transform of this object.", MessageType.Info);
        }
    }
}