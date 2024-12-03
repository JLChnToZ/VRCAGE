using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JLChnToZ.VRC.Foundation;
using VRC.SDK3.Data;

namespace JLChnToZ.VRC.AGE {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public partial class AntiGravityManager : UdonSharpBehaviour {
        [SerializeField] Transform root;
        [SerializeField] bool autoReattach;
        [SerializeField] bool autoUseOnLogin;
        [SerializeField] bool detachOnRespawn;
        [SerializeField] bool checkPickups;
        [SerializeField] bool dropUnsupportedPickups;
        [SerializeField] AntiGravityHandlerBase customPositionHandler;
        bool init;
        VRCPlayerApi localPlayer;
        AntiGravityEngine template;
        AntiGravityEngine localInstance;
        VRC_Pickup leftHandItem, rightHandItem;
        bool leftHandSupported, rightHandSupported;
        AntiGravityAutoSwitch leftHandSwitch, rightHandSwitch;
        DataDictionary instanceMap;

        public Transform Root => root;
        public AntiGravityHandlerBase CustomPositionHandler => customPositionHandler;
        public AntiGravityEngine ActiveInstance => localInstance;

        public AntiGravityEngine GetInstanceFromPlayer(VRCPlayerApi player) {
            if (Utilities.IsValid(instanceMap) && instanceMap.TryGetValue(player.playerId, TokenType.Reference, out var instance))
                return (AntiGravityEngine)instance.Reference;
            return null;
        }

        void Start() {
            if (init) return;
            init = true;
            if (!Utilities.IsValid(root)) root = transform;
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
            instance.root = root;
            instance.autoReattach = autoReattach;
            instance.detachOnRespawn = detachOnRespawn;
            instance.customPositionHandler = customPositionHandler;
            if (!player.isLocal) return;
            localInstance = instance;
            if (autoUseOnLogin) SendCustomEventDelayedSeconds(nameof(Use), 3F);
        }

        void Uptate() {
            if (checkPickups) CheckPickups();
        }

        void CheckPickups() {
            AntiGravityAutoSwitch handSwitch;
            var handItem = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            if (handItem != leftHandItem) {
                if (Utilities.IsValid(handItem)) {
                    handSwitch = handItem.GetComponent<AntiGravityAutoSwitch>();
                    leftHandSupported = Utilities.IsValid(handItem.GetComponent<AntiGravityObjectSync>());
                } else {
                    handSwitch = null;
                    leftHandSupported = true;
                }
                leftHandItem = handItem;
            } else
                handSwitch = leftHandSwitch;
            if (handSwitch != leftHandSwitch) {
                if (Utilities.IsValid(leftHandSwitch) && leftHandSwitch.ActiveManager == this)
                    leftHandSwitch.ActiveManager = null;
                leftHandSwitch = handSwitch;
            }
            if (Utilities.IsValid(handSwitch)) handSwitch.ActiveManager = this;
            else if (dropUnsupportedPickups && !leftHandSupported) handItem.Drop();

            handItem = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            if (handItem != rightHandItem) {
                if (Utilities.IsValid(handItem)) {
                    handSwitch = handItem.GetComponent<AntiGravityAutoSwitch>();
                    rightHandSupported = Utilities.IsValid(handItem.GetComponent<AntiGravityObjectSync>());
                } else {
                    handSwitch = null;
                    rightHandSupported = true;
                }
                rightHandItem = handItem;
            } else
                handSwitch = rightHandSwitch;
            if (handSwitch != rightHandSwitch) {
                if (Utilities.IsValid(rightHandSwitch) && rightHandSwitch.ActiveManager == this)
                    rightHandSwitch.ActiveManager = null;
                rightHandSwitch = handSwitch;
            }
            if (Utilities.IsValid(handSwitch)) handSwitch.ActiveManager = this;
            else if (dropUnsupportedPickups && !rightHandSupported) handItem.Drop();
        }

        public bool Use() {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            localInstance.Exit();
            if (localInstance.Use()) return true;
            return false;
        }

        public bool UseAt(Vector3 position, Quaternion rotation) {
            Start();
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
    }

#if !COMPILER_UDONSHARP
    public partial class AntiGravityManager : ISingleton<AntiGravityManager> {
        void ISingleton<AntiGravityManager>.Merge(AntiGravityManager[] others) { }
    }
#endif
}
