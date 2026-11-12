using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class BoosterColorPaletteTests
    {
        static readonly Color BlueWhite = new Color(0.9f, 1.4f, 2.0f, 1f);
        static readonly Color Orange = new Color(2.0f, 0.9f, 0.2f, 1f);

        static readonly BoosterState[] AllStates = { BoosterState.Normal, BoosterState.Quick, BoosterState.Hyper };

        const float Delta = 1e-3f;

        static float Hue(Color color)
        {
            var intensity = BoosterColorPalette.GetIntensity(color);
            Color.RGBToHSV(BoosterColorPalette.ToTint(color, intensity), out var h, out _, out _);
            return h;
        }

        static float Saturation(Color color)
        {
            var intensity = BoosterColorPalette.GetIntensity(color);
            Color.RGBToHSV(BoosterColorPalette.ToTint(color, intensity), out _, out var s, out _);
            return s;
        }

        [Test]
        public void 通常の外炎は基本色と同じ色になる()
        {
            var colors = BoosterColorPalette.Calculate(Orange, BoosterState.Normal);

            Assert.AreEqual(Orange.r, colors.Outer.r, Delta);
            Assert.AreEqual(Orange.g, colors.Outer.g, Delta);
            Assert.AreEqual(Orange.b, colors.Outer.b, Delta);
        }

        [Test]
        public void どの状態でも外炎と残光は基本色の色相を保つ([Values(0, 1)] int colorIndex)
        {
            var baseColor = colorIndex == 0 ? BlueWhite : Orange;

            foreach (var state in AllStates)
            {
                var colors = BoosterColorPalette.Calculate(baseColor, state);
                Assert.AreEqual(Hue(baseColor), Hue(colors.Outer), Delta, state.ToString());
                Assert.AreEqual(Hue(baseColor), Hue(colors.Trail), Delta, state.ToString());
            }
        }

        [Test]
        public void クイックとハイパーは通常より明るい()
        {
            var normal = BoosterColorPalette.Calculate(Orange, BoosterState.Normal);
            var quick = BoosterColorPalette.Calculate(Orange, BoosterState.Quick);
            var hyper = BoosterColorPalette.Calculate(Orange, BoosterState.Hyper);

            Assert.Greater(BoosterColorPalette.GetIntensity(quick.Outer), BoosterColorPalette.GetIntensity(normal.Outer));
            Assert.Greater(BoosterColorPalette.GetIntensity(hyper.Outer), BoosterColorPalette.GetIntensity(quick.Outer));
        }

        [Test]
        public void クイックは白っぽくハイパーは色が濃い()
        {
            var normal = BoosterColorPalette.Calculate(Orange, BoosterState.Normal);
            var quick = BoosterColorPalette.Calculate(Orange, BoosterState.Quick);
            var hyper = BoosterColorPalette.Calculate(Orange, BoosterState.Hyper);

            Assert.Less(Saturation(quick.Outer), Saturation(normal.Outer));
            Assert.Greater(Saturation(hyper.Outer), Saturation(normal.Outer));
        }

        [Test]
        public void 芯は外炎より白く明るい()
        {
            foreach (var state in AllStates)
            {
                var colors = BoosterColorPalette.Calculate(Orange, state);
                Assert.Less(Saturation(colors.Core), Saturation(colors.Outer), state.ToString());
                Assert.Greater(BoosterColorPalette.GetIntensity(colors.Core), BoosterColorPalette.GetIntensity(colors.Outer), state.ToString());
                Assert.AreEqual(BoosterColorPalette.GetIntensity(colors.Core), colors.Intensity, Delta, state.ToString());
            }
        }

        [Test]
        public void 残光は外炎より暗く半透明()
        {
            foreach (var state in AllStates)
            {
                var colors = BoosterColorPalette.Calculate(BlueWhite, state);
                Assert.Less(BoosterColorPalette.GetIntensity(colors.Trail), BoosterColorPalette.GetIntensity(colors.Outer), state.ToString());
                Assert.Less(colors.Trail.a, 1f, state.ToString());
                Assert.AreEqual(1f, colors.Outer.a, Delta, state.ToString());
            }
        }

        [Test]
        public void 基本色が黒なら炎も黒になる()
        {
            var colors = BoosterColorPalette.Calculate(Color.black, BoosterState.Hyper);

            Assert.AreEqual(0f, colors.Intensity, Delta);
            Assert.AreEqual(0f, BoosterColorPalette.GetIntensity(colors.Core), Delta);
            Assert.IsFalse(float.IsNaN(colors.Outer.r));
        }

        [Test]
        public void 色味は0から1に収まり強さを掛けると元の色に戻る()
        {
            var colors = BoosterColorPalette.Calculate(Orange, BoosterState.Hyper);
            var intensity = colors.Intensity;

            foreach (var hdr in new[] { colors.Core, colors.Outer, colors.Trail })
            {
                var tint = BoosterColorPalette.ToTint(hdr, intensity);
                Assert.LessOrEqual(BoosterColorPalette.GetIntensity(tint), 1f + Delta);
                Assert.AreEqual(hdr.r, tint.r * intensity, Delta);
                Assert.AreEqual(hdr.g, tint.g * intensity, Delta);
                Assert.AreEqual(hdr.b, tint.b * intensity, Delta);
                Assert.AreEqual(hdr.a, tint.a, Delta);
            }
        }
    }

    public class BoosterColorResolverTests
    {
        class FixedSource : IBoosterColorSource
        {
            readonly Color? color;

            public FixedSource(Color? color)
            {
                this.color = color;
            }

            public bool TryGetBaseColor(out Color baseColor)
            {
                baseColor = color ?? default;
                return color.HasValue;
            }
        }

        [Test]
        public void 最初に色を返した供給元の色を使う()
        {
            var sources = new IBoosterColorSource[]
            {
                null,
                new FixedSource(null),
                new FixedSource(Color.red),
                new FixedSource(Color.green),
            };

            Assert.AreEqual(Color.red, BoosterColorResolver.Resolve(sources, Color.blue));
        }

        [Test]
        public void どの供給元も色が無ければ既定の色を使う()
        {
            var sources = new IBoosterColorSource[] { new FixedSource(null), new BoosterPartColorSource(null) };

            Assert.AreEqual(Color.blue, BoosterColorResolver.Resolve(sources, Color.blue));
            Assert.AreEqual(Color.blue, BoosterColorResolver.Resolve(null, Color.blue));
        }

        [Test]
        public void ブースターパーツの色は内装などの上書きより後に使われる()
        {
            var settings = ScriptableObject.CreateInstance<BoosterSettings>();
            try
            {
                var partSource = new BoosterPartColorSource(settings);

                Assert.AreEqual(settings.BaseColor, BoosterColorResolver.Resolve(new[] { partSource }, Color.blue));
                Assert.AreEqual(Color.red, BoosterColorResolver.Resolve(new IBoosterColorSource[] { new FixedSource(Color.red), partSource }, Color.blue));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
