---
Issue: #8 カスタマイズ画面の実装
実装日: 2026-06-02
変更ファイル:
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs
- unity/Assets/Script/State/StateManager.cs
- unity/Assets/Script/Scene/SceneManager.cs
- unity/Assets/Script/UI/Briefing/BriefingManager.cs
- unity/Assets/Script/UI/Customize/CustomizeManager.cs（新規）
- unity/Assets/Script/UI/Customize/PartsSlotButton.cs（新規）
- unity/Assets/Script/UI/Customize/PartsSelectButton.cs（新規）

実装方針:
- 保存形式を BinaryFormatter + PlayerPrefs から JsonUtility + PlayerPrefs に変更
- GameState に Customize(6) を追加し、GoToState() で任意遷移できる方式を採用（線形 NextState() フローには組み込まない）
- SceneManager に "Customize" シーンを登録
- BriefingManager の customizeButton プレースホルダーを CustomizeManager 経由で接続
- カスタマイズ画面はスロット選択→パーツ選択→保存の2段UI構成
- 対象スロット：頭部・胴体・腕部・脚部・ブースター・右腕武装・左腕武装・サブ武装（Issue #3 で追加した Weapon_Sub を含む）

詰まった点・解決策:
- main ブランチに #3・#5・#7 等の変更が先にマージされており、GameState や SceneManager が既に更新済みだった。
  → main を最新化してから feature ブランチを作成することで競合を回避した。
- BriefingManager に customizeButton のプレースホルダーが既に用意されていたため、接続コストが低かった。
- PartMaster.GetSettings() は失敗時に LogError を出すため、未装備（id=-1）の場合は GetDictionary() で TryGetValue する方式にした。

残課題・関連Issue:
- Unity Editor での残作業が必要：Customize シーンの作成、UI プレハブの配置、PlayerPartsAssembleDataAsset のアサイン、Build Settings へのシーン追加
- 3Dプレビュー（機体リアルタイム表示）は未実装。#5 グラフィック方針確定後に検討。
- パーツのステータス比較機能は未実装。
- セーブスロット複数対応は未実装。
---
