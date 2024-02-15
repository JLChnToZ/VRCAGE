using System;
using UdonSharp;
using UnityEngine;

namespace JLChnToZ.VRC.AGE {
    public abstract class AntiGravityEngineBase : UdonSharpBehaviour {
        [Tooltip("Enable this if current instance is setted to manual sync.")]
        public bool isManualSync;
        public Transform root;
        [NonSerialized] public AntiGravityHandlerBase customPositionHandler;
        [NonSerialized] public Vector3 absolutePosition;
        [NonSerialized] public Quaternion absoluteRotation;
        [NonSerialized] public Vector3 relativePosition;
        [NonSerialized] public Quaternion relativeRotation;
        [UdonSynced] protected Vector3 position;
        [UdonSynced] protected Quaternion rotation;
    }
}