// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using osu.Desktop.IPC;
using osu.Desktop.Security;
using osu.Framework.Platform;
using osu.Game;
using osu.Desktop.Updater;
using osu.Framework;
using osu.Framework.Logging;
using osu.Game.Updater;
using osu.Desktop.MacOS;
using osu.Desktop.Windows;
using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Game.IO;
using osu.Game.IPC;
using osu.Game.Performance;
using osu.Game.Utils;

namespace osu.Desktop
{
    internal partial class OsuGameDesktop : OsuGame
    {
        private OsuSchemeLinkIPCChannel? osuSchemeLinkIPCChannel;
        private ArchiveImportIPCChannel? archiveImportIPCChannel;

        [Cached(typeof(IHighPerformanceSessionManager))]
        private readonly HighPerformanceSessionManager highPerformanceSessionManager = new HighPerformanceSessionManager();

        public bool IsFirstRun { get; init; }

        public bool EnableWebSocketServer { get; init; }

        public OsuGameDesktop(string[]? args = null)
            : base(args)
        {
        }

        protected override Storage CreateStorage(GameHost host, Storage defaultStorage)
        {
            seedSettingsFromOfficialInstall(defaultStorage);
            mirrorRealLazer(defaultStorage);
            return base.CreateStorage(host, defaultStorage);
        }

