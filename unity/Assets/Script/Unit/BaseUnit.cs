using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニット基底クラス（BaseObject を継承）
    /// 固定装備・特定ロジックで動くキャラクター全般の共通実装
    /// </summary>
    public abstract class BaseUnit : BaseObject
    {
        // ── Inspector ────────────────────────────────────────
        /// <summary>UnitSettings に ParameterSettings が無いとき（シーンに直接置いたときなど）に使う</summary>
        [SerializeField]
        UnitParameterSettings paramSettings;

        // ── Properties ───────────────────────────────────────
        /// <summary>このユニットの種別</summary>
        public abstract UnitType UnitType { get; }

        /// <summary>スポーンに使ったマスタ（シーンに直接置いたときは null）</summary>
        public UnitSettings UnitSettings { get; private set; }

        /// <summary>実行時パラメーター状態</summary>
        public UnitParam UnitParam { get; private set; }

        /// <summary>生存確認</summary>
        public bool IsAlive => UnitParam != null && UnitParam.IsAlive;

        public override int Ap    => UnitParam?.Ap    ?? int.MinValue;
        public override int MaxAp => UnitParam?.MaxAp ?? int.MinValue;

        UnitMover mover;
        bool moverChecked;

        /// <summary>移動パターンで動いているなら式で求めた速度、そうでなければ BaseObject の速度</summary>
        public override Vector3 Velocity
        {
            get
            {
                if (moverChecked == false)
                {
                    TryGetComponent(out mover);
                    moverChecked = true;
                }

                if (mover != null && mover.IsMoving)
                    return mover.Velocity;

                return base.Velocity;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────
        /// <summary>マスタを指定して初期化する（ParameterSettings と移動パターンをマスタから取る）</summary>
        public void Initialize(long id, UnitSide side, UnitSettings settings)
        {
            UnitSettings = settings;
            Initialize(id, side);
        }

        public override void Initialize(long id, UnitSide side)
        {
            base.Initialize(id, side);

            var param = UnitSettings != null && UnitSettings.ParameterSettings != null
                ? UnitSettings.ParameterSettings
                : paramSettings;

            UnitParam = new UnitParam();
            if (param != null)
                UnitParam.SetInitialInfo(param);
            else
                Debug.LogWarningFormat("[BaseUnit] paramSettings が未設定です。GameObject: {0}", gameObject.name);
        }

        // ── Update ────────────────────────────────────────────
        protected virtual void Update()
        {
            if (UnitParam == null) return;
            UnitParam.UpdateFireTimer(Time.deltaTime);
        }

        // ── Damage ───────────────────────────────────────────
        public void SetDamage(int damage)
        {
            UnitParam?.SetDamage(damage);
        }
    }
}
