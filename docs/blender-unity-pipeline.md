# Blender ↔ Unity 連携パイプライン

## ファイルフォーマット

**FBX** を採用します。

- Unityの標準インポーターで安定動作
- Humanoidリグ・アニメーションの互換性が高い
- マテリアルスロットとテクスチャをそのまま引き継ぎ可能

---

## ディレクトリ構成

```
unity/Assets/
├── Models/
│   ├── Parts/
│   │   ├── Body/       # 胴体パーツ (.fbx)
│   │   ├── Arms/       # 腕パーツ (.fbx)
│   │   ├── Legs/       # 脚パーツ (.fbx)
│   │   └── Head/       # 頭部パーツ (.fbx)
│   └── Weapons/        # 武装 (.fbx)
└── Textures/
    ├── Parts/          # パーツ用テクスチャ
    └── Weapons/        # 武装用テクスチャ
```

---

## 命名規約

### FBXファイル名

| カテゴリ | 規則 | 例 |
|---------|------|----|
| 胴体 | `body_{バリアント名}.fbx` | `body_standard.fbx` |
| 腕 | `arm_{left/right}_{バリアント名}.fbx` | `arm_left_blade.fbx` |
| 脚 | `leg_{left/right}_{バリアント名}.fbx` | `leg_left_standard.fbx` |
| 頭部 | `head_{バリアント名}.fbx` | `head_sensor.fbx` |
| 武装 | `weapon_{バリアント名}.fbx` | `weapon_rifle.fbx` |

### マテリアルスロット名（Blender側）

Blenderのマテリアルスロット名をUnityの`StylizedRealLit.shader`プロパティに対応させます。

| Blenderスロット名 | Unity対応プロパティ | 用途 |
|-----------------|-------------------|------|
| `Mat_Body` | `_BaseMap` | 胴体メイン |
| `Mat_Detail` | `_BaseMap` | ディテール・パネルライン |
| `Mat_Visor` | `_EmissionColor` | バイザー・発光部位 |
| `Mat_Thruster` | `_EmissionColor` | スラスター・排熱部位 |

### テクスチャファイル名

```
{パーツ名}_{テクスチャ種別}.png
例:
  body_standard_albedo.png
  body_standard_normal.png
```

プロトタイプ期間はテクスチャなし（単色マテリアル）で運用します。

---

## Blenderエクスポート設定

### 推奨エクスポート設定（File > Export > FBX）

```
Include
  └── Object Types: Armature, Mesh

Transform
  └── Scale: 0.01 （Blenderはデフォルトでmをcmとして扱うため）
  └── Apply Scalings: FBX All
  └── Forward: -Z Forward
  └── Up: Y Up

Geometry
  └── Smoothing: Face
  └── Apply Modifiers: ON

Armature
  └── Primary Bone Axis: Y Axis
  └── Secondary Bone Axis: X Axis
  └── Armature FBXNode Type: Null

Animation
  └── Baked Animation: ON（アニメーションがある場合）
```

### エクスポート先パスの設定

Blenderのワークスペースを `unity/Assets/Models/{カテゴリ}/` に設定しておくと、
上書きエクスポートでUnityが自動再インポートします。

---

## Unityインポート設定

### モデル（FBX）

| 設定項目 | 値 |
|---------|-----|
| Scale Factor | 1（FBXエクスポート時にScale 0.01適用済みのため） |
| Import Blendshapes | OFF（使用しない場合） |
| Generate Colliders | OFF（コライダーは別途設定） |
| Material Creation | Import via MaterialDescription |

### リグ

| 設定項目 | 値 |
|---------|-----|
| Animation Type | Humanoid |
| Avatar Definition | Create From This Model |

### テクスチャ

| 設定項目 | 値 |
|---------|-----|
| sRGB | ON（Albedoのみ） |
| Generate Mip Maps | ON |
| Max Size | 1024（プロトタイプ期間） → 2048（本実装） |

---

## 再インポートフロー

1. Blenderで編集・エクスポート（`unity/Assets/Models/` 以下にFBX上書き）
2. Unityが自動検出して再インポート
3. マテリアルスロット名が一致していれば既存マテリアルが自動適用される

### マテリアルが外れた場合の復旧

モデルの Inspector > Materials タブ > 「Extract Materials」で
マテリアルをAssets以下に実体化してから手動で `StylizedRealLit` マテリアルを割り当てる。

---

## プロトタイプ期間の運用方針

- FBXはリグのみ正確に設定し、テクスチャは `GreyboxMaterial` で代替
- アニメーション検証が目的のFBXはアニメーションのみ（メッシュなし）でエクスポートしてもよい
- 本実装移行時にテクスチャを `Textures/` に追加してマテリアルを差し替える
