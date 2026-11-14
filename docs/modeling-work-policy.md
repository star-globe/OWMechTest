# モデリング作業のポリシー（トークン使用量の削減）

Blender でモデルを作り Unity に取り込む作業（#68 など）で、AI エージェントが従うポリシーです。
#68（機体本体のパーツ）の作業でトークンを多く使った処理と、その改善案をまとめています。
手順そのものは `docs/blender-unity-pipeline.md` を参照してください。

## 基本方針

1. **Unity と Blender の切り替え回数を最小にする**。切り替えは 1 回ごとに数分かかり、ツールの読み込みや状態の確認でトークンも使う
2. **ツールの戻り値を最小にする**。ログや一覧をそのまま返さず、判断に必要な値だけを dict / 文字列にまとめて返す
3. **形や動きは数値で確かめる**。スクリーンショットは指示があったときだけ撮る（CLAUDE.md「作業上のルール」）。頂点の範囲・ボーンの位置・付け替え漏れの数などを `eval` で返す
4. **モデルはスクリプトで作る**。MCP で 1 操作ずつ組み立てず、定義を Python のスクリプト（例: `tools/blender/mech_frame_parts.py`）に書いて一度に作る。作り直しも同じスクリプトで行う
5. **Blender に送るコードの先頭に、目的をコメントで 1〜2 行書く**。承認するかどうかを人間と管理用セッションが判断しやすくするため（`docs/management-session-policy.md` の「Blender のコマンドを評価して解説する」）

## #68 でトークンを多く使った処理と改善案

| 処理 | 何が起きたか | 改善案 |
|---|---|---|
| Unity ↔ Blender の往復 | FBX の縮尺の不具合（`global_scale=0.01` で 1/100 になる）に Unity への取り込み後に気づき、Blender に戻ってエクスポートし直した。切り替えが 1 往復増えた | 新しい種類のモデルを作るときは、**最初に 1 つだけエクスポートして Unity で縮尺・向き・ボーンの位置を確かめてから**、残りを作る。下の「作業の順序」に従う |
| 切り替えスクリプトの実行 | PowerShell ツールから `powershell -File` を呼ぶと、入れ子のプロセスとして毎回承認が必要になる | `switch-to-blender.ps1` / `switch-to-unity.ps1` は **Bash ツール**で実行する（許可ルールで確認なしに通る） |
| FBX エクスポートのログ | `export_objects()` の標準出力（FBX ごとに数行）がそのままツールの結果に入った | `contextlib.redirect_stdout(io.StringIO())` で標準出力を捨て、`result` にはエクスポートした数と規約違反だけを入れる（下の例） |
| `eval` のコンパイルエラー | `using` を書いたため、20 件近いエラーの診断がまとめて返った | `eval` では `using` を書かず、型名を `UnityEditor.AssetDatabase` のように完全修飾する。ローカル関数の代わりに `System.Func` / `System.Action` を使う |
| プレハブの構成の調査 | `.prefab` の YAML を `m_Name` / `m_LocalScale` で Grep し、結果が 100 行以上になった | 構成は `eval` で階層・位置・コンポーネント名だけを出力する。YAML の Grep は GUID の参照先を探すときに限る |
| EditMode テストの実行 | `run_tests` が全件（95 件以上）の結果を返す。`async_tests: true` にしても同じだった | 途中の確認は `filter_type: testName` で関係するテストだけを実行する。全件の実行は PR を作る前の 1 回にする |
| ファイルをまるごと読む | `FootIK.cs`（345 行）、`export_to_unity.py`（387 行）、関連 Issue の本文全体などを読み、結果的に不要な部分も多かった | Grep で該当箇所を探し、`Read` の `offset` / `limit` で必要な範囲だけを読む。関連 Issue は依存関係を確認するのに必要な部分だけでよい |
| Play での確認の繰り返し | 構えのタイマー（1 秒）が `eval` の呼び出しの間に切れ、確認をやり直した | 時間に依存する確認は、1 回の `eval` の中で `Animator.Update(dt)` を呼んで進め、結果をまとめて返す |

## 作業の順序

1. **計画**: 作るモデル・ボーン・アニメーションの一覧と寸法を決める（既存の `.blend` とスクリプトがあれば、その定義を読むだけでよい）
2. **Blender（1 回目）**: リグと代表のパーツを 1 つだけ作り、試しにエクスポートする
3. **Unity（1 回目）**: 縮尺・向き・ボーンの位置・クリップを `eval` で確かめる。問題があればスクリプトか設定を直す
4. **Blender（2 回目）**: 残りのパーツ・アニメーションをまとめて作り、規約を検証してまとめてエクスポートする
5. **Unity（2 回目）**: インポート設定・プレハブ・マスタを `eval` でまとめて作り、Play で数値を確かめる

- Unity を閉じている間にできること（`export.sh` でのエクスポート、`tools/unity/*.sh` でのコンパイル・テスト）は、その間にまとめて行う
- Unity 側の C# の変更は、Blender に切り替える前に済ませ、コンパイルとテストを通しておく

## 例

### Blender: ログを捨ててエクスポートする

```python
import sys, importlib, io, contextlib
sys.path.insert(0, r"<リポジトリ>/tools/blender")
import export_to_unity as ex
importlib.reload(ex)
names = ["head_light", "head_heavy"]
issues = ex.validate_objects(names)
exported = 0
if not any(i["level"] == "error" for i in issues):
    with contextlib.redirect_stdout(io.StringIO()):
        exported = sum(r["exported"] for r in ex.export_objects(names))
result = {"issues": issues, "exported": exported}
```

### Unity: 階層を短く出力する

```csharp
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/PlayerSettings/Player.prefab");
var sb = new System.Text.StringBuilder();
foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true)) {
  int d = 0; var p = t; while (p.parent != null) { d++; p = p.parent; }
  if (d > 2) continue;  // 必要な深さだけ
  sb.AppendLine(new string(' ', d * 2) + t.name + " " + t.localPosition.ToString("F2"));
}
return sb.ToString();
```

### Unity: 形を数値で確かめる

`SkinnedMeshRenderer.BakeMesh` で現在のポーズの頂点を取り、地面からの高さの最小・最大だけを返す（#78 で 81 通りの組み合わせを確認した方法）。
