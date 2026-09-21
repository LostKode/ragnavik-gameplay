using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Utils;

namespace RagnavikGameplay;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public sealed class GameplayPlugin : BaseUnityPlugin
{
    public const string ModGuid = "lostkode.ragnavik.gameplay";
    public const string ModName = "Ragnavik Gameplay";
    public const string ModVersion = "1.0.0";

    private StarterChestModule? _starterChest;

    internal ManualLogSource Log => Logger;

    private void Awake()
    {
        var enabled = Config.Bind("Starter Chest", "Enabled", true, "Place and enable the personalized starter chest.");
        var kitId = Config.Bind("Starter Chest", "KitId", "starter-v1", "Changing this value allows every account to claim the new kit once.");
        var items = Config.Bind("Starter Chest", "Items", StarterKit.Default, "Comma-separated Prefab:Amount entries.");
        var offsetX = Config.Bind("Starter Chest", "OffsetX", 0f, "World-space X offset from the center of StartTemple.");
        var offsetZ = Config.Bind("Starter Chest", "OffsetZ", 4f, "World-space Z offset from the center of StartTemple.");
        var rotation = Config.Bind("Starter Chest", "Rotation", 180f, "Chest rotation around the Y axis.");

        _starterChest = new StarterChestModule(this, enabled, kitId, items, offsetX, offsetZ, rotation);
        _starterChest.Install();
        Logger.LogInfo($"{ModName} {ModVersion} loaded.");
    }

    private void Update() => _starterChest?.Update();

    private void OnDestroy() => _starterChest?.Dispose();
}

