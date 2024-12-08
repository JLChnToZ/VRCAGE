using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

namespace JLChnToZ.VRC.AGE {
    /// <summary>
    /// The component acts likes <see cref="VRCObjectSync"/> but with Anti Gravity Engine support.
    /// </summary>
    [AddComponentMenu("Anti Gravity Engine/Anti Gravity Object Sync")]
    public class AntiGravityObjectSync : AntiGravityEngineBase {
        [SerializeField] bool localOnly;
        [SerializeField] int initialSelectedHandler = -1;
        /// <summary>If <see langword="true"/>, the object will be pickupable.</summary>
        public bool pickupable = true;
        [NonSerialized]
        #if COMPILER_UDONSHARP
        public
        #else
        internal
        #endif
        float lerpScale = 10;
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
            if (Networking.IsOwner(gameObject) && initialSelectedHandler >= 0) {
                selectedHandler = (byte)(initialSelectedHandler + 1);
                positionHandler = manager.GetHandlerOf(initialSelectedHandler);
                if (isManualSync) RequestSerialization();
            }
        }

        void FixedUpdate() {
            var hand = Hand;
            var owner = Networking.GetOwner(gameObject);
            if (!Utilities.IsValid(playerAttachedAGE) || owner != ageOwner) {
                playerAttachedAGE = manager.GetInstanceFromPlayer(owner);
                ageOwner = owner;
            }
            bool isLocal = owner.isLocal;
            if (hand == VRC_Pickup.PickupHand.None)
                HandleSync(isLocal);
            else
                HandlePickup(hand, isLocal);
        }

        void HandleSync(bool isLocal) {
            Vector3 newPosition;
            Quaternion newRotation;
            int newRotationBits;
            if (isLocal || localOnly) {
                newPosition = transform.position;
                newRotation = transform.rotation;
                SerializePosition(ref newPosition, ref newRotation);
                smoothPosition = newPosition;
                smoothRotation = newRotation;
                if (!localOnly) {
                    newRotationBits = PackQuaternion(newRotation);
                    if (position != newPosition || rotationBits != newRotationBits) {
                        position = newPosition;
                        rotationBits = newRotationBits;
                        if (isManualSync) RequestSerialization();
                    }
                }
            } else {
                float t = Time.fixedDeltaTime * lerpScale;
                newPosition = smoothPosition = Vector3.Lerp(smoothPosition, position, t);
                newRotation = smoothRotation = Quaternion.Slerp(smoothRotation, UnpackRotation(rotationBits), t);
            }
            if (!DeserializePosition(ref newPosition, ref newRotation)) {
                newPosition = smoothPosition;
                newRotation = smoothRotation;
            }
            transform.SetPositionAndRotation(newPosition, newRotation);
            if (Utilities.IsValid(rigidbody)) {
                rigidbody.useGravity = useGravity;
                rigidbody.isKinematic = isKinematic;
                rigidbody.position = newPosition;
                rigidbody.rotation = newRotation;
            }
            if (Utilities.IsValid(pickup)) pickup.pickupable = pickupable;
        }

        void HandlePickup(VRC_Pickup.PickupHand hand, bool isLocal) {
            if (localOnly || !Utilities.IsValid(playerAttachedAGE)) return;
            Vector3 position;
            Quaternion rotation;
            Matrix4x4 matrix;
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
                position = matrix.inverse.MultiplyPoint(transform.position);
                var newRotationBits = PackQuaternion(Quaternion.Inverse(rotation) * transform.rotation);
                if (position != this.position || rotationBits != newRotationBits) {
                    this.position = position;
                    rotationBits = newRotationBits;
                    if (isManualSync) RequestSerialization();
                }
                return;
            }
            position = matrix.MultiplyPoint(this.position);
            rotation *= UnpackRotation(rotationBits);
            var t = Time.fixedDeltaTime * lerpScale;
            smoothPosition = Vector3.Lerp(smoothPosition, position, t);
            smoothRotation = Quaternion.Slerp(smoothRotation, rotation, t);
            transform.SetPositionAndRotation(smoothPosition, smoothRotation);
            if (Utilities.IsValid(rigidbody)) {
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.position = smoothPosition;
                rigidbody.rotation = smoothRotation;
            }
            if (Utilities.IsValid(pickup)) pickup.pickupable = pickupable && !pickup.DisallowTheft;
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

        #if COMPILER_UDONSHARP
        public
        #endif
        void _UpdatePickupState() {
            if (localOnly || Networking.IsOwner(gameObject))
                Hand = Utilities.IsValid(pickup) && pickup.IsHeld ?
                    pickup.currentHand : VRC_Pickup.PickupHand.None;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi to, VRCPlayerApi from) =>
            hand == 0 || !Utilities.IsValid(pickup) || !pickup.DisallowTheft;

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            if (!player.isLocal && hand != 0 && Utilities.IsValid(pickup)) SendCustomEventDelayedFrames(nameof(_Drop), 0);
        }

        public override void OnDeserialization() {
            base.OnDeserialization();
            if (localHand != hand) {
                localHand = hand;
                if (localHand == 0) {
                    smoothPosition = position;
                    smoothRotation = UnpackRotation(rotationBits);
                } else {
                    smoothPosition = transform.position;
                    smoothRotation = transform.rotation;
                }
            }
        }

        #if COMPILER_UDONSHARP
        public
        #endif
        void _Drop() => pickup.Drop();
    }
}
