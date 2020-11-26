// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using Oculus.Avatar;

public class OvrAvatarTouchController : OvrAvatarComponent
{
    public  bool                         isLeftHand = true;
    private ovrAvatarControllerComponent component  = new ovrAvatarControllerComponent();

    private void Update()
    {
        if (owner == null)
        {
            return;
        }

        var hasComponent = false;
        if (isLeftHand)
        {
            hasComponent = CAPI.ovrAvatarPose_GetLeftControllerComponent(owner.sdkAvatar, ref component);
        }
        else
        {
            hasComponent = CAPI.ovrAvatarPose_GetRightControllerComponent(owner.sdkAvatar, ref component);
        }

        if (hasComponent)
        {
            UpdateAvatar(component.renderComponent);
        }
        else
        {
            if (isLeftHand)
            {
                owner.ControllerLeft = null;
            }
            else
            {
                owner.ControllerRight = null;
            }

            Destroy(this);
        }
    }
}
