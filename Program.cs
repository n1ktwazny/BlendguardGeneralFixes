using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.AI;

[BepInPlugin("com.nikt.BlendGuardGFixes", "Blendguard General Fixes", "0.1.0")]
public class BGuardGeneralFixes : BaseUnityPlugin{
    private static ConfigEntry<bool> fixInvaderSpeedApply;
    private static ConfigEntry<bool> fixInvaderSpeed;
       
    private static readonly MethodInfo AssignAgentSpeedMethod = AccessTools.Method(typeof(Invader), "AssignAgentSpeed");
    void Awake(){
        fixInvaderSpeedApply = Config.Bind("Invaders", "Speed Apply", true, "Apply the unused speed variables to Invaders");
        fixInvaderSpeed = Config.Bind("Invaders", "Speed Correction", true, "Correct/Decrease the Invader Speed to make towers less likely to miss");
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
}