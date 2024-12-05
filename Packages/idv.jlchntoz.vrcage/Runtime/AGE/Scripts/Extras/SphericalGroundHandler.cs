using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE.Extras {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SphericalGroundHandler : AntiGravityHandler {
        [SerializeField] float height;
        [SerializeField] Transform idlePos;
        VRCPlayerApi localPlayer;

        void Start() {
            localPlayer = Networking.LocalPlayer;
        }

        void FixedUpdate() {
            if (!enabled) return;
            var origPos = transform.position;
            var newPos = localPlayer.GetPosition();
            var direction = newPos - origPos;
            direction.y = 0;
            var distance = direction.magnitude;
            if (Mathf.Approximately(0, distance)) return;
            transform.Rotate(
                Quaternion.LookRotation(direction, Vector3.up) * Vector3.left,
                distance / (newPos.y - height) * Mathf.Rad2Deg,
                Space.World
            );
            newPos.y = height;
            transform.position = newPos;
        }

        void OnDisable() {
            if (idlePos != null) transform.SetPositionAndRotation(idlePos.position, idlePos.rotation);
        }
    }
}
