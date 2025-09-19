using System.IO;
using System.Linq;
using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Quests;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Analytics;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.Coop;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.EditorMenus;
using Il2CppAssets.Scripts.Unity.UI_New.Quests;
using Il2CppAssets.Scripts.Utils;
using Il2CppFacepunch.Steamworks;
using Il2CppSystem.Threading.Tasks;
using UnityEngine;

#if V50_OR_GREATER
using Il2CppSystem.Linq;
#endif

#if V40_OR_GREATER
using System.Text;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Gameplay.Mods;
using Il2CppAssets.Scripts.Models.ServerEvents;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.DailyChallenge;
using Il2CppAssets.Scripts.Unity.UI_New.Main.EventPanel;
using Il2CppNewtonsoft.Json;
using Il2CppSystem.Collections.Generic;
using Action = System.Action;
#endif

namespace OldBtd6Helper;

/// <summary>
/// Use separate userdata from normal game
/// </summary>
[HarmonyPatch(typeof(User), nameof(User.GetUserDataFolder))]
internal static class User_GetUserDataFolder
{
    [HarmonyPostfix]
    internal static void Postfix(ref string __result)
    {
        __result = Path.Combine(__result, Game.Version.ToString());
        OldBtd6HelperMod.SuccessfullyPatchedProfilePath = true;
    }
}

/// <summary>
/// Skip Tutorial for new profiles, 
/// </summary>
[HarmonyPatch(typeof(AnalyticsManager), nameof(AnalyticsManager.PlayerLoaded))]
internal static class AnalyticsManager_PlayerLoaded
{
    [HarmonyPostfix]
    internal static void Postfix(Btd6Player player)
    {
#if V50_OR_GREATER
        var tutorialIncomplete = player.Data.tutorialTypeSelected != TutorialType.Original;
#else
        var tutorialIncomplete = player.Data.HasCompletedTutorial != true;
#endif

        if (!tutorialIncomplete) return;

#if V50_OR_GREATER
        player.Data.tutorialTypeSelected = TutorialType.Original;
#else
        player.Data.HasCompletedTutorial = true;
#endif



        player.Data.hasUnlockedMapEditor = true;
    }
}

/// <summary>
/// Prevent this free progress from being actually saved
/// </summary>
[HarmonyPatch(typeof(Btd6Player), nameof(Btd6Player.SaveNow))]
internal class Btd6Player_SaveNow
{
    [HarmonyPrefix]
    internal static void Prefix(Btd6Player __instance)
    {
#if V50_OR_GREATER
        var tutorialIncomplete = __instance.Data?.tutorialTypeSelected != TutorialType.Original;
#else
        var tutorialIncomplete = __instance.Data?.HasCompletedTutorial != true;
#endif

        if (tutorialIncomplete || !OldBtd6HelperMod.SuccessfullyPatchedProfilePath) return;

        var profile = __instance.Data;

        profile.rank.Value = 1;
        profile.xp.Value = 0;

        profile.unlockedTowers.Clear();
        profile.towerXp.Clear();
        profile.acquiredUpgrades.Clear();
        profile.savedStats.Clear();
        profile.unlockedHeroes.Clear();
        profile.unlockedTowerSkins.Clear();
        profile.KnowledgePoints = 0;
        profile.acquiredKnowledge.Clear();
    }

    [HarmonyPostfix]
    internal static void Postfix(Btd6Player __instance)
    {
#if V50_OR_GREATER
        var tutorialIncomplete = __instance.Data?.tutorialTypeSelected != TutorialType.Original;
#else
        var tutorialIncomplete = __instance.Data?.HasCompletedTutorial != true;
#endif

        if (tutorialIncomplete || !OldBtd6HelperMod.SuccessfullyPatchedProfilePath) return;

        var player = Game.Player;
        player.Data.rank.Value = 42;
        player.Data.xp.Value = 964000 + 42;

        foreach (var tower in Game.instance.model.towerSet)
        {
            player.UnlockTower(tower.towerId);
            player.AddTowerXP(tower.towerId, 42);

#if V50_OR_GREATER
            var towers = Game.instance.model.GetTowersWithBaseId(tower.towerId).ToArray();
#else
            var towers = Game.instance.model.GetTowersWithBaseId(tower.towerId);
#endif

            foreach (var upgrade in towers.SelectMany(t => t.appliedUpgrades).Distinct())
            {
                player.AcquireUpgrade(tower.towerId, upgrade, 0);
            }
        }

        foreach (var hero in Game.instance.model.heroSet)
        {
            player.UnlockHero(hero.towerId);
            player.SeenHeroUnlocked(hero.towerId);
            player.SeenNewHeroNotification(hero.towerId);
        }

        player.SeenHeroUnlockedNotification();

        foreach (var knowledge in Game.instance.model.allKnowledge)
        {
            player.AcquireKnowledge(knowledge.name);
        }

        player.Data.KnowledgePoints = 0;

        foreach (var map in GameData.Instance.mapSet.Maps.items)
        {
            player.UnlockMap(map.id);
        }

        foreach (var skin in GameData.Instance.skinsData.SkinList.items)
        {
            player.SeenTowerSkin(skin.name);
        }

        foreach (var quest in GameData.Instance.questData.quests)
        {
#if V42_OR_GREATER
            var questSaveData = player.GetQuestSaveData(quest.id);
#else
            player.GetQuestSaveData(quest.id, out var questSaveData);
#endif
            questSaveData.hasSeenQuest = true;
        }

        player.Data.unlockedBigBloons = true;
        player.Data.unlockedSmallBloons = true;
        player.Data.unlockedSmallTowers = true;
        player.Data.unlockedBigTowers = true;

#if V44_OR_GREATER
#else
        player.Data.seenIntermediateUnlock = true;
        player.Data.seenAdvancedUnlock = true;
        player.Data.seenExpertUnlock = true;
#endif
    }
}

