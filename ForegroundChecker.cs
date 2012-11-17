﻿using System;
using System.Threading;

using YetAnotherRelogger.Helpers;
using YetAnotherRelogger.Helpers.Tools;

namespace YetAnotherRelogger
{
    public sealed class ForegroundChecker
    {
        #region singleton
        static readonly ForegroundChecker instance = new ForegroundChecker();

        static ForegroundChecker()
        {
        }

        ForegroundChecker()
        {
        }

        public static ForegroundChecker Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        private Thread _fcThread;

        public void Start()
        {
            if (_fcThread != null)
                _fcThread.Abort();

            _fcThread = new Thread(new ThreadStart(ForegroundCheckerWorker));
            _fcThread.IsBackground = true;
            _fcThread.Start();
        }
        public void Stop()
        {
            _fcThread.Abort();
        }

        private IntPtr _lastDiablo;
        private IntPtr _lastDemonbuddy;
        private void ForegroundCheckerWorker()
        {
            try
            {
                while (true)
                {
                    var bots = BotSettings.Instance.Bots;
                    var hwnd = WinAPI.GetForegroundWindow();

                    if (_lastDemonbuddy != hwnd && _lastDiablo != hwnd)
                    {
                        _lastDemonbuddy = _lastDiablo = IntPtr.Zero;
                        foreach (var bot in bots)
                        {
                            if (!bot.IsStarted || !bot.IsRunning || !bot.Diablo.IsRunning || !bot.Demonbuddy.IsRunning)
                                continue;
                            if (bot.Diablo.Proc.MainWindowHandle != hwnd) 
                                continue;

                            _lastDiablo = bot.Diablo.Proc.MainWindowHandle;
                            _lastDemonbuddy = bot.Demonbuddy.Proc.MainWindowHandle;
                            Logger.Instance.WriteGlobal("<{0}> Diablo:{1}: has focus. Bring attached Demonbuddy to front", bot.Name, bot.Diablo.Proc.Id);
                                    
                            WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ForceMinimized);
                            Thread.Sleep(300);
                            WinAPI.ShowWindow(_lastDemonbuddy, WinAPI.WindowShowStyle.ShowNormal);
                            Thread.Sleep(300);
                            WinAPI.ShowWindow(_lastDiablo, WinAPI.WindowShowStyle.ShowNormal);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch
            {
                Thread.Sleep(5000);
                ForegroundCheckerWorker();
            }
        }
    }
}
