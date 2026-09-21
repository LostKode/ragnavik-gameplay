using UnityEngine;

namespace RagnavikGameplay;

internal sealed class StarterChestBehaviour : MonoBehaviour, Hoverable, Interactable
{
    public string GetHoverName() => "Starter Supplies";

    public string GetHoverText()
    {
        var useKey = ZInput.instance?.GetBoundKeyString("Use", false);
        if (string.IsNullOrWhiteSpace(useKey))
        {
            useKey = "E";
        }

        return $"[<color=yellow><b>{useKey}</b></color>] Claim starter supplies";
    }

    public float GetHoverOffset() => 1f;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer)
        {
            return false;
        }

        StarterChestModule.RequestClaim();
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}

