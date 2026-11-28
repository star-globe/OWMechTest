---
Issue: #210 カメラが床や障害物にめり込まないよう位置を調整する
実装日: 2026-11-28
変更ファイル:
- unity/Assets/Script/Player/FollowCamera.cs（遮蔽物の判定、CalcObstructedPosition を追加）
- unity/Assets/Script/Utils/GameLayers.cs（CameraObstacleMask を追加）
- unity/Assets/Tests/EditMode/FollowCameraTests.cs（寄せる位置の計算のテストを 4 件追加）
- docs/spec/プレイヤー操作.md（「遮蔽物への対応」に判定の始点・余白・他の機体を対象外にすることを追記）

実装方針:
- 注視点の基準（center + up × lookHeight = 機体の中心の高さ）から、予定のカメラ位置（camTarget）へ Physics.SphereCast する。遮られていれば、当たった距離の点（球の中心の位置。遮蔽物から半径分だけ手前）を追従の目標にする
- 補間で遅れた実カメラの位置も同じ判定で確かめ、遮蔽物の向こうに残っていれば補間を待たずに手前へ寄せる。遮蔽が無くなれば目標が camTarget に戻り、既存の UpdateCameraPos の補間で戻る
- 判定のレイヤーは RaycastObstacleMask に HitableObject を足した CameraObstacleMask。自機・他の機体・ユニット・ミサイルは除外し、トリガーは QueryTriggerInteraction.Ignore で無視する。他の機体を除くのは、動く相手で視点が揺れるのを避けるため
- 球の半径 collisionRadius（既定 0.5 m）と最小距離 minCollisionDistance（既定 0.5 m）はインスペクターで調整できる。ResetCamera も同じ判定を通す

詰まった点・解決策:
- 球の半径をカメラのニアクリップと同じ 0.3 m にしていたが、ニアクリップ面の角は半径より外に出るので 0.5 m に上げた
- バトルの確認は、eval で FieldManager.SetPendingMission(0) と StateManager.NextState() を呼んで進めた。カメラと機体の間に試験用の Cube（Default レイヤー）を置き、カメラが手前で止まり、外すと戻ることを確かめた
- Play 中の editor_status は MCP で 400 を返したので、`unity command editor_status` で確認した

残課題・関連Issue:
- #66 に追加: コントローラーで回したときの見え方の確認／上下の回転が下限（約 -81 度）のとき、カメラが camTarget から約 5 m 遅れて止まる件（遮蔽物の判定では何にも当たっておらず、既存の補間の調整によるものと思われる）
---
