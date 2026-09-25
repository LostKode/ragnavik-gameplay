using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RagnavikGameplay;

// A versioned gameplay rule: all clients and the server install the same rate.
internal sealed class ProfessionXpModule : IDisposable
{
    private readonly Harmony harmony = new(GameplayPlugin.ModGuid + ".professionxp");
    private static MethodInfo? fromSkill, activeProfessions;
    private static IDictionary? behaviours;
    private static Type? tooltipType;
    private static FieldInfo? tooltipTopic, tooltipText;

    internal void Install(ManualLogSource log)
    {
        if (!Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.professions", out var plugin) ||
            plugin.Metadata.Version != new System.Version(1, 4, 7))
        {
            log.LogWarning("Profession XP rule inactive: requires Professions 1.4.7.");
            return;
        }
        try
        {
            var assembly = plugin.Instance.GetType().Assembly;
            var main = assembly.GetType("Professions.Professions", true)!;
            var profession = assembly.GetType("Professions.Professions+Profession", true)!;
            fromSkill = AccessTools.Method(main, "fromSkill", new[] { typeof(Skills.SkillType) });
            activeProfessions = AccessTools.Method(assembly.GetType("Professions.Helper", true), "getActiveProfessions", Type.EmptyTypes);
            behaviours = AccessTools.Field(main, "blockOtherProfessions")?.GetValue(null) as IDictionary;
            var gate = AccessTools.Method(assembly.GetType("Professions.Professions+PreventExperience", true), "Prefix", new[] { typeof(Skills), typeof(Skills.SkillType) });
            var raise = AccessTools.Method(typeof(Skills), "RaiseSkill", new[] { typeof(Skills.SkillType), typeof(float) });
            var mode = assembly.GetType("Professions.Professions+ProfessionToggle", true)!;
            var tooltip = AccessTools.Method(assembly.GetType("Skill_Element", true), "UpdateImageDisplay", new[] { mode });
            tooltipType = AccessTools.TypeByName("UITooltip");
            tooltipTopic = tooltipType == null ? null : AccessTools.Field(tooltipType, "m_topic");
            tooltipText = tooltipType == null ? null : AccessTools.Field(tooltipType, "m_text");
            if (tooltipType == null || !typeof(Component).IsAssignableFrom(tooltipType) ||
                tooltipTopic?.FieldType != typeof(string) || tooltipText?.FieldType != typeof(string) || fromSkill == null || Nullable.GetUnderlyingType(fromSkill.ReturnType) != profession ||
                activeProfessions == null || !typeof(IEnumerable).IsAssignableFrom(activeProfessions.ReturnType) ||
                behaviours == null || behaviours.Count != Enum.GetValues(profession).Length ||
                gate?.ReturnType != typeof(bool) || raise?.ReturnType != typeof(void) || tooltip?.ReturnType != typeof(void))
                throw new InvalidOperationException("Profession or game API contract changed");
            harmony.Patch(gate, postfix: new HarmonyMethod(typeof(ProfessionXpModule), nameof(AllowBaselineExperience)));
            harmony.Patch(raise, prefix: new HarmonyMethod(typeof(ProfessionXpModule), nameof(ScaleExperience)) { priority = Priority.Last });
            harmony.Patch(tooltip, postfix: new HarmonyMethod(typeof(ProfessionXpModule), nameof(UpdateTooltip)));
            log.LogInfo("Profession XP rule active: selected professions 100%, unselected professions 50%; character XP unchanged.");
        }
        catch (Exception error)
        {
            harmony.UnpatchSelf();
            log.LogError("Profession XP rule inactive: " + error.GetBaseException().Message);
        }
    }

    private static bool Applies(Skills skills, Skills.SkillType skill, out bool selected)
    {
        selected = false;
        if (Player.m_localPlayer == null || Player.m_localPlayer.GetSkills() != skills) return false;
        var profession = fromSkill!.Invoke(null, new object[] { skill });
        if (profession == null || behaviours![profession] is not ConfigEntryBase entry ||
            entry.BoxedValue.ToString() != "BlockExperience") return false;
        foreach (var active in (IEnumerable)activeProfessions!.Invoke(null, null))
            if (Equals(active, profession)) { selected = true; break; }
        return true;
    }

    // This postfix patches the original mod's gate, not Skills.RaiseSkill itself.
    private static void AllowBaselineExperience(Skills __0, Skills.SkillType __1, ref bool __result)
    {
        if (Applies(__0, __1, out _)) __result = true;
    }

    private static void ScaleExperience(Skills __instance, Skills.SkillType __0, ref float __1)
    {
        if (Applies(__instance, __0, out var selected))
            __1 = ProfessionXpPolicy.Scale(__1, selected);
    }

    private static void UpdateTooltip(Component __instance, object __0)
    {
        if (__0.ToString() != "BlockExperience") return;
        foreach (var tooltip in __instance.GetComponentsInChildren(tooltipType!, true))
        {
            if (tooltipTopic!.GetValue(tooltip) as string != "Experience blocked") continue;
            tooltipTopic.SetValue(tooltip, "Profession experience");
            tooltipText!.SetValue(tooltip, "Selected profession: 100% skill XP. Unselected profession: 50% skill XP. Both can reach the normal maximum level. Character XP is unchanged.");
        }
    }

    public void Dispose() => harmony.UnpatchSelf();
}
