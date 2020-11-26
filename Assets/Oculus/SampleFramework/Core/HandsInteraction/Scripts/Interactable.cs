// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;
using UnityEngine.Events;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Interface for all objects interacted with in example code.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public    InteractableStateArgsEvent InteractableStateChanged;
        protected ColliderZone               _actionZoneCollider    = null;
        protected ColliderZone               _contactZoneCollider   = null;
        protected ColliderZone               _proximityZoneCollider = null;

        // Collider that indicates "am I close?"
        public ColliderZone ProximityCollider => _proximityZoneCollider;

        // Collider that indicates that contact has been made.
        public ColliderZone ContactCollider => _contactZoneCollider;

        // Indicates interactable has been activated. Like when
        // a button goes "click" and something interesting happens.
        public ColliderZone ActionCollider => _actionZoneCollider;

        // What kinds of tools works with this interactable?
        public virtual int ValidToolTagsMask => (int) InteractableToolTags.All;

        protected virtual void Awake()
        {
            InteractableRegistry.RegisterInteractable(this);
        }

        protected virtual void OnDestroy()
        {
            InteractableRegistry.UnregisterInteractable(this);
        }

        // The following events tell you if a tool is in a zone, which
        // might *not* mean the button is in the related zone state. This can happen
        // if a tool is in the contact zone but the interactable won't go into
        // the contact state if bad interactions (i.e. incorrect button presses)
        // are filtered out.

        public event Action<ColliderZoneArgs> ProximityZoneEvent;

        protected virtual void OnProximityZoneEvent(ColliderZoneArgs args)
        {
            if (ProximityZoneEvent != null)
            {
                ProximityZoneEvent.Invoke(args);
            }
        }

        public event Action<ColliderZoneArgs> ContactZoneEvent;

        protected virtual void OnContactZoneEvent(ColliderZoneArgs args)
        {
            if (ContactZoneEvent != null)
            {
                ContactZoneEvent.Invoke(args);
            }
        }

        public event Action<ColliderZoneArgs> ActionZoneEvent;

        protected virtual void OnActionZoneEvent(ColliderZoneArgs args)
        {
            if (ActionZoneEvent != null)
            {
                ActionZoneEvent.Invoke(args);
            }
        }

        public abstract void UpdateCollisionDepth(InteractableTool           interactableTool,
                                                  InteractableCollisionDepth oldCollisionDepth, InteractableCollisionDepth newCollisionDepth);

        [Serializable]
        public class InteractableStateArgsEvent : UnityEvent<InteractableStateArgs> { }
    }

    /// <summary>
    ///     Depth of collision, in order of "furthest" to "closest"
    /// </summary>
    public enum InteractableCollisionDepth
    {
        None = 0,
        Proximity,
        Contact,
        Action
    }

    public enum InteractableState
    {
        Default = 0,
        ProximityState, // in proximity -- close enough
        ContactState,   // contact has been made
        ActionState     // interactable activates
    }

    public class InteractableStateArgs : EventArgs
    {
        public readonly ColliderZoneArgs  ColliderArgs;
        public readonly Interactable      Interactable;
        public readonly InteractableState NewInteractableState;
        public readonly InteractableState OldInteractableState;
        public readonly InteractableTool  Tool;

        public InteractableStateArgs(Interactable      interactable,         InteractableTool  tool,
                                     InteractableState newInteractableState, InteractableState oldState,
                                     ColliderZoneArgs  colliderArgs)
        {
            Interactable         = interactable;
            Tool                 = tool;
            NewInteractableState = newInteractableState;
            OldInteractableState = oldState;
            ColliderArgs         = colliderArgs;
        }
    }
}
