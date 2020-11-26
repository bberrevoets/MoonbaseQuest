// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class ONSPPropagationSettings : MonoBehaviour
{
    // quality as a percentage
    public float quality = 100.0f;

    private void Update()
    {
        ONSPPropagation.Interface.SetPropagationQuality(quality / 100.0f);
    }
}
