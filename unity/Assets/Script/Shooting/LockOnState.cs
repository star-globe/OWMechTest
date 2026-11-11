namespace AdvancedGears
{
    /// <summary>ロックの段階</summary>
    public enum LockLevel
    {
        /// <summary>ロックしていない（正面などへ撃つ）</summary>
        None = 0,
        /// <summary>一次ロック：目標の現在位置を狙う（撃った側の速度の分だけ補正）</summary>
        Primary,
        /// <summary>二次ロック：目標の速度も使って偏差射撃する</summary>
        Secondary,
    }

    /// <summary>武器の取り付け位置</summary>
    public enum WeaponSlot
    {
        Right = 0,
        Left,
        Sub,
    }

    /// <summary>
    /// ロックの状態（目標と、同じ目標をロックし続けている時間）。プレイヤー・NPC・敵ユニットが 1 つずつ持つ。
    /// 二次ロックになるまでの時間は武器ごとに違うため、段階は時間を渡して GetLevel で求める
    /// </summary>
    public class LockOnState
    {
        public const long NoTargetId = long.MinValue;

        public long TargetId { get; private set; } = NoTargetId;

        /// <summary>ロックしている目標（ID だけで使う場合は null）</summary>
        public BaseObject Target { get; private set; }

        /// <summary>今の目標をロックし続けている時間（秒）</summary>
        public float Duration { get; private set; }

        public bool HasTarget => TargetId != NoTargetId;

        /// <summary>目標を設定する。今と違う目標なら時間を 0 に戻す。null ならロックを外す</summary>
        public void SetTarget(BaseObject target)
        {
            // 破棄された目標（Unity の null）もロックを外す
            if (target == null)
            {
                Clear();
                return;
            }

            SetTarget(target.ID);
            Target = target;
        }

        /// <summary>目標を ID で設定する。今と違う目標なら時間を 0 に戻す</summary>
        public void SetTarget(long targetId)
        {
            if (targetId == TargetId)
                return;

            TargetId = targetId;
            Target = null;
            Duration = 0f;
        }

        public void Clear()
        {
            TargetId = NoTargetId;
            Target = null;
            Duration = 0f;
        }

        public void AddTime(float deltaTime)
        {
            if (HasTarget)
                Duration += deltaTime;
        }

        /// <summary>二次ロックになるまでの時間を渡して、今のロックの段階を返す</summary>
        public LockLevel GetLevel(float secondaryLockTime)
        {
            if (HasTarget == false)
                return LockLevel.None;

            return Duration >= secondaryLockTime ? LockLevel.Secondary : LockLevel.Primary;
        }
    }
}
