using CheatEngine.Util;
using HarmonyLib;
using Kingmaker;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace CheatEngine
{
#if DEBUG
  internal class SearchTest
  {
    private static readonly ModLogger Logger = Logging.GetLogger(nameof(SearchTest));

    [HarmonyPatch(typeof(Player))]
    static class Player_Patch
    {
      private static bool Searched = false;

      [HarmonyPatch(nameof(Player.ApplyUpgrades)), HarmonyPostfix]
      static void PostLoad()
      {
        try
        {
          if (Searched)
            return;
          Searched = true;

          Task.Run(
            () =>
            {
              while (!BlueprintLibrary.ReadyForSearch())
                Thread.Sleep(1000);
              Search();
            });
        }
        catch (Exception e)
        {
          Logger.LogException("Player.ApplyUpgrades", e);
        }
      }

      private static void Search()
      {
        try
        {
          var nameSearch = "(Cleric|EldritchHeritage)"; // EldritchHeritage is from COP
          Logger.Log($"Searching for {nameSearch}");
          var stopwatch = Stopwatch.StartNew();
          var results = BlueprintLibrary.SearchByName(nameSearch);
          var count = results.Count(); // Make sure to actually execute!
          stopwatch.Stop();
          Logger.Log($"Search finished in {stopwatch.Elapsed} with {count} results");
          foreach (var bp in results)
            Logger.NativeLog($"Found: {bp.name} - {bp.GetType()}");

          var guidSearch = "abcd";
          Logger.Log($"Searching for {guidSearch}");
          stopwatch = Stopwatch.StartNew();
          results = BlueprintLibrary.SearchByGuid(guidSearch);
          count = results.Count();
          stopwatch.Stop();
          Logger.Log($"Search finished in {stopwatch.Elapsed} with {count} results");
          foreach (var bp in results)
            Logger.NativeLog($"Found: {bp.name} - {bp.GetType()}");

          var descriptionSearch = "competence bonus";
          Logger.Log($"Searching for {descriptionSearch}");
          stopwatch = Stopwatch.StartNew();
          results = BlueprintLibrary.SearchByDescription(descriptionSearch);
          count = results.Count();
          stopwatch.Stop();
          Logger.Log($"Search finished in {stopwatch.Elapsed} with {count} results");
          foreach (var bp in results)
            Logger.NativeLog($"Found: {bp.name} - {bp.GetType()}");
        }
        catch (Exception e)
        {
          Logger.LogException("Player.ApplyUpgrades", e);
        }
      }
    }
  }
#endif
}
