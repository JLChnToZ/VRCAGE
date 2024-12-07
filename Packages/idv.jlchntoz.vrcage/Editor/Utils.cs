using UnityEditor;
using UnityEngine;
using JLChnToZ.VRC.Foundation.Editors;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.AGE.Editors {
    public static class Utils {
        const string AGE_MANAGER_MENU_PATH = "GameObject/Anti Gravity Engine/Anti Gravity Manager";
        const string AGE_HANDLER_MENU_PATH = "Assets/Create/Anti Gravity Engine/Custom Anti Gravity Handler";
        const string AGE_MANAGER_PREFAB_GUID = "5797439e42a837f478205aa2b4c75a07";

        [MenuItem(AGE_MANAGER_MENU_PATH, false, 10)]
        static void CreateAGE() {
            var prefabPath = AssetDatabase.GUIDToAssetPath(AGE_MANAGER_PREFAB_GUID);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) {
                Debug.LogError("Failed to find Anti Gravity Manager prefab.");
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) {
                Debug.LogError("Failed to instantiate Anti Gravity Manager prefab.");
                return;
            }
            instance.name = prefab.name;
            Selection.activeGameObject = instance;
        }

        [MenuItem(AGE_MANAGER_MENU_PATH, true)]
        static bool CreateAGEValidate() => !UnityObject.FindObjectOfType<AntiGravityManager>();

        [MenuItem(AGE_HANDLER_MENU_PATH, false, 10)]
        static void CreateAGEHandler() => ProjectWindowUdonSharpAssetFactory.CreateAsset(
            "CustomAntiGravityHandler", @"using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;
using JLChnToZ.VRC.AGE;

public class #CLASS# : AntiGravityHandler {
    public override void _OnSerializePosition() {
        // `absolutePosition` and `absoluteRotation` are the input.
        // You can implement your own logic to calculate the `relativePosition` and `relativeRotation` here.
        // If you don't need the default behaviour (calculate `relativePosition` and `relativeRotation` based on the transform),
        // you can remove the follow line.
        base._OnSerializePosition();
    }

    public override void _OnDeserializePosition() {
        // `relativePosition` and `relativeRotation` are the input.
        // You can implement your own logic to calculate the `absolutePosition` and `absoluteRotation` here.
        // If you don't need the default behaviour (calculate `absolutePosition` and `absoluteRotation` based on the transform),
        // you can remove the follow line.
        base._OnDeserializePosition();
    }
}
");
    }
}