// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    public class TrainTrack : MonoBehaviour
    {
        [SerializeField] private float     _gridSize      = 0.5f;
        [SerializeField] private int       _subDivCount   = 20;
        [SerializeField] private Transform _segmentParent = null;

        [SerializeField] private Transform _trainParent = null;

        // regeneration is optional
        [SerializeField] private bool           _regnerateTrackMeshOnAwake = false;
        private                  TrackSegment[] _trackSegments             = null;

        private float _trainLength = -1.0f;

        public float TrackLength
        {
            get => _trainLength;
            private set => _trainLength = value;
        }

        private void Awake()
        {
            Assert.IsNotNull(_segmentParent);
            Assert.IsNotNull(_trainParent);
            Regenerate();
        }

        public TrackSegment GetSegment(float distance)
        {
            var childCount = _segmentParent.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var segment     = _trackSegments[i];
                var nextSegment = _trackSegments[(i + 1) % childCount];
                if (distance >= segment.StartDistance && (distance < nextSegment.StartDistance || i == childCount - 1))
                {
                    return segment;
                }
            }

            return null;
        }

        public void Regenerate()
        {
            _trackSegments = _segmentParent.GetComponentsInChildren<TrackSegment>();
            TrackLength    = 0;
            var          childCount  = _segmentParent.childCount;
            TrackSegment lastSegment = null;

            var ratio = 0.0f;
            for (var i = 0; i < childCount; i++)
            {
                var segment = _trackSegments[i];
                segment.SubDivCount = _subDivCount;
                ratio               = segment.setGridSize(_gridSize);
                if (lastSegment != null)
                {
                    var endPose = lastSegment.EndPose;
                    segment.transform.position = endPose.Position;
                    segment.transform.rotation = endPose.Rotation;
                    segment.StartDistance      = TrackLength;
                }

                if (_regnerateTrackMeshOnAwake)
                {
                    segment.RegenerateTrackAndMesh();
                }

                TrackLength += segment.SegmentLength;
                lastSegment =  segment;
            }

            SetScale(ratio);
        }

        private void SetScale(float ratio)
        {
            _trainParent.localScale = new Vector3(ratio, ratio, ratio);
            var cars       = _trainParent.GetComponentsInChildren<TrainCar>();
            var locomotive = _trainParent.GetComponentInChildren<TrainLocomotive>();
            locomotive.Scale = ratio;
            foreach (var car in cars)
            {
                car.Scale = ratio;
            }
        }
    }
}
