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

### エクスポート用スクリプト（`tools/blender/export_to_unity.py`）

上記の設定でのエクスポートと、命名規約の検証をまとめて行うスクリプトです。
オブジェクト名からカテゴリを判定し、`unity/Assets/Models/{カテゴリ}/{オブジェクト名（小文字）}.fbx` に書き出します。
上書きエクスポートすると Unity が自動で再インポートします。

- 選択したオブジェクトごとに 1 つの FBX を作り、**子孫（アーマチュアの子のメッシュなど）も同じ FBX に含めます**。親と子の両方を選択した場合は親の FBX にまとめます
- エクスポート前に検証を行い、**エラーがあるとエクスポートしません**

| 検証内容 | レベル |
|---|---|
| 名前が「FBXファイル名」の規約に合わない（Blender が付ける `.001` などの連番を含む） | error |
| メッシュのマテリアルが「マテリアルスロット名」の 4 種類以外、または空のスロットがある | error |
| メッシュもアーマチュアも含まれない | error |
| 名前に大文字が含まれる（ファイル名は小文字になる） | warning |
| メッシュにマテリアルスロットが無い | warning |
| スケールが負 | warning |

エクスポート先の Models フォルダは、関数の引数 `base_path` → 環境変数 `OWMECH_UNITY_MODELS` → リポジトリ内の `unity/Assets/Models`（スクリプトや `.blend` の場所から探す）の順で決まります。

#### Text Editor から使う

1. Blender の Text Editor で `tools/blender/export_to_unity.py` を開く
2. エクスポートしたいオブジェクトを選択して Run Script
3. 結果と規約違反は System Console（Window > Toggle System Console）に表示される

#### Python（Blender MCP の `execute_blender_code`）から使う

```python
import bpy, sys, importlib
sys.path.insert(0, r"C:\Users\<ユーザー>\Documents\GitHub\OWMechTest\tools\blender")
import export_to_unity as ex
importlib.reload(ex)                         # スクリプトを編集した後に読み直す

ex.validate_objects()                        # 選択中のオブジェクトを検証 → [{"object", "level", "message"}, ...]
ex.export_selected(dry_run=True)             # 出力先だけ確認 → [{"object", "path", "objects", "exported"}, ...]
ex.export_selected()                         # エクスポート（違反があれば ValueError）
ex.export_objects(["body_standard"])         # 名前を指定してエクスポート（選択は変えない）
ex.export_objects(["body_standard"], base_path=r"C:\temp\Models")   # 別の場所へ（試し出しなど）
```

- エクスポート後は、選択状態とアクティブなオブジェクトを元に戻します
- MCP のコードは `bpy.context.object` などの UI のコンテキストが無い状態で動くため、オブジェクトは名前か `bpy.data.objects` で渡します

#### コマンドラインから使う（`tools/blender/export.sh`）

Blender の GUI を開かずに（`blender --background`）、保存済みの `.blend` を検証・エクスポートします。Blender の起動や MCP の接続が不要で、1 回あたり数秒で終わります。Git Bash でリポジトリルートから実行します。

```bash
tools/blender/export.sh model.blend --all --validate-only          # 検証だけ
tools/blender/export.sh model.blend --all --dry-run                # 出力先だけ確認
tools/blender/export.sh model.blend --objects body_standard        # 名前を指定してエクスポート
tools/blender/export.sh model.blend --all --base-path /c/temp/Models   # 別の場所へ（試し出しなど）
```

| オプション | 内容 |
|---|---|
| `--objects <名前...>` | 対象のオブジェクト。`--objects` も `--all` も無いときは、`.blend` に保存された選択状態が対象 |
| `--all` | 名前がカテゴリ（`body` / `arm` / `leg` / `head` / `weapon`）で始まるオブジェクトをすべて対象にする |
| `--validate-only` / `--dry-run` | 検証だけ / 出力先の確認だけ（どちらも FBX を書き出さない） |
| `--base-path <path>` | エクスポート先の Models フォルダ |
| `--json-out <path>` | 結果の JSON をファイルにも書く |

- 標準出力は結果の JSON 1 行です（`{"mode", "ok", "issues", "exports", "error", "blend_file"}`。`issues` と `exports` の要素は Python の `validate_objects()` / `export_objects()` の戻り値と同じ）
- 終了コード: `0` = 成功、`2` = 規約違反（error）があり何もしなかった、`1` = その他のエラー（オブジェクトが無い、引数の誤りなど）
- Blender のログは `unity/Logs/blender-export.log` に保存します
- **Unity Editor の起動中は実行できません**（メモリ不足を避けるため。Editor が起動中なら案内を出して終了コード 1 で終わります）。Unity を閉じてから実行してください
- `.blend` は読むだけで保存しません。**GUI の Blender で編集中の内容は、保存してから実行してください**
- `--factory-startup` で起動するため、ユーザーのアドオン（MCP アドオンの Auto Start など）は読み込みません
- Blender は既定のインストール先（Windows は `C:\Program Files\Blender Foundation\Blender X.Y` の最新版）から探します。別の場所にある場合は環境変数 `BLENDER_EXE` を指定してください
- 直接呼ぶ場合: `blender --background --factory-startup model.blend --python tools/blender/export_to_unity.py -- [オプション]`。JSON は `OWMECH_RESULT:` で始まる行に出ます

#### MCP・コマンドラインの使い分け

| 方法 | 向いている作業 |
|---|---|
| Blender MCP（`execute_blender_code` など。GUI の Blender が必要） | モデリングなど、状態を確認しながら少しずつ進める作業。保存前の変更も扱える |
| `tools/blender/export.sh` | エクスポートと検証。手順が決まっていて、終了コードで成否が分かる |
| Blender MCP の `_for_cli` ツール（`execute_blender_code_for_cli` など） | GUI を開かずに `.blend` の中身を調べる、決まった手順に無い処理を試す |

`_for_cli` ツールは MCP サーバーが `blender --background <file> --python-expr <code>` を実行し、`result` に入れた dict を返します。

- Blender の場所は環境変数 `BLENDER_PATH`（`unity/.mcp.json` で既定値を指定済み。変えたら `/mcp` から `blender` を再接続する）
- GUI の Blender が同じ `.blend` を未保存のまま開いていると、その内容のコピー（`<名前>_mcp_0001.blend`）を保存して使い、終わったら削除します。GUI が起動していなければファイルをそのまま使います
- タイムアウトは 120 秒で、ユーザーのアドオンも読み込みます。終了コードは返りません

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
