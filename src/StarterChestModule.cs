using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RagnavikGameplay;

internal sealed class StarterChestModule : IDisposable
{
    private const string PrefabName = "RagnavikStarterChest";
    private const string ClaimRequestRpcName = "StarterChestClaimRequest";
    private const string ClaimResponseRpcName = "StarterChestClaimResponse";
    private static StarterChestModule? _instance;

    private readonly GameplayPlugin _plugin;
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<string> _kitId;
    private readonly ConfigEntry<string> _items;
    private readonly ConfigEntry<float> _offsetX;
    private readonly ConfigEntry<float> _offsetZ;
    private readonly ConfigEntry<float> _rotation;
    private CustomRPC? _claimRequestRpc;
    private CustomRPC? _claimResponseRpc;
    private GameObject? _prefab;
    private float _nextPlacementAttempt;
    private int _scanIndex;
    private readonly List<ZDO> _found = new();

    internal StarterChestModule(
        GameplayPlugin plugin,
        ConfigEntry<bool> enabled,
        ConfigEntry<string> kitId,
        ConfigEntry<string> items,
        ConfigEntry<float> offsetX,
        ConfigEntry<float> offsetZ,
        ConfigEntry<float> rotation)
    {
        _plugin = plugin;
        _enabled = enabled;
        _kitId = kitId;
        _items = items;
        _offsetX = offsetX;
        _offsetZ = offsetZ;
        _rotation = rotation;
    }

    internal void Install()
    {
        _instance = this;
        _claimRequestRpc = NetworkManager.Instance.AddRPC(ClaimRequestRpcName, ServerReceiveRequest, IgnorePackage);
        _claimResponseRpc = NetworkManager.Instance.AddRPC(ClaimResponseRpcName, IgnorePackage, ClientReceiveResponse);
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefab;
    }

    internal void Update()
    {
        if (!_enabled.Value || _prefab == null || Time.realtimeSinceStartup < _nextPlacementAttempt ||
            ZNet.instance == null || !ZNet.instance.IsServer() || ZoneSystem.instance == null || ZDOMan.instance == null)
        {
            return;
        }

        _nextPlacementAttempt = Time.realtimeSinceStartup + 5f;
        TryPlaceChest();
    }

    internal static void RequestClaim()
    {
        if (_instance?._claimRequestRpc == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            _instance.HandleLocalClaim();
            return;
        }

        _instance._claimRequestRpc.Initiate();
    }

    public void Dispose()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefab;
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void RegisterPrefab()
    {
        if (_prefab != null)
        {
            return;
        }

        var chest = PrefabManager.Instance.CreateClonedPrefab(PrefabName, "piece_chest_wood");
        var container = chest.GetComponent<Container>();
        if (container != null)
        {
            UnityEngine.Object.DestroyImmediate(container);
        }

        chest.AddComponent<StarterChestBehaviour>();
        PrefabManager.Instance.AddPrefab(new CustomPrefab(chest, false));
        _prefab = chest;
        _plugin.Log.LogInfo("Registered the personalized starter chest prefab.");
    }

    private void TryPlaceChest()
    {
        if (_scanIndex == 0)
        {
            _found.Clear();
        }

        if (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PrefabName, _found, ref _scanIndex))
        {
            // Continue the bounded ZDO scan on the next frame. Waiting five seconds
            // between batches made placement depend on how long the player roamed.
            _nextPlacementAttempt = Time.realtimeSinceStartup;
            return;
        }

        _scanIndex = 0;
        _found.RemoveAll(zdo => zdo == null || !zdo.IsValid());
        if (_found.Count > 0)
        {
            for (var index = 1; index < _found.Count; index++)
            {
                ZDOMan.instance.DestroyZDO(_found[index]);
            }

            if (_found.Count > 1)
            {
                _plugin.Log.LogWarning($"Removed {_found.Count - 1} duplicate starter chest(s); one remains.");
            }

            _nextPlacementAttempt = float.MaxValue;
            return;
        }

        var start = default(ZoneSystem.LocationInstance);
        if (!ZoneSystem.instance.FindClosestLocation("StartTemple", Vector3.zero, out start))
        {
            _plugin.Log.LogDebug("StartTemple is not available yet; placement will retry.");
            return;
        }

