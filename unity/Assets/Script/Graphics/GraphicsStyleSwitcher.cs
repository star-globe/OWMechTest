using UnityEngine;

/// <summary>
/// 検証シーンでグラフィックスタイルをランタイムに切り替えるユーティリティ。
/// プロトタイプ期間はグレーボックスマテリアルを、本実装移行時にPBRマテリアルを差し替えて比較する。
/// </summary>
public class GraphicsStyleSwitcher : MonoBehaviour
{
    public enum Style { Greybox, StylizedReal }

    [Header("対象レンダラー")]
    [SerializeField] Renderer[] targets;

    [Header("グレーボックス用マテリアル（プロトタイプ）")]
    [SerializeField] Material greyboxMaterial;

    [Header("スタイライズドリアル用マテリアル（本実装）")]
    [SerializeField] Material stylizedRealMaterial;

    [Header("現在のスタイル")]
    [SerializeField] Style currentStyle = Style.Greybox;

    public Style CurrentStyle => currentStyle;

    void Start() => Apply(currentStyle);

    public void SwitchToGreybox()     => Apply(Style.Greybox);
    public void SwitchToStylizedReal() => Apply(Style.StylizedReal);

    public void Toggle()
    {
        Apply(currentStyle == Style.Greybox ? Style.StylizedReal : Style.Greybox);
    }

    void Apply(Style style)
    {
        currentStyle = style;
        Material mat = style == Style.Greybox ? greyboxMaterial : stylizedRealMaterial;
        if (mat == null) return;

        foreach (var r in targets)
        {
            if (r != null) r.sharedMaterial = mat;
        }
    }

#if UNITY_EDITOR
    // エディタ上でインスペクターから即時プレビューできるようにする
    void OnValidate() => Apply(currentStyle);
#endif
}
