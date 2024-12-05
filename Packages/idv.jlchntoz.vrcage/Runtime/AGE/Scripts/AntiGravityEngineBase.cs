using System;
using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using JLChnToZ.VRC.Foundation;
using UdonSharpEditor;
#endif

namespace JLChnToZ.VRC.AGE {
    /// <summary>The base class for Anti Gravity Engine components.</summary>
    public abstract partial class AntiGravityEngineBase : UdonSharpBehaviour {
        [SerializeField, HideInInspector] protected AntiGravityManager manager;
        [SerializeField, HideInInspector] internal bool isManualSync;
        [NonSerialized] internal protected AntiGravityHandler positionHandler;
        [UdonSynced] internal byte selectedHandler;
        [UdonSynced(UdonSyncMode.Smooth)] protected Vector3 position;
        [UdonSynced] protected int rotationBits;

        public override void OnDeserialization() {
            positionHandler = selectedHandler > 0 ? manager.GetHandlerOf(selectedHandler - 1) : null;
        }

        protected bool SerializePosition(ref Vector3 position, ref Quaternion rotation) {
            if (!Utilities.IsValid(positionHandler)) return false;
            positionHandler.absolutePosition = position;
            positionHandler.absoluteRotation = rotation;
            positionHandler.ageTarget = this;
            positionHandler._OnSerializePosition();
            position = positionHandler.relativePosition;
            rotation = positionHandler.relativeRotation;
            return true;
        }

        protected bool DeserializePosition(ref Vector3 position, ref Quaternion rotation) {
            if (!Utilities.IsValid(positionHandler)) return false;
            positionHandler.relativePosition = position;
            positionHandler.relativeRotation = rotation;
            positionHandler.ageTarget = this;
            positionHandler._OnDeserializePosition();
            position = positionHandler.absolutePosition;
            rotation = positionHandler.absoluteRotation;
            return true;
        }

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

        /// <summary>
        /// Pack a quaternion into an integer.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        protected int PackQuaternion(Quaternion q) {
            int x = Mathf.RoundToInt((q.x + 1) * scaleX);
            int y = Mathf.RoundToInt((q.y + 1) * scaleY);
            int z = Mathf.RoundToInt((q.z + 1) * scaleZ);
            int w = q.w < 0 ? signBit : 0;
            return (x << shiftX) | (y << shiftY) | (z << shiftZ) | w;
        }

        /// <summary>
        /// Unpack a quaternion from an integer.
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
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