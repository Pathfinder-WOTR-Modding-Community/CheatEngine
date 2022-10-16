using HarmonyLib;
using static UnityModManagerNet.UnityModManager.ModEntry;
using System;
using UnityModManagerNet;
using CheatEngine.Util;

namespace CheatEngine
{
  public static class Main
  {
    private static readonly ModLogger Logger = Logging.GetLogger(nameof(Main));

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
      try
      {
        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();

        Logger.Log("Finished patching.");
      }
      catch (Exception e)
      {
        Logger.LogException("Failed to patch", e);
      }
      return true;
    }
  }
}
