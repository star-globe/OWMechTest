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

        private static int? _hitLayerMask;
        public static int HitLayerMask =>
            _hitLayerMask ??= LayerMask.GetMask(OtherPlayer, NonPlayer, Unit, TargetableObject, HitableObject);

        private static int? _targetableLayerMask;
        public static int TargetableLayerMask =>
            _targetableLayerMask ??= LayerMask.GetMask(OtherPlayer, NonPlayer, Unit, TargetableObject);

        private static int? _raycastObstacleMask;
        public static int RaycastObstacleMask =>
            _raycastObstacleMask ??= ~LayerMask.GetMask(MyPlayer, OtherPlayer, NonPlayer, Unit, TargetableObject, HitableObject);

        private static int? _enemyLayerMask;
        public static int EnemyLayerMask =>
            _enemyLayerMask ??= LayerMask.GetMask(MyPlayer, OtherPlayer, NonPlayer, Unit);
    }
}
