// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    public abstract class TrainCarBase : MonoBehaviour
    {
        private const  float   WHEEL_RADIUS = 0.027f;
        private const  float   TWO_PI       = Mathf.PI * 2.0f;
        private static Vector3 OFFSET       = new Vector3(0f, 0.0195f, 0f);

        [SerializeField] protected Transform   _frontWheels      = null;
        [SerializeField] protected Transform   _rearWheels       = null;
        [SerializeField] protected TrainTrack  _trainTrack       = null;
        [SerializeField] protected Transform[] _individualWheels = null;

        private   Pose  _frontPose = new Pose(), _rearPose = new Pose();
        protected float scale      = 1.0f;

        public float Distance { get; protected set; }

        public float Scale
        {
            get => scale;
            set => scale = value;
        }

        protected virtual void Awake()
        {
            Assert.IsNotNull(_frontWheels);
            Assert.IsNotNull(_rearWheels);
            Assert.IsNotNull(_trainTrack);
            Assert.IsNotNull(_individualWheels);
        }

        public void UpdatePose(float distance, TrainCarBase train, Pose pose)
        {
            // distance could be negative; add track length to it in case that happens
            distance = (train._trainTrack.TrackLength + distance) % train._trainTrack.TrackLength;
            if (distance < 0)
            {
                distance += train._trainTrack.TrackLength;
            }

            var currentSegment = train._trainTrack.GetSegment(distance);
            var distanceInto   = distance - currentSegment.StartDistance;

            currentSegment.UpdatePose(distanceInto, pose);
        }

        protected void UpdateCarPosition()
        {
            UpdatePose(Distance + _frontWheels.transform.localPosition.z * scale,
                    this, _frontPose);
            UpdatePose(Distance + _rearWheels.transform.localPosition.z * scale,
                    this, _rearPose);

            var midPoint         = 0.5f * (_frontPose.Position + _rearPose.Position);
            var carLookDirection = _frontPose.Position - _rearPose.Position;

            transform.position              = midPoint + OFFSET;
            transform.rotation              = Quaternion.LookRotation(carLookDirection, transform.up);
            _frontWheels.transform.rotation = _frontPose.Rotation;
            _rearWheels.transform.rotation  = _rearPose.Rotation;
        }

        protected void RotateCarWheels()
        {
            // divide by radius to get angle
            var angleOfRot = (Distance / WHEEL_RADIUS) % TWO_PI;

            foreach (var individualWheel in _individualWheels)
            {
                individualWheel.localRotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angleOfRot,
                        Vector3.right);
            }
        }

        public abstract void UpdatePosition();
    }
}
