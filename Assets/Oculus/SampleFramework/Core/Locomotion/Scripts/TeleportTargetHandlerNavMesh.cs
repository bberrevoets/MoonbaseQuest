// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

// Enable this define to visualize the navigation solution that was used to validate access to the target location.
//#define SHOW_PATH_RESULT

using System.Diagnostics;

using UnityEngine;
using UnityEngine.AI;

public class TeleportTargetHandlerNavMesh : TeleportTargetHandler
{
    /// <summary>
    ///     Controls which areas are to be used when doing nav mesh queries.
    /// </summary>
    public int NavMeshAreaMask = NavMesh.AllAreas;

    /// <summary>
    ///     A NavMeshPath that is necessary for doing pathing queries and is reused with each request.
    /// </summary>
    private NavMeshPath _path;

    private void Awake()
    {
        _path = new NavMeshPath();
    }

    [Conditional("SHOW_PATH_RESULT")]
    private void OnDrawGizmos()
    {
        #if SHOW_PATH_RESULT
		if (_path == null)
			return;

		var corners = _path.corners;
		if (corners == null || corners.Length == 0)
			return;
		var p = corners[0];
		for(int i = 1; i < corners.Length; i++)
		{
			var p2 = corners[i];
			Gizmos.DrawLine(p, p2);
			p = p2;
		}
        #endif
    }

    /// <summary>
    ///     This method will be called while the LocmotionTeleport component is in the aiming state, once for each
    ///     line segment that the targeting beam requires.
    ///     The function should return true whenever an actual target location has been selected.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    protected override bool ConsiderTeleport(Vector3 start, ref Vector3 end)
    {
        // If the ray hits the world, consider it valid and update the aimRay to the end point.
        if (LocomotionTeleport.AimCollisionTest(start, end, AimCollisionLayerMask, out AimData.TargetHitInfo))
        {
            var d = (end - start).normalized;

            end = start + d * AimData.TargetHitInfo.distance;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     This version of ConsiderDestination will only return a valid location if the pathing system is able to find a route
    ///     from the current position to the candidate location.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Vector3? ConsiderDestination(Vector3 location)
    {
        var result = base.ConsiderDestination(location);
        if (result.HasValue)
        {
            var start = LocomotionTeleport.GetCharacterPosition();
            var dest  = result.GetValueOrDefault();
            NavMesh.CalculatePath(start, dest, NavMeshAreaMask, _path);

            if (_path.status == NavMeshPathStatus.PathComplete)
            {
                return result;
            }
        }

        return null;
    }
}
