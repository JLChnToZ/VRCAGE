using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JLChnToZ.VRC.Foundation;
using VRC.SDK3.Data;

namespace JLChnToZ.VRC.AGE {
    /// <summary>The manager of Anti Gravity Engine.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("/Anti Gravity Engine/Anti Gravity Manager")]
    public partial class AntiGravityManager : UdonSharpBehaviour {
        [SerializeField] AntiGravityHandler[] handlers;
        [SerializeField] int initialSelectedHandler;
        [SerializeField] bool autoReattach;
        [SerializeField] bool autoUseOnLogin;
        [SerializeField] bool detachOnRespawn;
        bool init;
        VRCPlayerApi localPlayer;
        AntiGravityEngine template;
        AntiGravityEngine localInstance;
        DataDictionary instanceMap;

        public AntiGravityHandler CustomPositionHandler => null;
        public AntiGravityEngine ActiveInstance => localInstance;

        public AntiGravityEngine GetInstanceFromPlayer(VRCPlayerApi player) {
            if (Utilities.IsValid(instanceMap) && instanceMap.TryGetValue(player.playerId, TokenType.Reference, out var instance))
                return (AntiGravityEngine)instance.Reference;
            return null;
        }

        public AntiGravityHandler GetHandlerOf(int index) {
            if (index < 0 || index >= handlers.Length) return null;
            return handlers[index];
        }

        void Start() {
            if (init) return;
            init = true;
            localPlayer = Networking.LocalPlayer;
            template = GetComponentInChildren<AntiGravityEngine>(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            var instance = (AntiGravityEngine)player.FindComponentInPlayerObjects(template);
            if (!Utilities.IsValid(instance)) {
                Debug.LogWarning("No AntiGravityEngine found in player objects.");
                return;
            }
            if (!Utilities.IsValid(instanceMap)) instanceMap = new DataDictionary();
            instanceMap[player.playerId] = instance;
            instance.autoReattach = autoReattach;
            instance.detachOnRespawn = detachOnRespawn;
            if (!player.isLocal) return;
            localInstance = instance;
            if (autoUseOnLogin) SendCustomEventDelayedSeconds(nameof(Use), 3F);
        }

        public bool Use() {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            localInstance.Exit();
            if (localInstance.selectedHandler == 0) SelectHandlerForLocal(initialSelectedHandler);
            if (localInstance.Use()) return true;
            return false;
            
        }

        public bool Use(int handlerIndex) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            localInstance.Exit();
            SelectHandlerForLocal(handlerIndex);
            if (localInstance.Use()) return true;
            return false;
        }

        public bool UseAt(Vector3 position, Quaternion rotation) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            if (localInstance.selectedHandler == 0) SelectHandlerForLocal(initialSelectedHandler);
            localInstance.UseAt(position, rotation);
            return true;
        }

        public bool UseAt(int handlerIndex, Vector3 position, Quaternion rotation) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            SelectHandlerForLocal(handlerIndex);
            localInstance.UseAt(position, rotation);
            return true;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation) {
            Start();
            if (Utilities.IsValid(localInstance))
                localInstance.TeleportTo(position, rotation);
            else
                Networking.LocalPlayer.TeleportTo(position, rotation);
        }

        public void Exit() {
            Start();
            localInstance.Exit();
        }

        void SelectHandlerForLocal(int index) {
            if (!Utilities.IsValid(localInstance)) return;
            localInstance.selectedHandler = (byte)(index + 1);
            localInstance.positionHandler = GetHandlerOf(index);
            if (localInstance.isManualSync) localInstance.RequestSerialization();
        }
    }

#if !COMPILER_UDONSHARP
    public partial class AntiGravityManager : ISingleton<AntiGravityManager> {
        void ISingleton<AntiGravityManager>.Merge(AntiGravityManager[] others) { }
    }
#endif
}
