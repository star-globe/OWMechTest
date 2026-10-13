---
Issue: #31 CLAUDE.md の修正・拡充（改善プラン P1）
実装日: 2026-10-12
変更ファイル:
- CLAUDE.md

実装方針:
- `docs/ai-dev-improvement-plan.md` の P1 を実施（PR #34）。AI が毎回コードを読み直さなくても構造を把握できるよう、前提知識を CLAUDE.md に集めた
- 仕様書の索引をリポジトリルート基準の `unity/docs/wiki/*.md` に修正し、wiki の全ページと `docs/` 配下を追加
- ディレクトリマップ、アーキテクチャ要約（namespace、シングルトン、起動の流れ、State/Scene 遷移、マスタデータ）、検証方法、禁止・注意事項、Blender 連携の参照先を追加
- 索引のパスが実在することをスクリプトで確認し、起動シーンとビルド対象シーンを実ファイルと照合した

詰まった点・解決策:
- wiki サブモジュールが detached HEAD のため、`unity/docs/wiki/` での素の `git pull` は失敗する → `git submodule update --init --remote unity/docs/wiki` を手順として記載
- `SceneManager` が読み込む `Customize` シーンが存在しないことが判明 → 事実として記載し、#37 で対応

残課題・関連Issue:
- #33（asmdef 導入）と #37（Customize シーン作成）の反映に合わせて記述を更新した（作業ログ作成時の PR で対応）
---
