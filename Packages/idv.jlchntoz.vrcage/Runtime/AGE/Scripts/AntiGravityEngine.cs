using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE {
    [RequireComponent(typeof(global::VRC.SDK3.Components.VRCStation))]
    [RequireComponent(typeof(global::VRC.SDK3.Components.VRCPlayerObject))]
    public class AntiGravityEngine : AntiGravityEngineBase {
        const int xBits = 10, yBits = 10, zBits = 10;
        const int shiftX = 0;
        const int shiftY = shiftX + xBits;
        const int shiftZ = shiftY + yBits;
        const int signBit = 1 << 31;
        const int maskX = (1 << xBits) - 1;
        const int maskY = (1 << yBits) - 1;
        const int maskZ = (1 << zBits) - 1;
        const float scaleX = (maskX + 1) / 2 - 1;
        const float scaleY = (maskY + 1) / 2 - 1;
        const float scaleZ = (maskZ + 1) / 2 - 1;

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
        
        public bool IsSeated => Utilities.IsValid(station) && station.seated;

        public Matrix4x4 LeftHandMatrix => Matrix4x4.TRS(leftHandPosition, leftHandRotation, Vector3.one) * baseMatrix;
        public Matrix4x4 RightHandMatrix => Matrix4x4.TRS(rightHandPosition, rightHandRotation, Vector3.one) * baseMatrix;

        void Start() {
            station = (VRCStation)GetComponent(typeof(VRCStation));
            owner = Networking.GetOwner(gameObject);
            if (root == null) {
                root = transform.parent;
                if (root == null) root = transform;
            }
            anchor = transform;
            SendCustomEventDelayedFrames(nameof(_CheckParent), 2);
        }

        void FixedUpdate() {
            if (Networking.IsOwner(gameObject)) {
                var newPosition = owner.GetPosition();
                var newRotation = owner.GetRotation();
                bool positionChanged = newPosition != lastPosition || newRotation != lastRotation;
                if (positionChanged) {
                    lastPosition = newPosition;
                    lastRotation = newRotation;
                    baseMatrix = Matrix4x4.TRS(newPosition, newRotation, Vector3.one);
                }
                bool leftHandUpdated, rightHandUpdated;
                if (owner.IsUserInVR()) {
                    leftHandUpdated = UpdateHandPosition(VRCPlayerApi.TrackingDataType.LeftHand, ref leftHandPosition, ref leftHandRotation, ref leftHandRotationBits);
                    rightHandUpdated = UpdateHandPosition(VRCPlayerApi.TrackingDataType.RightHand, ref rightHandPosition, ref rightHandRotation, ref rightHandRotationBits);
                } else {
                    leftHandUpdated = false;
                    rightHandUpdated = UpdateHandPosition(VRCPlayerApi.TrackingDataType.Head, ref rightHandPosition, ref rightHandRotation, ref rightHandRotationBits);
                }
                if (isManualSync && (positionChanged || leftHandUpdated || rightHandUpdated))
                    RequestSerialization();
            } else {
                var t = Time.deltaTime * lerpScale;
                smoothPosition = Vector3.Lerp(smoothPosition, position, t);
                smoothRotation = Quaternion.Slerp(smoothRotation, rotation, t);
                relativePosition = smoothPosition;
                relativeRotation = smoothRotation;
                absolutePosition = root.TransformPoint(relativePosition);
                absoluteRotation = root.rotation * relativeRotation;
                if (customPositionHandler != null) {
                    customPositionHandler.ageTarget = this;
                    customPositionHandler._OnDeserializePosition();
                }
                anchor.SetPositionAndRotation(absolutePosition, absoluteRotation);
                baseMatrix = Matrix4x4.TRS(absolutePosition, absoluteRotation, Vector3.one);
                if (owner.IsUserInVR())
                    RestoreRotation(leftHandRotationBits, out leftHandRotation);
                RestoreRotation(rightHandRotationBits, out rightHandRotation);
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
            int x = Mathf.RoundToInt((rotation.x + 1) * scaleX);
            int y = Mathf.RoundToInt((rotation.y + 1) * scaleY);
            int z = Mathf.RoundToInt((rotation.z + 1) * scaleZ);
            int w = rotation.w < 0 ? signBit : 0;
            rotationBits = (x << shiftX) | (y << shiftY) | (z << shiftZ) | w;
            return true;
        }

        void RestoreRotation(int rotationBits, out Quaternion rotation) {
            float x = ((rotationBits >> shiftX) & maskX) / scaleX - 1;
            float y = ((rotationBits >> shiftY) & maskY) / scaleY - 1;
            float z = ((rotationBits >> shiftZ) & maskZ) / scaleZ - 1;
            var v3 = new Vector3(x, y, z);
            float w = Vector3.Dot(v3, v3);
            if (w > 1) {
                v3 *= 1 / Mathf.Sqrt(w);
                w = 0;
            } else
                w = Mathf.Sqrt(1 - w) * (rotationBits < 0 ? -1 : 1);
            rotation = new Quaternion(v3.x, v3.y, v3.z, w).normalized;
        }

        void UpdatePlayerPosition() {
            absolutePosition = owner.GetPosition();
            absoluteRotation = owner.GetRotation();
            relativePosition = root.InverseTransformPoint(absolutePosition);
            relativeRotation = Quaternion.Inverse(root.rotation) * absoluteRotation;
            if (customPositionHandler != null) {
                customPositionHandler.ageTarget = this;
                customPositionHandler._OnSerializePosition();
            }
            smoothPosition = position = relativePosition;
            smoothRotation = rotation = relativeRotation;
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
                if (rotation != Quaternion.identity) {
                    var euler = rotation.eulerAngles;
                    root.rotation = Quaternion.Inverse(Quaternion.Euler(euler.x, 0, euler.z)) * root.rotation;
                    anchor.SetPositionAndRotation(position, Quaternion.Euler(0, euler.y, 0));
                } else anchor.position = position;
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

        public void _CheckParent() {
            if (anchor.IsChildOf(root)) // Prevent side effects
                anchor.SetParent(null);
        }

        public override void OnPreSerialization() {
            if (Networking.IsOwner(gameObject)) UpdatePlayerPosition();
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
