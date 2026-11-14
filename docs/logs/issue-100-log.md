---
Issue: #100 カスタマイズ画面にエネルギーの性能と組めない理由を表示する
実装日: 2026-11-13
変更ファイル:
- unity/Assets/Script/UI/Customize/CustomizeEnergyText.cs（新規）、CustomizeManager.cs、PartsSelectButton.cs
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs、PlayerPartsAssembleDataAsset.cs
- unity/Assets/Script/Player/PlayerPartsContainer.cs
- unity/Assets/Prefabs/UI/Customize/CustomizePanel.prefab
- unity/Assets/Tests/EditMode/CustomizeEnergyTextTests.cs（新規）
- docs/spec/基本仕様.md

実装方針:
- PR は #109 の 1 つ。カスタマイズ画面の右側に `EnergyPanel` を追加し、編集中の組み合わせの総容量・総出力・常時消費の合計・回復力（ブースト OFF / ON）を表示する。組み合わせを変えるたびに更新する
- 保存 / 戻る ボタンの間に `AssembleMessage` を追加した。組めないときは当てはまる理由（ジェネレーターが無い／ブースト OFF の回復力が 0 以下／ブースト ON の回復力が 0 以下）をすべて値付きで表示し、保存ボタンを押せなくする
- `PartsButton` の 2 行目に常時消費を出す。ジェネレーターは総容量と総出力、ブースターは ON の間の消費も出す
- 保存データが組めなくなったとき: カスタマイズ画面では保存データのまま編集でき、警告を出す（組める組み合わせを保存すると消える）。Battle では `PlayerPartsContainer.Awake` が検出し、初期の組み合わせを使う（`PlayerPartsAssembleDataAsset.UseDefaultData`）。PlayerPrefs は消さない
- エネルギーの計算（`EnergyPerformance` / `EnergyPerformanceResolver` / `GeneratorSettings`）は変えていない（#98 と重ならないようにするため）。常時消費の合計は「総出力 − ブースト OFF の回復力」で求める
- 文言の組み立ては Unity に依存しない `CustomizeEnergyText` に分け、`CustomizeEnergyTextTests`（8 件）を追加した
- レイアウト: SlotList（x 0.03〜0.36）、PartsList（x 0.37〜0.70）、EnergyPanel（x 0.71〜0.97）を横に並べた。`PartsButton` の高さを 60 から 80 にした。新しいテキストは NotoSansJP、Scale 1
- 仕様変更: 基本仕様の「機体のカスタマイズ」に、性能と組めない理由の表示、組めなくなった保存データの扱いを追記した

詰まった点・解決策:
- 文字の収まりはスクリーンショットを撮らずに確かめた。1920×1080 の基準解像度で、想定される最も長い文言を入れて TMP の preferred サイズと RectTransform を比べた（性能 383×193 / 枠 431×551、組めない理由 720×116 / 枠 730×216、ジェネレーターの選択肢 427×26 / 枠 558×32）
- 組めない状態は今のマスタでは作れないため、Play 確認ではマスタに無いジェネレーターを保存データに入れて確かめた（理由と警告の表示、保存ボタンの無効化、Battle での初期の組み合わせへの切り替え、直して保存すると警告が消えること）。確認後、PlayerPrefs は元の状態（保存データなし）に戻した

残課題・関連Issue:
- 未確認: 見た目（配色・余白）の目視確認。人間の確認待ち
- 回復力が 0 以下になる組み合わせは今のマスタには無い（#97 の残課題と同じ）。武器に常時消費を付けるか、出力の低いジェネレーターを追加したときに、回復力による「組めない理由」の表示を実際の組み合わせで確かめる
- #98: オーバーヒート。エネルギーの計算を変える場合は、`CustomizeEnergyText` の常時消費の合計（総出力 − OFF の回復力）の求め方が合うか確かめる
---
