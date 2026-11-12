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

    /// <summary>噴射を構成する子パーティクルの役割。役割ごとに基本色の使い方が変わる</summary>
    public enum BoosterParticleRole : byte
    {
        /// <summary>芯。芯の色から外炎の色へ変わる</summary>
        Core = 0,

        /// <summary>外炎。外炎の色から残光の色へ変わる</summary>
        Outer = 1,

        /// <summary>火花。芯の色で光り、外炎の色に冷えて消える</summary>
        Spark = 2,
    }

    public class BoosterPart : PartComponent
    {
        [Serializable]
        class ColoredParticle
        {
            [SerializeField]
            ParticleSystem particle;

            [SerializeField]
            BoosterParticleRole role;

            public ParticleSystem Particle => particle;
            public BoosterParticleRole Role => role;
        }

        [Serializable]
        class BoosterParticleSystem
        {
            /// <summary>再生と停止を行う親。子のパーティクルもまとめて再生・停止する</summary>
            [SerializeField]
            ParticleSystem particle;

            [SerializeField]
            bool isPulse = false;

            /// <summary>基本色で色を付ける子のパーティクル。ヒートヘイズなど色を持たないものは含めない</summary>
            [SerializeField]
            ColoredParticle[] coloredParticles = Array.Empty<ColoredParticle>();

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
            /// 炎の色を役割ごとに子のパーティクルへ反映する。頂点カラーは 0〜1 に丸められるため、
            /// 色味は colorOverLifetime、HDR の強さはマテリアルの _BaseColor で渡す
            /// </summary>
            public void SetColors(in BoosterFlameColors colors, MaterialPropertyBlock block)
            {
                foreach (var colored in coloredParticles)
                {
                    if (colored == null || colored.Particle == null)
                        continue;

                    var (from, to, holdTime) = GetRoleColors(colors, colored.Role);
                    var intensity = BoosterColorPalette.GetIntensity(from);
                    var ps = colored.Particle;

                    var main = ps.main;
                    main.startColor = Color.white;

                    var colorOverLifetime = ps.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    colorOverLifetime.color = CreateGradient(from, to, intensity, holdTime);

                    if (ps.TryGetComponent<ParticleSystemRenderer>(out var renderer))
                    {
                        renderer.GetPropertyBlock(block);
                        block.SetColor(BaseColorId, new Color(intensity, intensity, intensity, 1f));
                        renderer.SetPropertyBlock(block);
                    }
                }
            }

            /// <summary>役割ごとの始まりと終わりの色、不透明のまま保つ時間（寿命に対する割合）</summary>
            static (Color from, Color to, float holdTime) GetRoleColors(in BoosterFlameColors colors, BoosterParticleRole role)
            {
                return role switch
                {
                    BoosterParticleRole.Core  => (colors.Core, colors.Outer, 0.4f),
                    BoosterParticleRole.Spark => (colors.Core, colors.Outer, 0.6f),
                    _                         => (colors.Outer, colors.Trail, 0.3f),
                };
            }

            /// <summary>from から to へ変わり、holdTime を過ぎると消えていく</summary>
            static Gradient CreateGradient(Color from, Color to, float intensity, float holdTime)
            {
                var start = BoosterColorPalette.ToTint(from, intensity);
                var end = BoosterColorPalette.ToTint(to, intensity);

                var gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(start, 0f),
                        new GradientColorKey(end, 1f),
                    },
                    new[]
                    {
                        new GradientAlphaKey(start.a, 0f),
                        new GradientAlphaKey(Mathf.Lerp(start.a, end.a, holdTime), holdTime),
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
