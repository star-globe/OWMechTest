# FBX インポート設定リファレンス

新しいFBXをUnityにインポートした後、以下の設定をInspectorで適用してください。

## Model タブ
- Scale Factor: 1
- Import Blendshapes: OFF
- Generate Colliders: OFF
- Material Creation: Import via MaterialDescription

## Rig タブ
- Animation Type: Humanoid
- Avatar Definition: Create From This Model

## Animation タブ（アニメーション付きFBXのみ）
- Import Animation: ON
- Anim. Compression: Optimal

## Materials タブ
- Material Creation Mode: Standard
- Naming: By Base Texture Name
- Search: Recursive Up

インポート後、MaterialsタブでStylizedRealLit / GreyboxMaterialを手動アサインしてください。
