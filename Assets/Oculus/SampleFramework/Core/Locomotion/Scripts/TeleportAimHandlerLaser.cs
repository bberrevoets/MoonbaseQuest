// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;

public class TeleportAimHandlerLaser : TeleportAimHandler
{
    /// <summary>
    ///     Maximum range for aiming.
    /// </summary>
    [Tooltip("Maximum range for aiming.")]
    public float Range = 100;

    /// <summary>
    ///     Return the set of points that represent the aiming line.
    /// </summary>
    /// <param name="points"></param>
    public override void GetPoints(List<Vector3> points)
    {
        Ray aimRay;
        LocomotionTeleport.InputHandler.GetAimData(out aimRay);
        points.Add(aimRay.origin);
        points.Add(aimRay.origin + aimRay.direction * Range);
    }
}
