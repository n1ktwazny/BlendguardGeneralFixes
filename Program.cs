using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

[BepInPlugin("com.nikt.BlendguardGFixes", "Blendguard General Fixes", "0.1.1")]
public class BGuardGeneralFixes : BaseUnityPlugin{
    private static ConfigEntry<bool> enableScreenShake;
    private static ConfigEntry<bool> fixInvaderSpeedApply;
    private static ConfigEntry<bool> fixInvaderSpeed;
    private static ConfigEntry<bool> removeBullets;
    private static ConfigEntry<bool> fastProjectiles;
    private static ConfigEntry<float> bulletSpeed;
    private static float _speed;
       
    private static readonly MethodInfo AssignAgentSpeedMethod = AccessTools.Method(typeof(Invader), "AssignAgentSpeed");
    void Awake(){
        enableScreenShake = Config.Bind("Player", "Screen shaking", true, "Toggles the annoying camera screen shake");
        fastProjectiles = Config.Bind("Towers", "Speed Up Bullets", true, "Increase the speed of the tower's projectiles to make them more reliable");
        removeBullets = Config.Bind("Towers", "Remove Bullets Under The Map", true, "Remove bollets that fall under the map");
        bulletSpeed = Config.Bind("Towers", "Bullet Speed", 20f, "Updated speed");
        fixInvaderSpeedApply = Config.Bind("Invaders", "Speed Apply", true, "Apply the unused speed variables to Invaders");
        fixInvaderSpeed = Config.Bind("Invaders", "Speed Correction", true, "Correct/Decrease the Invader Speed to make towers less likely to miss");
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

    [HarmonyPatch(typeof(Invader), "AssignAgentSpeed")]
    [HarmonyPostfix]
    static void AdditionalSpeed(Invader __instance){
        var enemyNav = AccessTools.Field(typeof(Invader), "_navMeshAgent").GetValue(__instance);
        NavMeshAgent navAgent = (NavMeshAgent)enemyNav;
           
        if(fixInvaderSpeed.Value){
            if (navAgent.speed == 2f){
                navAgent.speed = 1.5f;
            }else if (navAgent.speed == 0.5f){
                navAgent.speed = 0.65f;
            }
        }
    }

    [HarmonyPatch(typeof(AttackerProjectile), "Update")]
    [HarmonyPostfix]
    static void ProjectileRemover(AttackerProjectile __instance){
        if (removeBullets.Value && __instance.transform.position.y < 0f){
            __instance.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(AttackerProjectile), "FixedUpdate")]
    [HarmonyPrefix]
    static bool projectileSpeedup(){
        if(fastProjectiles.Value){
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(AttackerProjectile), "Update")]
    [HarmonyPostfix]
    static void ProjSpeedApply(AttackerProjectile __instance){
        if(fastProjectiles.Value){
            __instance.transform.position += __instance.transform.forward * _speed * Time.deltaTime;
        }
    }
    
    [HarmonyPatch(typeof(MainVC), "Start")]
    [HarmonyPostfix]
    static void KillScreenshake(MainVC __instance){
        if (enableScreenShake.Value){
            AccessTools.Field(typeof(MainVC), "_shakeMax").SetValue(__instance, 0f);
        }
    }
    
}