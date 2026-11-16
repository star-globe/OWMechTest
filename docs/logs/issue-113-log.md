---
Issue: #113 デバッグシーンをエディタのメニューから起動できるようにする
実装日: 2026-11-16
変更ファイル:
- unity/Assets/Script/Editor/PlayMainSceneMenu.cs
- CLAUDE.md
- unity/.claude/settings.json
- unity/.claude/skills/unity-verify/SKILL.md

実装方針:
- `WarGame/Play Customize (Debug)`（priority 2）と `WarGame/Play Effect Check`（priority 3）を追加した
- Play の停止 → `SaveCurrentModifiedScenesIfUserWantsTo` → シーンを開く → Play の流れを共通の `PlayScene` にまとめ、`WarGame/Play Main Scene` もこれを使う（動作は変えていない）。コンパイル中は validate で無効
- CLAUDE.md「デバッグ実行」と `unity-verify` スキルを、デバッグ用のシーンはメニューから起動する形に書き換えた。シーンを追加したらメニューと一覧を足すことも書いた
- 人間と相談して、メニューで足りるため許可リストから `editor_play` を外した（`open_scene` は残す）
- PR #171

詰まった点・解決策:
- #170 と同じ箇所を書き換えるため、#170 のマージを待たずに #112 のブランチから作り、PR に #170 を先にマージするよう書いた
- メニューの中の保存確認はダイアログを出して Editor を止めるので、メニューの前にも `list_open_scenes` で未保存シーンを確かめる手順を残した

残課題・関連Issue:
- なし
---
