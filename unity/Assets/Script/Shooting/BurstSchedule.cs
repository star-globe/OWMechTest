namespace AdvancedGears
{
    /// <summary>
    /// 一斉発射の予定（決めた数を、決めた間隔で順に撃つ）。Unity に依存しないのでテストできる。
    /// 時刻は秒で渡す（Time.time など）
    /// </summary>
    public class BurstSchedule
    {
        int remaining;
        float interval;
        double nextTime;

        /// <summary>まだ撃っていない弾があるか</summary>
        public bool IsActive => remaining > 0;

        /// <summary>まだ撃っていない弾の数</summary>
        public int Remaining => remaining;

        /// <summary>一斉発射を始める。最初の 1 発は now に撃つ</summary>
        public void Start(int count, float interval, double now)
        {
            remaining = count < 1 ? 1 : count;
            this.interval = interval < 0f ? 0f : interval;
            nextTime = now;
        }

        /// <summary>
        /// now までに撃つ時刻が来た弾の数を返し、撃ったことにする。
        /// フレームが遅れても、遅れた分の弾はまとめて返す
        /// </summary>
        public int ConsumeDue(double now)
        {
            int count = 0;
            while (remaining > 0 && now >= nextTime)
            {
                remaining--;
                count++;
                nextTime += interval;
            }

            return count;
        }

        public void Cancel()
        {
            remaining = 0;
        }
    }
}
