---
Issue: #154 銃のモデルを作り直す（入れ替え用に 3 種類、左右対称の左用・右用）
実装日: 2026-11-17
変更ファイル:
- tools/blender/weapon_parts.py（新規）
- tools/blender/export_to_unity.py
- blender/weapon.blend（新規）
- unity/Assets/Models/Weapons/weapon_{left,right}_{rifle,smg,cannon}.fbx（新規）
- unity/Assets/Script/Parts/Weapon/Gun/GunPart.cs
- unity/Assets/Script/Parts/Weapon/Gun/GunAttach.cs
- unity/Assets/Prefabs/Parts/Gun/Gun_001_part.prefab（差し替え）、Gun_002_part.prefab・Gun_003_part.prefab（新規）
- unity/Assets/Prefabs/Parts/Resources/GunSettings/Gun_002.asset・Gun_003.asset（新規）
- unity/Assets/Prefabs/Mech/MechBody.prefab、unity/Assets/Resources/PlayerSettings/PlayerNPC.prefab
- docs/blender-unity-pipeline.md

実装方針:
- 銃は 3 種類。ライフル（Gun_001、2.6 m）、短機関銃（Gun_002、1.6 m、reloadInter 5）、大口径砲（Gun_003、3.4 m、reloadInter 30）。原点はグリップ、銃口は Blender の -Y（Unity の +Z）。弾は 3 種類とも Bullet_001
- 左用は弾倉やセンサーを外側（+X）に寄せる。右用は `weapon_parts.py` の `mirror()` で X を反転して作る（`PARTS` に `weapon_right_*` として登録。腕と同じ方式）。`mech_frame_parts.mirror()` はボーン名を置き換えるので使い回さなかった
- 命名規約を `weapon_{left/right}_{バリアント名}` にした。銃口に空オブジェクト `muzzle_{left/right}_{種類}` を置き、プレハブの発射位置にはそれをそのまま使う
- プレハブは 1 種類につき 1 つで、左右のモデルを両方入れる。`GunPart.SetSide()` で表示するモデルと発射位置を切り替え、`GunAttach.side` で持つ側を決める。カスタマイズの候補は 1 種類につき 1 つのまま
- Unity（C#）→ Blender（6 体をまとめて作り、エクスポート）→ Unity（プレハブ・マスタ・Play）の順にして、切り替えを 2 回に抑えた。縮尺と向きはブースターで確認済みだったので、試しのエクスポートは省いた
- PR #175

詰まった点・解決策:
- PR を作った後に #111（PR #167）がマージされ、`GunAttach` が `Player.prefab` から `MechBody.prefab` に移ったため、`Player.prefab` の左の持ち手の設定が衝突した。main をマージして `Player.prefab` は main の版を採用し、設定を `MechBody.prefab` に付け直した。カスタマイズ画面のプレビューでも左右のモデルが正しく出るようになった
- 構えの確認では、`MechRig.NotifyFire` の後に eval を分けると構えのタイマーが切れる。1 回の eval の中で `Animator.Update` を進めて確かめた
- `capture_game_view` の `save_path` は、プロジェクトの外を指定すると拒否される。プロジェクトの中の `Temp/` を指定すると `Assets/Temp/` に保存されて `.meta` ができたので、外へ移して消した（#66 に追加）
- Blender MCP で撮影用のレンダーをした後、`switch-to-unity.ps1` で Blender が閉じずに止まった。人間に閉じてもらった（#66 に追加）

残課題・関連Issue:
- #169 弾のモデルを作り直す（大口径砲に合う弾）
- サブスロット（左右で持ち替える武器）の仕様は別の PR で扱う。サブスロットの `GunAttach.side` は既定の Right のまま
---
