// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Runtime.InteropServices;

using Oculus.Avatar;

using UnityEngine;

public class OvrAvatarAssetMesh : OvrAvatarAsset
{
    public  string[]                 jointNames;
    public  Mesh                     mesh;
    private ovrAvatarSkinnedMeshPose skinnedBindPose;

    public OvrAvatarAssetMesh(ulong _assetId, IntPtr asset, ovrAvatarAssetType meshType)
    {
        assetID   = _assetId;
        mesh      = new Mesh();
        mesh.name = "Procedural Geometry for asset " + _assetId;

        SetSkinnedBindPose(asset, meshType);

        long vertexCount  = 0;
        var  vertexBuffer = IntPtr.Zero;
        uint indexCount   = 0;
        var  indexBuffer  = IntPtr.Zero;

        GetVertexAndIndexData(asset, meshType, out vertexCount, out vertexBuffer, out indexCount, out indexBuffer);

        AvatarLogger.Log("OvrAvatarAssetMesh: " + _assetId + " " + meshType + " VertexCount:" + vertexCount);

        var vertices    = new Vector3[vertexCount];
        var normals     = new Vector3[vertexCount];
        var tangents    = new Vector4[vertexCount];
        var uv          = new Vector2[vertexCount];
        var colors      = new Color[vertexCount];
        var boneWeights = new BoneWeight[vertexCount];

        var vertexBufferStart = vertexBuffer.ToInt64();

        // We have different underlying vertex types to unpack, so switch on mesh type. 
        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
            {
                long vertexSize = Marshal.SizeOf(typeof(ovrAvatarMeshVertex));

                for (long i = 0; i < vertexCount; i++)
                {
                    var offset = vertexSize * i;

                    var vertex = (ovrAvatarMeshVertex) Marshal.PtrToStructure(new IntPtr(vertexBufferStart + offset), typeof(ovrAvatarMeshVertex));
                    vertices[i] = new Vector3(vertex.x,  vertex.y,  -vertex.z);
                    normals[i]  = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                    tangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz, vertex.tw);
                    uv[i]       = new Vector2(vertex.u, vertex.v);
                    colors[i]   = new Color(0, 0, 0, 1);

                    boneWeights[i].boneIndex0 = vertex.blendIndices[0];
                    boneWeights[i].boneIndex1 = vertex.blendIndices[1];
                    boneWeights[i].boneIndex2 = vertex.blendIndices[2];
                    boneWeights[i].boneIndex3 = vertex.blendIndices[3];
                    boneWeights[i].weight0    = vertex.blendWeights[0];
                    boneWeights[i].weight1    = vertex.blendWeights[1];
                    boneWeights[i].weight2    = vertex.blendWeights[2];
                    boneWeights[i].weight3    = vertex.blendWeights[3];
                }
            }
                break;

