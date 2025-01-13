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

        BaseCharacter[] targetArray = new BaseCharacter[256];
        int targetCount;

        int lockCircleSize = 0;

        Camera cam = null;

        void Update()
        {
            var pc = PlayerManager.Instance.GetPlayer(playerId);
            if (pc == null)
                return;

            var param = pc.CharacterParam;
            if (param == null)
                return;

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
            targetCount = PhysicsUtils.OverlapShpere(playerTrans.position, lockLength, side, -1, "Player", targetArray);

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
                mark.SetTarget(tgt, cam);
            }

            for (int i = 0; i < lockMarkPool.ActiveCount; i++)
            {
                var mark = lockMarkPool.activeList[i];
                if (!mark.IsInside)
                    returnList.Add(mark);
            }

            foreach (var m in returnList)
                lockMarkPool.Return(m);

            returnList.Clear();
        }
    }
}

