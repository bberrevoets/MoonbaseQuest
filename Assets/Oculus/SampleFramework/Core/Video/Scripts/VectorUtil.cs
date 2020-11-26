// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public static class VectorUtil
{
    public static Vector4 ToVector(this Rect rect) => new Vector4(rect.x, rect.y, rect.width, rect.height);
}
