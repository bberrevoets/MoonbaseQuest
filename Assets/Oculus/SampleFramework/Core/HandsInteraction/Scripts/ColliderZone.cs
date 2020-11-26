// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Zone that can be collided with in example code.
    /// </summary>
    public interface ColliderZone
    {
        Collider Collider { get; }

        // Which interactable do we belong to?
        Interactable               ParentInteractable { get; }
        InteractableCollisionDepth CollisionDepth     { get; }
    }

    /// <summary>
    ///     Arguments for object interacting with collider zone.
    /// </summary>
    public class ColliderZoneArgs : EventArgs
    {
        public readonly ColliderZone     Collider;
        public readonly InteractableTool CollidingTool;
        public readonly float            FrameTime;
        public readonly InteractionType  InteractionT;

        public ColliderZoneArgs(ColliderZone     collider,      float           frameTime,
                                InteractableTool collidingTool, InteractionType interactionType)
        {
            Collider      = collider;
            FrameTime     = frameTime;
            CollidingTool = collidingTool;
            InteractionT  = interactionType;
        }
    }

    public enum InteractionType
    {
        Enter = 0,
        Stay,
        Exit
    }
}
