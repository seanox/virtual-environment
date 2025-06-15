using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal class Registry
    {
        internal static char PathSeparatorChar = '\\';
        internal static char ValueNameSeparatorChar = ':';
        
        internal static readonly Regex RegistryKeyPatttern = new Regex(
                @"(HKEY_CLASSES_ROOT|HKCR"
                + @"|HKEY_CURRENT_USER|HKCU"
                + @"|HKEY_LOCAL_MACHINE|HKLM" 
                + @"|HKEY_USERS|HKU"
                + @"|HKEY_CURRENT_CONFIG|HKCC)"
                + @"(?:\\((?:[^\x00-\x1F:\\]+)"
                + @"(?:\\[^\x00-\x1F:\\]+)*))?"
                + @"(?::(\w[^\x00-\x1F]*\w))?",
            RegexOptions.IgnoreCase);
        
        internal enum RootClass
        {
            HKEY_CLASSES_ROOT, HKCR,
            HKEY_CURRENT_USER, HKCU,
            HKEY_LOCAL_MACHINE, HKLM,
            HKEY_USERS, HKU,
            HKEY_CURRENT_CONFIG, HKCC
        }

        internal static bool Exists(RootClass registryRootClass, string registryKey, string valueName = null)
        {
            // TODO:
            return false;
        }
    }
}