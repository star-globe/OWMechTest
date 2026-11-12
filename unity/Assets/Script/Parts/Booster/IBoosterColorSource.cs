using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ブースターのエフェクトの基本色の供給元。
    /// 内装など、ブースターパーツの色を上書きするものはこれを実装して優先度の高い側に並べる
    /// </summary>
    public interface IBoosterColorSource
    {
        /// <summary>基本色を持っていれば true を返す</summary>
        bool TryGetBaseColor(out Color baseColor);
    }

    /// <summary>ブースターパーツ（BoosterSettings）の基本色</summary>
    public class BoosterPartColorSource : IBoosterColorSource
    {
        readonly BoosterSettings settings;

        public BoosterPartColorSource(BoosterSettings settings)
        {
            this.settings = settings;
        }

        public bool TryGetBaseColor(out Color baseColor)
        {
            if (settings == null)
            {
                baseColor = default;
                return false;
            }

            baseColor = settings.BaseColor;
            return true;
        }
    }

    public static class BoosterColorResolver
    {
        /// <summary>優先度の高い順に並べた供給元から、最初に見つかった基本色を返す。どれも無ければ fallback</summary>
        public static Color Resolve(IEnumerable<IBoosterColorSource> sources, Color fallback)
        {
            if (sources == null)
                return fallback;

            foreach (var source in sources)
            {
                if (source != null && source.TryGetBaseColor(out var baseColor))
                    return baseColor;
            }

            return fallback;
        }
    }
}
