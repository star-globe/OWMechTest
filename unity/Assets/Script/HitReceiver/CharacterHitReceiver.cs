using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 機体（プレイヤー・NPC）の被弾。HP は機体の MaxAp から始め、受けたダメージを CharacterParam にも反映する。
    /// AP が 0 になったときの撃破・任務失敗の処理は CharacterDeadComponent が行う（deadComp に設定する）
    /// </summary>
    public class CharacterHitReceiver : HitReceiver
    {
        [SerializeField]
        BaseCharacter character;

        private void Start()
        {
            if (character == null)
                TryGetComponent(out character);

            // PlayerManager.CreatePlayer は Instantiate の直後に Initialize するため、Start の時点で MaxAp が入っている
            if (character != null && character.MaxAp > 0)
                SetInitHitPoint(character.MaxAp);
        }

        protected override void OnDamaged(int damage)
        {
            if (character != null && character.CharacterParam != null)
                character.CharacterParam.SetDamage(damage);
        }
    }
}
