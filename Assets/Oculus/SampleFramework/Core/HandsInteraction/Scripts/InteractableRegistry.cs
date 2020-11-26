// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;

namespace OculusSampleFramework
{
    /// <summary>
    ///     In case someone wants to know about all interactables in a scene,
    ///     this registry is the easiest way to access that information.
    /// </summary>
    public class InteractableRegistry : MonoBehaviour
    {
        public static HashSet<Interactable> _interactables = new HashSet<Interactable>();

        public static HashSet<Interactable> Interactables => _interactables;

        public static void RegisterInteractable(Interactable interactable)
        {
            Interactables.Add(interactable);
        }

        public static void UnregisterInteractable(Interactable interactable)
        {
            Interactables.Remove(interactable);
        }
    }
}
