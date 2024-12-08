using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JLChnToZ.VRC.Foundation;
using VRC.SDK3.Data;

namespace JLChnToZ.VRC.AGE {
    /// <summary>The manager of Anti Gravity Engine.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("/Anti Gravity Engine/Anti Gravity Manager")]
    public partial class AntiGravityManager : UdonSharpBehaviour {
        [SerializeField] internal AntiGravityHandler[] handlers;
        [SerializeField] int initialSelectedHandler;
        [SerializeField] bool autoReattach;
        [SerializeField] bool autoUseOnLogin;
        [SerializeField] bool detachOnRespawn;
        bool init;
        VRCPlayerApi localPlayer;
        AntiGravityEngine template;
        AntiGravityEngine localInstance;
        DataDictionary instanceMap;

        /// <summary>
        /// Get the instance of <see cref="AntiGravityEngine"/> from a player.
        /// </summary>
        /// <param name="player">The player to get the instance from.</param>
        /// <returns>The instance of <see cref="AntiGravityEngine"/> from the player.</returns>
        public AntiGravityEngine GetInstanceFromPlayer(VRCPlayerApi player) {
            if (Utilities.IsValid(instanceMap) && instanceMap.TryGetValue(player.playerId, TokenType.Reference, out var instance))
                return (AntiGravityEngine)instance.Reference;
            return null;
        }

        /// <summary>
        /// Get the handler of the specified index.
        /// </summary>
        /// <param name="index">The index of the handler.</param>
        /// <returns>The handler of the specified index.</returns>
        public AntiGravityHandler GetHandlerOf(int index) {
            if (index < 0 || index >= handlers.Length) return null;
            return handlers[index];
        }

        void Start() {
            if (init) return;
            init = true;
            localPlayer = Networking.LocalPlayer;
            template = GetComponentInChildren<AntiGravityEngine>(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            var instance = (AntiGravityEngine)player.FindComponentInPlayerObjects(template);
            if (!Utilities.IsValid(instance)) {
                Debug.LogWarning("No AntiGravityEngine found in player objects.");
                return;
            }
            if (!Utilities.IsValid(instanceMap)) instanceMap = new DataDictionary();
            instanceMap[player.playerId] = instance;
            instance.autoReattach = autoReattach;
            instance.detachOnRespawn = detachOnRespawn;
            if (!player.isLocal) return;
            localInstance = instance;
            if (autoUseOnLogin) SendCustomEventDelayedSeconds(nameof(Use), 3F);
        }

        /// <summary>
        /// Attach local player to the Anti Gravity Engine with default or previous selected handler.
        /// </summary>
        /// <returns><langword>true</langword> if the player is successfully attached to the Anti Gravity Engine; otherwise, <langword>false</langword>.</returns>
        public bool Use() {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            localInstance._Exit();
            if (localInstance.selectedHandler == 0) SelectHandlerForLocal(initialSelectedHandler);
            if (localInstance._Use()) return true;
            return false;
            
        }

        /// <summary>
        /// Attach local player to the Anti Gravity Engine with specified handler.
        /// </summary>
        /// <param name="handlerIndex">The index of the handler to use.</param>
        /// <returns><langword>true</langword> if the player is successfully attached to the Anti Gravity Engine; otherwise, <langword>false</langword>.</returns>
        public bool Use(int handlerIndex) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            localInstance._Exit();
            SelectHandlerForLocal(handlerIndex);
            if (localInstance._Use()) return true;
            return false;
        }

        /// <summary>
        /// Attach local player to the Anti Gravity Engine with default or previous selected handler at specified position and rotation.
        /// </summary>
        /// <param name="position">The position to attach the player.</param>
        /// <param name="rotation">The rotation to attach the player.</param>
        /// <returns><langword>true</langword> if the player is successfully attached to the Anti Gravity Engine; otherwise, <langword>false</langword>.</returns>
        public bool UseAt(Vector3 position, Quaternion rotation) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            if (localInstance.selectedHandler == 0) SelectHandlerForLocal(initialSelectedHandler);
            localInstance.UseAt(position, rotation);
            return true;
        }

        /// <summary>
        /// Attach local player to the Anti Gravity Engine with specified handler at specified position and rotation.
        /// </summary>
        /// <param name="handlerIndex">The index of the handler to use.</param>
        /// <param name="position">The position to attach the player.</param>
        /// <param name="rotation">The rotation to attach the player.</param>
        /// <returns><langword>true</langword> if the player is successfully attached to the Anti Gravity Engine; otherwise, <langword>false</langword>.</returns>
        public bool UseAt(int handlerIndex, Vector3 position, Quaternion rotation) {
            Start();
            if (!Utilities.IsValid(localInstance)) return false;
            SelectHandlerForLocal(handlerIndex);
            localInstance.UseAt(position, rotation);
            return true;
        }

        /// <summary>
        /// Teleport local player to specified position and rotation.
        /// </summary>
        /// <param name="position">The position to teleport the player.</param>
        /// <param name="rotation">The rotation to teleport the player.</param>
        /// <remarks>
        /// This method will only work if the player is already attached to the Anti Gravity Engine.
        /// </remarks>
        public void TeleportTo(Vector3 position, Quaternion rotation) {
            Start();
            if (Utilities.IsValid(localInstance))
                localInstance.TeleportTo(position, rotation);
            else
                Networking.LocalPlayer.TeleportTo(position, rotation);
        }

        /// <summary>
        /// Exit the Anti Gravity Engine.
        /// </summary>
        public void Exit() {
            Start();
            localInstance._Exit();
        }

        void SelectHandlerForLocal(int index) {
            if (!Utilities.IsValid(localInstance)) return;
            localInstance.selectedHandler = (byte)(index + 1);
            localInstance.positionHandler = GetHandlerOf(index);
            if (localInstance.isManualSync) localInstance.RequestSerialization();
        }
    }

#if !COMPILER_UDONSHARP
    public partial class AntiGravityManager : ISingleton<AntiGravityManager> {
        void ISingleton<AntiGravityManager>.Merge(AntiGravityManager[] others) { }
    }
#endif
}
