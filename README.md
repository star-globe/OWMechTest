# OW Mech Test

カスタマイズ可能な人型兵器を駆り、戦術で勝利をつかむアクションシミュレーター（開発中）

## 概要

<img width="480" height="270" alt="omtest_play" src="https://github.com/user-attachments/assets/5a5c147f-bfb4-43c9-8483-42403b514188" />

プレイヤーは機体パーツを自由に組み合わせてメカをカスタマイズし、
NPC部隊が連携して動くオープンフィールドで戦うアクションシミュレーターです。

## 実装予定の機能

- **機体カスタマイズシステム** — パーツ選択・保存・読み込み
- **諸兵科連合AI** — 前衛/後衛/偵察などの役割分担で動く NPC 部隊
- **サブ武装スロット** — メイン左右 + サブの3武装同時携行
- **ゲームフロー** — ミッション選択 → ブリーフィング → カスタマイズ → 出撃
- **エフェクトシステム** — オブジェクトプール管理による爆発・被弾・推進炎エフェクト
- **グラフィックシステム** — URP カスタムシェーダー（スタイライズドリアル + スクリーンスペースアウトライン）
- **マスタデータビューア** — エディタ拡張による ScriptableObject の一覧表示・CSV エクスポート

## 技術スタック

| 項目 | 詳細 |
|------|------|
| エンジン | Unity 6 LTS (6000.4.6f1) |
| レンダーパイプライン | URP（Universal Render Pipeline） |
| 言語 | C# (.NET Standard 2.1) |
| 主要ライブラリ | UniRx / ProBuilder / NavMesh |
| 3D制作 | Blender（FBX パイプライン構築済み） |
| AI支援 | Claude Code（CLAUDE.md による開発フロー管理） |

## 開発環境セットアップ

### Blender MCP（Claude Code から Blender を操作する）

Claude Code は `unity/.mcp.json` に登録された `blender` サーバー経由で Blender を操作します。
構成は `Claude Code ⇐ stdio ⇒ blender-mcp（MCP サーバー） ⇐ TCP:9876 ⇒ Blender アドオン` です。

#### 前提

- Blender（Extensions 対応版。開発環境では 5.2 を使用）
- [uv](https://docs.astral.sh/uv/)（`uv --version` で確認）
- Git

#### 1. MCP サーバーを取得する

```powershell
git clone https://projects.blender.org/lab/blender_mcp.git C:/blender_mcp
```

- サーバー本体はリポジトリ内の **`mcp/` サブフォルダ**（`C:/blender_mcp/mcp`）です。`.mcp.json` はこのパスを既定値として参照します
- 別の場所に置く場合は、ユーザー環境変数 `BLENDER_MCP_DIR` に `<clone先>/mcp` を設定し、Claude Code を再起動してください

#### 2. 依存パッケージをインストールする

```powershell
uv --directory C:/blender_mcp/mcp run python -c "import blmcp; print('OK')"
```

初回は `.venv` の作成と依存パッケージのインストールが行われます。`OK` が表示されれば完了です。

#### 3. Blender アドオンを入れる

1. Blender の `Edit > Preferences > Get Extensions` を開き、Repositories に `https://lab.blender.org/` を追加
2. 「MCP」アドオンを検索してインストールし、有効化する
3. アドオン設定で **Auto Start を有効**にし、ポートが `9876` であることを確認する

#### 4. Claude Code から接続を確認する

1. Claude Code を **`unity/` ディレクトリで起動**する（初回はプロジェクトの MCP サーバー使用の承認を求められるので許可する）
2. `claude mcp list` で `blender: ... - √ Connected` と表示されることを確認する
3. Blender を起動すると、SessionStart フック（`unity/.claude/hooks/check-blender-mcp.ps1`）がアドオンに接続できるかを通知します

`√ Connected` は MCP サーバーの起動に成功したことを示すだけです。Blender への接続はツールを呼んだ時点で行われるため、Blender は後から起動しても構いません。

#### トラブルシューティング

| 症状 | 原因と対処 |
|------|------------|
| `blender` が `Failed to connect — CONNECTION_CLOSED` | サーバーのフォルダが見つからない。`C:/blender_mcp/mcp`（または `BLENDER_MCP_DIR`）に `pyproject.toml` があるか確認する。あれば手順 2 を再実行する |
| ツール呼び出しで接続エラー | Blender が未起動か、アドオンのサーバーが停止している。アドオン設定から Start するか、Auto Start を有効にする |
| ポートを変えたい | アドオン設定のポートと環境変数 `BLENDER_MCP_PORT` を同じ値にする |

## 開発状況

このリポジトリは個人開発のプロトタイプです。
Issue / PR ベースの開発フローで進めており、仕様は [Wiki](../../wiki) で管理しています。
