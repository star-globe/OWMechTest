namespace AdvancedGears
{
    public class CurrentLockStateInfo
    {
        public class LockStateEntry
        {
            public float Duration { get; private set; }
            public float CompletionTime { get; }

            public LockStateEntry(float completionTime)
            {
                CompletionTime = completionTime;
                Duration = 0f;
            }

            public void AddTime(float time)
            {
                Duration += time;
            }

            public void ResetDuration()
            {
                Duration = 0f;
            }

            public bool IsLocked => Duration >= CompletionTime;
        }

        private readonly LockStateEntry leftState;
        private readonly LockStateEntry rightState;

        public CurrentLockStateInfo(float leftCompletionTime, float rightCompletionTime)
        {
            leftState = new LockStateEntry(leftCompletionTime);
            rightState = new LockStateEntry(rightCompletionTime);
        }

        public void AddTime(float time)
        {
            leftState.AddTime(time);
            rightState.AddTime(time);
        }

        public void ResetDuration()
        {
            leftState.ResetDuration();
            rightState.ResetDuration();
        }

        public bool IsLeftLocked => leftState.IsLocked;
        public bool IsRightLocked => rightState.IsLocked;
    }
}
