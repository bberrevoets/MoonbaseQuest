// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;

public class SimpleCapsuleWithStickMovement : MonoBehaviour
{
    public  bool         EnableLinearMovement     = true;
    public  bool         EnableRotation           = true;
    public  bool         HMDRotatesPlayer         = true;
    public  bool         RotationEitherThumbstick = false;
    public  float        RotationAngle            = 45.0f;
    public  float        Speed                    = 0.0f;
    public  OVRCameraRig CameraRig;
    private Rigidbody    _rigidbody;

    private bool ReadyToSnapTurn;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (CameraRig == null)
        {
            CameraRig = GetComponentInChildren<OVRCameraRig>();
        }
    }

    private void Start() { }

    private void FixedUpdate()
    {
        if (CameraUpdated != null)
        {
            CameraUpdated();
        }

        if (PreCharacterMove != null)
        {
            PreCharacterMove();
        }

        if (HMDRotatesPlayer)
        {
            RotatePlayerToHMD();
        }

        if (EnableLinearMovement)
        {
            StickMovement();
        }

        if (EnableRotation)
        {
            SnapTurn();
        }
    }

    public event Action CameraUpdated;
    public event Action PreCharacterMove;

    private void RotatePlayerToHMD()
    {
        var root      = CameraRig.trackingSpace;
        var centerEye = CameraRig.centerEyeAnchor;

        var prevPos = root.position;
        var prevRot = root.rotation;

        transform.rotation = Quaternion.Euler(0.0f, centerEye.rotation.eulerAngles.y, 0.0f);

        root.position = prevPos;
        root.rotation = prevRot;
    }

    private void StickMovement()
    {
        var ort                 = CameraRig.centerEyeAnchor.rotation;
        var ortEuler            = ort.eulerAngles;
        ortEuler.z = ortEuler.x = 0f;
        ort        = Quaternion.Euler(ortEuler);

        var moveDir     = Vector3.zero;
        var primaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        moveDir += ort * (primaryAxis.x * Vector3.right);
        moveDir += ort * (primaryAxis.y * Vector3.forward);
        //_rigidbody.MovePosition(_rigidbody.transform.position + moveDir * Speed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(_rigidbody.position + moveDir * Speed * Time.fixedDeltaTime);
    }

    private void SnapTurn()
    {
        var euler = transform.rotation.eulerAngles;

        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft) ||
            (RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickLeft)))
        {
            if (ReadyToSnapTurn)
            {
                euler.y         -= RotationAngle;
                ReadyToSnapTurn =  false;
            }
        }
        else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickRight) ||
                 (RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickRight)))
        {
            if (ReadyToSnapTurn)
            {
                euler.y         += RotationAngle;
                ReadyToSnapTurn =  false;
            }
        }
        else
        {
            ReadyToSnapTurn = true;
        }

        transform.rotation = Quaternion.Euler(euler);
    }
}
