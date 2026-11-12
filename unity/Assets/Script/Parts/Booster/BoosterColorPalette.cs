using UnityEngine;

namespace AdvancedGears
{
    /// <summary>ブースターの噴射の状態</summary>
    public enum BoosterState : byte
    {
        Normal = 0,
        Quick = 1,
        Hyper = 2,
    }

    /// <summary>1 つの状態の炎の色（いずれも HDR）</summary>
    public readonly struct BoosterFlameColors
    {
        /// <summary>芯（白に近い色）</summary>
        public readonly Color Core;

        /// <summary>外炎</summary>
        public readonly Color Outer;

        /// <summary>残光（半透明）</summary>
        public readonly Color Trail;

        public BoosterFlameColors(Color core, Color outer, Color trail)
        {
            Core = core;
            Outer = outer;
            Trail = trail;
        }

        /// <summary>最も明るい芯の強さ。マテリアルの色に掛けて、残りを 0〜1 の色味で表す</summary>
        public float Intensity => BoosterColorPalette.GetIntensity(Core);
    }

    /// <summary>
    /// 1 つの基本色から、各状態の炎の色を求める。
    /// 色相は基本色のまま変えず、明るさと彩度で状態を区別する
    /// </summary>
    public static class BoosterColorPalette
    {
        /// <summary>色の供給元がどれも色を返さなかったときの基本色（青白）</summary>
        public static readonly Color DefaultBaseColor = new Color(0.9f, 1.4f, 2.0f, 1f);

        // 芯は外炎より彩度を下げて明るくする
        const float CoreSaturationRate = 0.2f;
        const float CoreIntensityRate = 1.5f;

        // 残光は外炎より暗く、半透明にする
        const float TrailIntensityRate = 0.35f;
        const float TrailAlpha = 0.5f;

        const float Epsilon = 1e-4f;

        /// <summary>RGB の最大値（HDR の強さ）</summary>
        public static float GetIntensity(Color color)
        {
            return Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        }

        /// <summary>HDR の色を強さで割り、0〜1 の色味にする（アルファはそのまま）</summary>
        public static Color ToTint(Color hdr, float intensity)
        {
            if (intensity <= Epsilon)
                return new Color(0f, 0f, 0f, hdr.a);

            return new Color(hdr.r / intensity, hdr.g / intensity, hdr.b / intensity, hdr.a);
        }

        public static BoosterFlameColors Calculate(Color baseColor, BoosterState state)
        {
            var intensity = GetIntensity(baseColor);
            if (intensity <= Epsilon)
            {
                var black = new Color(0f, 0f, 0f, 1f);
                return new BoosterFlameColors(black, black, new Color(0f, 0f, 0f, TrailAlpha));
            }

            Color.RGBToHSV(ToTint(baseColor, intensity), out var hue, out var saturation, out _);

            var (intensityRate, saturationRate) = GetStateRate(state);
            var outerIntensity = intensity * intensityRate;
            var outerSaturation = Mathf.Clamp01(saturation * saturationRate);

            var core = Make(hue, outerSaturation * CoreSaturationRate, outerIntensity * CoreIntensityRate, 1f);
            var outer = Make(hue, outerSaturation, outerIntensity, 1f);
            var trail = Make(hue, outerSaturation, outerIntensity * TrailIntensityRate, TrailAlpha);
            return new BoosterFlameColors(core, outer, trail);
        }

        /// <summary>通常に対する明るさと彩度の倍率。クイックは白く瞬き、ハイパーは強く濃くなる</summary>
        static (float intensity, float saturation) GetStateRate(BoosterState state)
        {
            return state switch
            {
                BoosterState.Quick => (2.0f, 0.6f),
                BoosterState.Hyper => (3.0f, 1.25f),
                _                  => (1.0f, 1.0f),
            };
        }

        static Color Make(float hue, float saturation, float intensity, float alpha)
        {
            var color = Color.HSVToRGB(hue, saturation, 1f) * intensity;
            color.a = alpha;
            return color;
        }
    }
}
