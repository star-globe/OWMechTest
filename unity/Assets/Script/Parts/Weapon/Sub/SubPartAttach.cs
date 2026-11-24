using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>
    /// サブ（肩）の取り付け位置。ISubPart を実装したパーツを付ける（銃に限らない）。
    /// サブに付けられないカテゴリのパーツ（PartSlotRule）は付けない
    /// </summary>
    public class SubPartAttach : MonoBehaviour
    {
        [SerializeField]
        PartConnector connector;

        ISubPart subPart = null;

        private void Awake()
        {
            Assert.IsNotNull(connector);
        }

        /// <summary>サブ装備を差し替える。負の ID は未装備</summary>
        public void SetPart(int partId)
        {
            subPart = null;
            connector.Exchange(CreatePart(partId));
        }

        GameObject CreatePart(int partId)
        {
            if (partId < 0)
                return null;

            var settings = PartMaster.Instance.GetSettings(partId);
            if (settings == null)
                return null;

            if (!PartSlotRule.CanEquip(PartsAttachType.Weapon_Sub, settings.CategoryType))
            {
                Debug.LogErrorFormat(this, "[SubPartAttach] サブに付けられないカテゴリのパーツです（ID: {0}、カテゴリ: {1}）", partId, settings.CategoryType);
                return null;
            }

            var part = settings.InstantiatePart();
            if (part == null)
                return null;

            if (part is not ISubPart sub)
            {
                Debug.LogErrorFormat(this, "[SubPartAttach] パーツ（ID: {0}）に ISubPart を実装したコンポーネントがありません", partId);
                Destroy(part.gameObject);
                return null;
            }

            subPart = sub;
            return part.gameObject;
        }

        /// <summary>二次ロックになるまでの時間（秒）。サブ装備が無ければ二次ロックにならない</summary>
        public float SecondaryLockTime => subPart != null ? subPart.SecondaryLockTime : AdvancedGears.SecondaryLockTime.Never;

        public void Use(in AimRequest request)
        {
            subPart?.Use(request);
        }
    }
}
