// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Describes how the tool will work with interactables. An interactable,
    ///     in turn, can tell us which tools they support via their flag bit mask.
    /// </summary>
    [Flags]
    public enum InteractableToolTags
    {
        None = 0,
        Ray  = 1 << 0,
        Poke = 1 << 2,
        All  = ~0
    }

    /// <summary>
    ///     Indicates if tool has been activated via some gesture, press, etc.
    /// </summary>
    public enum ToolInputState
    {
        Inactive = 0,
        PrimaryInputDown,
        PrimaryInputDownStay,
        PrimaryInputUp
    }

    /// <summary>
    ///     Describes tool-to-collision information.
    /// </summary>
    public class InteractableCollisionInfo
    {
        public InteractableTool           CollidingTool;
        public InteractableCollisionDepth CollisionDepth;

        public ColliderZone InteractableCollider;

        public InteractableCollisionInfo(ColliderZone     collider, InteractableCollisionDepth collisionDepth,
                                         InteractableTool collidingTool)
        {
            InteractableCollider = collider;
            CollisionDepth       = collisionDepth;
            CollidingTool        = collidingTool;
        }
    }

    /// <summary>
    ///     A tool that can engage interactables.
    /// </summary>
    public abstract class InteractableTool : MonoBehaviour
    {
        // lists created once so that they don't need to be created per frame
        private List<Interactable> _addedInteractables = new List<Interactable>();

        /// <summary>
        ///     List of objects that intersect tool.
        /// </summary>
        protected List<InteractableCollisionInfo> _currentIntersectingObjects =
                new List<InteractableCollisionInfo>();

        private Dictionary<Interactable, InteractableCollisionInfo> _currInteractableToCollisionInfos
                = new Dictionary<Interactable, InteractableCollisionInfo>();

        private Dictionary<Interactable, InteractableCollisionInfo> _prevInteractableToCollisionInfos
                = new Dictionary<Interactable, InteractableCollisionInfo>();

        private List<Interactable> _remainingInteractables = new List<Interactable>();
        private List<Interactable> _removedInteractables   = new List<Interactable>();
        public  Transform          ToolTransform     => transform;
        public  bool               IsRightHandedTool { get; set; }

        public abstract InteractableToolTags ToolTags { get; }

        public abstract ToolInputState ToolInputState { get; }

        public abstract bool IsFarFieldTool { get; }

        public Vector3 Velocity { get; protected set; }

        /// <summary>
        ///     Sometimes we want the position of a tool for stuff like pokes.
        /// </summary>
        public Vector3 InteractionPosition { get; protected set; }

        public abstract bool EnableState { get; set; }

        public List<InteractableCollisionInfo> GetCurrentIntersectingObjects() => _currentIntersectingObjects;

        public abstract List<InteractableCollisionInfo> GetNextIntersectingObjects();

        /// <summary>
        ///     Used to tell the tool to "focus" on a specific object, if
        ///     focusing is indeed possible given the tool type.
        /// </summary>
        /// <param name="focusedInteractable">Interactable to focus.</param>
        /// <param name="colliderZone">Collider zone of interactable.</param>
        public abstract void FocusOnInteractable(Interactable focusedInteractable,
                                                 ColliderZone colliderZone);

        public abstract void DeFocus();

        public abstract void Initialize();

        public KeyValuePair<Interactable, InteractableCollisionInfo> GetFirstCurrentCollisionInfo() => _currInteractableToCollisionInfos.First();

        public void ClearAllCurrentCollisionInfos()
        {
            _currInteractableToCollisionInfos.Clear();
        }

        /// <summary>
        ///     For each intersecting interactable, update meta data to indicate deepest collision only.
        /// </summary>
        public virtual void UpdateCurrentCollisionsBasedOnDepth()
        {
            _currInteractableToCollisionInfos.Clear();
            foreach (var interactableCollisionInfo in _currentIntersectingObjects)
            {
                var                       interactable         = interactableCollisionInfo.InteractableCollider.ParentInteractable;
                var                       depth                = interactableCollisionInfo.CollisionDepth;
                InteractableCollisionInfo collisionInfoFromMap = null;

                if (!_currInteractableToCollisionInfos.TryGetValue(interactable, out collisionInfoFromMap))
                {
                    _currInteractableToCollisionInfos[interactable] = interactableCollisionInfo;
                }
                else if (collisionInfoFromMap.CollisionDepth < depth)
                {
                    collisionInfoFromMap.InteractableCollider = interactableCollisionInfo.InteractableCollider;
                    collisionInfoFromMap.CollisionDepth       = depth;
                }
            }
        }

        /// <summary>
        ///     If our collision information changed per frame, make note of it.
        ///     Removed, added and remaining objects must get their proper events.
        /// </summary>
        public virtual void UpdateLatestCollisionData()
        {
            _addedInteractables.Clear();
            _removedInteractables.Clear();
            _remainingInteractables.Clear();

            foreach (var key in _currInteractableToCollisionInfos.Keys)
            {
                if (!_prevInteractableToCollisionInfos.ContainsKey(key))
                {
                    _addedInteractables.Add(key);
                }
                else
                {
                    _remainingInteractables.Add(key);
                }
            }

            foreach (var key in _prevInteractableToCollisionInfos.Keys)
            {
                if (!_currInteractableToCollisionInfos.ContainsKey(key))
                {
                    _removedInteractables.Add(key);
                }
            }

            // tell removed interactables that we are gone
            foreach (var removedInteractable in _removedInteractables)
            {
                removedInteractable.UpdateCollisionDepth(this,
                        _prevInteractableToCollisionInfos[removedInteractable].CollisionDepth,
                        InteractableCollisionDepth.None);
            }

            // tell added interactable what state we are now in
            foreach (var addedInteractableKey in _addedInteractables)
            {
                var addedInteractable = _currInteractableToCollisionInfos[addedInteractableKey];
                var collisionDepth    = addedInteractable.CollisionDepth;
                addedInteractableKey.UpdateCollisionDepth(this, InteractableCollisionDepth.None,
                        collisionDepth);
            }

            // remaining interactables must be updated
            foreach (var remainingInteractableKey in _remainingInteractables)
            {
                var newDepth = _currInteractableToCollisionInfos[remainingInteractableKey].CollisionDepth;
                var oldDepth = _prevInteractableToCollisionInfos[remainingInteractableKey].CollisionDepth;
                remainingInteractableKey.UpdateCollisionDepth(this, oldDepth, newDepth);
            }

            _prevInteractableToCollisionInfos = new Dictionary<Interactable, InteractableCollisionInfo>(
                    _currInteractableToCollisionInfos);
        }
    }
}
