// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using Oculus.Avatar;

public class OvrAvatarBody : OvrAvatarComponent
{
    public ovrAvatarBodyComponent component = new ovrAvatarBodyComponent();

    private void Update()
    {
        if (owner == null)
        {
            return;
        }

        if (CAPI.ovrAvatarPose_GetBodyComponent(owner.sdkAvatar, ref component))
        {
            UpdateAvatar(component.renderComponent);
        }
        else
        {
            owner.Body = null;
            Destroy(this);
        }
    }

    public ovrAvatarComponent? GetNativeAvatarComponent()
    {
        if (owner == null)
        {
            return null;
        }

        if (CAPI.ovrAvatarPose_GetBodyComponent(owner.sdkAvatar, ref component))
        {
            CAPI.ovrAvatarComponent_Get(component.renderComponent, true, ref nativeAvatarComponent);
            return nativeAvatarComponent;
        }

        return null;
    }
}
