using debstDevelopments.Common;
using debstDevelopments.HotKeyManager.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static debstDevelopments.HotKeyManager.Native.User32;

namespace debstDevelopments.HotKeyManager
{
    public static class WindowsManager
    {

        static WindowsManager()
        {
            MustHookFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MustHookFileName));
            var file = MustHookFile;
            MustHookFileWatcher = new FileSystemWatcher(file.DirectoryName, file.Name);
            MustHookFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            MustHookFileWatcher.Changed += OnMustHookFileChanged;
            MustHookFileWatcher.Created += OnMustHookFileChanged;
            MustHookFileWatcher.Deleted += OnMustHookFileChanged;
            MustHookFileWatcher.EnableRaisingEvents = true;
        }

        //https://stackoverflow.com/a/10280800
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private static readonly FileSystemWatcher MustHookFileWatcher;
        private static Process ProcessHooked;
        private static readonly string MustHookFileName = "hook";
        private static FileInfo MustHookFile { get; }
        private static string[] _MustHook;
        private static string[] MustHook
        {
            get
            {
                if (_MustHook == null && MustHookFile.ExistsNow())
                {
                    _MustHook = File.ReadAllLines(MustHookFile.FullName);
                }
                return _MustHook;
            }
        }

        private static bool IsKnowAppActive()
        {
            return HotKeyManager.WindowHasActionsRegistered(User32.GetForegroundWindow());
        }

        private static bool ShouldMonitorActiveAppChange()
        {
            return !IsKnowAppActive() && HotKeyManager.IsAnyHotkeyRegistered() && !MustHook.IsNullOrEmpty();
        }


        private static WinEventHookProc _handler; //Static so the GC does not clean it and the hook stop working
        private static IntPtr CurrentHook = IntPtr.Zero;
        public static void StartMonitoringActiveAppChangesIfNeeded()
        {
            if (ShouldMonitorActiveAppChange())
            {
                if(CurrentHook == IntPtr.Zero)
                {
                    _handler = new WinEventHookProc(OnActiveWindowChanged);
                    CurrentHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _handler, 0, 0, WINEVENT_OUTOFCONTEXT);
                }
                
                if (ProcessHooked == null)
                {
                    foreach (var p in Process.GetProcesses())
                    {
                        var fileName = p.GetFileName();
                        if (MustHook.Contains(fileName))
                        {
                            StartKeyboardHookOnProcess(p);
                            break;
                        }
                    }
                }
                else if (!MustHook.Contains(ProcessHooked.GetFileName()))
                {
                    StopKeyboardHook();
                }
            }else if(ProcessHooked != null)
            {
                StopKeyboardHook();
            }
        }

        public static void StopMonitoringActiveAppChanges()
        {
            if (CurrentHook != IntPtr.Zero)
            {
                UnhookWinEvent(CurrentHook);
                CurrentHook = IntPtr.Zero;
                StopKeyboardHook();
            }
        }

        private static void StartKeyboardHookOnProcess(Process p)
        {
            ProcessHooked = p;
            KeyboardManager.StartKeyboardHook(Application.Current.GetType().Module, null);
        }

        private static void StopKeyboardHook()
        {
            ProcessHooked = null;
            KeyboardManager.StopKeyboardHook();
        }

        private static void OnMustHookFileChanged(object sender, FileSystemEventArgs e)
        {
            _MustHook = null;
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (ShouldMonitorActiveAppChange())
                {
                    StartMonitoringActiveAppChangesIfNeeded();
                }
                else
                {
                    StopMonitoringActiveAppChanges();
                }
            }));
        }

        //This method is to detect an application that hooks the system messages and cancels our hotkey to be executed
        //When an app of this kind(this app are configured in the hook file) is detected we create a keyboardhook that gets the system messages before than
        //the conflictive app and we keep the hook active until we detect that application has exited.
        //Idealy we will activate the hook only when that application is active, but after some test I have noticed that when a conflictive app is starting the hook
        //works as expected, but if you change to another app and remove the hook, when you return and activate the conflictive app, this method is executed
        //once the application is already active and got eneught time to activate their hooks, so ours would be activate after theirs, but as they don't propagate
        //the system messages we dont get notified, and our hotkeys are not executed...
        //Because of that the first time a conflictive app is being activated, the hook is started and until the conflictive app is exited the hook will be there.
        //Another solution would be the use of SetWindowsHookEx instead of SetWinEventHook and use WH_CBT to be able to detect HCBT_ACTIVATE which seems to be executed
        //just before activate the window, but I would need to create a c++ DLL, because the only parameters allowed without a dll are WH_KEYBOARD_LL and WH_MOUSE_LL
        //Since the conflictive apps normaly are games executed in fullscreenwindow and normally when you play, you are not much things in the pc to have 
        //the hook activated while the game is running, does not seem the big deal
        private static void OnActiveWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                if (ProcessHooked == null)
                {
                    if (MustHook?.Length > 0)
                    {
                        uint pid;
                        GetWindowThreadProcessId(hwnd, out pid);
                        Process p = Process.GetProcessById((int)pid);
                        var activeProcessFileName = p.GetFileName();
                        if (MustHook.Contains(activeProcessFileName))
                        {
                            StartKeyboardHookOnProcess(p);
                        }
                    }
                }
                else if (ProcessHooked.HasExited)
                {
                    StopKeyboardHook();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
