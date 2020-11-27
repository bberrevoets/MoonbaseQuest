// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using TMPro;

using UnityEngine;

public class AsteroidCollision : MonoBehaviour
{
    [SerializeField] private GameObject asteroidExplosion = null;

    [Header("The pointCanvas prefab")]
    [SerializeField] private GameObject pointCanvas = null;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            // Destroy the Asteroid
            Destroy(collision.gameObject);

            // Instantiate explosion
            Instantiate(asteroidExplosion, collision.transform.position, collision.transform.rotation);

            var distanceFromPlayer = Vector3.Distance(transform.position, new Vector3(82f, 0.5f, 106f));
            var scoreToDisplay     = (int) distanceFromPlayer * 10;

            if (GameManager.GameStatus == GameManager.GameState.Playing)
            {
                // create the canvas
                var displayAsteroidScore = Instantiate(pointCanvas, collision.transform.position, Quaternion.identity);

                // assign the text for score
                displayAsteroidScore.transform.GetChild(0).transform.GetComponent<TextMeshProUGUI>().text = scoreToDisplay.ToString();

                displayAsteroidScore.transform.localScale = new Vector3(transform.localScale.x * (distanceFromPlayer / 10),
                        transform.localScale.y * (distanceFromPlayer / 10),
                        transform.localScale.z * (distanceFromPlayer / 10));
            }

            // Send notification to game manager that we hit an asteroid.
            GameManager.AsteroidHit(scoreToDisplay);

            // Destroy the laserBeam
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
