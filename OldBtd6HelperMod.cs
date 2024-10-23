using BTD_Mod_Helper;
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
    /// Skip popup forcing you to choose online mode
    /// </summary>
    public override void OnApplicationStart()
    {
        Game.EnableOfflineMode();
    }
}