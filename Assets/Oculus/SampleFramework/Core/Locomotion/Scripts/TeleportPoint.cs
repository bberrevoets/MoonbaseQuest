// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class TeleportPoint : MonoBehaviour
{
    public float dimmingSpeed  = 1;
    public float fullIntensity = 1;
    public float lowIntensity  = 0.5f;

    public Transform destTransform;

    private float lastLookAtTime = 0;

    // Use this for initialization
    private void Start() { }

    // Update is called once per frame
    private void Update()
    {
        var intensity = Mathf.SmoothStep(fullIntensity, lowIntensity, (Time.time - lastLookAtTime) * dimmingSpeed);
        GetComponent<MeshRenderer>().material.SetFloat("_Intensity", intensity);
    }

    public Transform GetDestTransform() => destTransform;

    public void OnLookAt()
    {
        lastLookAtTime = Time.time;
    }
}
