using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Player/PlayerControllerSettings", order = 0)]
    public class PlayerControllerSettings : ScriptableObject
    {
        #region Paramter
        [SerializeField]
        float vecSpeed = 5.0f;
        public float VecSpeed => vecSpeed;

        [SerializeField]
        float accelerateTime = 1.0f;
        public float AccelerateTime => accelerateTime;

        [SerializeField]
        float jumpSpeed = 10.0f;
        public float JumpSpeed => jumpSpeed;

        [SerializeField]
        float horizontalBoostRate = 1.5f;
        public float HorizontalBoostRate => horizontalBoostRate;

        [SerializeField]
        float floatRate = 0.9f;
        public float FloatRate => floatRate;

        [SerializeField]
        float rotSpeed = 180f;
        public float RotSpeed => rotSpeed;

        [SerializeField]
        float quickSpeed = 10.0f;
        public float QuickSpeed => quickSpeed;

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
        float hyperBoostSpeed = 10.0f;
        public float HyperBoostSpeed => hyperBoostSpeed;

        [SerializeField]
        [Tooltip("グライド中にジャンプボタンを押し続けたときの上昇の上限速度")]
        float ascendSpeed = 2.0f;
        public float AscendSpeed => ascendSpeed;

        [SerializeField]
        [Tooltip("グライド中の上昇の加速度（上限速度に達するまで）")]
        float ascendAccel = 8.0f;
        public float AscendAccel => ascendAccel;
        #endregion
    }
}
