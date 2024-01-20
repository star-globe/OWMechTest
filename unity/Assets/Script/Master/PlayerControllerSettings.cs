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
        #endregion
    }
}
