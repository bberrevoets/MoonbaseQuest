// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using UnityEngine;

public class AsteroidsMovement : MonoBehaviour
{
    [Header("Asteroid behaviour")]
    [SerializeField] private float minSpeed = 1f;

    [SerializeField] private float maxSpeed = 15f;

    [SerializeField] private Vector3 movementDirection = Vector3.zero;

    [SerializeField] private float rotationSpeedMin = 1.0f;
    [SerializeField] private float rotationSpeedMax = 10.0f;

    private float asteroidSpeed = 10f;
    private float rotationSpeed;
    private float xAngle, yAngle, zAngle;

    private void Start()
    {
        // get a random speed for asteroid
        asteroidSpeed = Random.Range(minSpeed, maxSpeed);

        // Get a random rotation
        xAngle = Random.Range(0, 360);
        yAngle = Random.Range(0, 360);
        zAngle = Random.Range(0, 360);

        transform.GetChild(0).transform.Rotate(xAngle, yAngle, zAngle, Space.Self);

        rotationSpeed = Random.Range(rotationSpeedMin, rotationSpeedMax);
    }

    // Update is called once per frame
    private void Update()
    {
        transform.Translate(movementDirection * (Time.deltaTime * asteroidSpeed));
        transform.GetChild(0).transform.Rotate(Vector3.up * (Time.deltaTime * rotationSpeed));
    }
}
