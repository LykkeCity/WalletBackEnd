
using System.Collections.Generic;


namespace Common.Platforms
{
    public static class WindowsVersions
    {
        private static readonly Dictionary<string, string> Versions = new Dictionary<string, string>
        {
            {"10.0","10"},
            {"6.3","8.1"},
            {"6.2","8"},
            {"6.1","7"},
            {"6.0","Vista"},
            {"5.2","XP Pro x64"},
            {"5.1","XP"},
            {"4.0","NT"},

        };

        public static string GetExtendedInfo(string version)
        {
            if (Versions.ContainsKey(version))
                return Versions[version];

            return version;
        }
    }
}