        /// <summary>
        /// Personal fork: this build uses its own data folder, so on first run copy the <i>settings files</i> (not beatmaps, skins or scores)
        /// from the official lazer data folder, which lives next to this one and is named "osu".
        /// Login details and the selected skin are intentionally not copied.
        /// </summary>
        private void seedSettingsFromOfficialInstall(Storage target)
        {
            try
            {
                string targetPath = Path.GetFullPath(target.GetFullPath(".")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string? parent = Path.GetDirectoryName(targetPath);

                if (parent == null)
                    return;

                string sourcePath = Path.Combine(parent, "osu");

                if (!Directory.Exists(sourcePath) || string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                    return;

                string targetGameConfig = UseDevelopmentServer ? "game.dev.ini" : "game.ini";

                // only ever seed once; after that this build owns its settings.
                if (File.Exists(Path.Combine(targetPath, targetGameConfig)))
                    return;

                Directory.CreateDirectory(targetPath);

                string sourceGameConfig = Path.Combine(sourcePath, "game.ini");

                if (File.Exists(sourceGameConfig))
                {
                    string[] privateKeys = { "Token", "Username", "Skin" };

                    var lines = File.ReadAllLines(sourceGameConfig)
                                    .Where(l => !privateKeys.Contains(l.Split('=')[0].Trim()));

                    File.WriteAllLines(Path.Combine(targetPath, targetGameConfig), lines);
                }

                foreach (string name in new[] { "framework.ini", "input.json" })
                {
                    string source = Path.Combine(sourcePath, name);
                    string destination = Path.Combine(targetPath, name);

                    if (File.Exists(source) && !File.Exists(destination))
                        File.Copy(source, destination);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Could not copy settings from the official install: {e.Message}", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);

        /// <summary>
        /// Personal fork: when the "use real osu!lazer maps and settings" marker exists, refresh this build's data folder from the official one on every start.
        /// The official folder is strictly a read-only source: nothing is ever written to, moved or deleted in it.
        /// </summary>
        private void mirrorRealLazer(Storage target)
        {
            try
            {
                string targetPath = Path.GetFullPath(target.GetFullPath(".")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string flagPath = Path.Combine(targetPath, osu.Game.Screens.Select.Stable.StableUi.REAL_LAZER_FLAG);

                // "on" = copy now; "done" = already copied once, from here on this build keeps its own changes (skin, settings, maps).
                if (!File.Exists(flagPath) || File.ReadAllText(flagPath).Trim() == "done")
                    return;

                string? parent = Path.GetDirectoryName(targetPath);
                if (parent == null) return;

                string sourcePath = Path.Combine(parent, "osu");

                if (!Directory.Exists(sourcePath) || string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                    return;

                string sourceRealm = Path.Combine(sourcePath, "client.realm");
                if (!File.Exists(sourceRealm)) return;

                string targetRealm = Path.Combine(targetPath, "client.realm");

                // keep the previous database around once, in case the mirror is ever not wanted.
                string backup = targetRealm + ".before-real-lazer";
                if (File.Exists(targetRealm) && !File.Exists(backup))
                    File.Copy(targetRealm, backup);

                // copy to a temporary name first so a failed copy never leaves a broken database behind.
                string temp = targetRealm + ".mirror-tmp";
                copyShared(sourceRealm, temp);

                foreach (string stale in new[] { targetRealm + ".lock", targetRealm + ".note" })
                {
                    if (File.Exists(stale)) File.Delete(stale);
                }

                string management = targetRealm + ".management";
                if (Directory.Exists(management)) Directory.Delete(management, true);

                File.Move(temp, targetRealm, true);

                // settings: take everything from the real install except what makes this build different.
                string gameConfigName = UseDevelopmentServer ? "game.dev.ini" : "game.ini";
                string sourceGameConfig = Path.Combine(sourcePath, "game.ini");
                string targetGameConfig = Path.Combine(targetPath, gameConfigName);

                if (File.Exists(sourceGameConfig))
                {
                    string[] keepOwn = { "UseStableUI", "Token", "Username" };

                    string[] own = File.Exists(targetGameConfig) ? File.ReadAllLines(targetGameConfig) : Array.Empty<string>();

                    var lines = File.ReadAllLines(sourceGameConfig)
                                    .Where(l => !keepOwn.Contains(l.Split('=')[0].Trim()))
                                    .Concat(own.Where(l => keepOwn.Contains(l.Split('=')[0].Trim())))
                                    .ToList();

                    File.WriteAllLines(targetGameConfig, lines);
                }

                foreach (string name in new[] { "framework.ini", "input.json" })
                {
                    string source = Path.Combine(sourcePath, name);
                    if (File.Exists(source)) copyShared(source, Path.Combine(targetPath, name));
                }

                // beatmap, skin and other stored files: link (or copy when linking is impossible) the ones that are missing.
                string sourceFiles = Path.Combine(sourcePath, "files");
                string targetFiles = Path.Combine(targetPath, "files");

                if (Directory.Exists(sourceFiles))
                {
                    int linked = 0, copied = 0;

                    foreach (string file in Directory.EnumerateFiles(sourceFiles, "*", SearchOption.AllDirectories))
                    {
                        string destination = Path.Combine(targetFiles, Path.GetRelativePath(sourceFiles, file));

                        if (File.Exists(destination)) continue;

                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                        if (OperatingSystem.IsWindows() && CreateHardLink(destination, file, IntPtr.Zero))
                            linked++;
                        else
                        {
                            copyShared(file, destination);
                            copied++;
                        }
                    }

                    Logger.Log($"Mirrored real osu!lazer data: {linked} files linked, {copied} copied.", LoggingTarget.Runtime, LogLevel.Important);
                }

                File.WriteAllText(flagPath, "done");
            }
            catch (Exception e)
            {
                Logger.Log($"Could not mirror the real osu!lazer data: {e}", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private static void copyShared(string source, string destination)
        {
            // open the source read-only and allow other processes to keep using it.
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                input.CopyTo(output);
        }

        public override StableStorage? GetStorageForStableInstall()
        {
            try
            {
                if (Host is DesktopGameHost desktopHost)
                {
                    string? stablePath = getStableInstallPath();
                    if (!string.IsNullOrEmpty(stablePath))
                        return new StableStorage(stablePath, desktopHost);
                }
            }
            catch (Exception)
            {
                Logger.Log("Could not find a stable install", LoggingTarget.Runtime, LogLevel.Important);
            }

            return null;
        }

        private string? getStableInstallPath()
        {
            static bool checkExists(string p) => Directory.Exists(Path.Combine(p, "Songs")) || File.Exists(Path.Combine(p, "osu!.cfg"));

            string? stableInstallPath;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    stableInstallPath = getStableInstallPathFromRegistry("osustable.File.osz");

                    if (!string.IsNullOrEmpty(stableInstallPath) && checkExists(stableInstallPath))
                        return stableInstallPath;

                    stableInstallPath = getStableInstallPathFromRegistry("osu!");

                    if (!string.IsNullOrEmpty(stableInstallPath) && checkExists(stableInstallPath))
                        return stableInstallPath;
                }
                catch
                {
                }
            }

            stableInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"osu!");
            if (checkExists(stableInstallPath))
                return stableInstallPath;

            stableInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".osu");
            if (checkExists(stableInstallPath))
                return stableInstallPath;

            return null;
        }

        [SupportedOSPlatform("windows")]
        private string? getStableInstallPathFromRegistry(string progId)
        {
            using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(progId))
                return key?.OpenSubKey(WindowsAssociationManager.SHELL_OPEN_COMMAND)?.GetValue(string.Empty)?.ToString()?.Split('"')[1].Replace("osu!.exe", "");
        }

        public static bool IsPackageManaged => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OSU_EXTERNAL_UPDATE_PROVIDER"));

        protected override UpdateManager CreateUpdateManager()
        {
            // If this is the first time we've run the game, ie it is being installed,
            // reset the user's release stream specified by the installation target.
            //
            // This ensures that if a user is trying to recover from a failed startup, it will keep them
            // on the stream which is imminently being reinstalled.
            if (IsFirstRun)
                LocalConfig.SetValue(OsuSetting.ReleaseStream, Version.Contains("-tachyon") ? ReleaseStream.Tachyon : ReleaseStream.Lazer);

            if (IsPackageManaged)
                return new NoActionUpdateManager();

            return new VelopackUpdateManager();
        }

        public override bool RestartAppWhenExited()
        {
            if (IsPackageManaged || !IsDeployedBuild)
                return false;

            RestartOnExitAction = () => Velopack.UpdateExe.Start(waitPid: (uint)Environment.ProcessId);
            return true;
        }

        protected override void LoadComplete()
        {
            // this is done before `base.LoadComplete` so that the game can immediately register data sources.
            if (EnableWebSocketServer)
            {
                var provider = new OsuWebSocketProvider();
                Add(provider);
                Dependencies.CacheAs<IWebSocketProvider>(provider);
            }

            base.LoadComplete();

            LoadComponentAsync(new DiscordRichPresence(), Add);

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    LoadComponentAsync(new GameplayWinKeyBlocker(), Add);
                    break;

                case RuntimeInfo.Platform.macOS when !IsPackageManaged && IsDeployedBuild:
                    if (!IsPackageManaged && IsDeployedBuild)
                        LoadComponentAsync(new MacOSAppLocationChecker(), Add);
                    break;
            }

            LoadComponentAsync(new ElevatedPrivilegesChecker(), Add);

            osuSchemeLinkIPCChannel = new OsuSchemeLinkIPCChannel(Host, this);
            archiveImportIPCChannel = new ArchiveImportIPCChannel(Host, this);
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            // Apple operating systems use a better icon provided via external assets.
            if (!RuntimeInfo.IsApple)
            {
                var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), "lazer.ico");
                if (iconStream != null)
                    host.Window.SetIconFromStream(iconStream);
            }

            host.Window.Title = Name;
        }

        protected override BatteryInfo CreateBatteryInfo() => FrameworkEnvironment.UseSDL3 ? new SDL3BatteryInfo() : new SDL2BatteryInfo();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            osuSchemeLinkIPCChannel?.Dispose();
            archiveImportIPCChannel?.Dispose();
        }
    }
}
