// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Ray tool used for far-field interactions.
    /// </summary>
    public class RayTool : InteractableTool
    {
        private const float MINIMUM_RAY_CAST_DISTANCE = 0.8f;
        private const float COLLIDER_RADIUS           = 0.01f;
        private const int   NUM_MAX_PRIMARY_HITS      = 10;
        private const int   NUM_MAX_SECONDARY_HITS    = 25;
        private const int   NUM_COLLIDERS_TO_TEST     = 20;

        [SerializeField]                      private RayToolView _rayToolView         = null;
        [Range(0.0f, 45.0f)] [SerializeField] private float       _coneAngleDegrees    = 20.0f;
        [SerializeField]                      private float       _farFieldMaxDistance = 5f;

        private Collider[] _collidersOverlapped = new Collider[NUM_COLLIDERS_TO_TEST];
        private float      _coneAngleReleaseDegrees;

        private Interactable _currInteractableCastedAgainst = null;
        private Interactable _focusedInteractable;
        private bool         _initialized = false;

        private PinchStateModule _pinchStateModule = new PinchStateModule();

        private RaycastHit[] _primaryHits             = new RaycastHit[NUM_MAX_PRIMARY_HITS];
        private Collider[]   _secondaryOverlapResults = new Collider[NUM_MAX_SECONDARY_HITS];

        public override InteractableToolTags ToolTags => InteractableToolTags.Ray;

        public override ToolInputState ToolInputState
        {
            get
            {
                if (_pinchStateModule.PinchDownOnFocusedObject)
                {
                    return ToolInputState.PrimaryInputDown;
                }

                if (_pinchStateModule.PinchSteadyOnFocusedObject)
                {
                    return ToolInputState.PrimaryInputDownStay;
                }

                if (_pinchStateModule.PinchUpAndDownOnFocusedObject)
                {
                    return ToolInputState.PrimaryInputUp;
                }

                return ToolInputState.Inactive;
            }
        }

        public override bool IsFarFieldTool => true;

        public override bool EnableState
        {
            get => _rayToolView.EnableState;
            set => _rayToolView.EnableState = value;
        }

        private void Update()
        {
            if (!HandsManager.Instance || !HandsManager.Instance.IsInitialized() || !_initialized)
            {
                return;
            }

            var hand    = IsRightHandedTool ? HandsManager.Instance.RightHand : HandsManager.Instance.LeftHand;
            var pointer = hand.PointerPose;
            transform.position = pointer.position;
            transform.rotation = pointer.rotation;

            var prevPosition = InteractionPosition;
            var currPosition = transform.position;
            Velocity            = (currPosition - prevPosition) / Time.deltaTime;
            InteractionPosition = currPosition;

            _pinchStateModule.UpdateState(hand, _focusedInteractable);
            _rayToolView.ToolActivateState = _pinchStateModule.PinchSteadyOnFocusedObject ||
                                             _pinchStateModule.PinchDownOnFocusedObject;
        }

        private void OnDestroy()
        {
            if (InteractableToolsInputRouter.Instance != null)
            {
                InteractableToolsInputRouter.Instance.UnregisterInteractableTool(this);
            }
        }

        public override void Initialize()
        {
            Assert.IsNotNull(_rayToolView);
            InteractableToolsInputRouter.Instance.RegisterInteractableTool(this);
            _rayToolView.InteractableTool = this;
            _coneAngleReleaseDegrees      = _coneAngleDegrees * 1.2f;
            _initialized                  = true;
        }

        /// <summary>
        ///     Avoid hand collider during raycasts so move origin some distance away from where tool is.
        /// </summary>
        /// <returns>Proper raycast origin.</returns>
        private Vector3 GetRayCastOrigin() => transform.position + MINIMUM_RAY_CAST_DISTANCE * transform.forward;

        public override List<InteractableCollisionInfo> GetNextIntersectingObjects()
        {
            if (!_initialized)
            {
                return _currentIntersectingObjects;
            }

            // if we already have focused on something, keep it until the angle between
            // our forward direction and object vector becomes too large
            if (_currInteractableCastedAgainst != null &&
                HasRayReleasedInteractable(_currInteractableCastedAgainst))
            {
                // reset state
                _currInteractableCastedAgainst = null;
            }

            // Find target interactable if we haven't found one before.
            if (_currInteractableCastedAgainst == null)
            {
                _currentIntersectingObjects.Clear();
                _currInteractableCastedAgainst = FindTargetInteractable();

                // If we have found one, query collision zones.
                if (_currInteractableCastedAgainst != null)
                {
                    var targetHitPoint = _currInteractableCastedAgainst.transform.position;
                    var numHits        = Physics.OverlapSphereNonAlloc(targetHitPoint, COLLIDER_RADIUS, _collidersOverlapped);

                    // find all colliders encountered; focus only on ones belonging to target element
                    for (var i = 0; i < numHits; i++)
                    {
                        var colliderHit  = _collidersOverlapped[i];
                        var colliderZone = colliderHit.GetComponent<ColliderZone>();
                        if (colliderZone == null)
                        {
                            continue;
                        }

                        var interactableComponent = colliderZone.ParentInteractable;
                        if (interactableComponent == null || interactableComponent
                            != _currInteractableCastedAgainst)
                        {
                            continue;
                        }

                        var collisionInfo = new InteractableCollisionInfo(colliderZone,
                                colliderZone.CollisionDepth, this);
                        _currentIntersectingObjects.Add(collisionInfo);
                    }

                    // clear intersecting object if no collisions were found
                    if (_currentIntersectingObjects.Count == 0)
                    {
                        _currInteractableCastedAgainst = null;
                    }
                }
            }

            return _currentIntersectingObjects;
        }

        private bool HasRayReleasedInteractable(Interactable focusedInteractable)
        {
            var ourPosition            = transform.position;
            var forwardDirection       = transform.forward;
            var hysteresisDotThreshold = Mathf.Cos(_coneAngleReleaseDegrees * Mathf.Deg2Rad);
            var vectorToFocusedObject  = focusedInteractable.transform.position - ourPosition;
            vectorToFocusedObject.Normalize();
            var hysteresisDotProduct = Vector3.Dot(vectorToFocusedObject, forwardDirection);
            return hysteresisDotProduct < hysteresisDotThreshold;
        }

        /// <summary>
        ///     Find all objects from primary ray cast or if that fails, all objects in a
        ///     cone around main ray direction via a "secondary" cast.
        /// </summary>
        private Interactable FindTargetInteractable()
        {
            var          rayOrigin          = GetRayCastOrigin();
            var          rayDirection       = transform.forward;
            Interactable targetInteractable = null;

            // attempt primary ray cast
            targetInteractable = FindPrimaryRaycastHit(rayOrigin, rayDirection);

            // if primary cast fails, try secondary cone test
            if (targetInteractable == null)
            {
                targetInteractable = FindInteractableViaConeTest(rayOrigin, rayDirection);
            }

            return targetInteractable;
        }

        /// <summary>
        ///     Find first hit that is supports our tool's method of interaction.
        /// </summary>
        private Interactable FindPrimaryRaycastHit(Vector3 rayOrigin, Vector3 rayDirection)
        {
            Interactable interactableCastedAgainst = null;

            // hit order not guaranteed, so find closest
            var numHits     = Physics.RaycastNonAlloc(new Ray(rayOrigin, rayDirection), _primaryHits, Mathf.Infinity);
            var minDistance = 0.0f;
            for (var hitIndex = 0; hitIndex < numHits; hitIndex++)
            {
                var raycastHit = _primaryHits[hitIndex];

                // continue if something occludes it and that object is not an interactable
                var currentHitColliderZone = raycastHit.transform.GetComponent<ColliderZone>();
                if (currentHitColliderZone == null)
                {
                    continue;
                }

                // at this point we have encountered an interactable. Only consider it if
                // it allows interaction with our tool. Otherwise ignore it.
                var currentInteractable = currentHitColliderZone.ParentInteractable;
                if (currentInteractable == null || (currentInteractable.ValidToolTagsMask & (int) ToolTags) == 0)
                {
                    continue;
                }

                var vectorToInteractable   = currentInteractable.transform.position - rayOrigin;
                var distanceToInteractable = vectorToInteractable.magnitude;
                if (interactableCastedAgainst == null || distanceToInteractable < minDistance)
                {
                    interactableCastedAgainst = currentInteractable;
                    minDistance               = distanceToInteractable;
                }
            }

            return interactableCastedAgainst;
        }

        /// <summary>
        ///     If primary cast fails, try secondary test to see if we can target an interactable.
        ///     This target has to be far enough and support our tool via appropriate
        ///     tags, and must be within a certain angle from our primary ray direction.
        /// </summary>
        /// <param name="rayOrigin">Primary ray origin.</param>
        /// <param name="rayDirection">Primary ray direction.</param>
        /// <returns>Interactable found, if any.</returns>
        private Interactable FindInteractableViaConeTest(Vector3 rayOrigin, Vector3 rayDirection)
        {
            Interactable targetInteractable = null;

            var minDistance            = 0.0f;
            var minDotProductThreshold = Mathf.Cos(_coneAngleDegrees * Mathf.Deg2Rad);
            // cone extends from center line, where angle is split between top and bottom half
            var halfAngle  = Mathf.Deg2Rad * _coneAngleDegrees * 0.5f;
            var coneRadius = Mathf.Tan(halfAngle) * _farFieldMaxDistance;

            var numColliders = Physics.OverlapBoxNonAlloc(
                    rayOrigin + rayDirection * _farFieldMaxDistance * 0.5f,           // center
                    new Vector3(coneRadius, coneRadius, _farFieldMaxDistance * 0.5f), //half extents
                    _secondaryOverlapResults, transform.rotation);

            for (var i = 0; i < numColliders; i++)
            {
                var colliderHit = _secondaryOverlapResults[i];
                // skip invalid colliders
                var colliderZone = colliderHit.GetComponent<ColliderZone>();
                if (colliderZone == null)
                {
                    continue;
                }

                // skip invalid components
                var interactableComponent = colliderZone.ParentInteractable;
                if (interactableComponent == null ||
                    (interactableComponent.ValidToolTagsMask & (int) ToolTags) == 0)
                {
                    continue;
                }

                var vectorToInteractable   = interactableComponent.transform.position - rayOrigin;
                var distanceToInteractable = vectorToInteractable.magnitude;
                vectorToInteractable /= distanceToInteractable;
                var dotProduct = Vector3.Dot(vectorToInteractable, rayDirection);
                // must be inside cone!
                if (dotProduct < minDotProductThreshold)
                {
                    continue;
                }

                if (targetInteractable == null || distanceToInteractable < minDistance)
                {
                    targetInteractable = interactableComponent;
                    minDistance        = distanceToInteractable;
                }
            }

            return targetInteractable;
        }

        public override void FocusOnInteractable(Interactable focusedInteractable,
                                                 ColliderZone colliderZone)
        {
            _rayToolView.SetFocusedInteractable(focusedInteractable);
            _focusedInteractable = focusedInteractable;
        }

        public override void DeFocus()
        {
            _rayToolView.SetFocusedInteractable(null);
            _focusedInteractable = null;
        }
    }
}