            case ovrAvatarAssetType.CombinedMesh:
            {
                long vertexSize = Marshal.SizeOf(typeof(ovrAvatarMeshVertexV2));

                for (long i = 0; i < vertexCount; i++)
                {
                    var offset = vertexSize * i;

                    var vertex = (ovrAvatarMeshVertexV2) Marshal.PtrToStructure(new IntPtr(vertexBufferStart + offset), typeof(ovrAvatarMeshVertexV2));
                    vertices[i] = new Vector3(vertex.x,  vertex.y,  -vertex.z);
                    normals[i]  = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                    tangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz, vertex.tw);
                    uv[i]       = new Vector2(vertex.u, vertex.v);
                    colors[i]   = new Color(vertex.r, vertex.g, vertex.b, vertex.a);

                    boneWeights[i].boneIndex0 = vertex.blendIndices[0];
                    boneWeights[i].boneIndex1 = vertex.blendIndices[1];
                    boneWeights[i].boneIndex2 = vertex.blendIndices[2];
                    boneWeights[i].boneIndex3 = vertex.blendIndices[3];
                    boneWeights[i].weight0    = vertex.blendWeights[0];
                    boneWeights[i].weight1    = vertex.blendWeights[1];
                    boneWeights[i].weight2    = vertex.blendWeights[2];
                    boneWeights[i].weight3    = vertex.blendWeights[3];
                }
            }
                break;
            default:
                throw new Exception("Bad Mesh Asset Type");
        }

        mesh.vertices    = vertices;
        mesh.normals     = normals;
        mesh.uv          = uv;
        mesh.tangents    = tangents;
        mesh.boneWeights = boneWeights;
        mesh.colors      = colors;

        LoadBlendShapes(asset, vertexCount);
        LoadSubmeshes(asset, indexBuffer, indexCount);

        var jointCount = skinnedBindPose.jointCount;
        jointNames = new string[jointCount];
        for (uint i = 0; i < jointCount; i++)
        {
            jointNames[i] = Marshal.PtrToStringAnsi(skinnedBindPose.jointNames[i]);
        }
    }

    private void LoadSubmeshes(IntPtr asset, IntPtr indexBufferPtr, ulong indexCount)
    {
        var subMeshCount = CAPI.ovrAvatarAsset_GetSubmeshCount(asset);

        AvatarLogger.Log("LoadSubmeshes: " + subMeshCount);

        var indices = new short[indexCount];
        Marshal.Copy(indexBufferPtr, indices, 0, (int) indexCount);

        mesh.subMeshCount = (int) subMeshCount;
        uint accumedOffset = 0;
        for (uint index = 0; index < subMeshCount; index++)
        {
            var submeshIndexCount = CAPI.ovrAvatarAsset_GetSubmeshLastIndex(asset, index);
            var currSpan          = submeshIndexCount - accumedOffset;

            var triangles = new int[currSpan];

            var triangleOffset = 0;
            for (ulong i = accumedOffset; i < submeshIndexCount; i += 3)
            {
                // NOTE: We are changing the order of each triangle to match unity expectations vs pipeline.
                triangles[triangleOffset + 2] = indices[i];
                triangles[triangleOffset + 1] = indices[i + 1];
                triangles[triangleOffset]     = indices[i + 2];

                triangleOffset += 3;
            }

            accumedOffset += currSpan;

            mesh.SetIndices(triangles, MeshTopology.Triangles, (int) index);
        }
    }

    private void LoadBlendShapes(IntPtr asset, long vertexCount)
    {
        var blendShapeCount = CAPI.ovrAvatarAsset_GetMeshBlendShapeCount(asset);
        var blendShapeVerts = CAPI.ovrAvatarAsset_GetMeshBlendShapeVertices(asset);

        AvatarLogger.Log("LoadBlendShapes: " + blendShapeCount);

        if (blendShapeVerts != IntPtr.Zero)
        {
            long offset                 = 0;
            long blendVertexSize        = Marshal.SizeOf(typeof(ovrAvatarBlendVertex));
            var  blendVertexBufferStart = blendShapeVerts.ToInt64();

            for (uint blendIndex = 0; blendIndex < blendShapeCount; blendIndex++)
            {
                var blendVerts    = new Vector3[vertexCount];
                var blendNormals  = new Vector3[vertexCount];
                var blendTangents = new Vector3[vertexCount];

                for (long i = 0; i < vertexCount; i++)
                {
                    var vertex = (ovrAvatarBlendVertex) Marshal.PtrToStructure(new IntPtr(blendVertexBufferStart + offset), typeof(ovrAvatarBlendVertex));
                    blendVerts[i]    = new Vector3(vertex.x,  vertex.y,  -vertex.z);
                    blendNormals[i]  = new Vector3(vertex.nx, vertex.ny, -vertex.nz);
                    blendTangents[i] = new Vector4(vertex.tx, vertex.ty, -vertex.tz);

                    offset += blendVertexSize;
                }

                var         namePtr     = CAPI.ovrAvatarAsset_GetMeshBlendShapeName(asset, blendIndex);
                var         name        = Marshal.PtrToStringAnsi(namePtr);
                const float frameWeight = 100f;
                mesh.AddBlendShapeFrame(name, frameWeight, blendVerts, blendNormals, blendTangents);
            }
        }
    }

    private void SetSkinnedBindPose(IntPtr asset, ovrAvatarAssetType meshType)
    {
        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
                skinnedBindPose = CAPI.ovrAvatarAsset_GetMeshData(asset).skinnedBindPose;
                break;
            case ovrAvatarAssetType.CombinedMesh:
                skinnedBindPose = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).skinnedBindPose;
                break;
        }
    }

    private void GetVertexAndIndexData(
            IntPtr             asset,
            ovrAvatarAssetType meshType,
            out long           vertexCount,
            out IntPtr         vertexBuffer,
            out uint           indexCount,
            out IntPtr         indexBuffer)
    {
        vertexCount  = 0;
        vertexBuffer = IntPtr.Zero;
        indexCount   = 0;
        indexBuffer  = IntPtr.Zero;

        switch (meshType)
        {
            case ovrAvatarAssetType.Mesh:
                vertexCount  = CAPI.ovrAvatarAsset_GetMeshData(asset).vertexCount;
                vertexBuffer = CAPI.ovrAvatarAsset_GetMeshData(asset).vertexBuffer;
                indexCount   = CAPI.ovrAvatarAsset_GetMeshData(asset).indexCount;
                indexBuffer  = CAPI.ovrAvatarAsset_GetMeshData(asset).indexBuffer;
                break;
            case ovrAvatarAssetType.CombinedMesh:
                vertexCount  = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).vertexCount;
                vertexBuffer = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).vertexBuffer;
                indexCount   = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).indexCount;
                indexBuffer  = CAPI.ovrAvatarAsset_GetCombinedMeshData(asset).indexBuffer;
                break;
        }
    }

    public SkinnedMeshRenderer CreateSkinnedMeshRendererOnObject(GameObject target)
    {
        var skinnedMeshRenderer = target.AddComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = mesh;
        mesh.name                      = "AvatarMesh_" + assetID;
        var jointCount     = skinnedBindPose.jointCount;
        var bones          = new GameObject[jointCount];
        var boneTransforms = new Transform[jointCount];
        var bindPoses      = new Matrix4x4[jointCount];
        for (uint i = 0; i < jointCount; i++)
        {
            bones[i]          = new GameObject();
            boneTransforms[i] = bones[i].transform;
            bones[i].name     = jointNames[i];
            var parentIndex = skinnedBindPose.jointParents[i];
            if (parentIndex == -1)
            {
                bones[i].transform.parent    = skinnedMeshRenderer.transform;
                skinnedMeshRenderer.rootBone = bones[i].transform;
            }
            else
            {
                bones[i].transform.parent = bones[parentIndex].transform;
            }

            // Set the position relative to the parent
            var position = skinnedBindPose.jointTransform[i].position;
            position.z                       = -position.z;
            bones[i].transform.localPosition = position;

            var orientation = skinnedBindPose.jointTransform[i].orientation;
            orientation.x                    = -orientation.x;
            orientation.y                    = -orientation.y;
            bones[i].transform.localRotation = orientation;

            bones[i].transform.localScale = skinnedBindPose.jointTransform[i].scale;

            bindPoses[i] = bones[i].transform.worldToLocalMatrix * skinnedMeshRenderer.transform.localToWorldMatrix;
        }

        skinnedMeshRenderer.bones = boneTransforms;
        mesh.bindposes            = bindPoses;
        return skinnedMeshRenderer;
    }
}
