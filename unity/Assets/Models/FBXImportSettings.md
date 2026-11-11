# FBX インポート設定リファレンス

新しいFBXをUnityにインポートした後、以下の設定をInspectorで適用してください。

## Model タブ
- Scale Factor: 1
- Import Blendshapes: OFF
- Generate Colliders: OFF
- Material Creation: Import via MaterialDescription

## Rig タブ
- Animation Type: Generic（メカの体型なので Humanoid は使わない。#68）
- Avatar Definition: Create From This Model

## Animation タブ
- 共通リグ（`Rig/rig_mech.fbx`）: Import Animation ON。クリップ名は `rig_mech|walk` → `walk` のように `|` より後ろにし、Loop Time を ON にする
- パーツ（`Parts/`）: Import Animation OFF（アニメーションは共通リグにだけ入れる）

## Materials タブ
- Material Creation Mode: Standard
- Naming: By Base Texture Name
- Search: Recursive Up

インポート後、MaterialsタブでStylizedRealLit / GreyboxMaterialを手動アサインしてください。
