// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

/// <summary>
///     This transition will move the player with no other side effects.
/// </summary>
public class TeleportTransitionInstant : TeleportTransition
{
    /// <summary>
    ///     When the teleport state is entered, simply move the player to the new location
    ///     without any delay or other side effects.
    /// </summary>
    protected override void LocomotionTeleportOnEnterStateTeleporting()
    {
        LocomotionTeleport.DoTeleport();
    }
}
