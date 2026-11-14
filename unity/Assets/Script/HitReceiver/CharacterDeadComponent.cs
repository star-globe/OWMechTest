using System;
using System.Collections;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    /// <summary>
    /// 機体（プレイヤー・NPC）の撃破。AP が 0 になったら操作を止め、撃破の演出（爆発）が終わったら機体を非表示にする。
    /// 自機のときは任務を失敗にして、そのあと Result へ遷移する（#108）
    /// </summary>
    public class CharacterDeadComponent : DeadComponent
    {
        [SerializeField]
        PlayerCharacter character;

        [SerializeField]
        PlayerController controller;

        [SerializeField]
        [Tooltip("撃破してから機体を非表示にするまでの秒数（撃破の演出の長さ）")]
        float hideDelay = 1.5f;

        [SerializeField]
        [Tooltip("自機を非表示にしてから Result へ遷移するまでの秒数")]
        float resultDelay = 1.0f;

        bool isDead;

        private void Awake()
        {
            if (character == null)
                TryGetComponent(out character);
            if (controller == null)
                TryGetComponent(out controller);
        }

        public override void DeadAction()
        {
            if (isDead)
                return;
            isDead = true;

            if (controller != null)
                controller.StopControl();

            bool isSelf = character != null && character.IsSelf;

            // 演出を待つ間に目標を壊しても成功にならないよう、失敗は撃破した時点で決める
            if (isSelf)
                StateManager.Instance.TryDecideMissionResult(MissionResult.Failed);

            StartCoroutine(HideAfterEffect(isSelf));
        }

        IEnumerator HideAfterEffect(bool isSelf)
        {
            yield return new WaitForSeconds(hideDelay);

            if (isSelf)
            {
                // 非表示にするとこのコンポーネントのコルーチンも止まるため、遷移の待ちは UniRx のタイマーで行う
                Observable.Timer(TimeSpan.FromSeconds(resultDelay))
                          .Subscribe(_ => FinishBattleIfFailed());
            }

            this.gameObject.SetActive(false);
        }

        static void FinishBattleIfFailed()
        {
            var state = StateManager.Instance;
            if (state.MissionResult == MissionResult.Failed)
                state.FinishBattle();
        }
    }
}
