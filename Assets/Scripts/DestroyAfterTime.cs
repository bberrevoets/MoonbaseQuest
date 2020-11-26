// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using UnityEngine;

internal class DestroyAfterTime : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 2.5f);
    }
}
