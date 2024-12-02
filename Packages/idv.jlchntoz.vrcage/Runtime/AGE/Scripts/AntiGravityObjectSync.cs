using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

namespace JLChnToZ.VRC.AGE {
    public class AntiGravityObjectSync : AntiGravityEngineBase {
        [SerializeField] bool localOnly;
        public bool pickupable = true;
        [NonSerialized] public float lerpScale = 10;
        [NonSerialized] public AntiGravityEngine playerAttachedAGE;
        [UdonSynced] byte pickupState;
        byte localPickupState;
        VRCPickup pickup;
        VRCPlayerApi localPlayer;
        new Rigidbody rigidbody;
        bool isKinematic;
        bool useGravity;
        Transform lastRoot;
        Vector3 lastPosition;
        Quaternion lastRotation;
        byte lastPickupState;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;

        void Start() {
            localPlayer = Networking.LocalPlayer;
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null) {
                isKinematic = rigidbody.isKinematic;
                useGravity = rigidbody.useGravity;
            }
        }

        void InitRoot() {
            if (lastRoot == root) return;
            lastRoot = root;
            if (root == null) {
                lastPosition = Vector3.zero;
                lastRotation = Quaternion.identity;
            } else {
                lastPosition = root.position;
                lastRotation = root.rotation;
            }
        }

        void FixedUpdate() {
            InitRoot();
            var pickupState = localOnly ? localPickupState : this.pickupState;
            if (localOnly || Networking.IsOwner(gameObject)) {
                Vector3 deltaPosition;
                Quaternion deltaRotation;
                if (root == null) {
                    deltaPosition = Vector3.zero;
                    deltaRotation = Quaternion.identity;
                } else {
                    absolutePosition = root.position;
                    absoluteRotation = root.rotation;
                    deltaPosition = absolutePosition - lastPosition;
                    deltaRotation = Quaternion.Inverse(lastRotation) * absoluteRotation;
                    lastPosition = absolutePosition;
                    lastRotation = absoluteRotation;
                }
                absolutePosition = transform.position + deltaPosition;
                absoluteRotation = deltaRotation * transform.rotation;
                if (root == null) {
                    relativePosition = absolutePosition;
                    relativeRotation = absoluteRotation;
                } else {
                    relativePosition = root.InverseTransformPoint(absolutePosition);
                    relativeRotation = Quaternion.Inverse(root.rotation) * absoluteRotation;
                }
                if (customPositionHandler != null) {
                    customPositionHandler.ageTarget = this;
                    customPositionHandler._OnSerializePosition();
                }
                if (pickupState == 0) {
                    smoothPosition = relativePosition;
                    smoothRotation = relativeRotation;
                } else {
                    smoothPosition = transform.position;
                    switch ((VRCPlayerApi.TrackingDataType)pickupState) {
                        case VRCPlayerApi.TrackingDataType.LeftHand:
                            smoothPosition = playerAttachedAGE.LeftHandMatrix.inverse.MultiplyPoint3x4(smoothPosition);
                        break;
                        case VRCPlayerApi.TrackingDataType.RightHand:
                            smoothPosition = playerAttachedAGE.RightHandMatrix.inverse.MultiplyPoint3x4(smoothPosition);
                        break;
                    }
                    smoothRotation = relativeRotation;
                }
                if (!localOnly && (smoothPosition != position || smoothRotation != rotation)) {
                    position = smoothPosition;
                    rotation = smoothRotation;
                    if (isManualSync) RequestSerialization();
                }
                if (customPositionHandler != null)
                    customPositionHandler._OnDeserializePosition();
                if (pickupState == 0) {
                    transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
                    if (rigidbody != null) {
                        rigidbody.position = absolutePosition;
                        rigidbody.rotation = absoluteRotation;
                    }
                }
                if (rigidbody != null) {
                    rigidbody.useGravity = useGravity && pickupState == 0;
                    rigidbody.isKinematic = isKinematic || pickupState != 0;
                }
                lastPickupState = pickupState;
            } else {
                if (lastPickupState == pickupState) {
                    float t = Time.deltaTime * lerpScale;
                    smoothPosition = Vector3.Lerp(smoothPosition, position, t);
                    smoothRotation = Quaternion.Slerp(smoothRotation, rotation, t);
                } else {
                    lastPickupState = pickupState;
                    smoothPosition = position;
                    smoothRotation = rotation;
                }
                relativePosition = smoothPosition;
                relativeRotation = smoothRotation;
                if (root == null) {
                    absolutePosition = relativePosition;
                    absoluteRotation = relativeRotation;
                } else {
                    absolutePosition = root.TransformPoint(relativePosition);
                    absoluteRotation = root.rotation * relativeRotation;
                    lastPosition = root.position;
                    lastRotation = root.rotation;
                }
                if (customPositionHandler != null) {
                    customPositionHandler.ageTarget = this;
                    customPositionHandler._OnDeserializePosition();
                }
                if (pickupState != 0) {
                    absolutePosition = smoothPosition;
                    switch ((VRCPlayerApi.TrackingDataType)pickupState) {
                        case VRCPlayerApi.TrackingDataType.LeftHand:
                            absolutePosition = playerAttachedAGE.LeftHandMatrix.MultiplyPoint3x4(absolutePosition);
                        break;
                        case VRCPlayerApi.TrackingDataType.RightHand:
                            absolutePosition = playerAttachedAGE.RightHandMatrix.MultiplyPoint3x4(absolutePosition);
                        break;
                    }
                }
                transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
                if (rigidbody != null) {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                    rigidbody.position = absolutePosition;
                    rigidbody.rotation = absoluteRotation;
                }
                if (pickup != null) pickup.pickupable = pickupable && (pickupState == 0 || !pickup.DisallowTheft);
            }
        }

        public override void OnPickup() {
            if (!localOnly && !Networking.IsOwner(gameObject)) {
                if (pickupState != 0 && pickup != null && pickup.DisallowTheft) {
                    SendCustomEventDelayedFrames(nameof(_Drop), 0);
                    return;
                }
                Networking.SetOwner(localPlayer, gameObject);
            }
            _UpdatePickupState();
        }

        public override void OnDrop() {
            if (localOnly || Networking.IsOwner(gameObject)) {
                pickupState = localPickupState = 0;
                SendCustomEventDelayedFrames(nameof(_UpdatePickupState), 0);
            }
        }

        public void _UpdatePickupState() {
            if (localOnly || Networking.IsOwner(gameObject)) {
                localPickupState = 0;
                if (pickup != null) {
                    if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left))
                        localPickupState = 1;
                    else if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right))
                        localPickupState = 2;
                }
                if (!localOnly) {
                    pickupState = localPickupState;
                    if (isManualSync) RequestSerialization();
                }
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi to, VRCPlayerApi from) =>
            pickupState == 0 || pickup == null || !pickup.DisallowTheft;

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            if (!player.isLocal && pickupState != 0 && pickup != null) SendCustomEventDelayedFrames(nameof(_Drop), 0);
        }

        public void _Drop() => pickup.Drop();
    }
}
