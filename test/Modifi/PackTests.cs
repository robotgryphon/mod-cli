using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Modifi.Storage;

namespace Modifi.Tests {
    
    [TestClass]
    public class PackTests {
        
        public static string dataDirectory = Path.Combine(Environment.CurrentDirectory, "test_data");

        [ClassInitialize]
        public static void Initialize(TestContext ctx) {
            Directory.CreateDirectory(dataDirectory);
        }

        [ClassCleanup]
        public static void Cleanup() {
            Directory.Delete(dataDirectory, true);
        }

        protected Pack CreatePack() {
            Pack p = new Pack();
            p.Name ="Pack";
            p.MinecraftVersion = "1.12.2";
            p.Mods = new System.Collections.Generic.Dictionary<string, string>() {
                { "curseforge:jei", "latest" }
            };

            return p;
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestInvalidPackLoad () {
            // Pack.json does not exist
            Pack p = Pack.Load("pack.json");
        }

        [TestMethod]
        public async Task TestPackCreation() {
            string filename = Path.Combine(dataDirectory, "test-pack.json");
            
            Debug.WriteLine("Creating pack object.");
            Pack p = new Pack();
            p.Name = "Testing";
            p.MinecraftVersion = "1.12.2";

            Debug.WriteLine("Saving to {0}...", filename);
            await p.SaveAs(filename);

            Assert.IsTrue(File.Exists(filename));
        }

        [TestMethod]
        public async Task TestPackRead() {
            Pack original = CreatePack();

            string file = Path.Combine(dataDirectory, "original.json");
            await original.SaveAs(file);

            Pack loaded = Pack.Load(file);

            Assert.AreEqual(loaded.Name, original.Name);
            Assert.AreEqual(loaded.MinecraftVersion, original.MinecraftVersion);
            Assert.AreEqual(loaded.Mods.Count, original.Mods.Count);
        }

        [TestMethod]
        public void TestPackAdd() {
            Pack original = CreatePack();
            Pack test = CreatePack();

            original.AddMod("test:testmod", "latest");

            Assert.AreNotEqual(original.Mods, test.Mods);
            Assert.IsTrue(original.Mods.Count > test.Mods.Count);
        }

        [TestMethod]
        public void TestPackRemove() {
            Pack original = CreatePack();
            Pack test = CreatePack();

            original.RemoveMod("curseforge:jei");

            Assert.AreNotEqual(original.Mods, test.Mods);
            Assert.IsTrue(original.Mods.Count < test.Mods.Count);
        }
    }
}