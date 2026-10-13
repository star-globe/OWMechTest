---
Issue: #37 Customize シーンの作成
実装日: 2026-10-13
変更ファイル:
- unity/Assets/Prefabs/UI/Customize/CustomizePanel.prefab
- unity/Assets/Scenes/Customize.unity
- unity/ProjectSettings/EditorBuildSettings.asset
- unity/Assets/Prefabs/UI/Briefing/BriefingPanel.prefab
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs
- unity/Assets/Script/Parts/PlayerPartsAssembleDataAsset.cs
- unity/Assets/Resources/PlayerSettings/PlayerPartsAssembleDataAsset.asset
- unity/Assets/Script/Master/MasterManager.cs
- CLAUDE.md、unity/.claude/skills/unity-verify/SKILL.md（Play 前のシーン保存状態確認ルール）

実装方針:
- #8 で実装済みのロジック（`CustomizeManager` など）に対し、不足していたプレハブとシーンを作成（PR #38）
- `CustomizePanel` プレハブ（スロット一覧・パーツ一覧・保存・戻る）を BriefingPanel と同じ配色・NotoSansJP で作成し、`UICanvas` の子にプレハブインスタンスとして配置
- ブリーフィング画面の非アクティブだった CUSTOMIZE ボタンを有効化
- Briefing → Customize → パーツ変更・保存 → Briefing の流れを Play で確認。テストで書き込んだ PlayerPrefs とメモリ上のアセットは元に戻した

詰まった点・解決策:
- `CustomizeManager` が要求する `PlayerPartsAssembleDataAsset` が基底クラスと同じファイルにあり、Unity がアセットと結びつけられなかった → 専用ファイルに分離し、既存アセットのスクリプトを切り替え（フィールドが同じなのでデータは維持）
- `MasterManager.LoadAll()` が `GunMaster` / `BoosterMaster` より先に `PartMaster` を読み込み、パーツが常に 0 件だった → 読み込み順を修正
- Play 実行時に「Scene(s) Have Been Modified」ダイアログで Editor が止まった。原因は Unity 6.6 による MainScene の自動再シリアライズ → Play 前に `isDirty` を確認し、意図しない変更は開き直して破棄するルールを CLAUDE.md と `/unity-verify` に追加
- プレハブ保存時に Unity 6.6 の形式更新（`m_ActiveFontFeatures` など）が差分に混ざる → YAML 直接編集は禁止のため、そのままコミットして PR に明記

残課題・関連Issue:
- #39 カスタマイズ結果のバトル反映とプレイヤー/NPC の機体データ分離
- #40 Editor の Play 中にカスタマイズを保存すると git 管理下のアセットが書き換わるおそれがある
---
