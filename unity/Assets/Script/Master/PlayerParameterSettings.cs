using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Player/PlayerParameterSettings", order = 0)]
    public class PlayerParameterSettings : ScriptableObject
    {
        #region Paramter
        [SerializeField]
        int maxAp = 10000;
        public int MaxAP => maxAp;

        [SerializeField]
        [Tooltip("ジェネレーターを読み込めないときの最大エネルギー（通常はジェネレーターの総容量を使う）")]
        int maxEnergy = 10000;
        public int MaxEnergy => maxEnergy;

        [SerializeField]
        [Tooltip("ジェネレーターを読み込めないときの回復力（1 秒あたり。通常は装備から計算する）")]
        int energyRecovery = 2500;
        public int EnergyRecovery => energyRecovery;

        [SerializeField]
        [Tooltip("ジェネレーターを読み込めないときに、ブースト ON の間に下がる回復力（1 秒あたり。通常はブースターの ON 消費を使う）")]
        int boostEnergy = 1500;
        public int BoostEnergy => boostEnergy;

        [SerializeField]
        int jumpEnergy = 3500;
        public int JumpEnergy => jumpEnergy;

        [SerializeField]
        int quickBoostEnergy = 3000;
        public int QuickBoostEnergy => quickBoostEnergy;

        [SerializeField]
        int hyperBoostEnergy = 2000;
        public int HyperBoostEnergy => hyperBoostEnergy;

        [SerializeField]
        [Tooltip("グライド中の上昇で消費するエネルギー（1 秒あたり。ブースト ON による回復力の低下に加えて消費する）")]
        int ascendEnergy = 5000;
        public int AscendEnergy => ascendEnergy;

        [SerializeField]
        float quickTime = 0.5f;
        public float QuickTime => quickTime;

        [SerializeField]
        float quickInterval = 0.5f;
        public float QuickInterval => quickInterval;

        [SerializeField]
        float touchQuickRate = 1.5f;
        public float TouchQuickRate => touchQuickRate;

        [SerializeField]
        int lockCircleSize = 250;
        public int LockCircleSize => lockCircleSize;

        [SerializeField]
        int lockLength = 250;
        public int LockLength => lockLength;
        #endregion
    }
}
