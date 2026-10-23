using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using MoonCleaners;

// Le jeu stocke outils, conteneurs et arbre d'upgrades par joueur.
// Côté hôte : chaque lecture renvoie le meilleur niveau possédé par n'importe
// quel joueur (validation des achats), et l'état envoyé aux clients via la
// synchro "truth" est l'état fusionné. Les clients sans mod se recalent dessus.
// La sauvegarde de l'hôte n'est pas modifiée.
[BepInPlugin("chachigo.moonsweepers.sharedupgrades", "Shared Upgrades", "1.2.0")]
public class SharedUpgrades : BaseUnityPlugin
{
    static readonly MethodInfo UpgradePlayers = AccessTools.Method(typeof(MoonWorld), "UpgradePlayers");
    static readonly MethodInfo PlayerUpgradeState = AccessTools.Method(typeof(MoonWorld), "PlayerUpgradeState");
    static readonly FieldInfo[] Maps = new[] { "tools", "containers", "tree" }
        .Select(n => AccessTools.Field(PlayerUpgradeState.ReturnType, n)).ToArray();

    [ThreadStatic] static bool busy;

    void Awake() => Harmony.CreateAndPatchAll(typeof(SharedUpgrades));

    static IEnumerable<string> Players(MoonWorld w) => (IEnumerable<string>)UpgradePlayers.Invoke(w, null);

    // ponytail: UpgradePlayers() alloue un SortedSet à chaque lecture ; mettre en cache si ça se voit au profiler.
    static int Max(MoonWorld w, int result, Func<string, int> level)
    {
        if (busy) return result;
        busy = true;
        try { foreach (var p in Players(w)) result = Math.Max(result, level(p)); }
        finally { busy = false; }
        return result;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), nameof(MoonWorld.ToolLevel))]
    static void Tool(MoonWorld __instance, string id, ref int __result) =>
        __result = Max(__instance, __result, p => __instance.ToolLevel(id, p));

    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), nameof(MoonWorld.ContainerTier))]
    static void Container(MoonWorld __instance, string toolId, ref int __result) =>
        __result = Max(__instance, __result, p => __instance.ContainerTier(p, toolId));

    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), nameof(MoonWorld.TreeLevel))]
    static void Tree(MoonWorld __instance, string id, ref int __result) =>
        __result = Max(__instance, __result, p => __instance.TreeLevel(p, id));

    // Les joueurs connectés qui n'ont encore rien acheté doivent aussi recevoir l'état partagé.
    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), "UpgradePlayers")]
    static void Roster(ref IEnumerable<string> __result) =>
        __result = __result.Union(MoonSteamLobby.Members.Select(m => MoonCrew.PlayerId(m.id))).ToList();

    static readonly AccessTools.FieldRef<MoonWorld, Dictionary<string, MoonOwnedVehicle>> Owned =
        AccessTools.FieldRefAccess<MoonWorld, Dictionary<string, MoonOwnedVehicle>>("ownedVehicles");

    // Améliorations de véhicules : tous les véhicules d'un même modèle prennent le meilleur niveau.
    // OwnedChanged() suit chaque achat/amélioration/chargement ; la liste envoyée aux clients en découle.
    // Contrairement au reste, ceci écrit dans la sauvegarde.
    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), "OwnedChanged")]
    static void Vehicles(MoonWorld __instance)
    {
        foreach (var model in Owned(__instance).Values.GroupBy(v => v.vehicle))
        {
            var best = new byte[7];
            foreach (var v in model)
                for (int i = 0; i < best.Length && i < (v.upgrades?.Length ?? 0); i++) best[i] = Math.Max(best[i], v.upgrades[i]);
            foreach (var v in model) v.upgrades = (byte[])best.Clone();
        }
    }

    // État envoyé aux clients (synchro truth) = fusion de tous les joueurs.
    [HarmonyPostfix, HarmonyPatch(typeof(MoonWorld), "PlayerUpgradeState")]
    static void State(MoonWorld __instance, object __result)
    {
        if (busy) return;
        busy = true;
        try
        {
            foreach (var p in Players(__instance))
            {
                var other = PlayerUpgradeState.Invoke(__instance, new object[] { p });
                foreach (var map in Maps)
                {
                    var dst = (SortedDictionary<string, int>)map.GetValue(__result);
                    foreach (var kv in (SortedDictionary<string, int>)map.GetValue(other))
                        if (!dst.TryGetValue(kv.Key, out var v) || v < kv.Value) dst[kv.Key] = kv.Value;
                }
            }
        }
        finally { busy = false; }
    }
}
