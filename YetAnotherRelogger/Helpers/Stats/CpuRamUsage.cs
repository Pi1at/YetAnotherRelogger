/* http://www.philosophicalgeek.com/2009/01/03/determine-cpu-usage-of-current-process-c-and-c/ */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace YetAnotherRelogger.Helpers.Stats
{
    public class CpuRamUsage
    {
        private bool _initialized;
        private FILETIME _lastSysIdle;
        private FILETIME _lastSysKernel;
        private FILETIME _lastSysUser;
        private HashSet<ProcUsage> _procUsageList;
        private bool glitchRecover;

        public CpuRamUsage()
        {
            TotalCpuUsage = 0;
            _procUsageList = new HashSet<ProcUsage>();
        }

        public double TotalCpuUsage { get; private set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime,
            out FILETIME lpUserTime);

        private bool Init()
        {
            return GetSystemTimes(out _lastSysIdle, out _lastSysKernel, out _lastSysUser);
        }

        public bool Update()
        {
            try
            {
                FILETIME sysIdle, sysKernel, sysUser;

                if (!_initialized)
                {
                    _initialized = Init();
                    return _initialized;
                }

                // Check if we can get current system cpu times
                if (!GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
                    return false;

                // Calculate tot system cpu time
                ulong sysKernelDiff = SubtractTimes(sysKernel, _lastSysKernel);
                ulong sysUserDiff = SubtractTimes(sysUser, _lastSysUser);
                ulong sysIdleDiff = SubtractTimes(sysIdle, _lastSysIdle);
                ulong sysTotal = sysKernelDiff + sysUserDiff;

                if (!validDiff((long) sysKernelDiff) || !validDiff((long) sysUserDiff) || !validDiff((long) sysIdleDiff))
                {
                    //Debug.WriteLine("Stats: Negative Tick Difference");
                    //Debug.WriteLine("kernel: {0,-20} :: {1,-20} Diff:{2,-20} :: {3} miliseconds", ((UInt64)(sysKernel.dwHighDateTime << 32)) | (UInt64)sysKernel.dwLowDateTime, ((UInt64)(_lastSysKernel.dwHighDateTime << 32)) | (UInt64)_lastSysKernel.dwLowDateTime, sysKernelDiff, TimeSpan.FromTicks((long)sysKernelDiff).TotalMilliseconds);
                    //Debug.WriteLine("user  : {0,-20} :: {1,-20} Diff:{2,-20} :: {3} miliseconds", ((UInt64)(sysUser.dwHighDateTime << 32)) | (UInt64)sysUser.dwLowDateTime, ((UInt64)(_lastSysUser.dwHighDateTime << 32)) | (UInt64)_lastSysUser.dwLowDateTime, sysUserDiff, TimeSpan.FromTicks((long)sysUserDiff).TotalMilliseconds);
                    //Debug.WriteLine("idle  : {0,-20} :: {1,-20} Diff:{2,-20} :: {3} miliseconds", ((UInt64)(sysIdle.dwHighDateTime << 32)) | (UInt64)sysIdle.dwLowDateTime, ((UInt64)(_lastSysIdle.dwHighDateTime << 32)) | (UInt64)_lastSysIdle.dwLowDateTime, sysIdleDiff, TimeSpan.FromTicks((long)sysIdleDiff).TotalMilliseconds);

                    glitchRecover = true; // mark to recover from glitch
                    _lastSysKernel = sysKernel;
                    _lastSysUser = sysUser;
                    _lastSysIdle = sysIdle;
                    Thread.Sleep(100); // give windows time to recover
                    return Update();
                }

                // Calculate total Cpu usage
                double totalUsage = sysTotal > 0 ? ((sysTotal - sysIdleDiff)*100d/sysTotal) : TotalCpuUsage;
                TotalCpuUsage = totalUsage < 0 ? TotalCpuUsage : totalUsage;

                var newList = new HashSet<ProcUsage>();
                foreach (Process proc in Process.GetProcesses())
                {
                    try
                    {
                        // Skip proc with id 0
                        if (proc.Id == 0)
                            continue;

                        Int64 procTotal;
                        double oldCpuUsage = 0d;
                        ProcUsage p = GetById(proc.Id);
                        if (p != null)
                        {
                            procTotal = proc.TotalProcessorTime.Ticks - p.LastProcTime.Ticks;
                            oldCpuUsage = p.Usage.Cpu;
                        }
                        else
                            procTotal = 0;

                        double usage = glitchRecover ? oldCpuUsage : ((100.0*procTotal)/sysTotal);
                            // Calculate process CPU Usage
                        // Add Process to list
                        newList.Add(new ProcUsage
                        {
                            Process = proc,
                            Usage = new Usage
                            {
                                Cpu = usage,
                                Memory = proc.PrivateMemorySize64
                            },
                            LastProcTime = proc.TotalProcessorTime
                        });
                    }
                    catch
                    {
                        continue;
                    }
                    Thread.Sleep(1); // be nice for cpu
                }

                // Update last system times
                _lastSysKernel = sysKernel;
                _lastSysUser = sysUser;
                _lastSysIdle = sysIdle;

                // unmark glitch recover
                if (glitchRecover)
                {
                    glitchRecover = false;
                    Update(); // Update again
                }

                // Update Process list
                _procUsageList = newList;
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal(ex.ToString());
                return false;
            }
            return true;
        }

        private bool validDiff(long ticks)
        {
            return TimeSpan.FromTicks(ticks).TotalMilliseconds > 0;
        }

        public ProcUsage GetById(int id)
        {
            try
            {
                return _procUsageList.FirstOrDefault(x => x.Process.Id == id);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Get Process CPU Usage
        /// </summary>
        /// <param name="id">Process Id</param>
        /// <returns>Cpu usage</returns>
        public Usage GetUsageById(int id)
        {
            ProcUsage p = GetById(id);
            return p != null ? p.Usage : null;
        }

        private static UInt64 SubtractTimes(FILETIME a, FILETIME b)
        {
            ulong aInt = ((UInt64) (a.dwHighDateTime << 32)) | (UInt64) a.dwLowDateTime;
            ulong bInt = ((UInt64) (b.dwHighDateTime << 32)) | (UInt64) b.dwLowDateTime;
            return aInt - bInt;
        }

        public class ProcUsage
        {
            public TimeSpan LastProcTime;
            public Process Process;
            public Usage Usage;

            public ProcUsage()
            {
                LastProcTime = TimeSpan.MinValue;
                Process = new Process();
            }
        }

        public class Usage
        {
            public double Cpu;
            public long Memory;
        }
    }
}