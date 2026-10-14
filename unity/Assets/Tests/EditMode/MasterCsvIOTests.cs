using System.IO;
using System.Linq;
using AdvancedGears.Editor;
using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class MasterCsvIOTests
    {
        public class CsvTestSettings : ScriptableObject
        {
            [SerializeField] int number = 0;
            [SerializeField] string label = "";
            [SerializeField] Vector3 notExported = Vector3.zero;   // プリミティブ以外は出力しない
            public float publicValue;
            int privateNotSerialized = 0;                           // SerializeField でない private は出力しない

            public void Set(int n, string l, float p) { number = n; label = l; publicValue = p; }
            public int Dummy => privateNotSerialized + (int)notExported.x;
        }

        string path;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Path.GetTempPath(), "MasterCsvIOTests_" + System.Guid.NewGuid().ToString("N") + ".csv");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        [Test]
        public void GetExportableFields_OnlySerializedPrimitiveFields()
        {
            var names = MasterCsvIO.GetExportableFields(typeof(CsvTestSettings)).Select(f => f.Name).ToList();

            CollectionAssert.AreEquivalent(new[] { "number", "label", "publicValue" }, names);
        }

        [Test]
        public void Export_WritesHeaderAndEscapesValues()
        {
            var a = ScriptableObject.CreateInstance<CsvTestSettings>();
            a.Set(1, "plain", 0.5f);
            var b = ScriptableObject.CreateInstance<CsvTestSettings>();
            b.Set(2, "has,comma \"quoted\"", 1f);

            var fields = MasterCsvIO.GetExportableFields(typeof(CsvTestSettings));
            MasterCsvIO.Export(new ScriptableObject[] { a, b }, fields, path);

            var bytes = File.ReadAllBytes(path);
            Assert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray(), "UTF-8 BOM 付きで出力する（Excel で文字化けしないため）");

            var lines = File.ReadAllLines(path);
            Assert.AreEqual(string.Join(",", fields.Select(f => f.Name)), lines[0]);
            StringAssert.Contains("\"has,comma \"\"quoted\"\"\"", lines[2]);
            StringAssert.Contains("plain", lines[1]);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
