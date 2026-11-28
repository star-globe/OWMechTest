using System;

namespace AdvancedGears
{
    /// <summary>パーツのマテリアルの種類（docs/spec/アートディレクション.md の「塗装とマーキング」）</summary>
    public enum PartMaterialKind : byte
    {
        /// <summary>規約の名前に当てはまらないマテリアル</summary>
        None = 0,
        /// <summary>パーツの主な面</summary>
        Main = 1,
        /// <summary>ディテール・パネルライン・関節など、Main と色を分けたい面</summary>
        Sub = 2,
        /// <summary>光らせたい箇所（カメラアイ・バイザー・ライトなど）</summary>
        Light = 3,
        /// <summary>ブースターのノズル。色は BoosterSettings の基本色で決まる</summary>
        Thruster = 4,
    }

    /// <summary>
    /// パーツのマテリアルスロット名（docs/blender-unity-pipeline.md の「マテリアルスロット名」）。
    /// レンダラーのマテリアルは名前の先頭で見分ける（インスタンス化で付く「 (Instance)」などを無視するため）
    /// </summary>
    public static class PartMaterialSlots
    {
        public const string Main = "Mat_Main";
        public const string Sub = "Mat_Sub";
        public const string Light = "Mat_Light";
        public const string Thruster = "Mat_Thruster";

        /// <summary>マテリアル名から種類を返す。当てはまらなければ None</summary>
        public static PartMaterialKind GetKind(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return PartMaterialKind.None;
            if (materialName.StartsWith(Main, StringComparison.Ordinal))
                return PartMaterialKind.Main;
            if (materialName.StartsWith(Sub, StringComparison.Ordinal))
                return PartMaterialKind.Sub;
            if (materialName.StartsWith(Light, StringComparison.Ordinal))
                return PartMaterialKind.Light;
            if (materialName.StartsWith(Thruster, StringComparison.Ordinal))
                return PartMaterialKind.Thruster;
            return PartMaterialKind.None;
        }
    }
}
