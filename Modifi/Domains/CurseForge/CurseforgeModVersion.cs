﻿using RobotGryphon.Modifi.Mods;
using System;

namespace RobotGryphon.Modifi.Domains.CurseForge {
    public class CurseforgeModVersion : IModVersion {

        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Version { get; set; }

        [Newtonsoft.Json.JsonProperty("versions")]
        public string[] MinecraftVersions { get; set; }

        /// <summary>
        /// The type of release the mod is.
        /// </summary>
        public ModReleaseType Type { get; set; }

        [Newtonsoft.Json.JsonProperty("url")]
        public string DownloadURL { get; set; }

        [Newtonsoft.Json.JsonProperty("id")]
        public string FileId { get; set; }

        /// <summary>
        /// Mod identifier (i.e. jei)
        /// </summary>
        public string ModIdentifier { get; internal set; }

        string IModVersion.GetDomain() {
            return "curseforge";
        }

        string IModVersion.GetModIdentifier() {
            return ModIdentifier;
        }

        string IModVersion.GetModVersion() {
            return Version;
        }
    }
}
