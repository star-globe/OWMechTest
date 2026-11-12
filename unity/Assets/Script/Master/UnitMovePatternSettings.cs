using UnityEngine;

namespace AdvancedGears
{
    /// <summary>移動パターンの種類</summary>
    public enum UnitMovePatternType
    {
        /// <summary>移動しない</summary>
        None = 0,
        /// <summary>等速の円運動</summary>
        Circle = 1,
        /// <summary>一定間隔で停止と直進を繰り返す</summary>
        StopAndGo = 2,
    }

    // ─────────────────────────────────────────────────────────
    //  UnitMovePatternSettings  ScriptableObject
    //  ユニットの移動パターンのマスタ。UnitSettings から ID で参照する。
    //  距離・速さは m 単位（実際の移動では GlobalParam の WorldSizeRate で割る）
    // ─────────────────────────────────────────────────────────

    [CreateAssetMenu(menuName = "TestProject/Unit/UnitMovePatternSettings", order = 0)]
    public class UnitMovePatternSettings : ScriptableObject, IDBasedMasterSettings
    {
        [Header("識別")]
        [SerializeField]
        int patternId;
        public int ID => patternId;

        [SerializeField]
        UnitMovePatternType patternType;
        public UnitMovePatternType PatternType => patternType;

        // ── 等速の円運動 ─────────────────────────────────
        [Header("円運動")]
        /// <summary>中心（スポーン位置からのオフセット、スポーン時の向き基準、m）</summary>
        [SerializeField] float centerOffsetX;
        [SerializeField] float centerOffsetY;
        [SerializeField] float centerOffsetZ;
        public Vector3 CenterOffset => new Vector3(centerOffsetX, centerOffsetY, centerOffsetZ);

        /// <summary>半径（m）</summary>
        [SerializeField] float radius = 10f;
        public float Radius => radius;

        /// <summary>周速（m/秒）</summary>
        [SerializeField] float circleSpeed = 5f;
        public float CircleSpeed => circleSpeed;

        [SerializeField] CircleDirection circleDirection;
        public CircleDirection CircleDirection => circleDirection;

        /// <summary>開始時の角度（度）。0 なら中心から面の前方向の点から始める</summary>
        [SerializeField] float startAngle;
        public float StartAngle => startAngle;

        [SerializeField] CirclePlane circlePlane;
        public CirclePlane CirclePlane => circlePlane;

        /// <summary>縦の面の向き（スポーン時の正面から右回りの角度、度）</summary>
        [SerializeField] float planeYaw;
        public float PlaneYaw => planeYaw;

        // ── 停止と直進 ───────────────────────────────────
        [Header("停止と直進")]
        /// <summary>最高速度（m/秒）</summary>
        [SerializeField] float maxSpeed = 10f;
        public float MaxSpeed => maxSpeed;

        /// <summary>加速度（m/秒²）。0 なら即座に最高速度</summary>
        [SerializeField] float acceleration = 5f;
        public float Acceleration => acceleration;

        /// <summary>減速度（m/秒²）。0 なら即座に停止</summary>
        [SerializeField] float deceleration = 5f;
        public float Deceleration => deceleration;

        /// <summary>直進する時間（加速の開始から減速の終了まで、秒）</summary>
        [SerializeField] float moveTime = 4f;
        public float MoveTime => moveTime;

        /// <summary>停止する時間（秒）</summary>
        [SerializeField] float stopTime = 1.5f;
        public float StopTime => stopTime;

        /// <summary>直進するたびに右へ曲がる角度（度）。180 で往復、90 で四角形</summary>
        [SerializeField] float turnAngle = 180f;
        public float TurnAngle => turnAngle;

        /// <summary>このマスタの値で移動パターンを作る（None なら null）</summary>
        public IUnitMovePattern CreatePattern()
        {
            switch (patternType)
            {
                case UnitMovePatternType.Circle:
                    return new CircularMovePattern(CenterOffset, radius, circleSpeed, circleDirection,
                                                   startAngle, circlePlane, planeYaw);
                case UnitMovePatternType.StopAndGo:
                    return new StopAndGoMovePattern(maxSpeed, acceleration, deceleration, moveTime, stopTime, turnAngle);
                default:
                    return null;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Master Container / Master
    // ─────────────────────────────────────────────────────────

    public class UnitMovePatternMasterContainer : IDBasedMasterContainer<UnitMovePatternSettings>
    {
        protected override string resourcesFolder => "UnitMovePatternSettings";
    }

    public class UnitMovePatternMaster : Singleton<UnitMovePatternMasterContainer>
    {
        /// <summary>ID からパターンを作る。ID が 0 以下・見つからない・None なら null（移動しない）</summary>
        public static IUnitMovePattern CreatePattern(int patternId)
        {
            if (patternId <= 0)
                return null;

            return Instance.GetSettings(patternId)?.CreatePattern();
        }
    }
}
