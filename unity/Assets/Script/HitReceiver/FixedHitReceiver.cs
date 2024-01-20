using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class FixedHitReceiver : HitReceiver
    {
        [SerializeField]
        int initHitPoint;

        private void Start()
        {
            SetInitHitPoint(initHitPoint);
        }
    }
}
