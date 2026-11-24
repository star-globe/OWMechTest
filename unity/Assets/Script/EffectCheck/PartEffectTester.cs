using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクト確認シーンで、パーツ単体をマスタから選んで台に呼び出す処理の基底クラス。
    /// パーツの一覧の表示・生成・差し替えを受け持ち、操作（噴射・発射など）は種類ごとの派生クラスで行う。
    /// 武器に広げるときは MissileEffectTester のような派生クラスを足す
    /// </summary>
    public abstract class PartEffectTester : MonoBehaviour
    {
        [Header("パーツ")]
        [Tooltip("呼び出したパーツを付ける位置")]
        [SerializeField] Transform mount;

        [Header("UI")]
        [SerializeField] ObjectPool<PartsSelectButton> partsPool;
        [SerializeField] TextMeshProUGUI statusText;

        [Header("カメラ（テスターが複数あるとき、ボタンでこのテスターを見る）")]
        [SerializeField] EffectCheckCamera checkCamera;
        [SerializeField] Transform cameraTarget;
        [SerializeField] float cameraDistance = 4.0f;
        [SerializeField] Button focusButton;

        PartComponent currentPart;
        int currentPartId = -1;

        protected PartComponent CurrentPart => currentPart;
        protected int CurrentPartId => currentPartId;

        /// <summary>選べるパーツ（マスタにある全件）</summary>
        protected abstract IEnumerable<PartSettings> GetCandidates();

        /// <summary>パーツを生成して台に付けた直後に呼ぶ</summary>
        protected virtual void OnSpawned(PartComponent part) { }

        /// <summary>ステータスの表示に足す文言（操作の状態など）</summary>
        protected virtual string GetStatus() => string.Empty;

        protected virtual void Awake()
        {
            Assert.IsNotNull(mount);

            // MainScene の初期化を通らずに Play するシーンなので、マスタの読み込みなどをここで行う
            DebugBootstrap.EnsureInitialized();

            focusButton?.onClick.AddListener(Focus);
        }

        /// <summary>カメラをこのテスターに向ける</summary>
        public void Focus()
        {
            if (checkCamera != null && cameraTarget != null)
                checkCamera.SetTarget(cameraTarget, cameraDistance);
        }

        protected virtual void Start()
        {
            partsPool.Initialize();
            ShowCandidates();
        }

        void ShowCandidates()
        {
            partsPool.ReturnAll();

            var first = -1;
            foreach (var settings in GetCandidates())
            {
                if (settings == null)
                    continue;

                if (first < 0)
                    first = settings.PartID;

                var btn = partsPool.Borrow();
                btn.SetData(settings, Spawn);
            }

            if (first >= 0)
                Spawn(first);
            else
                UpdateStatus();
        }

        /// <summary>パーツを生成して台に付ける。付いていたパーツは外す</summary>
        public void Spawn(int partId)
        {
            if (currentPart != null)
                Destroy(currentPart.gameObject);

            currentPart = null;
            currentPartId = -1;

            var settings = PartMaster.Instance.GetSettings(partId);
            var part = settings?.InstantiatePart();
            if (part != null)
            {
                part.transform.SetParent(mount, false);
                part.transform.localPosition = Vector3.zero;
                part.transform.localRotation = Quaternion.identity;

                currentPart = part;
                currentPartId = partId;
                OnSpawned(part);
            }
            else
            {
                Debug.LogErrorFormat(this, "[PartEffectTester] パーツ（ID: {0}）を呼び出せませんでした", partId);
            }

            UpdateStatus();
        }

        /// <summary>ステータスの表示を更新する。操作の状態が変わったら派生クラスから呼ぶ</summary>
        protected void UpdateStatus()
        {
            if (statusText == null)
                return;

            var partName = currentPartId >= 0 ? PartMaster.Instance.GetSettings(currentPartId)?.name : "（なし）";
            var status = GetStatus();
            statusText.SetText(string.IsNullOrEmpty(status) ? $"パーツ: {partName}" : $"パーツ: {partName}\n{status}");
        }
    }
}
