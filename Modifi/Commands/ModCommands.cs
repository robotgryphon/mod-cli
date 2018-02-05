﻿using Modifi.Domains;
using Modifi.Mods;
using Modifi.Packs;
using Modifi.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modifi.Commands {

    public class ModCommandHandler {
        public enum ModActions {
            ADD,
            REMOVE,
            INFO,
            LIST,
            DOWNLOAD,
            VERSIONS
        }

        internal static void HandleModAction(IEnumerable<string> arguments) {

            ModActions? action = CommandHandler.ParseCommandOption<ModActions>(arguments.First());
            if (action == null) return;

            IEnumerable<string> mods = arguments.Skip(1);

            Pack pack = Modifi.DefaultPack;

            foreach (string mod in mods) {
                string domainName = ModHelper.GetDomainName(mod);
                IDomain handler = pack.GetDomain(domainName);

                if (handler == null || !(handler is IDomain)) {
                    Modifi.DefaultLogger.Error("No domain handler found for {0}. Aborting.", domainName);
                    return;
                }

                string modIdentifier = ModHelper.GetModIdentifier(mod);
                string modVersion = ModHelper.GetModVersion(mod);

                switch (action) {
                    case ModActions.ADD:
                        HandleModAdd(handler, modIdentifier, modVersion);
                        break;

                    case ModActions.REMOVE:
                        HandleModRemove(handler, modIdentifier);
                        break;

                    case ModActions.INFO:
                        HandleModInformation(handler, modIdentifier, modVersion);
                        break;

                    case ModActions.VERSIONS:
                        HandleModVersions(handler, modIdentifier);
                        break;

                    case ModActions.DOWNLOAD:
                        HandleModDownload(handler, modIdentifier, modVersion);
                        break;

                    default:
                        throw new Exception("Invalid mod action.");
                }
            }


        }

        /// <summary>
        /// Handles the mods versions {modid} command.
        /// </summary>
        /// <param name="domain">Domain handler to use for lookup.</param>
        /// <param name="modIdentifier">Mod to lookup versions for.</param>
        public static void HandleModVersions(IDomain domain, string modIdentifier) {
            IDomainHandler handler = domain.GetDomainHandler();

            ModMetadata meta = handler.GetModMetadata(Modifi.DefaultPack.MinecraftVersion, modIdentifier).Result;

            IEnumerable<ModVersion> latestVersions = handler.GetRecentVersions(meta).Result;

            // ModHelper.PrintModInformation(meta);
            foreach (ModVersion version in latestVersions) {
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(version.GetModVersion());
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("] ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(version.GetVersionName());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" (");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(version.GetReleaseType());
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(")");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        public static void HandleModAdd(IDomain domain, string modIdentifier, string modVersion = null) {

            IDomainHandler handler = domain.GetDomainHandler();
            ModMetadata meta = handler.GetModMetadata(Modifi.DefaultPack.MinecraftVersion, modIdentifier).Result;

            // Check mod installation status, error out if already requested/installed
            using (IModStorage storage = new ModStorage(Modifi.DefaultPack.Installed, domain)) {
                try {
                    ModStatus status = storage.GetModStatus(meta);
                    switch (status) {
                        case ModStatus.Requested:
                        case ModStatus.Installed:
                            Modifi.DefaultLogger.Error("Mod {0} is already marked as requested, or already installed.", meta.GetName());
                            return;

                        case ModStatus.Disabled:
                            Modifi.DefaultLogger.Information("Mod {0} is marked at disabled. Please either delete it, or re-enable it.");
                            return;
                    }
                }

                catch(Exception e) {
                    Modifi.DefaultLogger.Error(e.Message);
                    return;
                }
            }

            // If the version is already specified, don't ask, just add it
            if (!String.IsNullOrEmpty(modVersion)) {
                ModVersion v = handler.GetModVersion(meta, modVersion).Result;

                // Connect to storage and mark the mod as requested
                using (ModStorage storage = new ModStorage(Modifi.DefaultPack.MinecraftVersion, domain)) {
                    storage.MarkRequested(meta, v);
                    return;
                }
            }

            // ============================================================
            // TODO: ModHelper.PrintModInformation(meta);

            Menu<ModVersion> menu = new Menu<ModVersion>();

            menu.OptionFormatter = (opt) => {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(opt.GetVersionName());
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write(" [");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(opt.GetModVersion());
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("]");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" [{0}]", opt.GetReleaseType());
                Console.WriteLine();
            };

            ModVersion latestRelease = handler.GetRecentVersions(meta, 1, ModReleaseType.Release).Result.First();
            ModVersion latestVersion = handler.GetRecentVersions(meta, 1, ModReleaseType.Any).Result.First();

            IEnumerable<ModVersion> versions = handler.GetRecentVersions(meta, 6, ModReleaseType.Any).Result.Skip(1);

            menu.AddItem(latestRelease);
            if (latestVersion.GetModVersion() != latestRelease.GetModVersion())
                menu.AddItem(latestVersion);

            menu.AddSpacer();
            foreach (ModVersion v in versions.Take(5)) menu.AddItem(v);


            menu.DrawMenu();

            Console.ResetColor();

            Console.WriteLine();
            ModVersion version = menu.SelectedOption;
            Console.WriteLine("Selected Version: " + version.GetModVersion());

            // Create a storage handler for the domain and mark the version as requested
            using (ModStorage storage = new ModStorage(Modifi.DefaultPack.Installed, domain)) {
                storage.MarkRequested(meta, version);
            }
        }

        public static void HandleModRemove(IDomain domain, string modIdentifier) {
            IDomainHandler handler = domain.GetDomainHandler();
            ModStorage storage = new ModStorage(Modifi.DefaultPack.Installed, domain);

            ModMetadata meta = storage.GetMetadata(modIdentifier);
            if(meta == null) {
                Modifi.DefaultLogger.Error("Cannot uninstall {0}; it is not installed.", modIdentifier);
                return;
            }

            ModVersion installed = storage.GetMod(meta);
            ModStatus status = storage.GetModStatus(meta);

            switch(status) {
                case ModStatus.NotInstalled:
                    Modifi.DefaultLogger.Error("Cannot uninstall {0}; it is not installed.", meta.GetName());
                    return;

                case ModStatus.Requested:
                    Modifi.DefaultLogger.Information("Removing {0}...", meta.GetName());
                    storage.Delete(meta);
                    Modifi.DefaultLogger.Information("Done.");
                    return;

                case ModStatus.Installed:
                    Modifi.DefaultLogger.Information("Removing {0} and deleting files...", meta.GetName());
                    storage.Delete(meta);
                    string filePath = Path.Combine(Settings.ModPath, installed.GetFilename());
                    bool correctChecksum = ModUtilities.ChecksumMatches(filePath, installed.GetChecksum());
                    if (correctChecksum) {
                        try {
                            File.Delete(filePath);
                        }

                        catch (Exception e) {
                            Modifi.DefaultLogger.Error("Error deleting {0}, please delete it manually.", filePath);
                            Modifi.DefaultLogger.Error(e.Message);
                        }
                    } else {
                        Modifi.DefaultLogger.Information("File for {0} found at {1}, but the checksum did not match. Delete?", meta.GetName(), filePath);
                        Menu<string> delete = new Menu<string>();
                        delete.AddItem("Delete");
                        delete.AddItem("Leave");

                        delete.DrawMenu();
                        switch (delete.SelectedOption.ToLower()) {
                            case "delete":
                                File.Delete(filePath);
                                Modifi.DefaultLogger.Information("File deleted.");
                                break;

                            case "leave":
                                Modifi.DefaultLogger.Information("File left in place.");
                                break;
                        }
                    }

                    break;
            }

            storage.Dispose();
        }

        public static void HandleModInformation(IDomain domain, string modIdentifier, string modVersion = null) {
            ModMetadata meta = domain.GetDomainHandler().GetModMetadata(Modifi.DefaultPack.MinecraftVersion, modIdentifier).Result;

            ILogger log = Modifi.DefaultLogger;

            log.Information(meta.GetName());
            if (meta.HasDescription())
                log.Information(meta.GetDescription());
        }

        public static ModDownloadResult? HandleModDownload(IDomain domain, string modIdentifier, string modVersion = null) {

            IDomainHandler handler = domain.GetDomainHandler();

            // Fetch the mod version information from the Curseforge API
            try {
                ModMetadata meta = handler.GetModMetadata(Modifi.DefaultPack.MinecraftVersion, modIdentifier).Result;
                ModVersion mod = handler.GetModVersion(meta, modVersion).Result;

                return handler.DownloadMod(mod, Settings.ModPath).Result;
            }

            catch (Exception) { return null; }
        }
    }
}
