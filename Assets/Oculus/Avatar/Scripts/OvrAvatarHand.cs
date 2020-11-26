// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using Oculus.Avatar;

public class OvrAvatarHand : OvrAvatarComponent
{
    public  bool                   isLeftHand = true;
    private ovrAvatarHandComponent component  = new ovrAvatarHandComponent();

    private void Update()
    {
        if (owner == null)
        {
            return;
        }

        var hasComponent = false;
        if (isLeftHand)
        {
            hasComponent = CAPI.ovrAvatarPose_GetLeftHandComponent(owner.sdkAvatar, ref component);
        }
        else
        {
            hasComponent = CAPI.ovrAvatarPose_GetRightHandComponent(owner.sdkAvatar, ref component);
        }

        if (hasComponent)
        {
            UpdateAvatar(component.renderComponent);
        }
        else
        {
            if (isLeftHand)
            {
                owner.HandLeft = null;
            }
            else
            {
                owner.HandRight = null;
            }

            Destroy(this);
        }
    }
}
