## グラフィック検証シーン セットアップ手順

### シーン構成
1. 新規シーンを作成し、このフォルダに GraphicsTest.unity として保存する
2. ProBuilder で基本形状（人型シルエット相当のボックス群）を配置
3. 各オブジェクトに GraphicsStyleSwitcher コンポーネントを追加
   - Greybox Material   : Assets/Prefabs/Material/GreyboxMaterial
   - Stylized Material  : Assets/Prefabs/Material/StylizedRealPrototypeMaterial
4. Canvas > Panel にボタンを2つ配置し GraphicsTestUI をアタッチ

### アウトライン設定
1. Project Settings > Graphics > URP Renderer Asset を開く
2. Add Renderer Feature > OutlineRendererFeature を追加
3. Outline Material に Assets/Shader/StylizedReal/Outline.shader を使用したマテリアルをセット
4. Depth Normals を有効にする（Camera の Depth Texture mode = DepthNormals）

### プロトタイプ運用方針
- 当面は Greybox マテリアルで動作確認を行う
- 本実装移行時はテクスチャを差し替えて StylizedRealPrototypeMaterial に切り替える
- GraphicsStyleSwitcher の currentStyle をインスペクターで変更するだけで即プレビュー可能
