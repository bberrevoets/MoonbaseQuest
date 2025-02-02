// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Allows a bone to keep track of interactables that it has touched. This information
    ///     can be used by a tool.
    /// </summary>
    public class BoneCapsuleTriggerLogic : MonoBehaviour
    {
        public InteractableToolTags ToolTags;

        public  HashSet<ColliderZone> CollidersTouchingUs = new HashSet<ColliderZone>();
        private List<ColliderZone>    _elementsToCleanUp  = new List<ColliderZone>();

        private void Update()
        {
            CleanUpDeadColliders();
        }

        /// <summary>
        ///     If we get disabled, clear our colliders. Otherwise, on trigger exit may not get called.
        /// </summary>
        private void OnDisable()
        {
            CollidersTouchingUs.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var triggerZone = other.GetComponent<ButtonTriggerZone>();
            if (triggerZone != null && (triggerZone.ParentInteractable.ValidToolTagsMask & (int) ToolTags) != 0)
            {
                CollidersTouchingUs.Add(triggerZone);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var triggerZone = other.GetComponent<ButtonTriggerZone>();
            if (triggerZone != null && (triggerZone.ParentInteractable.ValidToolTagsMask & (int) ToolTags) != 0)
            {
                CollidersTouchingUs.Remove(triggerZone);
            }
        }

        /// <summary>
        ///     Sometimes colliders get disabled and trigger exit doesn't get called.
        ///     Take care of that edge case.
        /// </summary>
        private void CleanUpDeadColliders()
        {
            _elementsToCleanUp.Clear();
            foreach (var colliderTouching in CollidersTouchingUs)
            {
                if (!colliderTouching.Collider.gameObject.activeInHierarchy)
                {
                    _elementsToCleanUp.Add(colliderTouching);
                }
            }

            foreach (var colliderZone in _elementsToCleanUp)
            {
                CollidersTouchingUs.Remove(colliderZone);
            }
        }
    }
}
