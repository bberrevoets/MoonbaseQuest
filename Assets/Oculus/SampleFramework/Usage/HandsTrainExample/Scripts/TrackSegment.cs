// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    public class TrackSegment : MonoBehaviour
    {
        public enum SegmentType
        {
            Straight = 0,
            LeftTurn,
            RightTurn,
            Switch
        }

        private const float _originalGridSize = 0.8f;
        private const float _trackWidth       = 0.15f;

        [SerializeField] private SegmentType _segmentType = SegmentType.Straight;
        [SerializeField] private MeshFilter  _straight    = null;
        [SerializeField] private MeshFilter  _leftTurn    = null;
        [SerializeField] private MeshFilter  _rightTurn   = null;

        private Pose       _endPose  = new Pose();
        private float      _gridSize = 0.8f;
        private GameObject _mesh     = null;

        // create variables here to avoid realtime allocation
        private Pose  _p1          = new Pose();
        private Pose  _p2          = new Pose();
        private int   _subDivCount = 20;
        public  float StartDistance { get; set; }

        public float GridSize
        {
            get => _gridSize;
            private set => _gridSize = value;
        }

        public int SubDivCount
        {
            get => _subDivCount;
            set => _subDivCount = value;
        }

        public SegmentType Type => _segmentType;

        public Pose EndPose
        {
            get
            {
                UpdatePose(SegmentLength, _endPose);
                return _endPose;
            }
        }

        public float Radius => 0.5f * GridSize;

        public float SegmentLength
        {
            get
            {
                switch (Type)
                {
                    case SegmentType.Straight:
                        return GridSize;
                    case SegmentType.LeftTurn:
                    case SegmentType.RightTurn:
                        // return quarter of circumference.
                        return 0.5f * Mathf.PI * Radius;
                }

                return 1f;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(_straight);
            Assert.IsNotNull(_leftTurn);
            Assert.IsNotNull(_rightTurn);
        }

        private void Update()
        {
            // uncomment to debug the track path
            //DrawDebugLines();
        }

        private void OnDisable()
        {
            Destroy(_mesh);
        }

        public float setGridSize(float size)
        {
            GridSize = size;
            return GridSize / _originalGridSize;
        }

        /// <summary>
        ///     Updates pose given distance into segment. While this mutates a value,
        ///     it avoids generating a new object.
        /// </summary>
        public void UpdatePose(float distanceIntoSegment, Pose pose)
        {
            if (Type == SegmentType.Straight)
            {
                pose.Position = transform.position + distanceIntoSegment * transform.forward;
                pose.Rotation = transform.rotation;
            }
            else if (Type == SegmentType.LeftTurn)
            {
                var normalizedDistanceIntoSegment = distanceIntoSegment / SegmentLength;
                // the turn is 90 degrees, so find out how far we are into it
                var angle = 0.5f * Mathf.PI * normalizedDistanceIntoSegment;
                // unity is left handed so the rotations go the opposite directions in X for left turns --
                // invert that by subtracting by radius. also note the angle negation below
                var localPosition = new Vector3(Radius * Mathf.Cos(angle) - Radius, 0, Radius * Mathf.Sin(angle));
                var localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                pose.Position = transform.TransformPoint(localPosition);
                pose.Rotation = transform.rotation * localRotation;
            }
            else if (Type == SegmentType.RightTurn)
            {
                // when going to right, start from PI (180) and go toward 90
                var angle = Mathf.PI - 0.5f * Mathf.PI * distanceIntoSegment / SegmentLength;
                // going right means we start at radius distance away, and decrease toward zero
                var localPosition = new Vector3(Radius * Mathf.Cos(angle) + Radius, 0, Radius * Mathf.Sin(angle));
                var localRotation = Quaternion.Euler(0, (Mathf.PI - angle) * Mathf.Rad2Deg, 0);
                pose.Position = transform.TransformPoint(localPosition);
                pose.Rotation = transform.rotation * localRotation;
            }
            else
            {
                pose.Position = Vector3.zero;
                pose.Rotation = Quaternion.identity;
            }
        }

        private void DrawDebugLines()
        {
            for (var i = 1; i < SubDivCount + 1; i++)
            {
                var len = SegmentLength / SubDivCount;
                UpdatePose((i - 1) * len, _p1);
                UpdatePose(i * len,       _p2);
                // right segment from p1 to p2
                var halfTrackWidth = 0.5f * _trackWidth;
                Debug.DrawLine(_p1.Position + halfTrackWidth * (_p1.Rotation * Vector3.right),
                        _p2.Position + halfTrackWidth * (_p2.Rotation * Vector3.right));
                // left segment from p1 to p2
                Debug.DrawLine(_p1.Position - halfTrackWidth * (_p1.Rotation * Vector3.right),
                        _p2.Position - halfTrackWidth * (_p2.Rotation * Vector3.right));
            }

            // bottom bound
            Debug.DrawLine(transform.position - 0.5f * GridSize * transform.right,
                    transform.position + 0.5f * GridSize * transform.right, Color.yellow);
            // left bound
            Debug.DrawLine(transform.position - 0.5f * GridSize * transform.right,
                    transform.position - 0.5f * GridSize * transform.right + GridSize * transform.forward, Color.yellow);
            // right bound
            Debug.DrawLine(transform.position + 0.5f * GridSize * transform.right,
                    transform.position + 0.5f * GridSize * transform.right + GridSize * transform.forward, Color.yellow);
            // top bound
            Debug.DrawLine(transform.position - 0.5f * GridSize * transform.right +
                           GridSize * transform.forward, transform.position +
                                                         0.5f * GridSize * transform.right + GridSize * transform.forward,
                    Color.yellow);
        }

        public void RegenerateTrackAndMesh()
        {
            if (transform.childCount > 0 && !_mesh)
            {
                _mesh = transform.GetChild(0).gameObject;
            }

            if (_mesh)
            {
                DestroyImmediate(_mesh);
            }

            if (_segmentType == SegmentType.LeftTurn)
            {
                _mesh = Instantiate(_leftTurn.gameObject);
            }
            else if (_segmentType == SegmentType.RightTurn)
            {
                _mesh = Instantiate(_rightTurn.gameObject);
            }
            else
            {
                _mesh = Instantiate(_straight.gameObject);
            }

            _mesh.transform.SetParent(transform, false);
            _mesh.transform.position += GridSize / 2.0f * transform.forward;
            _mesh.transform.localScale = new Vector3(GridSize / _originalGridSize, GridSize / _originalGridSize,
                    GridSize / _originalGridSize);
        }
    }
}
