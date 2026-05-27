using System.Collections;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 固定ユニットを自身の位置・回転でスポーンさせるコンポーネント。
    /// PlayerSpawner に倣い、マスターデータのロード完了を待ってからスポーンする。
    /// </summary>
    public class FixedUnitSpawner : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────
        [SerializeField]
        UnitSide unitSide;

        [SerializeField]
        int unitId;

        [SerializeField]
        float inter = 0.1f;

        // ── Internal ─────────────────────────────────────
        private WaitForSeconds waiter;
        private WaitForSeconds Waiter => waiter ??= new WaitForSeconds(inter);

        // ── Lifecycle ─────────────────────────────────────
        private void Start()
        {
            StartCoroutine(WaitAndSpawn());
        }

        // ── Coroutine ─────────────────────────────────────
        private IEnumerator WaitAndSpawn()
        {
            // UnitMaster にプレハブが登録されるまで待機
            while (UnitMaster.GetUnitPrefab(unitId) == null)
            {
                yield return Waiter;
            }

            SpawnUnit(unitId);
        }

        // ── Spawn ─────────────────────────────────────────
        private void SpawnUnit(int unitId)
        {
            var prefab = UnitMaster.GetUnitPrefab(unitId);
            if (prefab == null)
            {
                Debug.LogErrorFormat("[FixedUnitSpawner] Prefab が見つかりません。UnitId:{0}", unitId);
                return;
            }

            var go = Instantiate(prefab, transform.position, transform.rotation);

            var unit = go.GetComponent<BaseUnit>();
            if (unit == null)
            {
                Debug.LogErrorFormat("[FixedUnitSpawner] スポーンした Prefab に BaseUnit コンポーネントがありません。UnitId:{0}", unitId);
                Destroy(go);
                return;
            }

            unit.Initialize((long)go.GetInstanceID(), unitSide);
        }
    }
}
