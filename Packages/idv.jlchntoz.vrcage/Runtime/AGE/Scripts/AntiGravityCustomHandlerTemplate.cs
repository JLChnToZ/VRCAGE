using UdonSharp;
using JLChnToZ.VRC.AGE;

// This is a template can be use as a starting point for writing custom mechanics to use in AGE.
// You can create a separate UDON behaviour, put it in your world and assign it to `AntiGravityManager` and/or `AntiGravityObjectSync`,
// then the system will use your logic to calculate the positions and rotations.
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class AntiGravityCustomHandlerTemplate : AntiGravityHandlerBase {

    // This will be called when local player need to sync the position to others.
    public override void _OnSerializePosition() {
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
    public override void _OnDeserializePosition() {
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
