using System.Linq;
using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using MelonLoader;
using OldBtd6Helper;

[assembly: MelonInfo(typeof(OldBtd6HelperMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace OldBtd6Helper;

public class OldBtd6HelperMod : BloonsTD6Mod
{
    public static bool SuccessfullyPatchedProfilePath;
    
    /// <summary>
    /// Skip popup forcing you to choose online mode anyway
    /// </summary>
    public override void OnApplicationStart()
    {
        Game.EnableOfflineMode();
    }

    public override void OnMainMenu()
    {
        // FileIOHelper.SaveObject("roundSets.json", SkuSettings.instance.gameEvents.roundSets);
    }

    /// <summary>
    /// Don't save any of the goodies to profile
    /// </summary>
    public override void PreCleanProfile(ProfileModel profile)
    {
        if (!SuccessfullyPatchedProfilePath) return;
        
        profile.rank.Value = 1;
        profile.xp.Value = 0;

        profile.unlockedTowers.Clear();
        profile.towerXp.Clear();
        profile.acquiredUpgrades.Clear();
        profile.savedMaps.Clear();
        profile.unlockedHeroes.Clear();
        profile.unlockedTowerSkins.Clear();
        profile.KnowledgePoints = 0;
        profile.acquiredKnowledge.Clear();
    }

    /// <summary>
    /// Still let goodies be used while in game
    /// </summary>
    public override void PostCleanProfile(ProfileModel profile)
    {
        if (!SuccessfullyPatchedProfilePath) return;
        
        var player = Game.Player;
        player.Data.rank.Value = 42;
        player.Data.xp.Value = 964000 + 42;

        foreach (var tower in Game.instance.model.towerSet)
        {
            player.UnlockTower(tower.towerId);
            player.AddTowerXP(tower.towerId, 42);
            foreach (var upgrade in Game.instance.model.GetTowersWithBaseId(tower.towerId)
                         .SelectMany(t => t.appliedUpgrades).Distinct())
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