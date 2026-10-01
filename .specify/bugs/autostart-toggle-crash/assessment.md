# Bug Assessment: 詳細設定で自動起動を切り替えたときに失敗すると、アプリごと落ちる

- **Slug**: autostart-toggle-crash
- **Created**: 2026-09-28
- **Source**: desktop-clock-widget (非公開の開発用リポジトリ) の Issue #18
  - host: github.com (allowlisted)。`gh issue view` で本文とコメントを取得した
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

issue #18 の要約 (外部の AI によるコードベースレビューでの指摘):

- タイトルは「自動起動設定 (AutoStartManager.SetEnabled) が起動時に例外を投げるとアプリ全体が落ちる」
- `AutoStartManager.SetEnabled` の呼び出し元に `try/catch` がなく、例外が出るとアプリが落ちる
- 修正方針 (案): 呼び出し側を `try/catch` で囲む、または `TrySetEnabled` で成功/失敗を `bool` で返す

2026-09-27 のコメントで、現状が次のように更新されている:

- 起動時の呼び出し (`OnStartup` 内) は #20 の修正で削除済み
- 残っているのは、詳細設定で自動起動を切り替えたときの `App.xaml.cs:261` (`OnAutoStartChanged`) の 1 か所
- 起動できなくなる心配はなくなったが、切り替え操作で落ちる可能性は残っている

## Symptom

- 詳細設定で自動起動のチェックを切り替えたとき、ショートカットの作成・削除に失敗すると、未処理例外でアプリごと落ちる
- 期待する動作は、失敗しても落ちずに動き続け、失敗したことが分かること (constitution 原則 VI「失敗しても落ちない」)

## Reproduction

1. Startup フォルダ (`shell:startup`) に書き込めない状態にする
   - 例: `OkidokeiWidget.lnk` という名前のフォルダを Startup フォルダに作っておく
2. アプリを起動し、詳細設定で「Windows 起動時に自動的に起動する」をオンにする
3. ショートカットの保存に失敗し、アプリが落ちる

- 手順 1 の「同名のフォルダ」は、書き込み権限がない状態の代わりに失敗を起こす方法
- 手順 2〜3 はコードからの推定。`AutoStartManager.SetEnabled` が例外を投げること自体は、fix の単体テストで確かめる

## Suspected Code Paths

- `src/OkidokeiWidget.App/App.xaml.cs:259-263` (`OnAutoStartChanged`): `AutoStartManager.SetEnabled` を `try/catch` なしで呼んでいる
- `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs:17-32` (`SetEnabled`): 次の例外を投げうる
  - `Environment.ProcessPath` が null のときの `InvalidOperationException`
  - `Directory.CreateDirectory` の `IOException`・`UnauthorizedAccessException`
  - `IPersistFile.Save` の `COMException` (書き込み先がない・権限がない等)
  - `File.Delete` の `IOException`・`UnauthorizedAccessException` (ショートカットが使用中・権限がない等)
- `src/OkidokeiWidget.App/SettingsWindow.xaml.cs:130-139` (`AutoStartCheckBox_Changed`): 設定値を書き換えてから `OnAutoStartChanged` を呼ぶ

## Root Cause Hypothesis

- 設定ファイルの保存は FR-022 で「失敗してもクラッシュしない」ようにしてある
- 一方、自動起動のショートカットの作成・削除だけは例外を受け止めておらず、失敗がそのまま未処理例外になる
- 確信度: high (コード上 `try/catch` がないことは明らか)

## Proposed Remediation

**Preferred**:

- `AutoStartManager` に `TrySetEnabled` を追加し、上記の例外を受け止めて成功/失敗を `bool` で返す
  - Core 側に置くことで、失敗したときの動きを単体テストで確かめられる
- `App.OnAutoStartChanged` は `TrySetEnabled` を使い、失敗したら次のようにする
  - 設定値 (`AutoStartEnabled`) を切り替え前に戻し、保存しない
  - 「自動起動の設定を変更できませんでした」と MessageBox で知らせる
- `SettingsWindow` は、切り替えの後にチェックボックスを設定値に合わせ直す
  - 失敗して設定値が戻ったときに、チェックの表示と実際の状態が食い違わないようにするため

**Alternatives**:

- `App.OnAutoStartChanged` の中で `try/catch` するだけにする
  - 変更は少ないが、失敗時の動きを単体テストで確かめられない
- 失敗しても設定値は戻さない
  - チェックの表示と実際のショートカットの有無が食い違い、次の起動で自動起動するかが分からなくなる

**Files likely to change**:

- `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs`
- `src/OkidokeiWidget.App/App.xaml.cs`
- `src/OkidokeiWidget.App/SettingsWindow.xaml.cs`
- `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs`
- `specs/001-clock-widget/tasks.md` (Phase の追記)

**Tests to add or update**:

- ショートカットを保存できない場合 (同名のフォルダがある)、`TrySetEnabled(true)` が例外を投げずに false を返す
- Startup フォルダの場所にファイルがあってフォルダを作れない場合も、false を返す
- 成功する場合は true を返し、ショートカットが作られる

## Risks & Considerations

- 受け止める例外を広げすぎると、本当のバグを隠してしまう
  - なので `catch (Exception)` にはせず、上に挙げた種類に絞る (`SettingsRepository.Save` と同じ方針)
- spec.md の変更は不要
  - FR-019 (自動起動の ON/OFF) の実装バグであり、失敗時に落ちないのは原則 VI の守る品質に当たる

## Open Questions

- なし
