using UnityEngine;

namespace AdvancedGears
{
    public static class GameLayers
    {
        public const string MyPlayer      = "MyPlayer";
        public const string OtherPlayer  = "OtherPlayer";
        public const string NonPlayer    = "NonPlayer";
        public const string Unit         = "Unit";
        public const string TargetableObject = "TargetableObject";
        public const string HitableObject = "HitableObject";
        /// <summary>ミサイル（弾で撃ち落とせるが、ロックの対象にはならない。物理の衝突はどのレイヤーとも起こさない）</summary>
        public const string Missile      = "Missile";

        private static int? _missileLayer;
        public static int MissileLayer => _missileLayer ??= LayerMask.NameToLayer(Missile);

        private static int? _missileLayerMask;
        public static int MissileLayerMask => _missileLayerMask ??= LayerMask.GetMask(Missile);

        /// <summary>弾がダメージを与えるレイヤー（撃った本人には当たらない。Bullet で除外する）</summary>
        private static int? _hitLayerMask;
        public static int HitLayerMask =>
            _hitLayerMask ??= LayerMask.GetMask(MyPlayer, OtherPlayer, NonPlayer, Unit, TargetableObject, HitableObject);

        private static int? _targetableLayerMask;
        public static int TargetableLayerMask =>
            _targetableLayerMask ??= LayerMask.GetMask(OtherPlayer, NonPlayer, Unit, TargetableObject);

        private static int? _raycastObstacleMask;
        public static int RaycastObstacleMask =>
            _raycastObstacleMask ??= ~LayerMask.GetMask(MyPlayer, OtherPlayer, NonPlayer, Unit, TargetableObject, HitableObject, Missile);

        /// <summary>
        /// カメラのめり込みを防ぐ判定の対象（地形・障害物と、被弾可能なマップ上のオブジェクト）。
        /// 自機・他の機体・ユニット・ミサイルは除く。トリガーは判定側で QueryTriggerInteraction.Ignore にする
        /// </summary>
        private static int? _cameraObstacleMask;
        public static int CameraObstacleMask =>
            _cameraObstacleMask ??= RaycastObstacleMask | LayerMask.GetMask(HitableObject);

        private static int? _enemyLayerMask;
        public static int EnemyLayerMask =>
            _enemyLayerMask ??= LayerMask.GetMask(MyPlayer, OtherPlayer, NonPlayer, Unit);
    }
}
