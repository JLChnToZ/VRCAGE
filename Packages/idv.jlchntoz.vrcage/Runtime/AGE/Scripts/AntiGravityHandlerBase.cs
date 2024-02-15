using System;
using UdonSharp;
using UnityEngine;

namespace JLChnToZ.VRC.AGE {
    public abstract class AntiGravityHandlerBase : UdonSharpBehaviour {
        /// <summary>
        /// When any AGE pickup/station instance requesting for position sync, it will auto assign itself to here.
        /// </summary>
        [NonSerialized] public AntiGravityEngineBase ageTarget;

        /// <summary>
        /// Relative position that were/will sync from/to the network.
        /// </summary>
        protected Vector3 RelativePosition {
            get => ageTarget.relativePosition;
            set => ageTarget.relativePosition = value;
        }

        /// <summary>
        /// Relative rotation that were/will sync from/to the network.
        /// </summary>
        protected Quaternion RelativeRotation {
            get => ageTarget.relativeRotation;
            set => ageTarget.relativeRotation = value;
        }

        /// <summary>
        /// Absolute (world) position that the object/player were/will place at.
        /// </summary>
        protected Vector3 AbsolutePosition {
            get => ageTarget.absolutePosition;
            set => ageTarget.absolutePosition = value;
        }

        /// <summary>
        /// Absolute (world) rotation that the object/player were/will place at.
        /// </summary>
        protected Quaternion AbsoluteRotation {
            get => ageTarget.absoluteRotation;
            set => ageTarget.absoluteRotation = value;
        }

        /// <summary>
        /// This will be called when local player need to sync the position to others.
        /// </summary>
        public virtual void _OnSerializePosition() {}

        /// <summary>
        /// This will be called when received position update from others.
        /// </summary>
        public virtual void _OnDeserializePosition() {}
    }
}