using UnityEngine;

namespace AdvancedGears
{
    /// <summary>ジェネレーター。機体のエネルギーの総容量（最大エネルギー）と総出力を決める</summary>
    [CreateAssetMenu(menuName = "TestProject/Parts/GeneratorSettings", order = 0)]
    public class GeneratorSettings : PartSettings
    {
        [SerializeField, Min(0)]
        int energyCapacity = 10000;
        public int EnergyCapacity => energyCapacity;

        /// <summary>総出力（1 秒あたり）。ここから各パーツの常時消費を引いたものが回復力になる</summary>
        [SerializeField, Min(0)]
        int energyOutput = 5000;
        public int EnergyOutput => energyOutput;

        /// <summary>オーバーヒートしてから解除されるまでの時間（秒）</summary>
        [SerializeField, Min(0)]
        float overheatTime = EnergyPerformance.DefaultOverheatTime;
        public float OverheatTime => overheatTime;

        /// <summary>復帰エネルギー（オーバーヒートの解除時の残量）の、最大エネルギーに対する割合</summary>
        [SerializeField, Range(0f, 1f)]
        float overheatRecoveryRate = EnergyPerformance.DefaultOverheatRecoveryRate;
        public float OverheatRecoveryRate => overheatRecoveryRate;
    }

    public class GeneratorMasterContainer : IDBasedMasterContainer<GeneratorSettings>
    {
        protected override string resourcesFolder => "GeneratorSettings";
    }

    public class GeneratorMaster : Singleton<GeneratorMasterContainer>
    {
    }
}
