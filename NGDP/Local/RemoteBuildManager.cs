using System.Collections.Generic;

namespace NGDP.Local
{
    public static class RemoteBuildManager
    {
        public static Dictionary<string, BuildInfo> Builds { get; } = new Dictionary<string, BuildInfo>();

        public static bool IsBuildKnown(string buildName) => Builds.ContainsKey(buildName.Trim());

        public static void AddBuild(BuildInfo build) => Builds.Add(build.VersionName.Trim(), build);

        public static BuildInfo GetBuild(string buildName) => Builds[buildName.Trim()];

        public static void ClearExpiredBuilds()
        {
            foreach (var build in Builds)
                build.Value.Unload();
        }
    }
}
