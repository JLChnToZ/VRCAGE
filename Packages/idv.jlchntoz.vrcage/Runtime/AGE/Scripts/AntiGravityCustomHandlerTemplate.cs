using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JLChnToZ.VRC.AGE;

// This is a template can be use as a starting point for writing custom mechanics to use in AGE.
// You can create a separate UDON behaviour, put it in your world and assign it to `AntiGravityManager` and/or `AntiGravityObjectSync`,
// then the system will use your logic to calculate the positions and rotations.
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class AntiGravityCustomHandlerTemplate : UdonSharpBehaviour {
    // When any AGE pickup/station instance requesting for position sync, it will auto assign itself to here.
    [NonSerialized] public UdonSharpBehaviour ageTarget;

    // Relative position that were/will sync from/to the network.
    Vector3 RelativePosition {
        get => (Vector3)ageTarget.GetProgramVariable(nameof(AntiGravityEngine.relativePosition));
        set => ageTarget.SetProgramVariable(nameof(AntiGravityEngine.relativePosition), value);
    }

    // Relative rotation that were/will sync from/to the network.
    Quaternion RelativeRotation {
        get => (Quaternion)ageTarget.GetProgramVariable(nameof(AntiGravityEngine.relativeRotation));
        set => ageTarget.SetProgramVariable(nameof(AntiGravityEngine.relativeRotation), value);
    }

    // Absolute (world) position that the object/player were/will place at.
    Vector3 AbsolutePosition {
        get => (Vector3)ageTarget.GetProgramVariable(nameof(AntiGravityEngine.absolutePosition));
        set => ageTarget.SetProgramVariable(nameof(AntiGravityEngine.absolutePosition), value);
    }

    // Absolute (world) rotation that the object/player were/will place at.
    Quaternion AbsoluteRotation {
        get => (Quaternion)ageTarget.GetProgramVariable(nameof(AntiGravityEngine.absoluteRotation));
        set => ageTarget.SetProgramVariable(nameof(AntiGravityEngine.absoluteRotation), value);
    }

    // This will be called when local player need to sync the position to others.
    public void _OnSerializePosition() {
        // Uncomment following lines to get position and rotation from requested AGE instance.
        // var relativePosition = RelativePosition;
        // var relativeRotation = RelativeRotation;
        // var absolutePosition = AbsolutePosition;
        // var absoluteRotation = AbsoluteRotation;

        // After adjusting the relative position / rotation, use following lines to return the value back.
        // RelativePosition = relativePosition;
        // RelativePosition = relativeRotation;
    }

    // This will be called when received position update from others.
    public void _OnDeserializePosition() {
        // Uncomment following lines to get position and rotation from requested AGE instance.
        // var relativePosition = RelativePosition;
        // var relativeRotation = RelativeRotation;
        // var absolutePosition = AbsolutePosition;
        // var absoluteRotation = AbsoluteRotation;

        // After adjusting the absolute position / rotation, use following lines to return the value back.
        // AbsolutePosition = absolutePosition;
        // AbsoluteRotation = absoluteRotation;
    }
}
