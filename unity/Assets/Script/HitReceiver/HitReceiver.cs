using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public abstract class HitReceiver : MonoBehaviour
    {
        [SerializeField]
        DeadComponent deadComp = null;

        private int hitPoint;
        protected void SetInitHitPoint(int hit)
        {
            hitPoint = hit;
        }

        public void Damage(int damage)
        {
            hitPoint -= damage;

            if (hitPoint <= 0)
            {
                deadComp?.DeadAction();
                hitPoint = 0;
            }
        }
    }
}
