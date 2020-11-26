// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 22/11/2020
// ==========================================================================

using UnityEngine;

public static class TransformExtension
{
    public static void Clear(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            Object.Destroy(child.gameObject);
        }
    }
}
