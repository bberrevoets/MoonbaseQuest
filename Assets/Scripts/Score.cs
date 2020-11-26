// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 22/11/2020
// ==========================================================================

using TMPro;

using UnityEngine;

public class Score : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.AsteroidDestroyed += DisplayScore;
    }

    private void OnDisable()
    {
        GameManager.AsteroidDestroyed -= DisplayScore;
    }

    private void DisplayScore()
    {
        GetComponent<TextMeshProUGUI>().text = $"Score: {GameManager.playerScore}";
    }
}
