using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

namespace JLChnToZ.VRC.AGE {
    public class AntiGravityObjectSync : AntiGravityEngineBase {
        [SerializeField, HideInInspector] AntiGravityManager manager;
        [SerializeField] bool localOnly;
        public bool pickupable = true;
        [NonSerialized] public float lerpScale = 10;
        AntiGravityEngine playerAttachedAGE;
        VRCPlayerApi ageOwner;
        [UdonSynced] byte pickupState;
        byte localPickupState;
        VRCPickup pickup;
        VRCPlayerApi localPlayer;
        new Rigidbody rigidbody;
        bool isKinematic;
        bool useGravity;
        Transform lastRoot;
        Vector3 lastRootPosition, lastPosition;
        Quaternion lastRootRotation, lastRotation;
        byte lastPickupState;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;

        void Start() {
            localPlayer = Networking.LocalPlayer;
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            rigidbody = GetComponent<Rigidbody>();
            if (Utilities.IsValid(rigidbody)) {
                isKinematic = rigidbody.isKinematic;
                useGravity = rigidbody.useGravity;
            }
        }

        void InitRoot() {
            if (lastRoot == root) return;
            lastRoot = root;
            if (Utilities.IsValid(root)) {
                lastRootPosition = root.position;
                lastRootRotation = root.rotation;
            } else {
                lastRootPosition = Vector3.zero;
                lastRootRotation = Quaternion.identity;
            }
        }

        void FixedUpdate() {
            InitRoot();
            var pickupState = localOnly ? localPickupState : this.pickupState;
            if (localOnly || Networking.IsOwner(gameObject)) {
                Vector3 deltaPosition;
                Quaternion deltaRotation;
                if (Utilities.IsValid(root)) {
                    absolutePosition = root.position;
                    absoluteRotation = root.rotation;
                    deltaPosition = absolutePosition - lastRootPosition;
                    deltaRotation = Quaternion.Inverse(lastRootRotation) * absoluteRotation;
                    lastRootPosition = absolutePosition;
                    lastRootRotation = absoluteRotation;
                } else {
                    deltaPosition = Vector3.zero;
                    deltaRotation = Quaternion.identity;
                }
                absolutePosition = transform.position + deltaPosition;
                absoluteRotation = deltaRotation * transform.rotation;
                if (Utilities.IsValid(root)) {
                    relativePosition = root.InverseTransformPoint(absolutePosition);
                    relativeRotation = Quaternion.Inverse(root.rotation) * absoluteRotation;
                } else {
                    relativePosition = absolutePosition;
                    relativeRotation = absoluteRotation;
                }
                if (Utilities.IsValid(customPositionHandler)) {
                    customPositionHandler.ageTarget = this;
                    customPositionHandler._OnSerializePosition();
                }
                smoothPosition = pickupState == 0 ? relativePosition :
                    GetOnHandMatrix().inverse.MultiplyPoint3x4(transform.position);
                smoothRotation = relativeRotation;
                if (!localOnly) {
                    int newRotationBits = PackQuaternion(smoothRotation);
                    if (smoothPosition != position || newRotationBits != rotationBits) {
                        position = smoothPosition;
                        rotationBits = newRotationBits;
                        if (isManualSync) RequestSerialization();
                    }
                }
                if (Utilities.IsValid(customPositionHandler))
                    customPositionHandler._OnDeserializePosition();
                if (pickupState == 0) {
                    transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
                    if (Utilities.IsValid(rigidbody)) {
                        rigidbody.position = absolutePosition;
                        rigidbody.rotation = absoluteRotation;
                    }
                }
                if (Utilities.IsValid(rigidbody)) {
                    rigidbody.useGravity = useGravity && pickupState == 0;
                    rigidbody.isKinematic = isKinematic || pickupState != 0;
                }
                lastPickupState = pickupState;
            } else {
                var rotation = UnpackRotation(rotationBits);
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
                if (Utilities.IsValid(root)) {
                    absolutePosition = root.TransformPoint(relativePosition);
                    absoluteRotation = root.rotation * relativeRotation;
                    lastRootPosition = root.position;
                    lastRootRotation = root.rotation;
                } else {
                    absolutePosition = relativePosition;
                    absoluteRotation = relativeRotation;
                }
                if (Utilities.IsValid(customPositionHandler)) {
                    customPositionHandler.ageTarget = this;
                    customPositionHandler._OnDeserializePosition();
                }
                if (pickupState != 0) absolutePosition = GetOnHandMatrix().MultiplyPoint3x4(smoothPosition);
                transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
                if (Utilities.IsValid(rigidbody)) {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                    rigidbody.position = absolutePosition;
                    rigidbody.rotation = absoluteRotation;
                }
                if (Utilities.IsValid(pickup)) pickup.pickupable = pickupable && (pickupState == 0 || !pickup.DisallowTheft);
            }
        }

        Matrix4x4 GetOnHandMatrix() {
            var owner = Networking.GetOwner(gameObject);
            if (!Utilities.IsValid(playerAttachedAGE) || owner != ageOwner) {
                playerAttachedAGE = manager.GetInstanceFromPlayer(owner);
                ageOwner = owner;
                if (!Utilities.IsValid(playerAttachedAGE)) return Matrix4x4.identity;
            }
            var pickupStateEnum = (VRCPlayerApi.TrackingDataType)(int)pickupState;
            switch (pickupStateEnum) {
                case VRCPlayerApi.TrackingDataType.LeftHand: return playerAttachedAGE.LeftHandMatrix;
                case VRCPlayerApi.TrackingDataType.RightHand: return playerAttachedAGE.RightHandMatrix;
                default: return Matrix4x4.identity;
            }
        }

        public override void OnPickup() {
            if (!localOnly && !Networking.IsOwner(gameObject)) {
                if (pickupState != 0 && Utilities.IsValid(pickup) && pickup.DisallowTheft) {
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
                if (Utilities.IsValid(pickup))
                    localPickupState = pickup.IsHeld ? (byte)(int)pickup.currentHand : (byte)0;
                if (!localOnly) {
                    pickupState = localPickupState;
                    if (isManualSync) RequestSerialization();
                }
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi to, VRCPlayerApi from) =>
            pickupState == 0 || !Utilities.IsValid(pickup) || !pickup.DisallowTheft;

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            if (!player.isLocal && pickupState != 0 && Utilities.IsValid(pickup)) SendCustomEventDelayedFrames(nameof(_Drop), 0);
        }

        public void _Drop() => pickup.Drop();
    }
}
