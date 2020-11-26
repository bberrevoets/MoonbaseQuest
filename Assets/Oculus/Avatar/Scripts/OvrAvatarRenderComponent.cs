// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Avatar;

using UnityEngine;
using UnityEngine.Rendering;

public class OvrAvatarRenderComponent : MonoBehaviour
{
    public SkinnedMeshRenderer mesh;
    public Transform[]         bones;

    private bool firstSkinnedUpdate = true;
    private bool isBodyComponent    = false;

    protected void UpdateActive(OvrAvatar avatar, ovrAvatarVisibilityFlags mask)
    {
        var doActiveHack = isBodyComponent && avatar.EnableExpressive && avatar.ShowFirstPerson && !avatar.ShowThirdPerson;
        if (doActiveHack)
        {
            var showFirstPerson = (mask & ovrAvatarVisibilityFlags.FirstPerson) != 0;
            var showThirdPerson = (mask & ovrAvatarVisibilityFlags.ThirdPerson) != 0;
            gameObject.SetActive(showThirdPerson || showThirdPerson);

            if (!showFirstPerson)
            {
                mesh.enabled = false;
            }
        }
        else
        {
            var active = avatar.ShowFirstPerson && (mask & ovrAvatarVisibilityFlags.FirstPerson) != 0;
            active |= avatar.ShowThirdPerson && (mask & ovrAvatarVisibilityFlags.ThirdPerson) != 0;
            gameObject.SetActive(active);
            mesh.enabled = active;
        }
    }

    protected SkinnedMeshRenderer CreateSkinnedMesh(ulong assetID, ovrAvatarVisibilityFlags visibilityMask, int thirdPersonLayer, int firstPersonLayer)
    {
        isBodyComponent = name.Contains("body");

        var meshAsset = (OvrAvatarAssetMesh) OvrAvatarSDKManager.Instance.GetAsset(assetID);
        if (meshAsset == null)
        {
            throw new Exception("Couldn't find mesh for asset " + assetID);
        }

        if ((visibilityMask & ovrAvatarVisibilityFlags.ThirdPerson) != 0)
        {
            gameObject.layer = thirdPersonLayer;
        }
        else
        {
            gameObject.layer = firstPersonLayer;
        }

        var renderer = meshAsset.CreateSkinnedMeshRendererOnObject(gameObject);
        #if UNITY_ANDROID
        renderer.quality = SkinQuality.Bone2;
        #else
        renderer.quality = SkinQuality.Bone4;
        #endif
        renderer.updateWhenOffscreen = true;
        if ((visibilityMask & ovrAvatarVisibilityFlags.SelfOccluding) == 0)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        gameObject.SetActive(false);

        return renderer;
    }

    protected void UpdateSkinnedMesh(OvrAvatar avatar, Transform[] bones, ovrAvatarTransform localTransform, ovrAvatarVisibilityFlags visibilityMask, IntPtr renderPart)
    {
        UpdateActive(avatar, visibilityMask);
        OvrAvatar.ConvertTransform(localTransform, this.transform);
        var   type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
        ulong dirtyJoints;
        switch (type)
        {
            case ovrAvatarRenderPartType.SkinnedMeshRender:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRender_GetDirtyJoints(renderPart);
                break;
            case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetDirtyJoints(renderPart);
                break;
            case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetDirtyJoints(renderPart);
                break;
            default:
                throw new Exception("Unhandled render part type: " + type);
        }

        for (uint i = 0; i < 64; i++)
        {
            var dirtyMask = (ulong) 1 << (int) i;
            // We need to make sure that we fully update the initial position of
            // Skinned mesh renderers, then, thereafter, we can only update dirty joints
            if ((firstSkinnedUpdate && i < bones.Length) ||
                (dirtyMask & dirtyJoints) != 0)
            {
                //This joint is dirty and needs to be updated
                var                targetBone = bones[i];
                ovrAvatarTransform transform;
                switch (type)
                {
                    case ovrAvatarRenderPartType.SkinnedMeshRender:
                        transform = CAPI.ovrAvatarSkinnedMeshRender_GetJointTransform(renderPart, i);
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                        transform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetJointTransform(renderPart, i);
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                        transform = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetJointTransform(renderPart, i);
                        break;
                    default:
                        throw new Exception("Unhandled render part type: " + type);
                }

                OvrAvatar.ConvertTransform(transform, targetBone);
            }
        }

        firstSkinnedUpdate = false;
    }

    protected Material CreateAvatarMaterial(string name, Shader shader)
    {
        if (shader == null)
        {
            throw new Exception("No shader provided for avatar material.");
        }

        var mat = new Material(shader);
        mat.name = name;
        return mat;
    }
}
