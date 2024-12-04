using System;
using UnityEngine;
using UdonSharp;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using JLChnToZ.VRC.Foundation;
using UdonSharpEditor;
#endif

namespace JLChnToZ.VRC.AGE {
    public abstract partial class AntiGravityEngineBase : UdonSharpBehaviour {
        [SerializeField, HideInInspector] protected bool isManualSync;
        public Transform root;
        [NonSerialized] public AntiGravityHandlerBase customPositionHandler;
        [NonSerialized] public Vector3 absolutePosition;
        [NonSerialized] public Quaternion absoluteRotation;
        [NonSerialized] public Vector3 relativePosition;
        [NonSerialized] public Quaternion relativeRotation;
        [UdonSynced(UdonSyncMode.Smooth)] protected Vector3 position;
        [UdonSynced] protected int rotationBits;

        #region Quaternion Encode Decoder
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

        // Pack a quaternion into an integer.
        protected int PackQuaternion(Quaternion q) {
            int x = Mathf.RoundToInt((q.x + 1) * scaleX);
            int y = Mathf.RoundToInt((q.y + 1) * scaleY);
            int z = Mathf.RoundToInt((q.z + 1) * scaleZ);
            int w = q.w < 0 ? signBit : 0;
            return (x << shiftX) | (y << shiftY) | (z << shiftZ) | w;
        }

        // Unpack a quaternion from an integer.
        protected Quaternion UnpackRotation(int bits) {
            float x = ((bits >> shiftX) & maskX) / scaleX - 1;
            float y = ((bits >> shiftY) & maskY) / scaleY - 1;
            float z = ((bits >> shiftZ) & maskZ) / scaleZ - 1;
            var v = new Vector3(x, y, z);
            float w = Vector3.Dot(v, v);
            if (w > 1)
            {
                v *= 1 / Mathf.Sqrt(w);
                w = 0;
            }
            else
                w = Mathf.Sqrt(1 - w) * Mathf.Sign(bits);
            return new Quaternion(v.x, v.y, v.z, w).normalized;
        }
        #endregion
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    public partial class AntiGravityEngineBase : ISelfPreProcess {
        public int Priority => 0;

        public virtual void PreProcess() {
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(this);
            if (udon == null) return;
            isManualSync = udon.SyncIsManual;
            UdonSharpEditorUtility.CopyProxyToUdon(this);
        }
    }
#endif
}