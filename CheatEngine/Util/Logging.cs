using System.Collections.Generic;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace CheatEngine.Util
{
  public static class Logging
  {
    private const string BaseChannel = "Cheats";

    private static readonly Dictionary<string, ModLogger> Loggers = new();

    public static ModLogger GetLogger(string channel)
    {
      if (Loggers.ContainsKey(channel))
      {
        return Loggers[channel];
      }
      var logger = new ModLogger($"{BaseChannel}+{channel}");
      Loggers[channel] = logger;
      return logger;
    }
  }
}
