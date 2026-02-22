using System;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Core.Helpers;

public static class BarricadeHelper
{
    public struct BarricadeInfo
    {
        public Transform Transform;
        public Vector3 Position;
        public Vector3 Rotation;
        public string AssetName;
        public ushort AssetId;
        public string State;
    }

    public static bool TryGetBarricadeFromHit(Transform hitTransform, out BarricadeInfo info)
    {
        info = default;
        var transform = hitTransform;

        while (transform != null)
        {
            if (BarricadeManager.tryGetInfo(transform, out _, out _, out _, out var index, out var region))
            {
                info.Transform = transform;
                info.Position = transform.position;
                info.Rotation = transform.rotation.eulerAngles;

                var data = region.barricades[index];
                info.AssetName = data.barricade.asset?.name;
                info.AssetId = data.barricade.asset?.id ?? 0;
                info.State = Convert.ToBase64String(data.barricade.state);
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    public static void ChangeBarricadeOwner(Transform transform, CSteamID steamID, CSteamID groupID)
    {
        var arr = new byte[17];
        BitConverter.GetBytes(steamID.m_SteamID).CopyTo(arr, 0);
        BitConverter.GetBytes(groupID.m_SteamID).CopyTo(arr, 8);
        BitConverter.GetBytes(false).CopyTo(arr, 16);
        BarricadeManager.updateReplicatedState(transform, arr, 17);
        BarricadeManager.changeOwnerAndGroup(transform, steamID.m_SteamID, groupID.m_SteamID);
    }
}
