using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class PlayerMoveCalculatorTests
    {
        const float Delta = 0.02f;

        static PlayerMoveParams CreateParams()
        {
            return new PlayerMoveParams
            {
                VecSpeed = 10f,
                AccelerateTime = 0.5f,
                JumpSpeed = 8f,
                HorizontalBoostRate = 2f,
                FloatRate = 0.5f,
                RotSpeed = 90f,
                QuickSpeed = 20f,
                TouchQuickRate = 1.5f,
                QuickTime = 0.2f,
                QuickInterval = 1f,
                HyperBoostSpeed = 40f,
                AscendSpeed = 3f,
                AscendAccel = 10f,
                RotXMin = 0.1f,
                WallJumpRate = 1.5f,
                JumpTime = 0.5f,
                TouchTime = 0.1f,
                JumpInterval = 0.3f,
                AirBrakeRate = 1f,
                TouchBrakeRateAdd = 2f,
                QuickYDelta = 0.2f,
            };
        }

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-5f, "x");
            Assert.AreEqual(expected.y, actual.y, 1e-5f, "y");
            Assert.AreEqual(expected.z, actual.z, 1e-5f, "z");
        }

        [TestCase(false, false, 10f)]
        [TestCase(false, true, 10f)]
        [TestCase(true, false, 20f)]
        [TestCase(true, true, 40f)]
        public void LimitSpeed_DependsOnBoost(bool isBoost, bool isHyperBoost, float expected)
        {
            Assert.AreEqual(expected, PlayerMoveCalculator.LimitSpeed(CreateParams(), isBoost, isHyperBoost));
        }

        [Test]
        public void CalcJumpDirection_NoInput_JumpsUp()
        {
            PlayerMoveCalculator.CalcJumpDirection(0, 0, out var horizontal, out var local);

            Assert.AreEqual(Vector3.zero, horizontal);
            Assert.AreEqual(Vector3.up, local);
        }

        [Test]
        public void CalcJumpDirection_WithInput_Normalized()
        {
            PlayerMoveCalculator.CalcJumpDirection(3, 4, out var horizontal, out var local);

            AssertVector(new Vector3(0.6f, 0, 0.8f), horizontal);
            Assert.AreEqual(horizontal, local);
        }

        [Test]
        public void KickAndJump_Ground_JumpsUp()
        {
            var vec = PlayerMoveCalculator.KickAndJump(CreateParams(), TouchState.Ground, Vector3.up, Vector3.forward);

            AssertVector(Vector3.up * 8f, vec);
        }

        [Test]
        public void KickAndJump_WallTowardWall_JumpsUpHigher()
        {
            // 壁の法線は -Z、入力は +Z（壁に向かう）
            var vec = PlayerMoveCalculator.KickAndJump(CreateParams(), TouchState.Wall, Vector3.back, Vector3.forward);

            AssertVector(Vector3.up * 12f, vec);
        }

        [Test]
        public void KickAndJump_WallAwayFromWall_KicksHorizontally()
        {
            var vec = PlayerMoveCalculator.KickAndJump(CreateParams(), TouchState.Wall, Vector3.back, Vector3.back);

            AssertVector(Vector3.back * 8f, vec);
        }

        [Test]
        public void KickAndJump_NotTouched_Zero()
        {
            var vec = PlayerMoveCalculator.KickAndJump(CreateParams(), TouchState.None, Vector3.zero, Vector3.forward);

            Assert.AreEqual(Vector3.zero, vec);
        }

        [Test]
        public void CalcQuickLocalVector_NoInputOnGround_ForwardAndSlightlyUp()
        {
            var vec = PlayerMoveCalculator.CalcQuickLocalVector(CreateParams(), 0, 0, isFloating: false);

            AssertVector(new Vector3(0, 0.2f, 1), vec);
        }

        [Test]
        public void CalcQuickLocalVector_InputInAir_Horizontal()
        {
            var vec = PlayerMoveCalculator.CalcQuickLocalVector(CreateParams(), -2, 0, isFloating: true);

            AssertVector(Vector3.left, vec);
        }

        [Test]
        public void CalcHyperBoostInput_NoInput_Forward()
        {
            Assert.AreEqual(Vector2.up, PlayerMoveCalculator.CalcHyperBoostInput(0, 0));
        }

        [TestCase(true, 0f, 1f, true)]    // ジャンプした
        [TestCase(false, 0f, 0f, true)]   // 入力が無い
        [TestCase(false, 0f, -1f, true)]  // 逆向き
        [TestCase(false, 1f, 1f, false)]  // 斜め前（同じ向き）
        public void ShouldCancelHyperBoost(bool isJump, float inputX, float inputZ, bool expected)
        {
            Assert.AreEqual(expected, PlayerMoveCalculator.ShouldCancelHyperBoost(isJump, inputX, inputZ, Vector2.up));
        }

        [Test]
        public void CalcAccel_OnGround_AcceleratesTowardInput()
        {
            var state = new PlayerMoveState { IsTouched = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, false, Vector3.zero, Delta);

            // VecSpeed * delta / AccelerateTime = 10 * 0.02 / 0.5
            AssertVector(Vector3.forward * 0.4f, vec);
        }

        [Test]
        public void CalcAccel_Boost_MultipliesHorizontalBoostRate()
        {
            var state = new PlayerMoveState { IsBoost = true, IsTouched = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, false, Vector3.zero, Delta);

            AssertVector(Vector3.forward * 0.8f, vec);
        }

        [Test]
        public void CalcAccel_FloatingBoost_HalvesAcceleration()
        {
            var state = new PlayerMoveState { IsBoost = true, IsFloating = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, false, Vector3.zero, Delta);

            AssertVector(Vector3.forward * 0.4f, vec);
        }

        [Test]
        public void CalcAccel_FloatingWithoutBoost_NoAcceleration()
        {
            var state = new PlayerMoveState { IsFloating = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, false, Vector3.zero, Delta);

            Assert.AreEqual(Vector3.zero, vec);
        }

        [Test]
        public void CalcAccel_AtLimitSpeed_NoAcceleration()
        {
            var state = new PlayerMoveState { IsTouched = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.forward * 10f, false, Vector3.zero, Delta);

            Assert.AreEqual(Vector3.zero, vec);
        }

        [Test]
        public void CalcAccel_HyperBoost_AddsBoostVector()
        {
            var state = new PlayerMoveState { IsBoost = true, IsHyperBoost = true, IsTouched = true };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, false, Vector3.forward * 40f, Delta);

            // 入力方向 + ブーストベクトル * (delta / AccelerateTime) = 1 + 40 * 0.04
            AssertVector(Vector3.forward * 2.6f, vec);
        }

        [Test]
        public void CalcAccel_TowardWall_CancelsNormalComponent()
        {
            // 法線 -Z の壁に向かって斜めに進む → 壁に向かう Z 成分が打ち消され、X 成分だけ残る
            var state = new PlayerMoveState { IsFloating = true, IsBoost = true, IsTouched = true, IsWallTouched = true, TouchNormal = Vector3.back };
            var dir = new Vector3(1, 0, 1).normalized;

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, dir, Vector3.zero, false, Vector3.zero, Delta);

            Assert.AreEqual(0f, vec.z, 1e-5f);
            Assert.Greater(vec.x, 0f);
        }

        [Test]
        public void CalcAccel_JumpingOnWall_NoAcceleration()
        {
            var state = new PlayerMoveState { IsFloating = true, IsBoost = true, IsTouched = true, IsWallTouched = true, TouchNormal = Vector3.back };

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, Vector3.forward, Vector3.zero, true, Vector3.zero, Delta);

            Assert.AreEqual(Vector3.zero, vec);
        }

        [Test]
        public void CalcAccel_Turning_DoesNotExceedLimitSpeed()
        {
            // 上限の速さ（10）で前進中に 45 度旋回した → 入力方向への射影は約 7.07 なので加速の判定は通るが、
            // 加速後の水平方向の速さは上限を超えない
            var state = new PlayerMoveState { IsTouched = true };
            var velocity = Vector3.forward * 10f;
            var dir = new Vector3(1, 0, 1).normalized;

            var vec = PlayerMoveCalculator.CalcAccel(CreateParams(), state, true, dir, velocity, false, Vector3.zero, Delta);

            var next = velocity + vec;
            Assert.LessOrEqual(new Vector3(next.x, 0, next.z).magnitude, 10f + 1e-4f);
            // 向きは入力方向へ曲がる
            Assert.Greater(vec.x, 0f);
        }

        [Test]
        public void CalcAccel_KeepTurning_SpeedStaysAtLimit()
        {
            // 上限の速さで走りながら毎ステップ入力方向を回し続けても、水平方向の速さは上限に留まる
            var p = CreateParams();
            var state = new PlayerMoveState { IsTouched = true };
            var velocity = Vector3.forward * 10f;

            for (int i = 1; i <= 200; i++)
            {
                var dir = Quaternion.Euler(0, i * 3f, 0) * Vector3.forward;
                velocity += PlayerMoveCalculator.CalcAccel(p, state, true, dir, velocity, false, Vector3.zero, Delta);
            }

            Assert.LessOrEqual(new Vector3(velocity.x, 0, velocity.z).magnitude, 10f + 1e-3f);
        }

        [Test]
        public void ClampHorizontalSpeed_BelowLimit_Unchanged()
        {
            var accel = new Vector3(0.4f, 0.1f, 0);

            var vec = PlayerMoveCalculator.ClampHorizontalSpeed(accel, Vector3.forward * 5f, 10f);

            AssertVector(accel, vec);
        }

        [Test]
        public void ClampHorizontalSpeed_AlreadyOverLimit_KeepsCurrentSpeedAndYComponent()
        {
            // クイック直後などで上限（10）を超えている（15）ときは、速さを増やさずに向きだけ変える
            var velocity = Vector3.forward * 15f;
            var accel = new Vector3(1f, 0.5f, 0);

            var vec = PlayerMoveCalculator.ClampHorizontalSpeed(accel, velocity, 10f);

            var next = velocity + vec;
            Assert.AreEqual(15f, new Vector3(next.x, 0, next.z).magnitude, 1e-4f);
            Assert.AreEqual(0.5f, vec.y, 1e-5f);
            Assert.Greater(vec.x, 0f);
        }

        [Test]
        public void StepImpulse_NotActive_ReducesInterval()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up, Vector3.up, 0.3f);
            info.ResetJump();

            var vec = PlayerMoveCalculator.StepImpulse(ref info, 0.1f, 0.5f, false, Delta, out var ended);

            Assert.AreEqual(Vector3.zero, vec);
            Assert.IsFalse(ended);
            Assert.AreEqual(0.28f, info.interval, 1e-5f);
        }

        [Test]
        public void StepImpulse_DuringPush_AddsImpulseAndAdvancesTime()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up * 8f, Vector3.up, 0.3f);

            var vec = PlayerMoveCalculator.StepImpulse(ref info, 0.1f, 0.5f, true, Delta, out var ended);

            // jumpVector * delta / pushTime = 8 * 0.02 / 0.1
            AssertVector(Vector3.up * 1.6f, vec);
            Assert.IsFalse(ended);
            Assert.IsTrue(info.isJump);
            Assert.AreEqual(Delta, info.jumpSumTime, 1e-6f);
        }

        [Test]
        public void StepImpulse_AfterPushAndTouched_Ends()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up * 8f, Vector3.up, 0.3f);
            info.UpdateJumpSumTime(0.2f);

            var vec = PlayerMoveCalculator.StepImpulse(ref info, 0.1f, 0.5f, true, Delta, out var ended);

            Assert.AreEqual(Vector3.zero, vec);
            Assert.IsTrue(ended);
            Assert.IsFalse(info.isJump);
        }

        [Test]
        public void StepImpulse_AfterPushInAir_ContinuesUntilEndTime()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up * 8f, Vector3.up, 0.3f);
            info.UpdateJumpSumTime(0.2f);

            PlayerMoveCalculator.StepImpulse(ref info, 0.1f, 0.5f, false, Delta, out var ended);
            Assert.IsFalse(ended);

            info.UpdateJumpSumTime(0.49f);
            PlayerMoveCalculator.StepImpulse(ref info, 0.1f, 0.5f, false, Delta, out ended);
            Assert.IsTrue(ended);
        }

        [TestCase(true, false, false, false, true)]
        [TestCase(true, true, false, false, false)]
        [TestCase(true, false, true, false, false)]
        [TestCase(true, false, false, true, false)]
        [TestCase(false, false, false, false, false)]
        public void ShouldBrake(bool isBoost, bool hasInput, bool isJump, bool isQuick, bool expected)
        {
            Assert.AreEqual(expected, PlayerMoveCalculator.ShouldBrake(isBoost, hasInput, isJump, isQuick));
        }

        [Test]
        public void CalcBrake_CancelsHorizontalVelocity()
        {
            var vec = PlayerMoveCalculator.CalcBrake(CreateParams(), new Vector3(5, -3, 10), false, Delta);

            // -velocity(XZ) * AirBrakeRate * delta
            AssertVector(new Vector3(-0.1f, 0, -0.2f), vec);
        }

        [Test]
        public void CalcBrake_Touched_AddsTouchBrakeRate()
        {
            var vec = PlayerMoveCalculator.CalcBrake(CreateParams(), new Vector3(5, 0, 0), true, Delta);

            // (AirBrakeRate + TouchBrakeRateAdd) = 3
            AssertVector(new Vector3(-0.3f, 0, 0), vec);
        }

        [Test]
        public void CalcFloatForce_CancelsPartOfGravity()
        {
            var vec = PlayerMoveCalculator.CalcFloatForce(CreateParams(), -9.8f, Delta);

            // 9.8 * 0.02 * 0.5
            AssertVector(Vector3.up * 0.098f, vec);
        }

        [TestCase(true, true, -1f, true)]
        [TestCase(true, true, 1f, false)]
        [TestCase(false, true, -1f, false)]
        [TestCase(true, false, -1f, false)]
        public void ShouldFloat(bool isFloating, bool isBoost, float velocityY, bool expected)
        {
            Assert.AreEqual(expected, PlayerMoveCalculator.ShouldFloat(isFloating, isBoost, velocityY));
        }

        [TestCase(true, true, true, true, true)]
        [TestCase(false, true, true, true, false)]
        [TestCase(true, false, true, true, false)]
        [TestCase(true, true, false, true, false)]
        [TestCase(true, true, true, false, false)]
        public void ShouldAscend(bool hasInput, bool isFloating, bool isBoost, bool canAscend, bool expected)
        {
            Assert.AreEqual(expected, PlayerMoveCalculator.ShouldAscend(hasInput, isFloating, isBoost, canAscend));
        }

        [Test]
        public void CalcAscendForce_BelowLimit_AcceleratesByAscendAccel()
        {
            // 重力を加えた後の速度が vy + AscendAccel * delta になる: 0 + 10 * 0.02 = 0.2
            var vec = PlayerMoveCalculator.CalcAscendForce(CreateParams(), 0f, -9.8f, Delta);
            Assert.AreEqual(0.2f, 0f + vec.y + (-9.8f * Delta), 1e-5f);
            Assert.AreEqual(0f, vec.x);
            Assert.AreEqual(0f, vec.z);
        }

        [Test]
        public void CalcAscendForce_Falling_DeceleratesByAscendAccel()
        {
            var vec = PlayerMoveCalculator.CalcAscendForce(CreateParams(), -2f, -9.8f, Delta);
            Assert.AreEqual(-1.8f, -2f + vec.y + (-9.8f * Delta), 1e-5f);
        }

        [Test]
        public void CalcAscendForce_NearLimit_ClampsToAscendSpeed()
        {
            var vec = PlayerMoveCalculator.CalcAscendForce(CreateParams(), 2.9f, -9.8f, Delta);
            Assert.AreEqual(3f, 2.9f + vec.y + (-9.8f * Delta), 1e-5f);
        }

        [Test]
        public void CalcAscendForce_AtLimit_KeepsAscendSpeed()
        {
            var vec = PlayerMoveCalculator.CalcAscendForce(CreateParams(), 3f, -9.8f, Delta);
            Assert.AreEqual(3f, 3f + vec.y + (-9.8f * Delta), 1e-5f);
        }

        [Test]
        public void CalcAscendForce_AboveLimit_NoForce()
        {
            // ジャンプ直後など、重力を加えても上限速度を超えているときは何もしない
            AssertVector(Vector3.zero, PlayerMoveCalculator.CalcAscendForce(CreateParams(), 8f, -9.8f, Delta));
        }

        [Test]
        public void TryCalcRotateDegree_BelowThreshold_NoRotation()
        {
            Assert.IsFalse(PlayerMoveCalculator.TryCalcRotateDegree(CreateParams(), 0.05f, Delta, out var degree));
            Assert.AreEqual(0f, degree);
        }

        [Test]
        public void TryCalcRotateDegree_AboveThreshold_Rotates()
        {
            Assert.IsTrue(PlayerMoveCalculator.TryCalcRotateDegree(CreateParams(), -0.5f, Delta, out var degree));
            Assert.AreEqual(-0.9f, degree, 1e-5f);
        }
    }

    public class PlayerEffectPresenterTests
    {
        [Test]
        public void CalcBoostBit_NotBoost_Zero()
        {
            Assert.AreEqual(0, PlayerEffectPresenter.CalcBoostBit(false, false, false, 1, 1));
        }

        [Test]
        public void CalcBoostBit_Floating_FloatingBit()
        {
            Assert.AreEqual(ActionUtils.FloatingBit, PlayerEffectPresenter.CalcBoostBit(true, true, false, 1, 0));
        }

        [Test]
        public void CalcBoostBit_OnGround_InputDirection()
        {
            Assert.AreEqual(ActionUtils.ConvertBitXZ(1, -1), PlayerEffectPresenter.CalcBoostBit(true, false, false, 1, -1));
        }

        [Test]
        public void CalcBoostBit_Ascend_AllDirections()
        {
            Assert.AreEqual(ActionUtils.AscendBit, PlayerEffectPresenter.CalcBoostBit(true, true, true, 1, 0));
        }

        [Test]
        public void IsImpulseStarted_OnlyOnFirstFrame()
        {
            var info = new JumpInfo();
            Assert.IsFalse(PlayerEffectPresenter.IsImpulseStarted(info));

            info.SetJump(Vector3.up, Vector3.up, 0.3f);
            Assert.IsTrue(PlayerEffectPresenter.IsImpulseStarted(info));

            info.UpdateJumpSumTime(0.02f);
            Assert.IsFalse(PlayerEffectPresenter.IsImpulseStarted(info));
        }

        [Test]
        public void CalcHyperBoostBit_OffIsZero_OnIsBoostDirection()
        {
            var info = new HyperBoostInfo();
            Assert.AreEqual(0, PlayerEffectPresenter.CalcHyperBoostBit(info));

            info.SetBoost(Vector2.up, 40f);
            Assert.AreEqual(ActionUtils.ConvertBit(Vector3.forward * 40f), PlayerEffectPresenter.CalcHyperBoostBit(info));
        }
    }
}
