using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    public class LockSiteDisplay : MonoBehaviour
    {
        [SerializeField]
        RingImage cicle = null;

        [SerializeField]
        ObjectPool<LockImage> lockMarkPool;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        BaseObject[] targetArray = new BaseObject[256];
        int targetCount;

        int lockCircleSize = 0;

        Camera cam = null;

        [SerializeField]
        LockTargetImage lockTargetImage;

        [SerializeField]
        float leftLockCompletionTime = 1.0f;

        [SerializeField]
        float rightLockCompletionTime = 1.0f;

        CurrentLockStateInfo lockStateInfo;
        long currentLockTargetId = long.MinValue;

        private void Awake()
        {
            lockMarkPool.Initialize();
        }

        void Update()
        {
            var pc = PlayerManager.Instance.GetPlayer(playerId);
            if (pc == null)
                return;

            var param = pc.CharacterParam;
            if (param == null)
                return;

            if (lockStateInfo == null)
                lockStateInfo = new CurrentLockStateInfo(leftLockCompletionTime, rightLockCompletionTime);

            lockStateInfo.AddTime(Time.deltaTime);

            if (lockCircleSize != param.LockCircleSize)
            {
                lockCircleSize = param.LockCircleSize;
                cicle.rectTransform.sizeDelta = Vector2.one * lockCircleSize;
            }

            UpdateMarks(pc.transform, param.LockLength, pc.Side);
        }

        private List<LockImage> returnList = new List<LockImage>();

        private void UpdateMarks(Transform playerTrans, int lockLength, UnitSide side)
        {
            targetCount = PhysicsUtils.OverlapShpereOthers(playerTrans.position, lockLength, UnitSide.None, GameLayers.TargetableLayerMask, string.Empty, targetArray);

            cam = cam ?? Camera.main;

            var center = 0.5f * new Vector3(Screen.width, Screen.height);
            var widthSqrMax = center.x * center.x;
            var heightSqrMax = center.y * center.y;

            for (int i = 0; i < targetCount; i++)
            {
                var tgt = targetArray[i];
                var index = lockMarkPool.activeList.FindIndex(m => m.ID == tgt.ID);
                if (index >= 0)
                    continue;

                var pos = cam.WorldToScreenPoint(tgt.transform.position) - center;
                if (pos.z < 0)
                    continue;

                if (pos.x * pos.x > widthSqrMax || pos.y * pos.y > heightSqrMax)
                    continue;

                var mark = lockMarkPool.Borrow();
                mark.SetTarget(tgt, cam, lockCircleSize);
            }

            for (int i = 0; i < lockMarkPool.ActiveCount; i++)
            {
                var mark = lockMarkPool.activeList[i];
                if (!mark.IsInside || !mark.IsTargetActive)
                    returnList.Add(mark);
            }

            foreach (var m in returnList)
                lockMarkPool.Return(m);

            returnList.Clear();

            if (lockTargetImage == null)
                return;

            LockImage closestMark = null;
            float closestDistSqr = float.MaxValue;
            for (int i = 0; i < lockMarkPool.ActiveCount; i++)
            {
                var mark = lockMarkPool.activeList[i];
                if (mark.IsInCircle && mark.WorldDistanceSqr < closestDistSqr)
                {
                    closestDistSqr = mark.WorldDistanceSqr;
                    closestMark = mark;
                }
            }

            var newTargetId = closestMark != null ? closestMark.ID : long.MinValue;
            if (newTargetId != currentLockTargetId)
            {
                currentLockTargetId = newTargetId;
                lockStateInfo?.ResetDuration();
                lockTargetImage.SetTarget(closestMark?.Target, cam, lockStateInfo);
            }
            var shouldBeActive = currentLockTargetId != long.MinValue;
            if (lockTargetImage.gameObject.activeSelf != shouldBeActive)
                lockTargetImage.gameObject.SetActive(shouldBeActive);
        }
    }
}

