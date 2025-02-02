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
    ///     A button interactable used by the train scene.
    /// </summary>
    public class ButtonController : Interactable
    {
        public enum ContactTest
        {
            PerpenTest = 0, // is tool pointing along button normal?
            BackwardsPress  // filter out presses coming backwards?
        }

        private const float ENTRY_DOT_THRESHOLD = 0.8f;
        private const float PERP_DOT_THRESHOLD  = 0.5f;

        [SerializeField] private GameObject _proximityZone = null;
        [SerializeField] private GameObject _contactZone   = null;
        [SerializeField] private GameObject _actionZone    = null;

        [SerializeField] private ContactTest[] _contactTests = null;

        // for positive side tests, the contact position must be on the positive side of the plane
        // determined by this transform
        [SerializeField] private Transform _buttonPlaneCenter = null;

        // make sure press is coming from "positive" side of button, i.e. above it
        [SerializeField] private bool _makeSureToolIsOnPositiveSide = true;

        // depending on the geometry used, the direction might not always be downwards.
        [SerializeField] private Vector3 _localButtonDirection = Vector3.down;

        [SerializeField]
        private InteractableToolTags[] _allValidToolsTags =
                {InteractableToolTags.All};

        private InteractableState _currentButtonState = InteractableState.Default;
        private int               _toolTagsMask;

        private Dictionary<InteractableTool, InteractableState> _toolToState =
                new Dictionary<InteractableTool, InteractableState>();

        public override int ValidToolTagsMask => _toolTagsMask;

        public Vector3 LocalButtonDirection => _localButtonDirection;

        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(_proximityZone);
            Assert.IsNotNull(_contactZone);
            Assert.IsNotNull(_actionZone);
            Assert.IsNotNull(_buttonPlaneCenter);

            foreach (var interactableToolTags in _allValidToolsTags)
            {
                _toolTagsMask |= (int) interactableToolTags;
            }

            _proximityZoneCollider = _proximityZone.GetComponent<ColliderZone>();
            _contactZoneCollider   = _contactZone.GetComponent<ColliderZone>();
            _actionZoneCollider    = _actionZone.GetComponent<ColliderZone>();
        }

        private void FireInteractionEventsOnDepth(InteractableCollisionDepth oldDepth,
                                                  InteractableTool           collidingTool, InteractionType interactionType)
        {
            switch (oldDepth)
            {
                case InteractableCollisionDepth.Action:
                    OnActionZoneEvent(new ColliderZoneArgs(ActionCollider, Time.frameCount,
                            collidingTool, interactionType));
                    break;
                case InteractableCollisionDepth.Contact:
                    OnContactZoneEvent(new ColliderZoneArgs(ContactCollider, Time.frameCount,
                            collidingTool, interactionType));
                    break;
                case InteractableCollisionDepth.Proximity:
                    OnProximityZoneEvent(new ColliderZoneArgs(ProximityCollider, Time.frameCount,
                            collidingTool, interactionType));
                    break;
            }
        }

        public override void UpdateCollisionDepth(InteractableTool           interactableTool,
                                                  InteractableCollisionDepth oldCollisionDepth,
                                                  InteractableCollisionDepth newCollisionDepth)
        {
            var isFarFieldTool = interactableTool.IsFarFieldTool;

            // if this is a near field tool and another tool already controls it, bail.
            if (!isFarFieldTool && _toolToState.Keys.Count > 0 && !_toolToState.ContainsKey(interactableTool))
            {
                return;
            }

            var oldState = _currentButtonState;

            // ignore contact test if you are using the far field tool
            var currButtonDirection = transform.TransformDirection(_localButtonDirection);
            var validContact = IsValidContact(interactableTool, currButtonDirection)
                               || interactableTool.IsFarFieldTool;
            // in case tool enters contact zone first, we are in proximity as well
            var toolIsInProximity = newCollisionDepth >= InteractableCollisionDepth.Proximity;
            var toolInContactZone = newCollisionDepth == InteractableCollisionDepth.Contact;
            var toolInActionZone  = newCollisionDepth == InteractableCollisionDepth.Action;

            var switchingStates = oldCollisionDepth != newCollisionDepth;
            if (switchingStates)
            {
                FireInteractionEventsOnDepth(oldCollisionDepth, interactableTool,
                        InteractionType.Exit);
                FireInteractionEventsOnDepth(newCollisionDepth, interactableTool,
                        InteractionType.Enter);
            }
            else
            {
                FireInteractionEventsOnDepth(newCollisionDepth, interactableTool,
                        InteractionType.Stay);
            }

            var upcomingState = oldState;
            if (interactableTool.IsFarFieldTool)
            {
                upcomingState = toolInContactZone ? InteractableState.ContactState :
                                toolInActionZone  ? InteractableState.ActionState : InteractableState.Default;
            }
            else
            {
                // plane describing positive side of button
                var buttonZonePlane = new Plane(-currButtonDirection, _buttonPlaneCenter.position);
                // skip plane test if the boolean flag tells us not to test it
                var onPositiveSideOfButton = !_makeSureToolIsOnPositiveSide ||
                                             buttonZonePlane.GetSide(interactableTool.InteractionPosition);
                upcomingState = GetUpcomingStateNearField(oldState, newCollisionDepth,
                        toolInActionZone,                           toolInContactZone, toolIsInProximity,
                        validContact,                               onPositiveSideOfButton);
            }

            if (upcomingState != InteractableState.Default)
            {
                _toolToState[interactableTool] = upcomingState;
            }
            else
            {
                _toolToState.Remove(interactableTool);
            }

            // if using far field tool, the upcoming state is based
            // on the far field tool that has the greatest max state so far
            // (since there can be multiple far field tools interacting
            // with button)
            if (isFarFieldTool)
            {
                foreach (var toolState in _toolToState.Values)
                {
                    if (upcomingState < toolState)
                    {
                        upcomingState = toolState;
                    }
                }
            }

            if (oldState != upcomingState)
            {
                _currentButtonState = upcomingState;

                var interactionType = !switchingStates                                     ? InteractionType.Stay :
                                      newCollisionDepth == InteractableCollisionDepth.None ? InteractionType.Exit :
                                                                                             InteractionType.Enter;
                var CurrentCollider =
                        _currentButtonState == InteractableState.ProximityState ? ProximityCollider :
                        _currentButtonState == InteractableState.ContactState   ? ContactCollider :
                        _currentButtonState == InteractableState.ActionState    ? ActionCollider : null;
                if (InteractableStateChanged != null)
                {
                    InteractableStateChanged.Invoke(new InteractableStateArgs(this, interactableTool,
                            _currentButtonState, oldState, new ColliderZoneArgs(CurrentCollider, Time.frameCount,
                                    interactableTool, interactionType)));
                }
            }
        }

        private InteractableState GetUpcomingStateNearField(InteractableState          oldState,
                                                            InteractableCollisionDepth newCollisionDepth,   bool toolIsInActionZone,
                                                            bool                       toolIsInContactZone, bool toolIsInProximity,
                                                            bool                       validContact,        bool onPositiveSideOfInteractable)
        {
            var upcomingState = oldState;

            switch (oldState)
            {
                case InteractableState.ActionState:
                    if (!toolIsInActionZone)
                    {
                        // if retreating from action, can go back into action state even if contact
                        // is not legal (i.e. tool/finger retracts)
                        if (toolIsInContactZone)
                        {
                            upcomingState = InteractableState.ContactState;
                        }
                        else if (toolIsInProximity)
                        {
                            upcomingState = InteractableState.ProximityState;
                        }
                        else
                        {
                            upcomingState = InteractableState.Default;
                        }
                    }

                    break;
                case InteractableState.ContactState:
                    if (newCollisionDepth < InteractableCollisionDepth.Contact)
                    {
                        upcomingState = toolIsInProximity ? InteractableState.ProximityState : InteractableState.Default;
                    }
                    // can only go to action state if contact is legal
                    // if tool goes into contact state due to proper movement, but does not maintain
                    // that movement throughout (i.e. a tool/finger presses downwards initially but
                    // moves in random directions afterwards), then don't go into action
                    else if (toolIsInActionZone && validContact && onPositiveSideOfInteractable)
                    {
                        upcomingState = InteractableState.ActionState;
                    }

                    break;
                case InteractableState.ProximityState:
                    if (newCollisionDepth < InteractableCollisionDepth.Proximity)
                    {
                        upcomingState = InteractableState.Default;
                    }
                    else if (validContact && onPositiveSideOfInteractable &&
                             newCollisionDepth > InteractableCollisionDepth.Proximity)
                    {
                        upcomingState = newCollisionDepth == InteractableCollisionDepth.Action
                                                ? InteractableState.ActionState
                                                : InteractableState.ContactState;
                    }

                    break;
                case InteractableState.Default:
                    // test contact, action first then proximity (more important states
                    // take precedence)
                    if (validContact && onPositiveSideOfInteractable &&
                        newCollisionDepth > InteractableCollisionDepth.Proximity)
                    {
                        upcomingState = newCollisionDepth == InteractableCollisionDepth.Action
                                                ? InteractableState.ActionState
                                                : InteractableState.ContactState;
                    }
                    else if (toolIsInProximity)
                    {
                        upcomingState = InteractableState.ProximityState;
                    }

                    break;
            }

            return upcomingState;
        }

        private bool IsValidContact(InteractableTool collidingTool, Vector3 buttonDirection)
        {
            if (_contactTests == null || collidingTool.IsFarFieldTool)
            {
                return true;
            }

            foreach (var contactTest in _contactTests)
            {
                switch (contactTest)
                {
                    case ContactTest.BackwardsPress:
                        if (!PassEntryTest(collidingTool, buttonDirection))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (!PassPerpTest(collidingTool, buttonDirection))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        /// <summary>
        ///     Is tool entering button correctly? Check velocity and make sure that
        ///     tool is not below action zone.
        /// </summary>
        private bool PassEntryTest(InteractableTool collidingTool, Vector3 buttonDirection)
        {
            var jointVelocityVector = collidingTool.Velocity.normalized;
            var dotProduct          = Vector3.Dot(jointVelocityVector, buttonDirection);
            if (dotProduct < ENTRY_DOT_THRESHOLD)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Is our tool pointing in opposite direction compared to button?
        /// </summary>
        private bool PassPerpTest(InteractableTool collidingTool, Vector3 buttonDirection)
        {
            // the "right" vector points along tool by default
            // if it's right hand, then flip that direction
            var toolDirection = collidingTool.ToolTransform.right;
            if (collidingTool.IsRightHandedTool)
            {
                toolDirection = -toolDirection;
            }

            var dotProduct = Vector3.Dot(toolDirection, buttonDirection);
            if (dotProduct < PERP_DOT_THRESHOLD)
            {
                return false;
            }

            return true;
        }
    }
}
