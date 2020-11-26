// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class FireLaserGun : MonoBehaviour
{
    private static readonly int FIRE = Animator.StringToHash("Fire");

    [SerializeField] private Animator   gunAnimator     = null;
    [SerializeField] private GameObject laserBeamModel  = null;
    [SerializeField] private Transform  laserSpawnPoint = null;
    [SerializeField] private Transform  laserParent     = null;

    public void FireGun()
    {
        // Access the Animator on the gun model, trigger the fire animation.
        gunAnimator.SetTrigger(FIRE);

        // When the trigger is pressed, play the audio.
        GetComponent<AudioSource>().Play();

        // instantiate the laser beam
        var generatedLaserBeam = Instantiate(laserBeamModel, laserSpawnPoint.position, laserSpawnPoint.rotation);

        // put new laser in to the correct object. - tidy up the hierarchy!
        generatedLaserBeam.transform.SetParent(laserParent);
    }
}
