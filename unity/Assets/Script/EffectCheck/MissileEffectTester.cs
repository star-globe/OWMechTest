using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクト確認シーンのミサイルランチャー。台に付けたランチャーにゲーム内と同じ呼び方（ISubPart.Use）で一斉発射させ、
    /// 発射・飛行中の噴射・命中（前方の的）・撃墜（正面から撃ち返す弾）のエフェクトを確かめる。
    /// ロックはしないので、ミサイルは誘導せずに台の正面へまっすぐ飛ぶ
    /// </summary>
    public class MissileEffectTester : PartEffectTester
    {
        [Header("操作")]
        [SerializeField] Button fireButton;
        [SerializeField] Button shootDownButton;
        [SerializeField] Button targetButton;

        [Header("的と撃ち返し")]
        [Tooltip("命中を確かめる的（コライダー付き）。ボタンで出し入れする")]
        [SerializeField] GameObject targetWall;

        [Tooltip("撃墜を確かめるときに、ランチャーへ向けて弾を撃ち返す位置")]
        [SerializeField] Transform counterMuzzle;

        [Tooltip("撃ち返す弾の ID（通常弾）")]
        [SerializeField] int counterBulletId = 1;

        [Tooltip("ミサイルの発射から撃ち返すまでの遅れ（秒）")]
        [SerializeField] float counterDelay = 0.05f;

        [Tooltip("照準点（台の正面）までの距離（m）")]
        [SerializeField] float aimDistance = 200.0f;

        ISubPart subPart;
        Coroutine counterRoutine;

        protected override void Awake()
        {
            base.Awake();

            fireButton?.onClick.AddListener(OnClickFire);
            shootDownButton?.onClick.AddListener(OnClickShootDown);
            targetButton?.onClick.AddListener(OnClickTarget);
        }

        protected override IEnumerable<PartSettings> GetCandidates()
        {
            var list = new List<PartSettings>();
            foreach (var settings in GunMaster.Instance.GetDictionary().Values)
            {
                if (settings != null && settings.CategoryType == PartCategoryType.Missile)
                    list.Add(settings);
            }

            list.Sort((a, b) => a.PartID.CompareTo(b.PartID));
            return list;
        }

        protected override void OnSpawned(PartComponent part)
        {
            subPart = part as ISubPart;
            if (subPart == null)
                Debug.LogErrorFormat(this, "[MissileEffectTester] {0} は ISubPart ではありません", part.name);
        }

        /// <summary>ロックなしで撃つ。照準点は台の正面</summary>
        bool Fire()
        {
            if (subPart == null || CurrentPart == null)
                return false;

            var origin = CurrentPart.transform;
            var request = new AimRequest(null, LockLevel.None, origin.position + origin.forward * aimDistance, Vector3.zero);
            subPart.Use(request);
            return true;
        }

        private void OnClickFire()
        {
            Fire();
        }

        private void OnClickShootDown()
        {
            if (!Fire() || counterMuzzle == null)
                return;

            if (counterRoutine != null)
                StopCoroutine(counterRoutine);
            counterRoutine = StartCoroutine(CounterFire());
        }

        /// <summary>一斉発射の 1 発ごとに、正面から台へ向けて弾を 1 発ずつ撃ち返す</summary>
        IEnumerator CounterFire()
        {
            var settings = GunMaster.Instance.GetSettings(CurrentPartId);
            var count = settings != null ? settings.BurstCount : 1;
            var interval = settings != null ? settings.BurstInterval : 0f;

            yield return new WaitForSeconds(counterDelay);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    yield return new WaitForSeconds(interval);

                if (CurrentPart == null)
                    break;

                var from = counterMuzzle.position;
                var dir = CurrentPart.transform.position - from;
                BulletManager.Instance.Fire(counterBulletId, from, dir, Vector3.zero);
            }

            counterRoutine = null;
        }

        private void OnClickTarget()
        {
            if (targetWall == null)
                return;

            targetWall.SetActive(!targetWall.activeSelf);
            UpdateStatus();
        }

        protected override string GetStatus()
        {
            var target = targetWall != null && targetWall.activeSelf ? "あり" : "なし";
            return $"的: {target}（撃墜は的の手前から撃ち返す）";
        }
    }
}
