using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public enum BoostVector : byte
    {
        Forward = 0,
        Left = 1,
        Right = 2,
        Back = 3,
    }

    public class BoosterPart : PartComponent
    {
        [Serializable]
        class BoosterParticleSystem
        {
            [SerializeField]
            ParticleSystem particle;

            [SerializeField]
            bool isPulse = false;

            bool isOn = false;
            int boostBit = 0;

            public void SetBoostVector(BoostVector vec)
            {
                boostBit = 1 << (byte) vec;
            }

            public void Boost(int vectorBit)
            {
                if (isPulse)
                {
                    particle.Play();
                }
                else
                {
                    bool on = (boostBit & vectorBit) != 0;
                    if (this.isOn == on)
                        return;

                    this.isOn = on;
                    if (isOn)
                        particle.Play();
                    else
                        particle.Stop();
                }
            }

            /// <summary>
            /// 炎の色を子のパーティクルまで反映する。頂点カラーは 0〜1 に丸められるため、
            /// 色味は colorOverLifetime、HDR の強さはマテリアルの _BaseColor で渡す
            /// </summary>
            public void SetColors(in BoosterFlameColors colors, MaterialPropertyBlock block)
            {
                if (particle == null)
                    return;

                var gradient = CreateGradient(colors);
                var intensity = colors.Intensity;
                var baseColor = new Color(intensity, intensity, intensity, 1f);

                foreach (var ps in particle.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.startColor = Color.white;

                    var colorOverLifetime = ps.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    colorOverLifetime.color = gradient;

                    if (ps.TryGetComponent<ParticleSystemRenderer>(out var renderer))
                    {
                        renderer.GetPropertyBlock(block);
                        block.SetColor(BaseColorId, baseColor);
                        renderer.SetPropertyBlock(block);
                    }
                }
            }

            /// <summary>芯 → 外炎 → 残光の順に変わり、最後に消える</summary>
            static Gradient CreateGradient(in BoosterFlameColors colors)
            {
                var intensity = colors.Intensity;
                var core = BoosterColorPalette.ToTint(colors.Core, intensity);
                var outer = BoosterColorPalette.ToTint(colors.Outer, intensity);
                var trail = BoosterColorPalette.ToTint(colors.Trail, intensity);

                var gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(core, 0f),
                        new GradientColorKey(outer, 0.25f),
                        new GradientColorKey(trail, 1f),
                    },
                    new[]
                    {
                        new GradientAlphaKey(core.a, 0f),
                        new GradientAlphaKey(outer.a, 0.25f),
                        new GradientAlphaKey(trail.a, 0.7f),
                        new GradientAlphaKey(0f, 1f),
                    });
                return gradient;
            }
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>ノズルの発光に使うマテリアルスロット（docs/blender-unity-pipeline.md の命名規約）</summary>
        const string ThrusterMaterialPrefix = "Mat_Thruster";

        [SerializeField]
        BoosterParticleSystem normal;

        [SerializeField]
        BoosterParticleSystem quick;

        [SerializeField]
        BoosterParticleSystem hyper;

        public void SetBoostVector(BoostVector vec)
        {
            normal.SetBoostVector(vec);
            quick.SetBoostVector(vec);
            hyper.SetBoostVector(vec);
        }

        /// <summary>基本色から各状態の炎の色を求め、エフェクトとノズルの発光に反映する</summary>
        public void SetBaseColor(Color baseColor)
        {
            var block = new MaterialPropertyBlock();
            normal.SetColors(BoosterColorPalette.Calculate(baseColor, BoosterState.Normal), block);
            quick.SetColors(BoosterColorPalette.Calculate(baseColor, BoosterState.Quick), block);
            hyper.SetColors(BoosterColorPalette.Calculate(baseColor, BoosterState.Hyper), block);
            SetNozzleColor(baseColor, block);
        }

        /// <summary>Mat_Thruster のスロットの _EmissionColor を、マテリアルを複製せずに変える</summary>
        void SetNozzleColor(Color baseColor, MaterialPropertyBlock block)
        {
            var emission = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.name.StartsWith(ThrusterMaterialPrefix, StringComparison.Ordinal))
                        continue;

                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(EmissionColorId, emission);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        public void Boost(int vectorBit)
        {
            normal.Boost(vectorBit);
        }

        public void HyperBoost(int vectorBit)
        {
            hyper.Boost(vectorBit);
        }

        public void Quick(int vectorBit)
        {
            quick.Boost(vectorBit);
        }
    }
}
