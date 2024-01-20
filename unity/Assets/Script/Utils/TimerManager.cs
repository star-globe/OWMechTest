using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class TimerManager : SingletonMonoBehaviour<TimerManager>
    {
        private void Update()
        {
            TimeUtils.UpdateTime();
        }
    }

    public static class TimeUtils
    {
        public static DateTime? now = null;
        public static DateTime Now
        {
            get
            {
                if (now == null)
                {
                    var instance = TimerManager.Instance;
                    UpdateTime();
                }

                return now.Value;
            }
        }

        public static void UpdateTime()
        {
            now = DateTime.Now;
        }
    }
}
