using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>
    /// 機体の基準リグ。共通スケルトンと Animator を持ち、装着したパーツのメッシュを自分のボーンで動かす。
    /// Animator のパラメーター（移動速度・接地・腕の構え）もここで設定する。
    /// アニメーションがこのオブジェクト自身（アーマチュアのノード）の位置・回転も上書きするので、
    /// 機体への配置は親のオブジェクトで行う
    /// </summary>
    public class MechRig : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        static readonly int AimRightHash = Animator.StringToHash("AimRight");
        static readonly int AimLeftHash = Animator.StringToHash("AimLeft");

        [SerializeField]
        Animator animator = null;

        [SerializeField]
        Transform skeletonRoot = null;

        [SerializeField]
        Transform partsRoot = null;

        [SerializeField, Tooltip("射撃してから腕の構えを保つ秒数")]
        float aimHoldTime = 1.0f;

        Dictionary<string, Transform> bones = null;
        readonly Dictionary<PartsAttachType, GameObject> attached = new Dictionary<PartsAttachType, GameObject>();

        AimHoldTimer rightAim;
        AimHoldTimer leftAim;

        private void Awake()
        {
            Assert.IsNotNull(animator);
            Assert.IsNotNull(skeletonRoot);
            Assert.IsNotNull(partsRoot);

            rightAim = new AimHoldTimer(aimHoldTime);
            leftAim = new AimHoldTimer(aimHoldTime);
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            rightAim.Tick(delta);
            leftAim.Tick(delta);

            animator.SetBool(AimRightHash, rightAim.IsActive);
            animator.SetBool(AimLeftHash, leftAim.IsActive);
        }

        Dictionary<string, Transform> Bones
        {
            get
            {
                if (bones == null)
                {
                    var duplicates = new List<string>();
                    var pairs = new List<KeyValuePair<string, Transform>>();
                    foreach (var t in skeletonRoot.GetComponentsInChildren<Transform>(true))
                        pairs.Add(new KeyValuePair<string, Transform>(t.name, t));

                    bones = MechBoneMap.BuildDictionary(pairs, duplicates);
                    if (duplicates.Count > 0)
                        Debug.LogWarningFormat("[MechRig] ボーン名が重複しています: {0}", string.Join(", ", duplicates));
                }
                return bones;
            }
        }

        /// <summary>スロットのパーツを差し替える。part が null ならスロットを空にする</summary>
        public void Attach(PartsAttachType slot, MechFramePart part)
        {
            if (attached.TryGetValue(slot, out var old) && old != null)
                Destroy(old);
            attached.Remove(slot);

            if (part == null)
                return;

            // ボーン名（Head など）と紛らわしくならないよう接頭辞を付ける
            var container = new GameObject("Part_" + slot);
            container.transform.SetParent(partsRoot, false);

            var missing = new List<string>();
            foreach (var renderer in part.GetRenderers())
            {
                var names = new string[renderer.bones.Length];
                for (int i = 0; i < names.Length; i++)
                    names[i] = renderer.bones[i] != null ? renderer.bones[i].name : null;

                var rootName = renderer.rootBone != null ? renderer.rootBone.name : null;

                renderer.bones = MechBoneMap.Remap(names, Bones, missing);
                renderer.rootBone = rootName != null && Bones.TryGetValue(rootName, out var root) ? root : skeletonRoot;
                renderer.transform.SetParent(container.transform, false);
            }

            if (missing.Count > 0)
                Debug.LogErrorFormat("[MechRig] {0} のボーンが基準リグにありません: {1}", part.name, string.Join(", ", missing));

            // メッシュを移したので、パーツ側に残ったスケルトンは不要
            Destroy(part.gameObject);
            attached[slot] = container;
        }

        /// <summary>水平方向の移動速度と接地状態を Animator に渡す</summary>
        public void SetMove(float horizontalSpeed, bool isGrounded)
        {
            animator.SetFloat(SpeedHash, horizontalSpeed);
            animator.SetBool(IsGroundedHash, isGrounded);
        }

        /// <summary>射撃したときに呼ぶ。しばらく腕を構える</summary>
        public void NotifyFire(bool isRight)
        {
            if (isRight)
                rightAim.Trigger();
            else
                leftAim.Trigger();
        }
    }
}
