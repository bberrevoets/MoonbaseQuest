// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Poke tool used for near-field (touching) interactions. Assumes that it will be placed on
    ///     finger tips.
    /// </summary>
    public class FingerTipPokeTool : InteractableTool
    {
        private const int NUM_VELOCITY_FRAMES = 10;

        [SerializeField] private FingerTipPokeToolView _fingerTipPokeToolView = null;
        [SerializeField] private OVRPlugin.HandFinger  _fingerToFollow        = OVRPlugin.HandFinger.Index;

        private BoneCapsuleTriggerLogic[] _boneCapsuleTriggerLogic;
        private OVRBoneCapsule            _capsuleToTrack;
        private int                       _currVelocityFrame = 0;
        private bool                      _isInitialized     = false;

        private float   _lastScale = 1.0f;
        private Vector3 _position;
        private bool    _sampledMaxFramesAlready;

        private Vector3[] _velocityFrames;

        public override InteractableToolTags ToolTags => InteractableToolTags.Poke;

        public override ToolInputState ToolInputState => ToolInputState.Inactive;

        public override bool IsFarFieldTool => false;

        public override bool EnableState
        {
            get => _fingerTipPokeToolView.gameObject.activeSelf;
            set => _fingerTipPokeToolView.gameObject.SetActive(value);
        }

        private void Update()
        {
            if (!HandsManager.Instance || !HandsManager.Instance.IsInitialized() || !_isInitialized || _capsuleToTrack == null)
            {
                return;
            }

            var hand         = IsRightHandedTool ? HandsManager.Instance.RightHand : HandsManager.Instance.LeftHand;
            var currentScale = hand.HandScale;
            // push tool into the tip based on how wide it is. so negate the direction
            var capsuleTransform = _capsuleToTrack.CapsuleCollider.transform;
            // NOTE: use time settings 0.0111111/0.02 to make collisions work correctly!
            var capsuleDirection = capsuleTransform.right;
            var capsuleTipPosition = capsuleTransform.position + _capsuleToTrack.CapsuleCollider.height * 0.5f
                                                                                                        * capsuleDirection;
            var toolSphereRadiusOffsetFromTip = currentScale * _fingerTipPokeToolView.SphereRadius *
                                                capsuleDirection;
            // push tool back so that it's centered on transform/bone
            var toolPosition = capsuleTipPosition + toolSphereRadiusOffsetFromTip;
            transform.position  = toolPosition;
            transform.rotation  = capsuleTransform.rotation;
            InteractionPosition = capsuleTipPosition;

            UpdateAverageVelocity();

            CheckAndUpdateScale();
        }

        public override void Initialize()
        {
            Assert.IsNotNull(_fingerTipPokeToolView);

            InteractableToolsInputRouter.Instance.RegisterInteractableTool(this);
            _fingerTipPokeToolView.InteractableTool = this;

            _velocityFrames = new Vector3[NUM_VELOCITY_FRAMES];
            Array.Clear(_velocityFrames, 0, NUM_VELOCITY_FRAMES);

            StartCoroutine(AttachTriggerLogic());
        }

        private IEnumerator AttachTriggerLogic()
        {
            while (!HandsManager.Instance || !HandsManager.Instance.IsInitialized())
            {
                yield return null;
            }

            var handSkeleton = IsRightHandedTool ? HandsManager.Instance.RightHandSkeleton : HandsManager.Instance.LeftHandSkeleton;

            var boneToTestCollisions = OVRSkeleton.BoneId.Hand_Pinky3;
            switch (_fingerToFollow)
            {
                case OVRPlugin.HandFinger.Thumb:
                    boneToTestCollisions = OVRSkeleton.BoneId.Hand_Thumb3;
                    break;
                case OVRPlugin.HandFinger.Index:
                    boneToTestCollisions = OVRSkeleton.BoneId.Hand_Index3;
                    break;
                case OVRPlugin.HandFinger.Middle:
                    boneToTestCollisions = OVRSkeleton.BoneId.Hand_Middle3;
                    break;
                case OVRPlugin.HandFinger.Ring:
                    boneToTestCollisions = OVRSkeleton.BoneId.Hand_Ring3;
                    break;
                default:
                    boneToTestCollisions = OVRSkeleton.BoneId.Hand_Pinky3;
                    break;
            }

            var boneCapsuleTriggerLogic = new List<BoneCapsuleTriggerLogic>();
            var boneCapsules            = HandsManager.GetCapsulesPerBone(handSkeleton, boneToTestCollisions);
            foreach (var ovrCapsuleInfo in boneCapsules)
            {
                var boneCapsuleTrigger = ovrCapsuleInfo.CapsuleRigidbody.gameObject.AddComponent<BoneCapsuleTriggerLogic>();
                ovrCapsuleInfo.CapsuleCollider.isTrigger = true;
                boneCapsuleTrigger.ToolTags              = ToolTags;
                boneCapsuleTriggerLogic.Add(boneCapsuleTrigger);
            }

            _boneCapsuleTriggerLogic = boneCapsuleTriggerLogic.ToArray();
            // finger tip should have only one capsule
            if (boneCapsules.Count > 0)
            {
                _capsuleToTrack = boneCapsules[0];
            }

            _isInitialized = true;
        }

        private void UpdateAverageVelocity()
        {
            var prevPosition    = _position;
            var currPosition    = transform.position;
            var currentVelocity = (currPosition - prevPosition) / Time.deltaTime;
            _position                           = currPosition;
            _velocityFrames[_currVelocityFrame] = currentVelocity;
            // if sampled more than allowed, loop back toward the beginning
            _currVelocityFrame = (_currVelocityFrame + 1) % NUM_VELOCITY_FRAMES;

            Velocity = Vector3.zero;
            // edge case; when we first start up, we will have only sampled less than the
            // max frames. so only compute the average over that subset. After that, the
            // frame samples will act like an array that loops back toward to the beginning
            if (!_sampledMaxFramesAlready && _currVelocityFrame == NUM_VELOCITY_FRAMES - 1)
            {
                _sampledMaxFramesAlready = true;
            }

            var numFramesToSamples = _sampledMaxFramesAlready ? NUM_VELOCITY_FRAMES : _currVelocityFrame + 1;
            for (var frameIndex = 0; frameIndex < numFramesToSamples; frameIndex++)
            {
                Velocity += _velocityFrames[frameIndex];
            }

            Velocity /= numFramesToSamples;
        }

        private void CheckAndUpdateScale()
        {
            var currentScale = IsRightHandedTool
                                       ? HandsManager.Instance.RightHand.HandScale
                                       : HandsManager.Instance.LeftHand.HandScale;
            if (Mathf.Abs(currentScale - _lastScale) > Mathf.Epsilon)
            {
                transform.localScale = new Vector3(currentScale, currentScale, currentScale);
                _lastScale           = currentScale;
            }
        }

        public override List<InteractableCollisionInfo> GetNextIntersectingObjects()
        {
            _currentIntersectingObjects.Clear();

            foreach (var boneCapsuleTriggerLogic in _boneCapsuleTriggerLogic)
            {
                var collidersTouching = boneCapsuleTriggerLogic.CollidersTouchingUs;
                foreach (var colliderTouching in collidersTouching)
                {
                    _currentIntersectingObjects.Add(new InteractableCollisionInfo(colliderTouching,
                            colliderTouching.CollisionDepth, this));
                }
            }

            return _currentIntersectingObjects;
        }

        public override void FocusOnInteractable(Interactable focusedInteractable,
                                                 ColliderZone colliderZone)
        {
            // no need for focus
        }

        public override void DeFocus()
        {
            // no need for focus
        }
    }
}
