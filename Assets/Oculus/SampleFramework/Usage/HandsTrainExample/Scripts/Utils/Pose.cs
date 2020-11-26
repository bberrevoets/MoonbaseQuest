// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OculusSampleFramework
{
    public class Pose
    {
        public Vector3    Position;
        public Quaternion Rotation;

        public Pose()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }

        public Pose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
