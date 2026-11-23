---
Issue: #184 Battle 移行時にマウスポインタを画面中心へ移動し、カメラが勝手に動くのを防ぐ
実装日: 2026-11-23
変更ファイル:
- unity/Assets/Script/Utils/InputUtils.cs
- unity/Assets/Script/Input/PlayerInputManager.cs
- unity/Assets/Script/Scene/SceneManager.cs
- unity/Assets/Script/Field/FieldManager.cs

実装方針:
- カメラはマウスポインタの「画面中心からのずれ」で回る方式（`InputUtils.GetCenterMouse()`）のため、Battle シーンとフィールドの読み込み完了時に `InputUtils.WarpMouseToCenter()`（`Mouse.WarpCursorPosition`）でポインタを画面中心へ移動する
- フィールドの読み込み完了を待つため、`FieldManager.IsLoadingField`（読み込み中のシーン数）を追加した
- `Application.isFocused` が false の間はカメラ入力を 0 にする
- フォーカスが戻った後は、マウスの `lastUpdateTime` がフォーカスの戻った時刻（`InputState.currentTime`）を超えるまでカメラ入力を 0 にする
- 戦闘中（`InputUtils.RecenterOnFocus`）は、フォーカスが戻った次のフレームでポインタを画面中心へ移動する。メニュー画面では移動しない

詰まった点・解決策:
- ゲーム画面が背面にある間、Input System（`backgroundBehavior=ResetAndDisableNonBackgroundDevices`）がマウスを初期状態に戻し、位置が (0, 0)＝画面左下として読まれてカメラが下を向いていた。フォーカスが無い間は入力を 0 にして対応した
- フォーカスが戻っても、マウスから位置の通知が来るまで位置は (0, 0) のまま残り、カメラが激しく動いた。通知が届くまで入力を 0 にし、戦闘中はポインタを中心へ移動して通知を起こすようにした
- フォーカスが戻った同じフレームではデバイスがまだ有効に戻っていないことがあるため、ポインタの移動は次の Update で行う
- AI エージェントの操作では Game ビューにフォーカスを移せず（`Application.isFocused` が false のまま）、フォーカスが戻ったときの動作は人が手元で確認した

残課題・関連Issue:
- なし
---
