using System;
using System.Diagnostics;
using debstDevelopments.Common;
namespace debstDevelopments.HotKeyManager
{
    public static class ExtensionsOfProcess
    {
        public static string GetFileName(this Process p)
        {
            try
            {
                return p.MainModule.FileName.ToFileInfo().Name;
            }
            catch (Exception ex)
            {
                return p.ProcessName;
            }
        }
    }
}
