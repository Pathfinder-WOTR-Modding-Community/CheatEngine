using CheatEngine.Util;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.BundlesLoading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace CheatEngine
{
  /// <summary>
  /// Tracks all blueprints in the game. 
  /// 
  /// 
  /// </summary>
  /// 
  /// <remarks>
  /// <para>
  /// One limitation of the current approach: we are not actually loading into BlueprintsCache which means we actually
  /// have a different copy of blueprints loaded from the pack file.
  /// </para>
  /// 
  /// <para>
  /// One benefit of the current approach: we can actually diff blueprints as they exist in the game and blueprints as
  /// they exist in the base game.
  /// </para>
  /// </remarks>
  public class BlueprintLibrary
  {
    private static readonly ModLogger Logger = Logging.GetLogger(nameof(BlueprintLibrary));

    private static readonly ConcurrentDictionary<BlueprintGuid, SimpleBlueprint> LoadedBlueprints = new();

    private static SimpleBlueprint[] BaseBlueprints;
    private static Thread MainThread;

    private static void LoadBlueprints()
    {
      try
      {
        Logger.Log($"Starting Blueprint load.");

        MainThread = new Thread(new ThreadStart(LoadMain));
        MainThread.Start();
      }
      catch (Exception e)
      {
        Logger.LogException("BlueprintLibrary.LoadBlueprints", e);
      }
    }

    private const int NumThreads = 4;
    private static void LoadMain()
    {
      try
      {
        var timer = Stopwatch.StartNew();
        List<uint> cacheEntries = new();
        using (
          var packFile =
            new FileStream(BundlesLoadService.BundlesPath("blueprints-pack.bbp"), FileMode.Open, FileAccess.Read))
        {
          byte[] guidBuffer = new byte[16];
          using (var reader = new BinaryReader(packFile, Encoding.UTF8, leaveOpen: true))
          {
            int size = reader.ReadInt32();
            for (int i = 0; i < size; i++)
            {
              reader.Read(guidBuffer, 0, 16);
              cacheEntries.Add(reader.ReadUInt32());
            }
          }
        }

        Logger.Log($"Loading {cacheEntries.Count} blueprints.");
        BaseBlueprints = new SimpleBlueprint[cacheEntries.Count];

        List<Thread> children = new();
        var partitionSize = cacheEntries.Count / NumThreads;
        Parallel.For(0, NumThreads, thread =>
        {
          var startIndex = thread * partitionSize;
          var length = thread < NumThreads - 1 ? partitionSize : cacheEntries.Count - startIndex;
          LoadChild(cacheEntries.GetRange(startIndex, length).ToArray(), startIndex, thread);
        });

        foreach (var bp in BaseBlueprints)
        {
          if (bp is null)
            continue;
          LoadedBlueprints.TryAdd(bp.AssetGuid, bp);
        }
        timer.Stop();

        Logger.Log($"Finished loading {cacheEntries.Count} blueprints in {timer.Elapsed}");
      }
      catch (Exception e)
      {
        Logger.LogException("BlueprintLibrary.LoadMain", e);
      }
    }

    private static void LoadChild(uint[] offsets, int startIndex, int threadNum)
    {
      try
      {
        Logger.Log($"{threadNum}: Loading {offsets.Length} blueprints");

        using (
          var packFile =
            new FileStream(BundlesLoadService.BundlesPath("blueprints-pack.bbp"), FileMode.Open, FileAccess.Read))
        {
          using (var memoryStream = new MemoryStream())
          {
            packFile.CopyTo(memoryStream);
            using (var reader = new BinaryReader(memoryStream))
            {
              var serializer =
                new ReflectionBasedSerializer(new PrimitiveSerializer(reader, UnityObjectConverter.AssetList));
              int index = startIndex;
              for (int i = 0; i < offsets.Length; i++)
              {
                memoryStream.Seek(offsets[i], SeekOrigin.Begin);
                serializer.Blueprint(ref BaseBlueprints[startIndex + i]);
                if (i % 100 == 0)
                  Thread.Sleep(10);
              }
            }
          }
        }

        Logger.Log($"{threadNum}: Done loading blueprints");
      }
      catch (Exception e)
      {
        Logger.LogException($"BlueprintLibrary.LoadChild-{threadNum}", e);
      }
    }

    [HarmonyPatch(typeof(BlueprintsCache))]
    static class BlueprintsCache_Patch
    {
      [HarmonyPatch(nameof(BlueprintsCache.AddCachedBlueprint)), HarmonyPrefix]
      static void AddCachedBlueprint(BlueprintGuid guid, SimpleBlueprint bp)
      {
        try
        {
          LoadedBlueprints.TryAdd(guid, bp);
        }
        catch (Exception e)
        {
          Logger.LogException("BlueprintsCache_Patch.AddCachedBlueprint", e);
        }
      }
    }

    private static Stopwatch LoadToMenu;
    [HarmonyPatch(typeof(StartGameLoader))]
    static class StartGameLoader_Patch
    {
      [HarmonyPatch(nameof(StartGameLoader.LoadDirectReferencesList)), HarmonyPostfix]
      static void LoadDirectReferencesList()
      {
        try
        {
          LoadToMenu = Stopwatch.StartNew();
          LoadBlueprints();
        }
        catch (Exception e)
        {
          Logger.LogException("StartGameLoader_Patch.LoadDirectReferencesList", e);
        }
      }

      [HarmonyPatch(nameof(StartGameLoader.LoadPackTOC)), HarmonyPostfix]
      static void LoadPackTOC()
      {
        try
        {
          LoadToMenu.Stop();
          Logger.Log($"Time to load to menu: {LoadToMenu.Elapsed}");
        }
        catch (Exception e)
        {
          Logger.LogException("StartGameLoader_Patch.LoadPackTOC", e);
        }
      }
    }
  }
}
