// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using UnityEngine;

public class AsteroidKillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Asteroid"))
        {
            Destroy(other.gameObject);
        }
    }
}
