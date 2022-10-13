using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AntiGravityManager : UdonSharpBehaviour {
        [SerializeField] Transform root;
        [SerializeField] bool autoReattach;
        [SerializeField] bool autoUseOnLogin;
        [SerializeField] bool detachOnRespawn;
        [SerializeField] bool checkPickups;
        [SerializeField] bool dropUnsupportedPickups;
        [SerializeField] UdonSharpBehaviour customPositionHandler;
        AntiGravityEngine[] instances;
        bool init;
        VRCPlayerApi localPlayer;
        AntiGravityEngine activeInstance;
        VRC_Pickup leftHandItem, rightHandItem;
        bool leftHandSupported, rightHandSupported;
        AntiGravityAutoSwitch leftHandSwitch, rightHandSwitch;

        public Transform Root => root;
        public UdonSharpBehaviour CustomPositionHandler => customPositionHandler;
        public AntiGravityEngine ActiveInstance {
            get {
                if (Utilities.IsValid(activeInstance) && !activeInstance.LocalOccupied)
                    activeInstance = null;
                return activeInstance;
            }
        }

        void Start() {
            if (init) return;
            init = true;
            instances = GetComponentsInChildren<AntiGravityEngine>(true);
            if (root == null) root = transform;
            foreach (var instance in instances) {
                instance.root = root;
                instance.autoReattach = autoReattach;
                instance.detachOnRespawn = detachOnRespawn;
                instance.customPositionHandler = customPositionHandler;
            }
            if (autoUseOnLogin) SendCustomEventDelayedSeconds(nameof(Use), Random.Range(3F, 6F));
            localPlayer = Networking.LocalPlayer;
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
            if (Utilities.IsValid(activeInstance))
                activeInstance.Exit();
            foreach (var age in instances)
                if (age.Use()) {
                    activeInstance = age;
                    SendCustomEventDelayedSeconds(nameof(_CheckUse), 0.5F);
                    return true;
                }
            return false;
        }

        public bool UseAt(Vector3 position, Quaternion rotation) {
            Start();
            if (Utilities.IsValid(activeInstance))
                activeInstance.Exit();
            foreach (var age in instances)
                if (age.UseAt(position, rotation)) {
                    activeInstance = age;
                    return true;
                }
            return false;
        }

        public void _CheckUse() {
            if (Utilities.IsValid(activeInstance) && !activeInstance.LocalOccupied) {
                Use();
                SendCustomEventDelayedSeconds(nameof(_CheckUse), 0.5F);
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation) {
            Start();
            if (Utilities.IsValid(activeInstance))
                activeInstance.TeleportTo(position, rotation);
            else
                Networking.LocalPlayer.TeleportTo(position, rotation);
        }

        public void Exit() {
            Start();
            foreach (var age in instances)
                age.Exit();
            activeInstance = null;
        }
    }
}
