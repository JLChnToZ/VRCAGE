using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class AntiGravityAutoSwitch : UdonSharpBehaviour {
        [UdonSynced] byte activeManager;
        [SerializeField] AntiGravityManager defaultManager;
        [SerializeField] AntiGravityManager[] managers;
        AntiGravityObjectSync objectSync;

        public AntiGravityManager ActiveManager {
            get => activeManager == 0 ? null : managers[activeManager - 1];
            set {
                if (!Networking.IsOwner(gameObject) || ActiveManager == value) return;
                activeManager = value == null ? (byte)0 : (byte)(Array.IndexOf(managers, value) + 1);
                UpdateActiveManager();
            }
        }

        void Start() {
            objectSync = GetComponent<AntiGravityObjectSync>();
            if (objectSync == null) {
                Debug.LogWarning("AntiGravityObjectSync is not attached, auto switch will not works.");
                enabled = false;
                return;
            }
            if (defaultManager != null)
                ActiveManager = defaultManager;
        }

        public override void OnDeserialization() => UpdateActiveManager();

        void UpdateActiveManager() {
            if (activeManager == 0) {
                objectSync.root = null;
                objectSync.customPositionHandler = null;
            } else {
                var activeManagerInstance = managers[activeManager - 1];
                objectSync.root = activeManagerInstance.Root;
                objectSync.customPositionHandler = activeManagerInstance.CustomPositionHandler;
            }
        }
    }
}
