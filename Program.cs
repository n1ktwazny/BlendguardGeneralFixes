using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Windows;

[BepInPlugin("com.nikt.BlendguardGFixes", "Blendguard General Fixes", "0.2.0")]
public class BGuardGeneralFixes : BaseUnityPlugin{
    private static ConfigEntry<bool> DisableUi;
    private static ConfigEntry<bool> DisableEnemies;
    private static ConfigEntry<bool> InfiniteMoney;
    // Player
    private static ConfigEntry<bool> enableScreenShake;
    // Tower
    private static ConfigEntry<bool> removeBullets;
    private static ConfigEntry<bool> fastProjectiles;
    private static ConfigEntry<float> bulletSpeed;
    // Invader
    private static ConfigEntry<bool> fixInvaderSpeedApply;
    private static ConfigEntry<bool> invaderSpeedCorrection;
    private static float _speed;
       
    private static readonly MethodInfo AssignAgentSpeedMethod = AccessTools.Method(typeof(Invader), "AssignAgentSpeed");
    void Awake(){
        DisableUi = Config.Bind("Debugging", "Disable UI Elements", false, "!! Disables all in-game UI elements after  (for like videos and stuff)!!");
        DisableEnemies = Config.Bind("Debugging", "Disable Enemies", false, "Overrides the spawning logic to stop enemies from spawning");
        //InfiniteMoney = Config.Bind("Debugging", "Infinite Money", false, "Well.. Infinite money.");
        enableScreenShake = Config.Bind("Player", "Screen shaking", false, "Toggles the annoying camera screen shake");
        removeBullets = Config.Bind("Towers", "Remove Bullets Under The Map", true, "Remove bullets that fall under the map");
        fastProjectiles = Config.Bind("Towers", "Speed Up Bullets", true, "Increase the speed of the tower's projectiles to make them more reliable");
        bulletSpeed = Config.Bind("Towers", "Bullet Speed", 20f, "New speed (Vanilla is like 5 or 7, idk, it changes the entire logic of shooting :P)");
        fixInvaderSpeedApply = Config.Bind("Invaders", "Speed Apply", true, "Apply the unused speed variables to Invaders");
        invaderSpeedCorrection = Config.Bind("Invaders", "Speed Correction", true, "Correct/Decrease the Invader Speed to make towers less likely to miss. (!! IF YOU DISABLE THIS TOWERS CAN MISS THE TARGET !!)");
        _speed = bulletSpeed.Value;
        
        Harmony.CreateAndPatchAll(typeof(BGuardGeneralFixes));
    }

    [HarmonyPatch(typeof(Invader), "Start")]
    [HarmonyPostfix]
    static void ActuallyAssignSpeed(Invader __instance){
        if(fixInvaderSpeedApply.Value){
            AssignAgentSpeedMethod?.Invoke(__instance, null);
        }
    }

    // Change the Invader speed
    [HarmonyPatch(typeof(Invader), "AssignAgentSpeed")]
    [HarmonyPostfix]
    static void SpeedCorector(Invader __instance){
        var enemyNav = AccessTools.Field(typeof(Invader), "_navMeshAgent").GetValue(__instance);
        NavMeshAgent navAgent = (NavMeshAgent)enemyNav;
           
        if(invaderSpeedCorrection.Value){
            if (navAgent.speed == 2f){
                navAgent.speed = 1.5f;
            }else if (navAgent.speed == 0.5f){
                navAgent.speed = 0.65f;
            }
        }
    }

    // Remove projectiles under Y = 0
    [HarmonyPatch(typeof(AttackerProjectile), "Update")]
    [HarmonyPostfix]
    static void ProjectileRemover(AttackerProjectile __instance){
        if (removeBullets.Value && __instance.transform.position.y < 0f){
            __instance.gameObject.SetActive(false);
        }
    }

    // Disable the old shooting logic
    [HarmonyPatch(typeof(AttackerProjectile), "FixedUpdate")]
    [HarmonyPrefix]
    static bool projectileSpeedup(){
        if(fastProjectiles.Value){
            return false;
        }
        return true;
    }

    // Override the shooting logic to make it possible to edit in a reasonable way (don't put important gameplay elements into FixedUpdate pls)
    [HarmonyPatch(typeof(AttackerProjectile), "Update")]
    [HarmonyPostfix]
    static void ProjSpeedApply(AttackerProjectile __instance){
        if(fastProjectiles.Value){
            __instance.transform.position += __instance.transform.forward * _speed * Time.deltaTime;
        }
    }

    //[HarmonyPatch(typeof(CurrencyManager), "RaiseCardCost")]
    //[HarmonyPrefix]
    //static void UIDisabler(){
    //    
    //}
    
    // Disable all screenshaking
    [HarmonyPatch(typeof(MainVC), "Start")]
    [HarmonyPostfix]
    static void KillScreenshake(MainVC __instance){
        if (!enableScreenShake.Value){
            AccessTools.Field(typeof(MainVC), "_shakeMax").SetValue(__instance, 0f);
            AccessTools.Field(typeof(MainVC), "_lowerDistanceTreshold").SetValue(__instance, 0f);
            AccessTools.Field(typeof(MainVC), "_higherDistanceTreshold").SetValue(__instance, 0f);
        }
    }

    //Cancel screenshake when it gets called
    [HarmonyPatch(typeof(MainVC), "Skake")]
    [HarmonyPrefix]
    static bool CancelScreenshake(){
        if (!enableScreenShake.Value){
            return false;
        }
        return true;
    }
    
    //Disable UI on tower kill
    [HarmonyPatch(typeof(StructureEliminator), "EliminateStructure")]
    [HarmonyPrefix]
    static void DisableUiOnKill(){
        if (DisableUi.Value){
            Destroy(MonoSingleton<UIManager>.Instance);
        }
    }

    //Change the text to say it's infinite money
    [HarmonyPatch(typeof(UIManager), "UpdateCurrency")]
    [HarmonyPrefix]
    static bool InfiniteText(UIManager __instance){
        if (InfiniteMoney.Value){
            //AccessTools.Field(typeof(UIManager), "_currencyText").SetValue(__instance, "-1");
            return false;
        }
        return true;
    }
    
    //[HarmonyPatch(typeof(UIManager), "Start")]
    //[HarmonyPrefix]
    //static void UIOverride(UIManager __instance){
    //    if (DisableUi.Value){
    //        RectTransform temp = __instance.GetComponent<RectTransform>();
    //        temp.position = Vector2.up * 2000f;
    //    }
    //}
    
    // Stops the enemy spawn
    [HarmonyPatch(typeof(InvaderSpawner), "SpawnEnemy")]
    [HarmonyPrefix]
    static bool EnemyOverride(){
        if (DisableEnemies.Value){
            return false;
        }
        return true;
    }

    // Gives a lot of money
    [HarmonyPatch(typeof(CurrencyManager), "Transaction")]
    [HarmonyPrefix]
    static void MonyAdder(CurrencyManager __instance){
        if (InfiniteMoney.Value){
            Debug.Log("Added money");
            AccessTools.Field(typeof(CurrencyManager), "playerCurrency").SetValue(__instance, 2000000f);
        }
    }
    
}