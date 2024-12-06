using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JLChnToZ.VRC.AGE.Extras {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TileLooper : AntiGravityHandler {
        [SerializeField] Transform[] sceneObjects;
        [SerializeField] Vector3 repeat;
        [SerializeField] float lookAtCenterDistance;

        VRCPlayerApi player;
        Transform[] alignX, alignY, alignZ;
        float[] orgX, orgY, orgZ;
        int xOffset, yOffset, zOffset;
        float[] tempFloatArray;
        Vector3 lastPos;
        bool firstRun = true;
        bool isTrackingPlayer = false;

        Vector3 PlayerPos {
            get {
                if (Utilities.IsValid(player)) {
                    var pos = player.GetPosition();
                    if (lookAtCenterDistance > 0) {
                        var tt = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                        pos += tt.rotation * Vector3.forward * lookAtCenterDistance;
                    }
                    return pos;
                }
                return Vector3.zero;
            }
        }

        void Start() {
            player = Networking.LocalPlayer;
            if (!Utilities.IsValid(player) || sceneObjects == null || sceneObjects.Length == 0) {
                Debug.LogWarning("[ScrollingSceneManager] Invalid state therfore will not run.");
                enabled = false;
                return;
            }
            if (repeat.x > 0) { alignX = SortSceneObjects(0); orgX = tempFloatArray; }
            if (repeat.y > 0) { alignY = SortSceneObjects(1); orgY = tempFloatArray; }
            if (repeat.z > 0) { alignZ = SortSceneObjects(2); orgZ = tempFloatArray; }
            lastPos = PlayerPos;
            SendCustomEventDelayedFrames(nameof(_SlowUpdate), 0);
        }

        public void _SlowUpdate() {
            SendCustomEventDelayedSeconds(nameof(_SlowUpdate), 0.5F);
            ClampLocalPlayer();
            var playerPos = PlayerPos;
            if (!firstRun && playerPos == lastPos) return;
            xOffset = UpdatePos(playerPos, alignX, orgX, xOffset, 0);
            yOffset = UpdatePos(playerPos, alignY, orgY, yOffset, 1);
            zOffset = UpdatePos(playerPos, alignZ, orgZ, zOffset, 2);
            firstRun = false;
        }

        Transform[] SortSceneObjects(int axis) {
            int count = sceneObjects.Length;
            var sortedSceneObjects = new Transform[count];
            tempFloatArray = new float[count];
            for (int i = 0; i < count; i++) {
                var t = sceneObjects[i];
                float pos = Mathf.Repeat(t.position[axis], repeat[axis]);
                int hit = Mathf.Max(0, BinarySearch(sortedSceneObjects, axis, pos, 0, i));
                if (hit <= i) {
                    Array.Copy(sortedSceneObjects, hit, sortedSceneObjects, hit + 1, i - hit);
                    Array.Copy(tempFloatArray, hit, tempFloatArray, hit + 1, i - hit);
                }
                sortedSceneObjects[hit] = t;
                tempFloatArray[hit] = pos;
            }
            return sortedSceneObjects;
        }

        int BinarySearch(Transform[] array, int axis, float key, int lowerBoundIndex, int upperBoundIndex) {
            while (lowerBoundIndex < upperBoundIndex) {
                int middleIndex = lowerBoundIndex + (upperBoundIndex - lowerBoundIndex) / 2;
                float current = array[middleIndex].position[axis];
                if (current < key)
                    lowerBoundIndex = middleIndex + 1;
                else if (current > key)
                    upperBoundIndex = middleIndex - 1;
                else if (upperBoundIndex - lowerBoundIndex < 2)
                    return key == array[lowerBoundIndex].position[axis] ? lowerBoundIndex : middleIndex;
                else
                    upperBoundIndex = middleIndex;
            }
            return lowerBoundIndex;
        }

        int UpdatePos(Vector3 playerPos, Transform[] sortedObjects, float[] originalPos, int offset, int axis) {
            float repeat = this.repeat[axis];
            if (repeat > 0) {
                int count = sortedObjects.Length;
                float lastPlayerPosAxis = lastPos[axis];
                float playerPosAxis = playerPos[axis];
                if (firstRun || !Mathf.Approximately(lastPlayerPosAxis, playerPosAxis)) {
                    lastPos[axis] = playerPosAxis;
                    for (int i = 0; i < count; i++) {
                        var obj = sortedObjects[i];
                        float orgPos = originalPos[i];
                        var objPos = obj.position;
                        float oldPos = objPos[axis];
                        float newPos = GetRelativePosUnchecked(playerPosAxis, orgPos, repeat);
                        if (!Mathf.Approximately(oldPos, newPos)) {
                            objPos[axis] = newPos;
                            obj.position = objPos;
                        }
                    }
                }
            }
            return offset;
        }

        public override void _OnSerializePosition() {
            base._OnSerializePosition();
            isTrackingPlayer = true;
        }

        public override void _OnDeserializePosition() {
            var playerPos = PlayerPos;
            if (repeat.x > 0) relativePosition.x = GetRelativePos(playerPos.x, relativePosition.x, repeat.x);
            if (repeat.y > 0) relativePosition.y = GetRelativePos(playerPos.y, relativePosition.y, repeat.y);
            if (repeat.z > 0) relativePosition.z = GetRelativePos(playerPos.z, relativePosition.z, repeat.z);
            base._OnDeserializePosition();
        }

        void ClampLocalPlayer() {
            if (!isTrackingPlayer) return;
            isTrackingPlayer = false;
            var oldPos = player.GetPosition();
            var newPos = new Vector3(
                ClampByAxis(oldPos.x, 0),
                ClampByAxis(oldPos.y, 1),
                ClampByAxis(oldPos.z, 2)
            );
            if (oldPos != newPos) player.TeleportTo(newPos, player.GetRotation());
        }

        float ClampByAxis(float pos, int axis) {
            float repeat = this.repeat[axis];
            return repeat > 0 && Mathf.Abs(pos) >= repeat * 2 ? Mathf.Repeat(pos, repeat) : pos;
        }

        float GetRelativePos(float playerPos, float objPos, float repeat) => GetRelativePosUnchecked(playerPos, Mathf.Repeat(objPos, repeat), repeat);

        float GetRelativePosUnchecked(float playerPos, float objPos, float repeat) => Mathf.Round((playerPos - objPos) / repeat) * repeat + objPos;
    }
}