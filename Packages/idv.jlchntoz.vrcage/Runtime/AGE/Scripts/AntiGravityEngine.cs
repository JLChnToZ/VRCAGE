using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE {
#if !COMPILER_UDONSHARP
    [RequireComponent(typeof(global::VRC.SDK3.Components.VRCStation))]
#endif
    public class AntiGravityEngine : UdonSharpBehaviour {
        [Tooltip("Enable this if current instance is setted to manual sync.")]
        public bool isManualSync;
        [NonSerialized] public Transform root;
        [NonSerialized] public UdonSharpBehaviour customPositionHandler;
        [NonSerialized] public bool autoReattach;
        [NonSerialized] public bool detachOnRespawn;
        [NonSerialized] public Vector3 absolutePosition;
        [NonSerialized] public Quaternion absoluteRotation;
        [NonSerialized] public Vector3 relativePosition;
        [NonSerialized] public Quaternion relativeRotation;
        [NonSerialized] public float lerpScale = 10;
        Transform anchor;
        VRCStation station;
        VRCPlayerApi localPlayer;
        [UdonSynced] ushort occupiedId;
        [UdonSynced] Vector3 position;
        [UdonSynced] Quaternion rotation;
        bool mobility = true;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;
        Vector3 teleportPosition;
        Quaternion teleportRotation;
        Vector3 lastPosition;
        Quaternion lastRotation;

        public bool Free => occupiedId == 0 || !Utilities.IsValid(VRCPlayerApi.GetPlayerById(occupiedId));

        public bool LocalOccupied => occupiedId == localPlayer.playerId;

        public bool Mobility {
            get => mobility;
            set {
                if (occupiedId == 0) {
                    mobility = value;
                } else if (LocalOccupied) {
                    mobility = value;
                    if (!value) UpdateAnchorPosition();
                    SendCustomEventDelayedFrames(nameof(_UpdateMobility), 0);
                }
            }
        }

        void Start() {
            station = (VRCStation)GetComponent(typeof(VRCStation));
            localPlayer = Networking.LocalPlayer;
            if (root == null) {
                root = transform.parent;
                if (root == null) root = transform;
            }
            anchor = transform;
            SendCustomEventDelayedFrames(nameof(_CheckParent), 2);
        }

        void FixedUpdate() {
            if (occupiedId == 0) return;
            if (LocalOccupied) {
                if (isManualSync) {
                    var newPosition = localPlayer.GetPosition();
                    var newRotation = localPlayer.GetRotation();
                    if (newPosition != lastPosition || newRotation != lastRotation) {
                        lastPosition = newPosition;
                        lastRotation = newRotation;
                        RequestSerialization();
                    }
                }
            } else {
                var t = Time.deltaTime * lerpScale;
                smoothPosition = Vector3.Lerp(smoothPosition, position, t);
                smoothRotation = Quaternion.Slerp(smoothRotation, rotation, t);
                relativePosition = smoothPosition;
                relativeRotation = smoothRotation;
                absolutePosition = root.TransformPoint(relativePosition);
                absoluteRotation = root.rotation * relativeRotation;
                if (customPositionHandler != null) {
                    customPositionHandler.SetProgramVariable("ageTarget", this);
                    customPositionHandler.SendCustomEvent("_OnDeserializePosition");
                }
                anchor.SetPositionAndRotation(absolutePosition, absoluteRotation);
            }
        }

        void UpdatePlayerPosition() {
            absolutePosition = localPlayer.GetPosition();
            absoluteRotation = localPlayer.GetRotation();
            relativePosition = root.InverseTransformPoint(absolutePosition);
            relativeRotation = Quaternion.Inverse(root.rotation) * absoluteRotation;
            if (customPositionHandler != null) {
                customPositionHandler.SetProgramVariable("ageTarget", this);
                customPositionHandler.SendCustomEvent("_OnSerializePosition");
            }
            smoothPosition = position = relativePosition;
            smoothRotation = rotation = relativeRotation;
        }

        void UpdateAnchorPosition() => anchor.SetPositionAndRotation(
            localPlayer.GetPosition(),
            localPlayer.GetRotation()
        );

        public void _UpdateMobility() =>
            station.PlayerMobility = mobility ?
                VRCStation.Mobility.Mobile :
                VRCStation.Mobility.Immobilize;

        public bool Use() {
            if (Free || LocalOccupied) {
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(localPlayer, gameObject);
                UpdateAnchorPosition();
                occupiedId = (ushort)localPlayer.playerId;
                if (isManualSync) RequestSerialization();
                _UncheckedUse();
                return true;
            }
            return false;
        }

        public bool UseAt(Vector3 position, Quaternion rotation) {
            if (Free || LocalOccupied) {
                if (!Networking.IsOwner(gameObject))
                    Networking.SetOwner(localPlayer, gameObject);
                if (rotation != Quaternion.identity) {
                    var euler = rotation.eulerAngles;
                    root.rotation = Quaternion.Inverse(Quaternion.Euler(euler.x, 0, euler.z)) * root.rotation;
                    anchor.SetPositionAndRotation(position, Quaternion.Euler(0, euler.y, 0));
                } else anchor.position = position;
                localPlayer.TeleportTo(position, rotation);
                occupiedId = (ushort)localPlayer.playerId;
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
            if (!LocalOccupied) return;
            station.UseStation(localPlayer);
            SendCustomEventDelayedFrames(nameof(_UpdateMobility), 0);
        }

        public void Exit() {
            station.ExitStation(localPlayer);
            if (!Networking.IsOwner(gameObject)) return;
            if (LocalOccupied) {
                occupiedId = 0;
                if (isManualSync) RequestSerialization();
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation) {
            if (!LocalOccupied) {
                localPlayer.TeleportTo(position, rotation);
                return;
            }
            teleportPosition = position;
            teleportRotation = rotation;
            SendCustomEventDelayedFrames(nameof(_DeferTeleport), 0);
        }

        public void _DeferTeleport() {
            if (!LocalOccupied) return;
            station.ExitStation(localPlayer);
            localPlayer.TeleportTo(teleportPosition, teleportRotation);
            anchor.SetPositionAndRotation(teleportPosition, teleportRotation);
            _UncheckedUse();
        }

        public void _CheckParent() {
            if (anchor.IsChildOf(root)) // Prevent side effects
                anchor.SetParent(null);
        }

        public override void OnPreSerialization() {
            if (LocalOccupied) UpdatePlayerPosition();
        }

        public override void OnStationEntered(VRCPlayerApi player) {
            if (player.isLocal) {
                occupiedId = (ushort)player.playerId;
                if (isManualSync) RequestSerialization();
            } else
                station.PlayerMobility = VRCStation.Mobility.Immobilize;
        }

        public override void OnStationExited(VRCPlayerApi player) {
            if (player.isLocal && occupiedId == player.playerId) {
                if (autoReattach) SendCustomEventDelayedFrames(nameof(_DeferUse), 0);
                else occupiedId = 0;
                if (isManualSync) RequestSerialization();
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi to, VRCPlayerApi from) => occupiedId == to.playerId || Free;

        public override void OnOwnershipTransferred(VRCPlayerApi to) {
            if (!to.isLocal && occupiedId != 0 && occupiedId != to.playerId)
                occupiedId = 0;
        }

        public override void OnPlayerRespawn(VRCPlayerApi player) {
            if (detachOnRespawn && player.isLocal) {
                if (occupiedId == player.playerId) occupiedId = 0;
                station.ExitStation(player);
            }
        }
    }
}
