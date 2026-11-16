using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面の機体プレビュー。編集中の組み合わせで MechBody を組み直し、
    /// 専用のカメラで RenderTexture に描いて UI の RawImage に表示する。
    /// 動作（待機・歩行・空中）の切り替えと回転表示ができる
    /// </summary>
    public class MechPreview : MonoBehaviour
    {
        public enum MotionType
        {
            Idle,
            Walk,
            Air,
        }

        [SerializeField] CustomizeManager customizeManager;

        [Header("機体")]
        [SerializeField] MechBody body;
        [Tooltip("回転させる台。body の親")]
        [SerializeField] Transform turntable;

        [Header("表示")]
        [SerializeField] Camera previewCamera;
        [SerializeField] RawImage previewImage;

        [Header("操作")]
        [SerializeField] Button motionButton;
        [SerializeField] TextMeshProUGUI motionButtonText;
        [SerializeField] Button rotateButton;
        [SerializeField] TextMeshProUGUI rotateButtonText;

        [Header("動作")]
        [Tooltip("歩行のときに Animator へ渡す水平速度（Animator は 0.5 より大きいと歩く）")]
        [SerializeField] float walkSpeed = 5.0f;
        [Tooltip("回転表示の速さ（度/秒）")]
        [SerializeField] float rotateSpeed = 30.0f;

        MotionType motion = MotionType.Idle;
        bool isRotating = true;
        RenderTexture renderTexture;

        // 組み付け済みの組み合わせ。変わったスロットだけ組み直す
        readonly Dictionary<PartsAttachType, int> assembled = new Dictionary<PartsAttachType, int>();

        private void Awake()
        {
            Assert.IsNotNull(customizeManager);
            Assert.IsNotNull(body);
            Assert.IsNotNull(turntable);
            Assert.IsNotNull(previewCamera);
            Assert.IsNotNull(previewImage);

            // CustomizeManager.Start が最初の組み合わせを通知するので、それより前（Awake）に購読する
            customizeManager.AssemblyChanged += OnAssemblyChanged;

            motionButton?.onClick.AddListener(OnClickMotion);
            rotateButton?.onClick.AddListener(OnClickRotate);
            UpdateButtonTexts();
        }

        private void Start()
        {
            SetupRenderTexture();
        }

        private void OnDestroy()
        {
            if (customizeManager != null)
                customizeManager.AssemblyChanged -= OnAssemblyChanged;

            if (previewCamera != null)
                previewCamera.targetTexture = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        private void Update()
        {
            var isGrounded = motion != MotionType.Air;
            var speed = motion == MotionType.Walk ? walkSpeed : 0.0f;
            body.Rig.SetMove(speed, isGrounded);

            if (isRotating)
                turntable.Rotate(0.0f, rotateSpeed * Time.deltaTime, 0.0f, Space.World);
        }

        /// <summary>RawImage の大きさに合わせた RenderTexture を作り、カメラの描画先にする</summary>
        private void SetupRenderTexture()
        {
            var rect = previewImage.rectTransform.rect;
            var canvas = previewImage.canvas;
            var scale = canvas != null ? canvas.scaleFactor : 1.0f;
            var width = Mathf.Max(1, Mathf.RoundToInt(rect.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(rect.height * scale));

            renderTexture = new RenderTexture(width, height, 24) { name = "MechPreview" };
            previewCamera.targetTexture = renderTexture;
            previewCamera.aspect = (float)width / height;
            previewImage.texture = renderTexture;
        }

        private void OnAssemblyChanged(IReadOnlyDictionary<PartsAttachType, int> assembly)
        {
            var assembler = body.Assembler;
            foreach (var slot in GetChangedSlots(assembled, assembly))
            {
                var partId = assembly[slot];
                assembler.Assemble(slot, partId);
                assembled[slot] = partId;
            }
        }

        private void OnClickMotion()
        {
            motion = NextMotion(motion);
            UpdateButtonTexts();
        }

        private void OnClickRotate()
        {
            isRotating = !isRotating;
            UpdateButtonTexts();
        }

        private void UpdateButtonTexts()
        {
            if (motionButtonText != null)
                motionButtonText.SetText("動作: " + MotionDisplayName(motion));
            if (rotateButtonText != null)
                rotateButtonText.SetText(isRotating ? "回転: ON" : "回転: OFF");
        }

        /// <summary>前回から変わったスロット。前回に無いスロットも変わったものとして返す</summary>
        public static List<PartsAttachType> GetChangedSlots(
            IReadOnlyDictionary<PartsAttachType, int> previous,
            IReadOnlyDictionary<PartsAttachType, int> current)
        {
            var changed = new List<PartsAttachType>();
            foreach (var kvp in current)
            {
                if (previous == null || !previous.TryGetValue(kvp.Key, out var prevId) || prevId != kvp.Value)
                    changed.Add(kvp.Key);
            }
            return changed;
        }

        public static MotionType NextMotion(MotionType current)
        {
            return current switch
            {
                MotionType.Idle => MotionType.Walk,
                MotionType.Walk => MotionType.Air,
                _               => MotionType.Idle,
            };
        }

        static string MotionDisplayName(MotionType type)
        {
            return type switch
            {
                MotionType.Walk => "歩行",
                MotionType.Air  => "空中",
                _               => "待機",
            };
        }
    }
}
