using System;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 一定間隔で停止と直進を繰り返す。1 周期は「停止 → 加速 → 最高速度で直進 → 減速」で、
    /// 周期ごとに直進する向きを turnAngle だけ右に曲げる（180 度で往復、90 度で四角形）。
    /// 最初の直進はスポーン時の正面（+Z）へ向かう。ランダムな要素はない
    /// </summary>
    public class StopAndGoMovePattern : IUnitMovePattern
    {
        readonly float stopTime;
        readonly float moveTime;
        readonly float accel;          // 0 なら即座に最高速度
        readonly float decel;          // 0 なら即座に停止
        readonly float turnRad;

        readonly float peakSpeed;      // 実際に出す最高速度（直進時間が短くて最高速度に届かないときは下がる）
        readonly float accelTime;
        readonly float cruiseTime;
        readonly float decelTime;

        /// <summary>1 回の直進で進む距離</summary>
        public float LegDistance { get; }

        /// <summary>1 周期（停止 + 直進）の時間</summary>
        public float CycleTime => stopTime + moveTime;

        /// <param name="maxSpeed">最高速度（距離/秒）</param>
        /// <param name="accel">加速度（距離/秒²）。0 以下なら即座に最高速度</param>
        /// <param name="decel">減速度（距離/秒²）。0 以下なら即座に停止</param>
        /// <param name="moveTime">直進する時間（加速の開始から減速の終了まで、秒）</param>
        /// <param name="stopTime">停止する時間（秒）</param>
        /// <param name="turnAngleDeg">直進するたびに右へ曲がる角度（度）。180 で往復、90 で四角形</param>
        public StopAndGoMovePattern(float maxSpeed, float accel, float decel, float moveTime, float stopTime, float turnAngleDeg)
        {
            this.stopTime = Mathf.Max(stopTime, 0f);
            this.moveTime = Mathf.Max(moveTime, 0f);
            this.accel = Mathf.Max(accel, 0f);
            this.decel = Mathf.Max(decel, 0f);
            turnRad = turnAngleDeg * Mathf.Deg2Rad;

            maxSpeed = Mathf.Max(maxSpeed, 0f);
            var invAccel = this.accel > 0f ? 1f / this.accel : 0f;
            var invDecel = this.decel > 0f ? 1f / this.decel : 0f;

            // 加速と減速だけで直進時間を使い切るなら、最高速度に届く前に減速を始める
            var invSum = invAccel + invDecel;
            peakSpeed = invSum > 0f ? Mathf.Min(maxSpeed, this.moveTime / invSum) : maxSpeed;
            accelTime = peakSpeed * invAccel;
            decelTime = peakSpeed * invDecel;
            cruiseTime = Mathf.Max(this.moveTime - accelTime - decelTime, 0f);

            LegDistance = peakSpeed * (accelTime * 0.5f + cruiseTime + decelTime * 0.5f);
        }

        public bool HasFixedForward => false;
        public Vector3 FixedForward => Vector3.forward;

        public void Evaluate(float time, out Vector3 localPosition, out Vector3 localVelocity)
        {
            var cycle = CycleTime;
            if (cycle <= 0f || time <= 0f)
            {
                localPosition = Vector3.zero;
                localVelocity = Vector3.zero;
                return;
            }

            var leg = (long)Math.Floor(time / cycle);
            var inCycle = time - leg * cycle;

            // 終わった直進の合計
            var start = SumOfLegs(leg);

            var moveElapsed = inCycle - stopTime;
            if (moveElapsed <= 0f)
            {
                localPosition = start;
                localVelocity = Vector3.zero;
                return;
            }

            CalcLeg(moveElapsed, out var distance, out var speed);
            var dir = Heading(leg);
            localPosition = start + dir * distance;
            localVelocity = dir * speed;
        }

        /// <summary>直進を始めてからの時間 s での、進んだ距離と速さ</summary>
        void CalcLeg(float s, out float distance, out float speed)
        {
            if (s < accelTime)
            {
                distance = 0.5f * accel * s * s;
                speed = accel * s;
                return;
            }

            var accelDistance = peakSpeed * accelTime * 0.5f;
            s -= accelTime;
            if (s < cruiseTime)
            {
                distance = accelDistance + peakSpeed * s;
                speed = peakSpeed;
                return;
            }

            s -= cruiseTime;
            if (s < decelTime)
            {
                distance = accelDistance + peakSpeed * cruiseTime + peakSpeed * s - 0.5f * decel * s * s;
                speed = peakSpeed - decel * s;
                return;
            }

            distance = LegDistance;
            speed = 0f;
        }

        /// <summary>leg 回目（0 始まり）の直進の向き</summary>
        Vector3 Heading(long leg)
        {
            var angle = turnRad * (double)leg;
            return new Vector3((float)Math.Sin(angle), 0f, (float)Math.Cos(angle));
        }

        /// <summary>
        /// 最初の count 回の直進で進んだ合計。向きを複素数 e^{iθ}（実部 = Z、虚部 = X）で表し、
        /// 等比級数 L × (1 - e^{ikφ}) / (1 - e^{iφ}) で求める（時間が経っても計算量が増えない）
        /// </summary>
        Vector3 SumOfLegs(long count)
        {
            if (count <= 0)
                return Vector3.zero;

            double phi = turnRad;
            double denomRe = 1.0 - Math.Cos(phi);
            double denomIm = -Math.Sin(phi);
            double denomSq = denomRe * denomRe + denomIm * denomIm;

            // 曲がらない（360 度の倍数）ときはまっすぐ進み続ける
            if (denomSq < 1e-12)
                return Vector3.forward * (float)(LegDistance * count);

            double kphi = phi * count;
            double numRe = 1.0 - Math.Cos(kphi);
            double numIm = -Math.Sin(kphi);

            // num / denom
            double re = (numRe * denomRe + numIm * denomIm) / denomSq;
            double im = (numIm * denomRe - numRe * denomIm) / denomSq;

            return new Vector3((float)(im * LegDistance), 0f, (float)(re * LegDistance));
        }
    }
}
