﻿using RobotGryphon.Modifi.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotGryphon.Modifi.Storage {
    public struct Pack {

        public string Name;

        /// <summary>
        /// The currently-installed version.
        /// </summary>
        public string Installed;

        // TODO
        public Dictionary<String, Domain> Domains;

        public string MinecraftVersion;
    }
}