using System;
using UdonSharp;
using UnityEngine;

namespace JLChnToZ.VRC.AGE {
    /// <summary>
    /// This class calculates the relative position and rotation of the object/player.
    /// </summary>
    /// <remarks>
    /// You can override this class to customize how the position and rotation are calculated.
    /// </remarks>
    [AddComponentMenu("Anti Gravity Engine/Default Anti Gravity Handler")]
    public class AntiGravityHandler : UdonSharpBehaviour {
        /// <summary>
        /// When any AGE pickup/station instance requesting for position sync, it will auto assign itself to here.
        /// </summary>
        [NonSerialized] public AntiGravityEngineBase ageTarget;
        /// <summary>
        /// Absolute (world) position that the object/player were/will place at.
        /// </summary>
        [NonSerialized] public Vector3 absolutePosition;
        /// <summary>
        /// Absolute (world) rotation that the object/player were/will place at.
        /// </summary>
        [NonSerialized] public Quaternion absoluteRotation;
        /// <summary>
        /// Relative position that were/will sync from/to the network.
        /// </summary>
        [NonSerialized] public Vector3 relativePosition;
        /// <summary>
        /// Relative rotation that were/will sync from/to the network.
        /// </summary>
        [NonSerialized] public Quaternion relativeRotation;

        /// <summary>
        /// This will be called when local player need to sync the position to others.
        /// </summary>
        /// <remarks>
        /// Override this method to customize how <see cref="relativePosition"/> and <see cref="relativeRotation"/> are calculated.
        /// By default, they are calculated based on the transform of the object this component attached to.
        /// </remarks>
        public virtual void _OnSerializePosition() {
            relativePosition = transform.InverseTransformPoint(absolutePosition);
            relativeRotation = Quaternion.Inverse(transform.rotation) * absoluteRotation;
        }

        /// <summary>
        /// This will be called when received position update from others.
        /// </summary>
        /// <remarks>
        /// Override this method to customize how <see cref="absolutePosition"/> and <see cref="absoluteRotation"/> are calculated.
        /// By default, they are calculated based on the transform of the object this component attached to.
        /// </remarks>
        public virtual void _OnDeserializePosition() {
            absolutePosition = transform.TransformPoint(relativePosition);
            absoluteRotation = transform.rotation * relativeRotation;
        }
    }
}