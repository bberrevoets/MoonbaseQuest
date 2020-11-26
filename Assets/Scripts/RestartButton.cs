﻿// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 22/11/2020
// ==========================================================================

using UnityEngine;

public class RestartButton : MonoBehaviour
{
    public GameManager gameManager;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Laser"))
        {
            gameManager.ResetGame();
        }
    }
}
