---
Issue: #187 FollowCamera をインゲームで調整できるようにする（注視点の高さ・距離・遊びの画角）
実装日: 2026-11-24
変更ファイル:
- unity/Assets/Script/Player/FollowCamera.cs
- unity/Assets/Script/Editor/FollowCameraEditor.cs（新規）
- unity/Assets/Tests/EditMode/FollowCameraTests.cs（新規）
- unity/Assets/Resources/PlayerSettings/Player.prefab

実装方針:
- カメラの位置を `camTarget` の localPosition（`Start()` で取得）から求める方式をやめ、距離 `cameraDistance` と見下ろす角度 `cameraElevation` から毎フレーム計算するようにした。Play 中にインスペクターで変えるとすぐに反映される
- 注視点の高さ `lookHeight` を追加した（center から上へずらした点を注視点の基準にする）
- 遊び `cameraDiffMin`（m）を `deadZoneAngle`（度）に置き換え、カメラの距離から m に換算する。距離を変えても見た目の遊びが変わらない
- 換算の計算は static メソッド（`CalcCameraLocalOffset` / `CalcDeadZoneDistance`）にして EditMode テストを書いた
- カスタムインスペクター `FollowCameraEditor` を追加した。Play 中の「プレハブに反映」で、参照以外の値を元のプレハブへ書き戻す。プレイヤーは Resources から Instantiate されるためプレハブとの接続が無く、FollowCamera を同じ位置に持つプレハブを探して対応付ける
- カメラリセット `ResetCamera()`（public）と、それを呼ぶボタンを追加した。注視点とカメラの位置の値を Play 開始時の値に戻し、上下の回転を 0 にしてカメラを目標位置へすぐ移す。「プレハブに反映」に成功したら、戻す値もその値に更新する
- 初期値は改修前の見た目（カメラ位置 (0, 6, -16)、遊び 0.3 m）に合わせてから、人間がゲーム内で調整して反映した（注視点の高さ 4、距離 13.6、角度 10、遊び 20°）。スライダー（`[Range]`）は人間が付けた

詰まった点・解決策:
- AI エージェントの操作では Play 中のインスペクターのボタンを押せないため、`SerializedObject` で値を変え、反映の処理をリフレクションで呼んで確認した。ボタンの操作は人間が確認した
- 調整中に Editor のフレーム落ちが気になったが、原因は Profiler ウィンドウ自体の描画（`ProfilerWindow.Paint`）だった

残課題・関連Issue:
- なし
---
