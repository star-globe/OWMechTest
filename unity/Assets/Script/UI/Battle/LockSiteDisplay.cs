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

        long currentLockTargetId = LockOnState.NoTargetId;

        // ロックの状態はプレイヤーの機体（PlayerController）が持つ。ここでは目標を選んで渡し、表示するだけ
        PlayerController controller;
        long controllerPlayerId = long.MinValue;

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

            if (controller == null || controllerPlayerId != pc.ID)
            {
                controller = pc.GetComponent<PlayerController>();
                controllerPlayerId = pc.ID;
            }

            if (lockCircleSize != param.LockCircleSize)
            {
                lockCircleSize = param.LockCircleSize;
                cicle.rectTransform.sizeDelta = Vector2.one * lockCircleSize;
            }

            UpdateMarks(param.LockLength / GlobalParamMaster.Instance.WorldSizeRate, pc.Side);
        }

        private List<LockImage> returnList = new List<LockImage>();

        private void UpdateMarks(float lockLength, UnitSide side)
        {
            cam = cam ?? Camera.main;

            targetCount = PhysicsUtils.OverlapShpereOthers(cam.transform.position, lockLength, 1 << (int)side, GameLayers.TargetableLayerMask, null, targetArray);

            var center = 0.5f * new Vector3(Screen.width, Screen.height);
            var widthSqrMax = center.x * center.x;
            var heightSqrMax = center.y * center.y;

            for (int i = 0; i < targetCount; i++)
            {
                var tgt = targetArray[i];

                bool alreadyActive = false;
                for (int j = 0; j < lockMarkPool.ActiveCount; j++)
                {
                    if (lockMarkPool.activeList[j].ID == tgt.ID)
                    {
                        alreadyActive = true;
                        break;
                    }
                }
                if (alreadyActive)
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
                if (!mark.IsInside || mark.WorldDistanceSqr > lockLength * lockLength || !mark.IsTargetActive)
                    returnList.Add(mark);
            }

            foreach (var m in returnList)
                lockMarkPool.Return(m);

            returnList.Clear();

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

            // 射撃の目標としてプレイヤーに渡す（同じ目標なら二次ロックまでの時間はそのまま）
            var newTarget = closestMark != null ? closestMark.Target : null;
            if (controller != null)
                controller.SetLockTarget(newTarget);

            if (lockTargetImage == null)
                return;

            var newTargetId = closestMark != null ? closestMark.ID : LockOnState.NoTargetId;
            if (newTargetId != currentLockTargetId)
            {
                currentLockTargetId = newTargetId;
                lockTargetImage.SetTarget(newTarget, cam, controller);
            }
            var shouldBeActive = currentLockTargetId != LockOnState.NoTargetId;
            if (lockTargetImage.gameObject.activeSelf != shouldBeActive)
                lockTargetImage.gameObject.SetActive(shouldBeActive);
        }
    }
}