        var position = start.m_position + new Vector3(_offsetX.Value, 0f, _offsetZ.Value);
        position.y = ZoneSystem.instance.GetGroundHeight(position);
        UnityEngine.Object.Instantiate(_prefab, position, Quaternion.Euler(0f, _rotation.Value, 0f));
        _nextPlacementAttempt = float.MaxValue;
        _plugin.Log.LogInfo($"Placed starter chest at {position} in the existing StartTemple ring.");
    }

    private IEnumerator ServerReceiveRequest(long sender, ZPackage package)
    {
        var peer = ZNet.instance.GetPeers().FirstOrDefault(candidate => candidate.m_uid == sender);
        var platformId = string.Empty;
        if (peer != null)
        {
            var playerInfo = ZNet.instance.GetPlayerList().FirstOrDefault(info => info.m_characterID.Equals(peer.m_characterID));
            platformId = playerInfo.m_userInfo.m_id.ToString();
        }
        if (string.IsNullOrWhiteSpace(platformId) && peer?.m_socket != null)
        {
            platformId = peer.m_socket.GetHostName();
        }

        _claimResponseRpc!.SendPackage(sender, BuildClaimResponse(platformId));
        yield break;
    }

    private IEnumerator ClientReceiveResponse(long sender, ZPackage package)
    {
        ApplyResponse(package);
        yield break;
    }

    private IEnumerator IgnorePackage(long sender, ZPackage package)
    {
        yield break;
    }

    private void HandleLocalClaim()
    {
        var platformId = string.Empty;
        if (ZNet.instance != null && Player.m_localPlayer != null)
        {
            var localName = Player.m_localPlayer.GetPlayerName();
            var playerInfo = ZNet.instance.GetPlayerList().FirstOrDefault(info => info.m_name == localName);
            platformId = playerInfo.m_userInfo.m_id.ToString();
        }

        var response = BuildClaimResponse(platformId);
        response.SetPos(0);
        ApplyResponse(response);
    }

    private ZPackage BuildClaimResponse(string platformId)
    {
        var response = new ZPackage();
        if (!_enabled.Value || ZoneSystem.instance == null || ZNet.instance == null)
        {
            WriteResponse(response, false, "Starter supplies are unavailable.", Array.Empty<StarterItem>());
            return response;
        }

        if (string.IsNullOrWhiteSpace(platformId))
        {
            WriteResponse(response, false, "Your platform account could not be verified.", Array.Empty<StarterItem>());
            return response;
        }

        IReadOnlyList<StarterItem> kit;
        try
        {
            kit = StarterKit.Parse(_items.Value);
        }
        catch (FormatException exception)
        {
            _plugin.Log.LogError(exception.Message);
            WriteResponse(response, false, "The starter kit is misconfigured.", Array.Empty<StarterItem>());
            return response;
        }

        var claimKey = StarterKit.ClaimKey(platformId, _kitId.Value);
        if (ZoneSystem.instance.GetGlobalKey(claimKey))
        {
            WriteResponse(response, false, "You have already claimed these starter supplies.", Array.Empty<StarterItem>());
            return response;
        }

        // The server records authorization before delivery, making reconnect and
        // character recreation unable to duplicate the kit.
        ZoneSystem.instance.SetGlobalKey(claimKey);
        WriteResponse(response, true, "Starter supplies claimed.", kit);
        return response;
    }

    private void ApplyResponse(ZPackage package)
    {
        var success = package.ReadBool();
        var message = package.ReadString();
        var count = package.ReadInt();
        if (success && Player.m_localPlayer != null && ObjectDB.instance != null)
        {
            for (var index = 0; index < count; index++)
            {
                var prefabName = package.ReadString();
                var amount = package.ReadInt();
                GiveItemOrDrop(prefabName, amount);
            }
        }

        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message, 0, null, false, false);
    }

    private void GiveItemOrDrop(string prefabName, int amount)
    {
        var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
        var drop = prefab?.GetComponent<ItemDrop>();
        if (prefab == null || drop == null)
        {
            _plugin.Log.LogError($"Starter item prefab '{prefabName}' was not found.");
            return;
        }

        var remaining = amount;
        var stackSize = Math.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
        while (remaining > 0)
        {
            var stack = Math.Min(remaining, stackSize);
            var item = Player.m_localPlayer.GetInventory().AddItem(prefabName, stack, drop.m_itemData.m_quality, 0, 0L, string.Empty, false, false);
            if (item == null)
            {
                var spawned = UnityEngine.Object.Instantiate(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward, Quaternion.identity);
                var spawnedDrop = spawned.GetComponent<ItemDrop>();
                if (spawnedDrop != null)
                {
                    spawnedDrop.m_itemData.m_stack = stack;
                }
            }
            remaining -= stack;
        }
    }

    private static void WriteResponse(ZPackage package, bool success, string message, IReadOnlyList<StarterItem> items)
    {
        package.Write(success);
        package.Write(message);
        package.Write(items.Count);
        foreach (var item in items)
        {
            package.Write(item.Prefab);
            package.Write(item.Amount);
        }
    }
}

