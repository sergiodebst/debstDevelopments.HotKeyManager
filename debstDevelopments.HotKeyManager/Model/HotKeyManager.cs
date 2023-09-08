using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using debstDevelopments.HotKeyManager.Native;

namespace debstDevelopments.HotKeyManager
{
    public static class HotKeyManager
    {
        private static Dictionary<IntPtr, HotKeySource> HotKeySources = new Dictionary<IntPtr, HotKeySource>();
        
        public static bool RegisterHotKey(IntPtr windowHandle, KeyboardShortcutAction action)
        {
            HotKeySource hotKeySource;
            if (!HotKeySources.TryGetValue(windowHandle, out hotKeySource))
            {
                hotKeySource = RegisterNewHotKeySource(windowHandle);
            }

            return hotKeySource.Register(action);
        }

        private static HotKeySource RegisterNewHotKeySource(IntPtr windowHandle)
        {

            var hotKeySource= new HotKeySource(windowHandle);
            hotKeySource.Source.AddHook(OnWin32Message);//This is made in order the WPF app be able to handle win32 messages
                                                        //1- We register the hotkeys in windows
                                                        //2- User press the keyboard shortkcut of the windows hotkey
                                                        //3- Windows send a message as the hotkey is invoked
                                                        //4- We recive the windows message and we handle it in HotKeyManager.OnWin32Message
                                                        //5- We check if the event is the execution of a hotkey (WM_HOTKEY), and if it is, we check if it is one of our shortcuts in order to execute it
            HotKeySources.Add(windowHandle, hotKeySource);
            return hotKeySource;
        }

        private static void UnregisterHotKeySource(HotKeySource hotKeySource)
        {
            hotKeySource.Source.RemoveHook(OnWin32Message);
            hotKeySource.Dispose();
            HotKeySources.Remove(hotKeySource.WindowHandle);
        }

        private static void UnregisterHotKey(IntPtr windowHandle, int actionId)
        {
            if (HotKeySources.ContainsKey(windowHandle))
            {
                var hotKeySource = HotKeySources[windowHandle];
                hotKeySource.Unregister(actionId);
                if (hotKeySource.ActionCount == 0)
                    UnregisterHotKeySource(hotKeySource);
            }
        }

        public static void UnregisterAllHotKeys(IntPtr windowHandle)
        {
            if (HotKeySources.ContainsKey(windowHandle))
            {
                var hotKeySource = HotKeySources[windowHandle];
                hotKeySource.UnregisterAll();
                UnregisterHotKeySource(hotKeySource);
            }
        }

        public static KeyboardShortcutAction CallActionIfRegistered(KeyboardShortcut shortcut)
        {
            foreach(var hotkeySource in HotKeySources.Values)
            {
                var action = hotkeySource.GetActionByShortcut(shortcut);
                if (action != null)
                {
                    action.Action.Invoke();
                    return action;
                }
            }
            return null;
        }

        public static bool WindowHasActionsRegistered(IntPtr windowHandle)
        {
            return HotKeySources.ContainsKey(windowHandle);
        }

        public static bool IsAnyHotkeyRegistered()
        {
            return HotKeySources.Count > 0;
        }

        public static IntPtr OnWin32Message(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //This method is a candidate to be moved somewhere else and do some refactoring
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && HotKeySources.ContainsKey(hwnd))
            {
                var hotKeySource = HotKeySources[hwnd];
                int actionId = wParam.ToInt32();
                if (hotKeySource.ExecuteActionIfExists(actionId))
                {
                    handled = true;
                }
                
            }
            return IntPtr.Zero;
        }
    }
}
