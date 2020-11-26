// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class LaserBeamMove : MonoBehaviour
{
    [SerializeField] private float thrust = 10.0f;

    private Rigidbody rb;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 2f);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        rb.velocity = transform.forward * thrust;
    }
}
