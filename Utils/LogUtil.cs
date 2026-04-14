using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.InfiniteBackups.Utils {
    public static class LogUtil {
        public static void Log(string text, LogLevel logLevel = LogLevel.Verbose) {
            Logger.Log(logLevel, InfiniteBackupsModule.LoggerTagName, text);
#if DEBUG
            if (InfiniteBackupsModule.Settings.LogToIngameConsole) {
                Color color;
                switch (logLevel) {
                    case LogLevel.Warn:
                        color = Color.Yellow;
                        break;
                    case LogLevel.Error:
                        color = Color.Red;
                        break;
                    default:
                        color = Color.Cyan;
                        break;
                }
                try {
                    Engine.Commands?.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{InfiniteBackupsModule.LoggerTagName}] {logLevel}: {text}", color);
                } catch (Exception) {
                    // ignored
                }
            }
#endif
        }
    }
}
