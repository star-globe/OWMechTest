using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>誘導する弾（ミサイル）が追う目標。目標が null か段階が None なら誘導しない</summary>
    public readonly struct BulletGuidance
    {
        public readonly BaseObject Target;
        public readonly LockLevel Level;

        public BulletGuidance(BaseObject target, LockLevel level)
        {
            Target = target;
            Level = level;
        }

        public bool HasTarget => Target != null && Level != LockLevel.None;

        public static readonly BulletGuidance None = default;

        /// <summary>射撃の要求から作る（ロックしていなければ None）</summary>
        public static BulletGuidance From(in AimRequest request)
            => request.IsLocked ? new BulletGuidance(request.Target, request.Level) : None;
    }

    /// <summary>
    /// 弾。Rigidbody で飛び（重力の有無はプレハブの Rigidbody の useGravity で決める）、
    /// 当たりは物理の衝突ではなく、FixedUpdate ごとに前の位置から今の位置までの SphereCast で判定する。
    /// 物理の衝突を使うと、当たった相手に弾の運動量が移って押し出され（入力なしで機体が流れる）、
    /// 弾速が速いと 1 ステップで薄いコライダーをすり抜けるため（300 m/s で 1 ステップ 6 m 進む）。
    /// そのため弾のプレハブにはコライダーを付けない。
    /// ただし撃ち落とせる弾（ミサイル）は、ほかの弾の SphereCast に当たるよう Missile レイヤーのコライダーを付ける
    /// （Missile レイヤーは物理の衝突をどのレイヤーとも起こさない設定にする）。
    /// 弾のマスタで誘導のパラメーターを設定した弾は、発射時のロックの目標へ向きを変える（MissileGuidance）
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

        /// <summary>
        /// 撃ち落とされる当たり判定（撃ち落とせる弾だけ。Missile レイヤー、+Z 向きのカプセル）。
        /// 弾どうしがすれ違うと、1 ステップでお互いを飛び越えて当たらないことがあるため、
        /// 前のステップの位置から今の位置まで後ろへ伸ばして、通った範囲を覆う
        /// </summary>
        [SerializeField]
        [Tooltip("撃ち落とされる当たり判定（撃ち落とせる弾だけ設定する。direction は Z 軸）")]
        CapsuleCollider shootDownCollider;

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

        /// <summary>誘導で追う目標（発射時のロック）</summary>
        BulletGuidance guidance;

        /// <summary>発射からの経過時間（秒。FixedUpdate で数える）</summary>
        float elapsed;

        /// <summary>目標を失ったか（失ったら以後は直進する）</summary>
        bool isTargetLost;

        /// <summary>撃ち落とせる弾か（Missile レイヤーに置いた弾）</summary>
        public bool IsShootDownable => gameObject.layer == GameLayers.MissileLayer;

        /// <summary>当たり判定の対象。撃ち落とせる弾どうしは当たらない</summary>
        int HitCastMask => IsShootDownable ? Physics.DefaultRaycastLayers & ~GameLayers.MissileLayerMask : Physics.DefaultRaycastLayers;

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
            => Fire(direction, pos, inheritVelocity, instanceId, shooter, BulletGuidance.None);

        /// <summary>発射する。誘導する弾は guidance の目標を追う</summary>
        public void Fire(Vector3 direction, Vector3 pos, Vector3 inheritVelocity, long instanceId, BaseObject shooter, in BulletGuidance guidance)
        {
            this.guidance = guidance;
            this.elapsed = 0f;
            this.isTargetLost = false;
            StretchShootDownCollider(0f);

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
            this.guidance = BulletGuidance.None;
        }

        private void FixedUpdate()
        {
            // FixedUpdate は物理の更新の前に呼ばれるため、rigid.position は前のステップで動いた後の位置
            var current = rigid.position;
            var move = current - prevPosition;
            var distance = move.magnitude;
            if (distance > 1e-5f)
            {
                if (TryFindHit(prevPosition, move / distance, distance, out var hit))
                {
                    OnHit(hit);
                    return;
                }

                prevPosition = current;
            }

            UpdateGuidance(Time.fixedDeltaTime);

            // 次のステップで動く距離（今の速さ × ステップ）だけ後ろへ伸ばす。
            // コライダーは弾に付いて動くので、物理の更新の後は「動く前の位置から動いた後の位置」を覆う
            StretchShootDownCollider(rigid.linearVelocity.magnitude * Time.fixedDeltaTime);
        }

        void StretchShootDownCollider(float length)
        {
            if (shootDownCollider == null)
                return;

            shootDownCollider.height = length + shootDownCollider.radius * 2f;
            shootDownCollider.center = new Vector3(0f, 0f, -length * 0.5f);
        }

        /// <summary>誘導する弾の向きを変える（速さは変えない）</summary>
        void UpdateGuidance(float deltaTime)
        {
            var settings = Settings;
            if (settings == null || settings.IsHoming == false)
                return;

            elapsed += deltaTime;

            var target = guidance.Target;
            if (isTargetLost == false && guidance.HasTarget
                && MissileGuidance.IsTargetLost(target != null, target != null && target.isActiveAndEnabled,
                                                target != null ? target.Ap : 0, target != null ? target.MaxAp : 0))
            {
                // 撃破された目標を再び追わないよう、以後は直進にする
                isTargetLost = true;
            }

            var phase = MissileGuidance.GetPhase(elapsed, settings.GuidanceDelay, settings.GuidanceTime,
                                                 guidance.HasTarget && isTargetLost == false);
            if (phase != GuidancePhase.Guiding)
                return;

            var velocity = rigid.linearVelocity;
            var speed = velocity.magnitude;
            if (speed <= 1e-5f)
                return;

            var position = rigid.position;
            var aimPoint = MissileGuidance.CalcAimPoint(guidance.Level, position, speed, target.AimPosition, target.Velocity);
            var dir = MissileGuidance.TurnTowards(velocity, aimPoint - position, settings.TurnRate, deltaTime);

            rigid.linearVelocity = dir * speed;
            rigid.rotation = Quaternion.LookRotation(dir);
        }

        /// <summary>撃ち落とされた。ダメージを与えずに、撃墜のエフェクトを出して消える</summary>
        public void ShootDown()
        {
            if (gameObject.activeSelf == false)
                return;

            var settings = Settings;
            var effectId = settings != null ? settings.ResolvedShootDownEffectId : EffectID.ExplosionSmall;
            EffectManager.Instance?.PlayEffect(effectId, transform.position, transform.rotation);

            BulletManager.Instance.Vanish(InstanceId);
            if (gameObject.activeSelf)
                Vanish();
        }

        /// <summary>origin から direction へ distance までで、最初に当たるコライダーを探す（撃った側とトリガーは除く）</summary>
        bool TryFindHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            nearest = default;
            var count = Physics.SphereCastNonAlloc(origin, hitRadius, direction, hitBuffer, distance,
                                                   HitCastMask, QueryTriggerInteraction.Ignore);

            var found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = hitBuffer[i];
                // 判定の開始時点で重なっているコライダー（distance が 0）は、撃った直後の自分の機体などなので飛ばす。
                // ただしミサイル（後ろへ伸ばした判定）は、開始時点で重なっていても撃ち落とす（飛ばすとすり抜ける）
                if (hit.distance <= 0f && hit.collider.gameObject.layer != GameLayers.MissileLayer)
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
            // 判定の開始時点で重なっていた当たり（distance が 0）は point と normal が入らないため、判定の開始位置を使う
            var point = hit.distance > 0f ? hit.point : prevPosition;
            transform.position = point;

            var rotation = hit.distance > 0f && hit.normal.sqrMagnitude > 0f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
            if (hitObject.layer == GameLayers.MissileLayer)
            {
                // 撃ち落とせる弾（ミサイル）は、攻撃力に関係なく 1 発で壊れる。撃った本人の弾でも撃ち落とせる
                var missile = hit.collider.GetComponentInParent<Bullet>();
                if (missile != null && missile != this)
                    missile.ShootDown();
            }
            else if (((1 << hitObject.layer) & GameLayers.HitLayerMask) != 0)
            {
                // コライダーが子のオブジェクトに付いていることがあるため、親までさかのぼって探す
                var receiver = hit.collider.GetComponentInParent<HitReceiver>();
                if (receiver != null)
                    receiver.Damage(Attack);
            }

            var settings = Settings;
            var effectId = settings != null ? settings.ResolvedHitEffectId : EffectID.HitSpark;
            EffectManager.Instance?.PlayEffect(effectId, point, rotation);

            // BulletManager を通して消し、プールに戻す
            BulletManager.Instance.Vanish(InstanceId);
            if (gameObject.activeSelf)
                Vanish();
        }
    }
}
