using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 移動の計算に使う接地状態とブーストの状態
    /// </summary>
    public struct PlayerMoveState
    {
        public bool IsBoost;
        public bool IsHyperBoost;
        /// <summary>地面に立っていない（空中または壁に接触）</summary>
        public bool IsFloating;
        /// <summary>地面か壁に接触している</summary>
        public bool IsTouched;
        public bool IsWallTouched;
        /// <summary>接触面の法線（接触していなければ zero）</summary>
        public Vector3 TouchNormal;
    }

    /// <summary>
    /// PlayerController の移動・ジャンプ・クイック・ハイパーブースト・回転の計算。
    /// Unity のシーンや Transform に依存しないので、EditMode テストで確認できる。
    /// ローカル座標とワールド座標の変換は PlayerController が行う。
    /// </summary>
    public static class PlayerMoveCalculator
    {
        public static bool HasInput(float inputX, float inputZ)
        {
            return inputX != 0 || inputZ != 0;
        }

        /// <summary>水平方向の速度の上限</summary>
        public static float LimitSpeed(in PlayerMoveParams p, bool isBoost, bool isHyperBoost)
        {
            if (isBoost)
            {
                if (isHyperBoost)
                    return p.HyperBoostSpeed;
                else
                    return p.VecSpeed * p.HorizontalBoostRate;
            }
            else
                return p.VecSpeed;
        }

        #region 入力から方向を決める

        /// <summary>
        /// ジャンプの方向（ローカル座標）。入力が無ければ horizontal は zero、local は真上
        /// </summary>
        public static void CalcJumpDirection(float inputX, float inputZ, out Vector3 horizontal, out Vector3 local)
        {
            if (!HasInput(inputX, inputZ))
            {
                horizontal = Vector3.zero;
                local = Vector3.up;
            }
            else
            {
                horizontal = new Vector3(inputX, 0, inputZ);
                horizontal.Normalize();
                local = horizontal;
            }
        }

        /// <summary>
        /// 壁や地面を蹴ったときのジャンプのベクトル（ワールド座標）
        /// </summary>
        /// <param name="horizontal">入力方向（ワールド座標、正規化済み）。入力が無ければ zero</param>
        public static Vector3 KickAndJump(in PlayerMoveParams p, TouchState touch, Vector3 normal, Vector3 horizontal)
        {
            switch (touch)
            {
                case TouchState.Wall:
                    if (horizontal == Vector3.zero ||
                        Vector3.Dot(horizontal, normal) < 0) {
                        return Vector3.up * p.JumpSpeed * p.WallJumpRate;
                    }
                    else {
                        return horizontal * p.JumpSpeed;
                    }

                case TouchState.Ground:
                    return Vector3.up * p.JumpSpeed;

                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// クイックの方向（ローカル座標）。入力が無ければ前方。地上では少し上向きにする
        /// </summary>
        public static Vector3 CalcQuickLocalVector(in PlayerMoveParams p, float inputX, float inputZ, bool isFloating)
        {
            Vector3 localVec;
            if (!HasInput(inputX, inputZ))
            {
                localVec = Vector3.forward;
            }
            else
            {
                localVec = new Vector3(inputX, 0, inputZ);
                localVec.Normalize();
            }

            if (!isFloating)
            {
                localVec += Vector3.up * p.QuickYDelta;
            }

            return localVec;
        }

        /// <summary>ハイパーブーストの方向（入力の XZ）。入力が無ければ前方</summary>
        public static Vector2 CalcHyperBoostInput(float inputX, float inputZ)
        {
            if (!HasInput(inputX, inputZ))
                return Vector2.up;

            var localVec = new Vector2(inputX, inputZ);
            localVec.Normalize();
            return localVec;
        }

        /// <summary>
        /// ハイパーブーストをやめるか。ジャンプしたとき、入力が無いとき、開始時と逆向きの入力のとき
        /// </summary>
        public static bool ShouldCancelHyperBoost(bool isJump, float inputX, float inputZ, Vector2 boostInput)
        {
            if (isJump)
                return true;

            if (!HasInput(inputX, inputZ))
                return true;

            var local = new Vector2(inputX, inputZ);
            local.Normalize();
            return Vector2.Dot(boostInput, local) < 0;
        }

        #endregion

        #region 移動

        /// <summary>
        /// 入力による加速（ForceMode.VelocityChange で加える値）
        /// </summary>
        /// <param name="hasInput">移動の入力があるか（HasInput の結果）</param>
        /// <param name="inputDir">入力方向（ワールド座標、正規化済み）。入力が無ければ zero</param>
        /// <param name="isJump">ジャンプ中か（ジャンプの推力を加える前の状態）</param>
        /// <param name="hyperBoostVector">ハイパーブーストのベクトル（ワールド座標）</param>
        public static Vector3 CalcAccel(in PlayerMoveParams p, in PlayerMoveState state, bool hasInput, Vector3 inputDir,
                                        Vector3 velocity, bool isJump, Vector3 hyperBoostVector, float delta)
        {
            var hVec = inputDir;
            bool canAccel = hasInput;

            var dotVec = velocity.x * hVec.x + velocity.z * hVec.z;
            var limitSpeed = LimitSpeed(p, state.IsBoost, state.IsHyperBoost);

            canAccel &= dotVec < limitSpeed;
            canAccel &= !isJump || !state.IsWallTouched;
            canAccel &= !state.IsFloating || state.IsBoost;

            Vector3 vec = Vector3.zero;
            if (!canAccel)
                return vec;

            var accelTime = p.AccelerateTime;
            if (state.IsFloating)
                accelTime *= 2;

            if (state.IsHyperBoost)
            {
                hVec += hyperBoostVector * (delta / accelTime);
            }
            else
            {
                hVec *= p.VecSpeed * delta / accelTime;

                if (state.IsBoost)
                    hVec *= p.HorizontalBoostRate;
            }

            vec += hVec;

            if (state.IsWallTouched)
            {
                // 壁に向かう成分を打ち消す
                var dot = Vector3.Dot(state.TouchNormal, vec.normalized);
                if (dot < 0)
                {
                    var normal = -vec.magnitude * state.TouchNormal * dot;
                    vec += new Vector3(normal.x, 0, normal.z);
                }
            }

            return ClampHorizontalSpeed(vec, velocity, limitSpeed);
        }

        /// <summary>
        /// 加速後の水平方向の速さが上限を超えないよう、加速の XZ 成分を抑える（Y 成分はそのまま）。
        /// 加速するかどうかは入力方向への速度の射影で判定しているため、旋回中は射影が上限より小さくなり、
        /// 入力方向と垂直な速度成分を残したまま加速し続けて上限を超えてしまう。それを防ぐ。
        /// すでに上限を超えている（ジャンプ・クイックの直後など）ときは、今の速さを上限として向きの変更だけ許す。
        /// </summary>
        public static Vector3 ClampHorizontalSpeed(Vector3 accel, Vector3 velocity, float limitSpeed)
        {
            var current = new Vector3(velocity.x, 0, velocity.z);
            var next = current + new Vector3(accel.x, 0, accel.z);

            var cap = Mathf.Max(limitSpeed, current.magnitude);
            if (next.sqrMagnitude <= cap * cap)
                return accel;

            next = next.normalized * cap;
            return new Vector3(next.x - current.x, accel.y, next.z - current.z);
        }

        /// <summary>
        /// ジャンプ・クイックの推力を1ステップ進め、加える値を返す。
        /// pushTime の間は推力を加え、endTime を過ぎるか、pushTime の後に接触したら終了する。
        /// 実行中でなければ、次に使えるまでの時間（interval）を減らす。
        /// </summary>
        /// <param name="ended">このステップで終了したか</param>
        public static Vector3 StepImpulse(ref JumpInfo info, float pushTime, float endTime, bool isTouched, float delta, out bool ended)
        {
            ended = false;
            if (!info.isJump)
            {
                info.ReduceIntervalTime(delta);
                return Vector3.zero;
            }

            Vector3 vec = Vector3.zero;
            bool forceReset = false;
            if (info.jumpSumTime < pushTime)
                vec = info.jumpVector * delta / pushTime;
            else
                forceReset = isTouched;

            info.UpdateJumpSumTime(info.jumpSumTime + delta);
            if (info.jumpSumTime >= endTime || forceReset)
            {
                info.ResetJump();
                ended = true;
            }

            return vec;
        }

        /// <summary>ブースト中に入力もジャンプもクイックも無ければブレーキをかける</summary>
        public static bool ShouldBrake(bool isBoost, bool hasInput, bool isJump, bool isQuick)
        {
            return isBoost && !hasInput && !isJump && !isQuick;
        }

        /// <summary>水平方向の速度を打ち消すブレーキ（加速の代わりに使う）</summary>
        public static Vector3 CalcBrake(in PlayerMoveParams p, Vector3 velocity, bool isTouched, float delta)
        {
            var vec = new Vector3(-velocity.x, 0, -velocity.z);
            var brake = p.AirBrakeRate;

            if (isTouched)
            {
                brake += p.TouchBrakeRateAdd;
            }

            vec *= brake * delta;
            return vec;
        }

        /// <summary>ブースト中に空中で落下しているときは、重力を一部打ち消す</summary>
        public static bool ShouldFloat(bool isFloating, bool isBoost, float velocityY)
        {
            return isFloating && isBoost && velocityY < 0;
        }

        public static Vector3 CalcFloatForce(in PlayerMoveParams p, float gravityY, float delta)
        {
            return -1.0f * Vector3.up * gravityY * delta * p.FloatRate;
        }

        /// <summary>
        /// グライド中の上昇をするか。空中・ブースト中に上昇の入力があり、エネルギーが残っていれば上昇する。
        /// ジャンプの推力が残っている間（ジャンプ直後）でも上昇できる
        /// </summary>
        public static bool ShouldAscend(bool hasAscendInput, bool isFloating, bool isBoost, bool canAscend)
        {
            return hasAscendInput && isFloating && isBoost && canAscend;
        }

        /// <summary>
        /// グライド中の上昇で加える値（ForceMode.VelocityChange）。重力を加えた後の上向きの速度が、
        /// 上限速度（AscendSpeed）まで加速度（AscendAccel）で加速するようにする。
        /// 下降中は加速度で減速してから上昇に転じる。上限速度を超えている（ジャンプ直後など）ときは何もしない（重力で上限速度まで減速する）
        /// </summary>
        public static Vector3 CalcAscendForce(in PlayerMoveParams p, float velocityY, float gravityY, float delta)
        {
            var natural = velocityY + gravityY * delta;
            if (natural >= p.AscendSpeed)
                return Vector3.zero;

            var next = Mathf.Min(velocityY + p.AscendAccel * delta, p.AscendSpeed);
            next = Mathf.Max(next, natural);
            return Vector3.up * (next - natural);
        }

        #endregion

        /// <summary>
        /// カメラ入力（X）による回転角（度）。入力が RotXMin 未満なら回転しない
        /// </summary>
        public static bool TryCalcRotateDegree(in PlayerMoveParams p, float cameraX, float delta, out float degree)
        {
            if (cameraX * cameraX < p.RotXMin * p.RotXMin)
            {
                degree = 0;
                return false;
            }

            degree = cameraX * p.RotSpeed * delta;
            return true;
        }
    }
}
