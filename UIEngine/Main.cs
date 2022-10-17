using HarmonyLib;
using System;
using static UnityModManagerNet.UnityModManager.ModEntry;
using UnityModManagerNet;
using UIEngine.Util;

namespace UIEngine
{
  public class Main
  {
    private static readonly ModLogger Logger = Logging.GetLogger("Main.Map");

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
