using System;
using System.Reflection;

namespace FoxIDs
{
    public static class AssemblyExtensions
    {
        public static string GetDisplayVersion(this Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var displayVersion = GetInformationalDisplayVersion(informationalVersion);
            if (!string.IsNullOrWhiteSpace(displayVersion))
            {
                return displayVersion;
            }

            var version = assembly.GetName().Version;
            if (version == null)
            {
                return null;
            }

            var maxFields = version.Build >= 0 ? 3 : version.Minor >= 0 ? 2 : 1;
            return version.ToString(maxFields);
        }

        private static string GetInformationalDisplayVersion(string informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion))
            {
                return informationalVersion;
            }

            var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return plusIndex > 0 ? informationalVersion.Substring(0, plusIndex) : informationalVersion;
        }
    }
}
