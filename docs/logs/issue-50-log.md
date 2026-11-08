---
Issue: #50 残っているコンパイル警告の解消（BoosterPart の未使用フィールド、MasterDataEditorWindow の AppDomain.GetAssemblies）
実装日: 2026-10-15
変更ファイル:
- unity/Assets/Script/Parts/Booster/BoosterPart.cs
- unity/Assets/Script/Editor/MasterDataEditorWindow.cs

実装方針:
- `BoosterPart` の外側のクラスにある未使用フィールド `boostBit` / `isOn` を削除した（PR #51）。状態は内側のクラス `BoosterParticleSystem` が持っている
- `MasterDataEditorWindow.FindMasterTypes()` の `AppDomain.GetAssemblies()` を使った型の列挙を、`TypeCache.GetTypesDerivedFrom<IDBasedMasterSettings>()` に置き換えた
- 確認：コンパイル警告が消えたこと、マスタの型の一覧（11 件）が置き換え前と変わらないこと

詰まった点・解決策:
- 同名のフィールドが内側のクラスにもあるため、どちらが未使用かを参照を追って確認してから削除した

残課題・関連Issue:
- `BoosterPart.Quick()` はどこからも呼ばれていない。機能が未接続の可能性があり、P6（責務分割）のときに確認する
---
