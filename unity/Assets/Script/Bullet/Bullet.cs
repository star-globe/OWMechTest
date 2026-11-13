using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>
    /// 弾。Rigidbody で飛び（重力あり）、当たりは物理の衝突ではなく、FixedUpdate ごとに前の位置から今の位置までの SphereCast で判定する。
    /// 物理の衝突を使うと、当たった相手に弾の運動量が移って押し出され（入力なしで機体が流れる）、
    /// 弾速が速いと 1 ステップで薄いコライダーをすり抜けるため（300 m/s で 1 ステップ 6 m 進む）。
    /// そのため弾のプレハブにはコライダーを付けない
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [SerializeField]
        TrailRenderer trail;

        [SerializeField]
        Rigidbody rigid;

        /// <summary>当たり判定の半径（m）</summary>
        [SerializeField]
        float hitRadius = 0.1f;

        // 当たり判定の結果バッファ（GC 抑制用。弾は同時に 1 つずつ判定するので共有してよい）
        static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private void Awake()
        {
            Assert.IsNotNull(rigid);
            Assert.IsNotNull(trail);
        }

        public int BulletId { get; private set; }
        public long InstanceId { get; private set; }
        public DateTime DeadTime { get; private set; }

        /// <summary>撃った側（この弾は撃った側には当たらない）</summary>
        BaseObject shooter;

        /// <summary>前の FixedUpdate での位置（ここから今の位置までを当たり判定する）</summary>
        Vector3 prevPosition;

        public void SetInfo(int bulletId)
        {
            BulletId = bulletId;
        }

        private BulletSettings Settings => BulletMaster.Instance.GetSettings(BulletId);

        public BulletCategoryType Category => Settings == null ? BulletCategoryType.None : Settings.BulletCategoryType;
        public float VecSpeed => Settings == null ? 0.0f : Settings.Speed;
        public float LifeTime => Settings == null ? 0.0f : Settings.LifeTime;
        public int Attack => Settings == null ? 0 : Settings.Attack;

        /// <summary>
        /// 発射する。初速は「direction × 弾速 + inheritVelocity（撃った側の速度）」
        /// </summary>
        public void Fire(Vector3 direction, Vector3 pos, Vector3 inheritVelocity, long instanceId, BaseObject shooter)
        {
            var vec = direction.normalized * this.VecSpeed + inheritVelocity;

            this.transform.position = pos;
            if (vec.sqrMagnitude > 0f)
                this.transform.forward = vec.normalized;

            trail.Clear();
            trail.AddPosition(pos);
            this.gameObject.SetActive(true);

            // プールから再利用したときに前の速度が残らないよう、加算ではなく代入する
            rigid.position = pos;
            rigid.linearVelocity = vec;
            rigid.angularVelocity = Vector3.zero;

            this.shooter = shooter;
            this.prevPosition = pos;
            this.InstanceId = instanceId;
            this.DeadTime = TimeUtils.Now + TimeSpan.FromSeconds(this.LifeTime);
        }

        public void Vanish()
        {
            this.gameObject.SetActive(false);
            this.shooter = null;
        }

        private void FixedUpdate()
        {
            // FixedUpdate は物理の更新の前に呼ばれるため、rigid.position は前のステップで動いた後の位置
            var current = rigid.position;
            var move = current - prevPosition;
            var distance = move.magnitude;
            if (distance <= 1e-5f)
                return;

            if (TryFindHit(prevPosition, move / distance, distance, out var hit))
                OnHit(hit);
            else
                prevPosition = current;
        }

        /// <summary>origin から direction へ distance までで、最初に当たるコライダーを探す（撃った側とトリガーは除く）</summary>
        bool TryFindHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            nearest = default;
            var count = Physics.SphereCastNonAlloc(origin, hitRadius, direction, hitBuffer, distance,
                                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            var found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = hitBuffer[i];
                // 判定の開始時点で重なっているコライダー（distance が 0）は、撃った直後の自分の機体などなので飛ばす
                if (hit.distance <= 0f)
                    continue;

                if (found && hit.distance >= nearest.distance)
                    continue;

                if (IsShooter(hit.collider))
                    continue;

                nearest = hit;
                found = true;
            }

            return found;
        }

        bool IsShooter(Collider col)
        {
            if (shooter == null)
                return false;

            var obj = col.GetComponentInParent<BaseObject>();
            return obj != null && obj == shooter;
        }

        void OnHit(RaycastHit hit)
        {
            var hitObject = hit.collider.gameObject;
            transform.position = hit.point;

            var rotation = hit.normal.sqrMagnitude > 0f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
            if (((1 << hitObject.layer) & GameLayers.HitLayerMask) != 0)
            {
                // コライダーが子のオブジェクトに付いていることがあるため、親までさかのぼって探す
                var receiver = hit.collider.GetComponentInParent<HitReceiver>();
                if (receiver != null)
                    receiver.Damage(Attack);
            }

            EffectManager.Instance?.PlayEffect(EffectID.HitSpark, hit.point, rotation);

            // BulletManager を通して消し、プールに戻す
            BulletManager.Instance.Vanish(InstanceId);
            if (gameObject.activeSelf)
                Vanish();
        }
    }
}
