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
        private bool isDead;

        protected void SetInitHitPoint(int hit)
        {
            hitPoint = hit;
            isDead = false;
        }

        /// <summary>
        /// 属性ごとの装甲値。装甲を持たない対象（目標オブジェクトなど）は null。
        /// 装甲値 0 とは区別する（ダメージ計算 #116 で扱いを分けられるようにする）
        /// </summary>
        public virtual ArmorValues? Armor => null;

        /// <summary>攻撃を受ける。ダメージは攻撃と同じ属性の装甲値で減る（DamageCalculator）</summary>
        public void Damage(AttackInfo attack)
        {
            // 撃破した後に当たっても、爆発のエフェクトや撃破の処理を繰り返さない
            // （機体は撃破の演出が終わるまで消えないため。CharacterDeadComponent）
            if (isDead)
                return;

            var damage = DamageCalculator.Calculate(attack, Armor, GlobalParamMaster.Instance.ArmorReferenceValue);

            hitPoint -= damage;
            OnDamaged(damage);

            if (hitPoint <= 0)
            {
                EffectManager.Instance?.PlayEffect(EffectID.Explosion, transform.position);
                deadComp?.DeadAction();
                hitPoint = 0;
                isDead = true;
            }
            else
            {
                EffectManager.Instance?.PlayEffect(EffectID.ExplosionSmall, transform.position);
            }
        }

        /// <summary>ダメージを受けたときに呼ぶ（派生クラスで本体のパラメーターに反映する）</summary>
        protected virtual void OnDamaged(int damage) { }
    }
}
