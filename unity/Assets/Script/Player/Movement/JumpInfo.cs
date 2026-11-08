using UnityEngine;

namespace AdvancedGears
{
    public struct JumpInfo
    {
        public bool isJump { get; private set; }
        public float jumpSumTime { get; private set; }
        public Vector3 jumpVector { get; private set; }
        public Vector3 localNor { get; private set; }
        public float interval { get; private set; }

        public void SetJump(Vector3 jumpVec, Vector3 local, float interval)
        {
            isJump = true;
            jumpSumTime = 0.0f;
            jumpVector = jumpVec;
            this.localNor = local;
            this.interval = interval;
        }

        public void UpdateJumpSumTime(float sum)
        {
            jumpSumTime = sum;
        }

        public void ReduceIntervalTime(float cool)
        {
            interval -= cool;
        }

        public void ResetJump()
        {
            isJump = false;
            jumpSumTime = 0.0f;
            jumpVector = Vector3.zero;
        }
    }
}
