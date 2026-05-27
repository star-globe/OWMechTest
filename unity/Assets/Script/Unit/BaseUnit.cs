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
        [SerializeField]
        UnitParameterSettings paramSettings;

        // ── Properties ───────────────────────────────────────
        /// <summary>このユニットの種別</summary>
        public abstract UnitType UnitType { get; }

        /// <summary>実行時パラメーター状態</summary>
        public UnitParam UnitParam { get; private set; }

        /// <summary>生存確認</summary>
        public bool IsAlive => UnitParam != null && UnitParam.IsAlive;

        public override int Ap    => UnitParam?.Ap    ?? int.MinValue;
        public override int MaxAp => UnitParam?.MaxAp ?? int.MinValue;

        // ── Lifecycle ─────────────────────────────────────────
        public override void Initialize(long id, UnitSide side)
        {
            base.Initialize(id, side);

            UnitParam = new UnitParam();
            if (paramSettings != null)
                UnitParam.SetInitialInfo(paramSettings);
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