/// <summary>
/// Allow user to pass CoOp just to get to the Social Screen, otherwise just Custom Maps
/// </summary>
[HarmonyPatch(typeof(SocialBoundaryUtils), nameof(SocialBoundaryUtils.TryPassSocialBoundary))]
internal static class SocialBoundaryUtils_TryPassSocialBoundary
{
    [HarmonyPrefix]
    internal static bool Prefix(ref Task<bool> __result, SocialGameplayType gameplayType)
    {
        if (gameplayType is not (SocialGameplayType.Coop or SocialGameplayType.CustomMap
            or SocialGameplayType.BossEvent)) return true;

        __result = Task.FromResult(true);
        return false;
    }
}

/// <summary>
/// Show offline compatible only
/// </summary>
[HarmonyPatch(typeof(PlaySocialScreen), nameof(PlaySocialScreen.Open))]
internal static class PlaySocialScreen_Open
{
    [HarmonyPostfix]
    internal static void Postfix(PlaySocialScreen __instance)
    {
        __instance.quickGameBtn.transform.parent.parent.gameObject.SetActive(false);

        __instance.contentBrowserPanel.transform.parent.localPosition = new Vector3(0, 0, 0);

        __instance.contentBrowserPanel.SetActive(true);
        __instance.viewBrowserBtn.gameObject.SetActive(false);
        __instance.editChallengeBtn.gameObject.SetActive(true);
        __instance.editOdysseyBtn.gameObject.SetActive(true);
        __instance.mapEditorBtn.gameObject.SetActive(true);
    }
}

/// <summary>
/// Show offline compatible only
/// </summary>
[HarmonyPatch(typeof(MapEditorScreen), nameof(MapEditorScreen.Open))]
internal static class MapEditorScreen_Open
{
    [HarmonyPostfix]
    internal static void Postfix(MapEditorScreen __instance)
    {
        __instance.myMapsButton.gameObject.SetActive(false);
    }
}

/// <summary>
/// Skip Tower Quests
/// </summary>
[HarmonyPatch(typeof(QuestData), nameof(QuestData.TryGetTowerUnlockData))]
internal static class QuestData_TryGetTowerUnlockData
{
    [HarmonyPrefix]
    internal static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

/// <summary>
/// Skip Tower Quests
/// </summary>
[HarmonyPatch(typeof(QuestTrackerManager), nameof(QuestTrackerManager.IsTowerLockedByQuest))]
internal static class QuestTrackerManager_IsTowerLockedByQuest
{
    [HarmonyPrefix]
    internal static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

/// <summary>
/// Modes Unlocked
/// </summary>
[HarmonyPatch(typeof(MapInfoManager), nameof(MapInfoManager.IsModeUnlocked))]
internal static class MapInfoManager_IsModeUnlocked
{
    [HarmonyPrefix]
    internal static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}

#if V40_OR_GREATER
/// <summary>
/// Boss Challenges only
/// </summary>
[HarmonyPatch(typeof(MainMenuEventPanel), nameof(MainMenuEventPanel.CreateBossIcon))]
internal static class MainMenuEventPanel_CreateBossIcon
{
    [HarmonyPrefix]
    internal static bool Prefix(MainMenuEventPanel __instance, ref Task __result)
    {
        __instance.gameObject.AddModHelperComponent(ModHelperButton.Create(new Info("Button", 300),
            VanillaSprites.VortexEventIcon,
            new Action(() =>
            {
                MenuManager.instance.OpenMenu("BossEventUI", new Il2CppSystem.Tuple<BossEvent?, bool>(null, false));
            })));

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// Boss Challenges only
/// </summary>
[HarmonyPatch(typeof(BossEventScreen), nameof(BossEventScreen.Open))]
internal static class BossEventScreen_Open
{
    [HarmonyPostfix]
    internal static void Postfix(BossEventScreen __instance)
    {
        __instance.BossChallengeClicked();
    }
}

/// <summary>
/// Fix boss challenge round sets
/// </summary>
[HarmonyPatch(typeof(RoundSetModModel), nameof(RoundSetModModel.Mutate))]
internal static class RoundSetModModel_Mutate
{
    [HarmonyPrefix]
    internal static void Prefix(RoundSetModModel __instance)
    {
        var gameEvents = SkuSettings.instance.gameEvents;

        if (gameEvents.roundSets.Count != 0) return;

        using var jsonStream = typeof(OldBtd6HelperMod).Assembly.GetEmbeddedResource("roundSets.json");
        using var streamReader = new StreamReader(jsonStream, Encoding.UTF8);
        var text = streamReader.ReadToEnd();

        gameEvents.roundSets = JsonConvert.DeserializeObject<Dictionary<string, GameEvents.RoundsContainer>>(text);
    }
}

#endif

#if V41_OR_GREATER

/// <summary>
/// No Co-Op bosses
/// </summary>
[HarmonyPatch(typeof(BossEventScreen), nameof(BossEventScreen.ToggleEventSpecificUI))]
internal static class BossEventScreen_ToggleEventSpecificUI
{
    [HarmonyPostfix]
    internal static void Postfix(BossEventScreen __instance)
    {
        __instance.coopBossChallengeObj.SetActive(false);
    }
}

#endif