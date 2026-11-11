namespace AdvancedGears
{
    /// <summary>
    /// 射撃してから一定時間、腕の構えを保つためのタイマー（Unity に依存しない）
    /// </summary>
    public class AimHoldTimer
    {
        readonly float holdTime;
        float remain = 0;

        public AimHoldTimer(float holdTime)
        {
            this.holdTime = holdTime;
        }

        /// <summary>構えている間 true</summary>
        public bool IsActive => remain > 0;

        /// <summary>射撃したときに呼ぶ。保持時間を最初からやり直す</summary>
        public void Trigger()
        {
            remain = holdTime;
        }

        public void Tick(float deltaTime)
        {
            if (remain <= 0)
                return;

            remain -= deltaTime;
            if (remain < 0)
                remain = 0;
        }

        public void Reset()
        {
            remain = 0;
        }
    }
}
