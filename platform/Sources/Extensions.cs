using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualEnvironment.Platform
{
    internal partial class Resources
    {
        internal static ResourceFiles Files { get; } = new ResourceFiles();
        internal static ResourceTexts Texts { get; } = new ResourceTexts();
        
        private static byte[] GetResource(string resourceName)
        {
            resourceName = $"{typeof(Resources).Namespace}.Resources.{resourceName}";
            resourceName = new Regex("[\\./\\\\]+").Replace(resourceName, ".");
            resourceName = new Regex("\\s").Replace(resourceName, "_");
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
        }

        internal class ResourceFiles
        {
            internal byte[] this[string resourceName] =>
                GetResource(resourceName);
        }

        internal class ResourceTexts
        {
            internal string this[string resourceName] =>
                Encoding.UTF8.GetString(GetResource(resourceName));
        }
    }
}