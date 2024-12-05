using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE {
    /// <summary>
    /// The component handles the player's position and rotation in the world.
    /// </summary>
    [RequireComponent(typeof(global::VRC.SDK3.Components.VRCStation))]
    [RequireComponent(typeof(global::VRC.SDK3.Components.VRCPlayerObject))]
    [AddComponentMenu("/Anti Gravity Engine/Anti Gravity Engine")]
    public class AntiGravityEngine : AntiGravityEngineBase {
        [NonSerialized] public bool autoReattach;
        [NonSerialized] public bool detachOnRespawn;
        [NonSerialized] public float lerpScale = 10;
        [UdonSynced] Vector3 leftHandPosition, rightHandPosition;
        [UdonSynced] int leftHandRotationBits, rightHandRotationBits;
        Transform anchor;
        VRCStation station;
        VRCPlayerApi owner;
        bool mobility = true;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;
        Vector3 teleportPosition;
        Quaternion teleportRotation;
        Vector3 lastPosition;
        Quaternion lastRotation;
        Quaternion leftHandRotation, rightHandRotation;
        Matrix4x4 baseMatrix = Matrix4x4.identity;

        public bool Mobility {
            get => mobility;
            set {
                if (Networking.IsOwner(gameObject)) {
                    mobility = value;
                    if (!value) UpdateAnchorPosition();
                    SendCustomEventDelayedFrames(nameof(_UpdateMobility), 0);
                }
            }
        }

        public Matrix4x4 LeftHandMatrix => baseMatrix * Matrix4x4.TRS(leftHandPosition, leftHandRotation, Vector3.one);

        public Matrix4x4 RightHandMatrix => baseMatrix * Matrix4x4.TRS(rightHandPosition, rightHandRotation, Vector3.one);

        public Quaternion LeftHandRotation => owner.GetRotation() * leftHandRotation;

        public Quaternion RightHandRotation => owner.GetRotation() * rightHandRotation;

        void Start() {
            station = (VRCStation)GetComponent(typeof(VRCStation));
            owner = Networking.GetOwner(gameObject);
            anchor = transform;
        }

        void FixedUpdate() {
            if (!Utilities.IsValid(positionHandler)) return;
            var ownerPosition = owner.GetPosition();
            var ownerRotation = owner.GetRotation();
            bool isVR = owner.IsUserInVR();
            if (owner.isLocal) {
                bool positionChanged = ownerPosition != lastPosition || ownerRotation != lastRotation;
                if (positionChanged) {
                    lastPosition = ownerPosition;
                    lastRotation = ownerRotation;
                    baseMatrix = Matrix4x4.TRS(ownerPosition, ownerRotation, Vector3.one);
                }
                bool leftHandUpdated, rightHandUpdated;
                leftHandUpdated = isVR && UpdateHandPosition(
                    VRCPlayerApi.TrackingDataType.LeftHand,
                    ref leftHandPosition,
                    ref leftHandRotation,
                    ref leftHandRotationBits
                );
                rightHandUpdated = UpdateHandPosition(
                    isVR ?
                        VRCPlayerApi.TrackingDataType.RightHand :
                        VRCPlayerApi.TrackingDataType.Head,
                    ref rightHandPosition,
                    ref rightHandRotation,
                    ref rightHandRotationBits
                );
                if (!isVR) {
                    leftHandPosition = rightHandPosition;
                    leftHandRotation = rightHandRotation;
                }
                if (isManualSync && (positionChanged || leftHandUpdated || rightHandUpdated))
                    RequestSerialization();
            } else {
                var t = Time.deltaTime * lerpScale;
                var newPosition = smoothPosition = Vector3.Lerp(smoothPosition, position, t);
                var newRotation = smoothRotation = Quaternion.Slerp(smoothRotation, UnpackRotation(rotationBits), t);
                DeserializePosition(ref newPosition, ref newRotation);
                anchor.SetPositionAndRotation(newPosition, newRotation);
                baseMatrix = anchor.localToWorldMatrix;
                rightHandRotation = UnpackRotation(rightHandRotationBits);
                leftHandRotation = isVR ? UnpackRotation(leftHandRotationBits) : rightHandRotation;
            }
        }

        bool UpdateHandPosition(VRCPlayerApi.TrackingDataType tt, ref Vector3 position, ref Quaternion rotation, ref int rotationBits) {
            var data = owner.GetTrackingData(tt);
            var newPosition = baseMatrix.inverse.MultiplyPoint3x4(data.position);
            var newRotation = Quaternion.Inverse(lastRotation) * data.rotation;
            if (position == newPosition && rotation == newRotation)
                return false;
            position = newPosition;
            rotation = newRotation;
            rotationBits = PackQuaternion(rotation);
            return true;
        }

        void UpdateAnchorPosition() => anchor.SetPositionAndRotation(
            owner.GetPosition(),
            owner.GetRotation()
        );

        public void _UpdateMobility() =>
            station.PlayerMobility = mobility ?
                VRCStation.Mobility.Mobile :
                VRCStation.Mobility.Immobilize;

        public bool Use() {
            if (Networking.IsOwner(gameObject)) {
                UpdateAnchorPosition();
                if (isManualSync) RequestSerialization();
                _UncheckedUse();
                return true;
            }
            return false;
        }

        public bool UseAt(Vector3 position, Quaternion rotation) {
            if (Networking.IsOwner(gameObject)) {
                anchor.SetPositionAndRotation(position, rotation);
                owner.TeleportTo(position, rotation);
                if (isManualSync) RequestSerialization();
                _UncheckedUse();
                return true;
            }
            return false;
        }

        void _UncheckedUse() {
            SendCustomEventDelayedFrames(nameof(_DeferUse), 0);
            station.PlayerMobility = VRCStation.Mobility.Mobile;
        }

        public void _DeferUse() {
            if (!Networking.IsOwner(gameObject)) return;
            station.UseStation(owner);
            Debug.Log("DeferUse");
            SendCustomEventDelayedFrames(nameof(_UpdateMobility), 0);
        }

        public void Exit() {
            station.ExitStation(owner);
            if (Networking.IsOwner(gameObject) && isManualSync) RequestSerialization();
        }

        public void TeleportTo(Vector3 position, Quaternion rotation) {
            if (!Networking.IsOwner(gameObject)) {
                Networking.LocalPlayer.TeleportTo(position, rotation);
                return;
            }
            teleportPosition = position;
            teleportRotation = rotation;
            SendCustomEventDelayedFrames(nameof(_DeferTeleport), 0);
        }

        public void _DeferTeleport() {
            if (!Networking.IsOwner(gameObject)) return;
            station.ExitStation(owner);
            owner.TeleportTo(teleportPosition, teleportRotation);
            anchor.SetPositionAndRotation(teleportPosition, teleportRotation);
            _UncheckedUse();
        }

        public override void OnPreSerialization() {
            var newPosition = owner.GetPosition();
            var newRotation = owner.GetRotation();
            if (SerializePosition(ref newPosition, ref newRotation)) {
                smoothPosition = position = newPosition;
                smoothRotation = newRotation;
                rotationBits = PackQuaternion(smoothRotation);
            }
        }

        public override void OnStationEntered(VRCPlayerApi player) {
            if (!player.isLocal)
                station.PlayerMobility = VRCStation.Mobility.Immobilize;
            else if (isManualSync)
                RequestSerialization();
        }

        public override void OnStationExited(VRCPlayerApi player) {
            if (!player.isLocal) return;
            if (autoReattach) SendCustomEventDelayedFrames(nameof(_DeferUse), 0);
            if (isManualSync) RequestSerialization();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player) {
            if (detachOnRespawn && player.isLocal) station.ExitStation(player);
        }
    }
}
