using System;

namespace YetAnotherRelogger.Helpers.Bot
{
    public class BotStats
    {
        public int Coinage;
        public bool IsInGame;
        public bool IsLoadingWorld;
        public bool IsPaused;
        public bool IsRunning;
        public long LastGame;
        public long LastPulse;
        public long LastRun;
        public int Pid;
        public long PluginPulse;

        public void Reset()
        {
            LastGame = PluginPulse = LastPulse = LastRun = DateTime.UtcNow.Ticks;
            IsPaused = IsRunning = IsInGame = false;
            Coinage = 0;
        }
    }
}