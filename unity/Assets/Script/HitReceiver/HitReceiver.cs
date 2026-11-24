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

        /// <summary>攻撃を受ける。攻撃の属性は、属性ごとの装甲とダメージ計算（#115 / #116）で使う</summary>
        public void Damage(AttackInfo attack)
        {
            // 撃破した後に当たっても、爆発のエフェクトや撃破の処理を繰り返さない
            // （機体は撃破の演出が終わるまで消えないため。CharacterDeadComponent）
            if (isDead)
                return;

            // 今は攻撃力をそのままダメージにする（装甲によるダメージ計算は #116）
            var damage = attack.Power;

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
