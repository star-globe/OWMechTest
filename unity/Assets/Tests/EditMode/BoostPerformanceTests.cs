using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class BoostPerformanceTests
    {
        static readonly BoostPerformance Fallback = new BoostPerformance(1.5f, 10f, 0.5f, 3000, 10f, 2000);

        BoosterSettings booster;

        [SetUp]
        public void SetUp()
        {
            booster = ScriptableObject.CreateInstance<BoosterSettings>();
            var so = new SerializedObject(booster);
            so.FindProperty("boostSpeedRate").floatValue = 2.5f;
            so.FindProperty("quickBoostSpeed").floatValue = 18f;
            so.FindProperty("quickBoostTime").floatValue = 0.2f;
            so.FindProperty("quickBoostEnergy").intValue = 2800;
            so.FindProperty("hyperBoostSpeed").floatValue = 35f;
            so.FindProperty("hyperBoostEnergy").intValue = 4500;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(booster);
        }

        [Test]
        public void FromBooster_CopiesAllValues()
        {
            var p = BoostPerformance.FromBooster(booster);

            Assert.AreEqual(2.5f, p.BoostSpeedRate);
            Assert.AreEqual(18f, p.QuickBoostSpeed);
            Assert.AreEqual(0.2f, p.QuickBoostTime);
            Assert.AreEqual(2800, p.QuickBoostEnergy);
            Assert.AreEqual(35f, p.HyperBoostSpeed);
            Assert.AreEqual(4500, p.HyperBoostEnergy);
        }

        [Test]
        public void Resolve_WithBooster_UsesBoosterValues()
        {
            var p = BoostPerformance.Resolve(booster, Fallback);

            Assert.AreEqual(2.5f, p.BoostSpeedRate);
            Assert.AreEqual(4500, p.HyperBoostEnergy);
        }

        [Test]
        public void Resolve_WithoutBooster_UsesFallback()
        {
            var p = BoostPerformance.Resolve(null, Fallback);

            Assert.AreEqual(Fallback, p);
        }

        [Test]
        public void Resolver_UnequippedBooster_UsesFallback()
        {
            var p = BoostPerformanceResolver.Resolve(_ => -1, Fallback);

            Assert.AreEqual(Fallback, p);
        }

        [Test]
        public void Resolver_UnknownBoosterId_UsesFallback()
        {
            var p = BoostPerformanceResolver.Resolve(_ => int.MaxValue, Fallback);

            Assert.AreEqual(Fallback, p);
        }

        [Test]
        public void PlayerMoveParams_WithBoost_OverridesOnlyBoostValues()
        {
            var original = new PlayerMoveParams
            {
                VecSpeed = 10f,
                HorizontalBoostRate = 1.5f,
                QuickSpeed = 10f,
                QuickTime = 0.5f,
                QuickInterval = 1f,
                HyperBoostSpeed = 10f,
                JumpSpeed = 8f,
            };

            var p = original.WithBoost(BoostPerformance.FromBooster(booster));

            Assert.AreEqual(2.5f, p.HorizontalBoostRate);
            Assert.AreEqual(18f, p.QuickSpeed);
            Assert.AreEqual(0.2f, p.QuickTime);
            Assert.AreEqual(35f, p.HyperBoostSpeed);
            Assert.AreEqual(10f, p.VecSpeed);
            Assert.AreEqual(1f, p.QuickInterval);
            Assert.AreEqual(8f, p.JumpSpeed);
        }

        [Test]
        public void CharacterParam_SetBoostPerformance_SetsEnergies()
        {
            var param = new CharacterParam();
            param.SetBoostPerformance(BoostPerformance.FromBooster(booster));

            Assert.AreEqual(2800, param.QuickBoostEnergy);
            Assert.AreEqual(4500, param.HyperBoostEnergy);
        }
    }
}
