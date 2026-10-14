using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AdvancedGears.Tests
{
    public class MasterContainerTests
    {
        public class TestSettings : ScriptableObject, IDBasedMasterSettings
        {
            public int id;
            public int ID => id;
        }

        class TestContainer : IDBasedMasterContainer<TestSettings> { }

        static TestSettings Create(int id)
        {
            var s = ScriptableObject.CreateInstance<TestSettings>();
            s.id = id;
            return s;
        }

        [Test]
        public void LoadRecord_ThenGetSettingsById()
        {
            var container = new TestContainer();
            var a = Create(1);
            var b = Create(2);
            container.LoadRecord(a);
            container.LoadRecord(b);

            Assert.AreSame(a, container.GetSettings(1));
            Assert.AreSame(b, container.GetSettings(2));
            Assert.AreEqual(2, container.GetDictionary().Count);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void LoadRecord_SameId_OverwritesPreviousRecord()
        {
            var container = new TestContainer();
            var first = Create(1);
            var second = Create(1);
            container.LoadRecord(first);
            container.LoadRecord(second);

            Assert.AreSame(second, container.GetSettings(1));
            Assert.AreEqual(1, container.GetDictionary().Count);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void GetSettings_UnknownId_LogsErrorAndReturnsNull()
        {
            var container = new TestContainer();

            LogAssert.Expect(LogType.Error, new Regex("There is no .*Id:99"));
            Assert.IsNull(container.GetSettings(99));
        }

        [Test]
        public void Clear_RemovesAllRecords()
        {
            var container = new TestContainer();
            var a = Create(1);
            container.LoadRecord(a);

            container.Clear();

            Assert.AreEqual(0, container.GetDictionary().Count);
            Object.DestroyImmediate(a);
        }
    }
}
