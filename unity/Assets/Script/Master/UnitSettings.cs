using UnityEngine;

namespace AdvancedGears
{
    // ─────────────────────────────────────────────────────────
    //  ユニットタイプ定義
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// ユニットの種別
    /// </summary>
    public enum UnitType
    {
        None    = 0,
        Turret  = 1,   // 固定砲台
        Drone   = 2,   // ドローン
        Tank    = 3,   // 戦車
    }

    /// <summary>
    /// ユニットの移動方式
    /// </summary>
    public enum UnitMoveType
    {
        Fixed   = 0,   // 固定（固定砲台）
        Ground  = 1,   // 地上移動（戦車）
        Air     = 2,   // 空中移動（ドローン）
    }

    // ─────────────────────────────────────────────────────────
    //  UnitSettings  ScriptableObject
    //  各ユニットの TypeID・UnitType・プレハブ等を保持するマスターデータ
    // ─────────────────────────────────────────────────────────

    [CreateAssetMenu(menuName = "TestProject/Unit/UnitSettings", order = 0)]
    public class UnitSettings : ScriptableObject, IDBasedMasterSettings
    {
        [Header("識別")]
        [SerializeField]
        int unitId;
        public int UnitId => unitId;

        [SerializeField]
        UnitType unitType;
        public UnitType UnitType => unitType;

        [SerializeField]
        UnitMoveType moveType;
        public UnitMoveType MoveType => moveType;

        [Header("プレハブ")]
        [SerializeField]
        GameObject prefab;
        public GameObject Prefab => prefab;

        [Header("パラメーター設定")]
        [SerializeField]
        UnitParameterSettings parameterSettings;
        public UnitParameterSettings ParameterSettings => parameterSettings;

        // IDBasedMasterSettings
        public int ID => unitId;
    }

    // ─────────────────────────────────────────────────────────
    //  Master Container / Master
    // ─────────────────────────────────────────────────────────

    public class UnitMasterContainer : IDBasedMasterContainer<UnitSettings>
    {
        protected override string resourcesFolder => "UnitSettings";
    }

    public class UnitMaster : Singleton<UnitMasterContainer>
    {
        public static UnitSettings GetSettings(int unitId)
        {
            return Instance.GetSettings(unitId);
        }

        public static GameObject GetUnitPrefab(int unitId)
        {
            return Instance.GetSettings(unitId)?.Prefab;
        }

        public static UnitType GetUnitType(int unitId)
        {
            return Instance.GetSettings(unitId)?.UnitType ?? UnitType.None;
        }

        public static UnitParameterSettings GetParameterSettings(int unitId)
        {
            return Instance.GetSettings(unitId)?.ParameterSettings;
        }
    }
}
