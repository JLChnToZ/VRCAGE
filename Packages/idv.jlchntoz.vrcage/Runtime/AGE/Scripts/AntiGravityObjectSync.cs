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
        [UdonSynced] byte hand;
        byte localHand;
        VRCPickup pickup;
        VRCPlayerApi localPlayer;
        new Rigidbody rigidbody;
        bool isKinematic;
        bool useGravity;
        Vector3 smoothPosition;
        Quaternion smoothRotation = Quaternion.identity;

        VRC_Pickup.PickupHand Hand {
            get => (VRC_Pickup.PickupHand)(int)(localOnly ? localHand : hand);
            set {
                localHand = (byte)(int)value;
                if (!localOnly && hand != localHand) {
                    hand = localHand;
                    if (isManualSync) RequestSerialization();
                }
            }
        }

        void Start() {
            localPlayer = Networking.LocalPlayer;
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            rigidbody = GetComponent<Rigidbody>();
            if (Utilities.IsValid(rigidbody)) {
                isKinematic = rigidbody.isKinematic;
                useGravity = rigidbody.useGravity;
            }
        }

        void FixedUpdate() {
            var hand = Hand;
            var owner = Networking.GetOwner(gameObject);
            if (!Utilities.IsValid(playerAttachedAGE) || owner != ageOwner) {
                playerAttachedAGE = manager.GetInstanceFromPlayer(owner);
                ageOwner = owner;
                if (!Utilities.IsValid(playerAttachedAGE)) return;
            }
            bool isLocal = owner.isLocal;
            if (hand == VRC_Pickup.PickupHand.None)
                HandleSync(isLocal);
            else
                HandlePickup(hand, isLocal);
        }

        void HandleSync(bool isLocal) {
            if (isLocal || localOnly) {
                absolutePosition = transform.position;
                absoluteRotation = transform.rotation;
                CalcRelativePosition();
                if (!localOnly) {
                    var newRotation = PackQuaternion(relativeRotation);
                    if (position != relativePosition || rotationBits != newRotation) {
                        position = relativePosition;
                        rotationBits = newRotation;
                        if (isManualSync) RequestSerialization();
                    }
                }
            } else {
                float t = Time.fixedDeltaTime * lerpScale;
                relativePosition = Vector3.Lerp(relativePosition, position, t);
                relativeRotation = Quaternion.Slerp(relativeRotation, UnpackRotation(rotationBits), t);
                if (Utilities.IsValid(root)) {
                    absolutePosition = root.TransformPoint(relativePosition);
                    absoluteRotation = root.rotation * relativeRotation;
                } else {
                    absolutePosition = relativePosition;
                    absoluteRotation = relativeRotation;
                }
            }
            if (Utilities.IsValid(customPositionHandler)) {
                customPositionHandler.ageTarget = this;
                customPositionHandler._OnDeserializePosition();
            }
            transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
            if (Utilities.IsValid(rigidbody)) {
                rigidbody.useGravity = useGravity;
                rigidbody.isKinematic = isKinematic;
                rigidbody.position = absolutePosition;
                rigidbody.rotation = absoluteRotation;
            }
            if (Utilities.IsValid(pickup)) pickup.pickupable = pickupable;
        }

        void HandlePickup(VRC_Pickup.PickupHand hand, bool isLocal) {
            if (localOnly) return;
            Matrix4x4 matrix;
            Quaternion rotation;
            switch (hand) {
                case VRC_Pickup.PickupHand.Left:
                    matrix = playerAttachedAGE.LeftHandMatrix;
                    rotation = playerAttachedAGE.LeftHandRotation;
                    break;
                case VRC_Pickup.PickupHand.Right:
                    matrix = playerAttachedAGE.RightHandMatrix;
                    rotation = playerAttachedAGE.RightHandRotation;
                    break;
                default:
                    matrix = Matrix4x4.identity;
                    rotation = Quaternion.identity;
                    break;
            }
            if (isLocal) {
                absolutePosition = transform.position;
                absoluteRotation = transform.rotation;
                var newPosition = matrix.inverse.MultiplyPoint(absolutePosition);
                var newRotation = PackQuaternion(Quaternion.Inverse(rotation) * absoluteRotation);
                if (newPosition != position || rotationBits != newRotation) {
                    position = newPosition;
                    rotationBits = newRotation;
                    if (isManualSync) RequestSerialization();
                }
                return;
            }
            absolutePosition = matrix.MultiplyPoint(position);
            absoluteRotation = rotation * UnpackRotation(rotationBits);
            transform.SetPositionAndRotation(absolutePosition, absoluteRotation);
            if (Utilities.IsValid(rigidbody)) {
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.position = absolutePosition;
                rigidbody.rotation = absoluteRotation;
            }
            if (Utilities.IsValid(pickup)) pickup.pickupable = pickupable && !pickup.DisallowTheft;
        }

        void CalcRelativePosition() {
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
        }

        public override void OnPickup() {
            if (!localOnly && !Networking.IsOwner(gameObject)) {
                if (hand != 0 && Utilities.IsValid(pickup) && pickup.DisallowTheft) {
                    SendCustomEventDelayedFrames(nameof(_Drop), 0);
                    return;
                }
                Networking.SetOwner(localPlayer, gameObject);
            }
            _UpdatePickupState();
        }

        public override void OnDrop() {
            if (localOnly || Networking.IsOwner(gameObject))
                SendCustomEventDelayedFrames(nameof(_UpdatePickupState), 0);
        }

        public void _UpdatePickupState() {
            if (localOnly || Networking.IsOwner(gameObject))
                Hand = Utilities.IsValid(pickup) && pickup.IsHeld ?
                    pickup.currentHand : VRC_Pickup.PickupHand.None;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi to, VRCPlayerApi from) =>
            hand == 0 || !Utilities.IsValid(pickup) || !pickup.DisallowTheft;

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            if (!player.isLocal && hand != 0 && Utilities.IsValid(pickup)) SendCustomEventDelayedFrames(nameof(_Drop), 0);
        }

        public void _Drop() => pickup.Drop();
    }
}
