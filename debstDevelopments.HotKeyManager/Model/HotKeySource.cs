using debstDevelopments.HotKeyManager.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace debstDevelopments.HotKeyManager
{
    internal class HotKeySource : IDisposable
    {
        private Dictionary<int, KeyboardShortcutAction> RegisteredActions = new Dictionary<int, KeyboardShortcutAction>();
        internal IntPtr WindowHandle { get; }
        private HwndSource _Source;
        internal HwndSource Source { get { return _Source; } }
        internal int ActionCount { get { return RegisteredActions.Count; } }

        internal HotKeySource(IntPtr windowHandle)
        {
            this.WindowHandle = windowHandle;
            _Source = HwndSource.FromHwnd(this.WindowHandle);
        }

        internal bool Register(KeyboardShortcutAction action)
        {
            UnregisterActionIfExists(action.Id);

            var win32Key = KeyInterop.VirtualKeyFromKey(action.Shortcut.Key);
            // Modifier keys codes: Alt = 1, Ctrl = 2, Shift = 4, Win = 8
            // Compute the addition of each combination of the keys you want to be pressed
            // ALT+CTRL = 1 + 2 = 3 , CTRL+SHIFT = 2 + 4 = 6...
            if (User32.RegisterHotKey(this.WindowHandle, action.Id, action.Modifiers(), (int)win32Key))
            {
                RegisteredActions[action.Id] = action;
                //MessageBox.Show($"Cannot use hotkey {action.Shortcut.ToString()}, probably windows or other application is using it.");
                return true;
            }
            return false;
        }

        internal void UnregisterActionIfExists(int actionId)
        {
            if (RegisteredActions.ContainsKey(actionId))
            {
                Unregister(actionId);
            }
        }

        internal void Unregister(int actionId)
        {
            User32.UnregisterHotKey(this.WindowHandle, actionId);
            RegisteredActions.Remove(actionId);
        }

        internal void UnregisterAll()
        {
            foreach (var action in RegisteredActions.Values.ToList())
            {
                Unregister(action.Id);
            }
        }

        internal bool ExecuteActionIfExists(int actionId)
        {
            if (RegisteredActions.ContainsKey(actionId))
            {
                var action = RegisteredActions[actionId];
                action.Action.Invoke();
                return true;
            }
            return false;
        }

        internal KeyboardShortcutAction GetActionByShortcut(KeyboardShortcut shortcut)
        {
            return RegisteredActions.Values.FirstOrDefault((hotKey) => hotKey.Shortcut.ToString() == shortcut.ToString());
        }

        public void Dispose()
        {
            _Source = null;
        }
    }
}
