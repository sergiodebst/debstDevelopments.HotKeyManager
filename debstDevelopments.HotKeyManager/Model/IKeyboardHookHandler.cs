using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debstDevelopments.HotKeyManager.KeyboardManager;

namespace debstDevelopments.HotKeyManager
{
    public interface IKeyboardHookHandler
    {
        bool CanHandleHook { get; }
        void HandleHook(KeyEvent e);
    }
}
