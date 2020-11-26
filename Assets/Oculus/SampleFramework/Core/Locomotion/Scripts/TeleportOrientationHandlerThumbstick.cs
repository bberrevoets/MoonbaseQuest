// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

/// <summary>
///     This orientation handler will use the specified thumbstick to adjust the landing orientation of the teleport.
/// </summary>
public class TeleportOrientationHandlerThumbstick : TeleportOrientationHandler
{
    /// <summary>
    ///     HeadRelative=Character will orient to match the arrow. ForwardFacing=When user orients to match the arrow, they
    ///     will be facing the sensors.
    /// </summary>
    [Tooltip("HeadRelative=Character will orient to match the arrow. ForwardFacing=When user orients to match the arrow, they will be facing the sensors.")]
    public OrientationModes OrientationMode;

    /// <summary>
    ///     Which thumbstick is to be used for adjusting the teleport orientation.
    /// </summary>
    [Tooltip("Which thumbstick is to be used for adjusting the teleport orientation. Supports LTouch, RTouch, or Touch for either.")]
    public OVRInput.Controller Thumbstick;

    /// <summary>
    ///     The orientation will only change if the thumbstick magnitude is above this value. This will usually be larger than
    ///     the TeleportInputHandlerTouch.ThumbstickTeleportThreshold.
    /// </summary>
    [Tooltip("The orientation will only change if the thumbstick magnitude is above this value. This will usually be larger than the TeleportInputHandlerTouch.ThumbstickTeleportThreshold.")]
    public float RotateStickThreshold = 0.8f;

    private Quaternion _currentRotation;

    private Quaternion _initialRotation;
    private Vector2    _lastValidDirection;

    protected override void InitializeTeleportDestination()
    {
        _initialRotation    = LocomotionTeleport.GetHeadRotationY();
        _currentRotation    = _initialRotation;
        _lastValidDirection = new Vector2();
    }

    protected override void UpdateTeleportDestination()
    {
        float   magnitude;
        Vector2 direction;
        if (Thumbstick == OVRInput.Controller.Touch)
        {
            var leftDir  = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            var rightDir = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            var leftMag  = leftDir.magnitude;
            var rightMag = rightDir.magnitude;
            if (leftMag > rightMag)
            {
                magnitude = leftMag;
                direction = leftDir;
            }
            else
            {
                magnitude = rightMag;
                direction = rightDir;
            }
        }
        else
        {
            if (Thumbstick == OVRInput.Controller.LTouch)
            {
                direction = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            }
            else
            {
                direction = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            }

            magnitude = direction.magnitude;
        }

        if (!AimData.TargetValid)
        {
            _lastValidDirection = new Vector2();
        }

        if (magnitude < RotateStickThreshold)
        {
            direction = _lastValidDirection;
            magnitude = direction.magnitude;

            if (magnitude < RotateStickThreshold)
            {
                _initialRotation = LocomotionTeleport.GetHeadRotationY();
                direction.x      = 0;
                direction.y      = 1;
            }
        }
        else
        {
            _lastValidDirection = direction;
        }

        var tracking = LocomotionTeleport.LocomotionController.CameraRig.trackingSpace.rotation;

        if (magnitude > RotateStickThreshold)
        {
            direction /= magnitude; // normalize the vector
            var rot = _initialRotation * Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);
            _currentRotation = tracking * rot;
        }
        else
        {
            _currentRotation = tracking * LocomotionTeleport.GetHeadRotationY();
        }

        LocomotionTeleport.OnUpdateTeleportDestination(AimData.TargetValid, AimData.Destination, _currentRotation, GetLandingOrientation(OrientationMode, _currentRotation));
    }
}
