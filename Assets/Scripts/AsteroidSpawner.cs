// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Size of spawn area")]
    [SerializeField] private Vector3 size = Vector3.zero;

    [Header("Rate of instantiation")]
    [SerializeField] private float spawnRate = 1f;

    [Header("Data used for spawned asteroids")]
    [SerializeField] private GameObject asteroidModel = null;

    [SerializeField] private Transform asteroidParent = null;

    private float nextSpawn = 0f;

    private void Update()
    {
        // Timer to control spawning
        if (Time.time > nextSpawn)
        {
            nextSpawn = Time.time + spawnRate;

            SpawnAnAsteroid();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(transform.position, size);
    }

    private void SpawnAnAsteroid()
    {
        var spawnPoint = transform.position + new Vector3(Random.Range(-size.x / 2, size.x / 2), Random.Range(-size.y / 2, size.y / 2), Random.Range(-size.z / 2, size.z / 2));
        // var spawnRotation = Random.rotation;
        var asteroid = Instantiate(asteroidModel, spawnPoint, transform.rotation);
        asteroid.transform.SetParent(asteroidParent);
    }
}
