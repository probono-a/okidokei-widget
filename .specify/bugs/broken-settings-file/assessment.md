# Bug Assessment: 壊れた設定ファイルが警告の前に上書きされる・一部が null だと起動直後に落ちる

- **Slug**: broken-settings-file
- **Created**: 2026-09-28
- **Source**:
  - desktop-clock-widget (非公開の開発用リポジトリ) の Issue #50
  - desktop-clock-widget (非公開の開発用リポジトリ) の Issue #51
  - host: github.com (allowlisted)。`gh issue view` で本文とコメントを取得した
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

2 つの issue はどちらも外部の AI による公開リポジトリの静的レビューでの指摘で、修正の順番に依存があるため 1 つのブランチで一緒に直す (マイルストーン 2 の説明のとおり)。

- issue #50: `settings.json` が JSON として読めないと、警告を出す前に初期設定で上書きしてしまい、元の設定が失われる
  - 修正方針 (案): 保存の前に元のファイルを `settings.json.bak` として残し、警告文にそのことを書き添える
- issue #51: `"Appearance": null` のように設定の一部が `null` だと、起動直後にアプリが落ちる
  - 修正方針 (案): `Load()` で `null` をチェックし、パース失敗と同じ「壊れたファイル」として扱う
  - #50 を先に直さないと、「起動時に落ちる」が「起動するが設定が消える」に変わるだけになる

どちらも、constitution v1.6.0 の原則 VI「失敗しても落ちない・設定を失わない。設定ファイルが壊れていたり、手で書き換えられていたりしても同じ」の守る側に当たる (各 issue のコメント)。

## Symptom

- #50: 読めない設定ファイルは、警告が出た時点で初期設定の内容に置き換わっている
  - 期待する動作は、元のファイルが残っていて、手で直せること
- #51: 一部が `null` の設定ファイルでは、何も表示されずにプロセスが終了する
  - 期待する動作は、読めないファイルと同じく初期設定で起動し、警告が出ること

## Reproduction

#50:

1. `%APPDATA%\OkidokeiWidget\settings.json` の末尾の `}` を消して保存する
2. アプリを起動する
3. 警告が出た時点で `settings.json` を開くと、初期設定の内容に置き換わっている

#51:

1. `%APPDATA%\OkidokeiWidget\settings.json` の `"Appearance": { ... }` を `"Appearance": null` に書き換えて保存する
2. アプリを起動する
3. ウィジェットが表示されずにプロセスが終了する

- どちらもコードからの推定。実機での再現はまだ行っていない
- 実機で試すには、動いているウィジェットを止め、利用中の `settings.json` を書き換える必要がある

## Suspected Code Paths

- `src/OkidokeiWidget.App/App.xaml.cs:41`: `SettingsRepository.Load()` がパースに失敗し、初期設定と `fellBackToDefaults = true` を返す
- `src/OkidokeiWidget.App/App.xaml.cs:45`: `SettingsRepository.Save(settings)` が無条件に実行され、初期設定で上書きする (#50)
- `src/OkidokeiWidget.App/App.xaml.cs:53-60`: その後で警告の MessageBox を出す
- `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs:37-38`: パースに成功すると、`null` を含む `WidgetSettings` をそのまま返す (#51)
  - `WidgetSettings` の初期値 (`= new()`) は、JSON に明示的な `null` があると上書きされる
- `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs:49`: `Save` の中の `settings.Appearance.BackgroundOpacity` で NullReferenceException になる (#51)
- `null` になりうる入れ子のオブジェクトは、`Appearance`・`WindowBehavior`・`Monitors` と、`Monitors` の各モニタの値
  - `"monitor-1": null` も `App.SyncClockWindows` の `placement.IsVisible` 等で落ちる。issue には書かれていないが同じ種類なので含める
  - 文字列の項目 (`FontFamily`・色・区切り文字) は、表示の前にどれも null を既定値へ置き換える処理 (`FontResolver`・`ColorHexResolver`・`DateSeparatorResolver`) を通るので対象外

## Root Cause Hypothesis

- #50: 起動時の保存 (`MonitorSettingsReconciler` で補ったモニタの設定を書き出すためのもの) が、読み込みに失敗したかどうかに関係なく実行される
- #51: デシリアライズの結果に `null` が含まれうることを考慮していない
- 確信度: high (どちらもコード上の順序と null チェックの有無から明らか)

## Proposed Remediation

**Preferred**:

- #51: `SettingsRepository.Load()` で、デシリアライズの後に `Appearance`・`WindowBehavior`・`Monitors`・各モニタの値の `null` を調べる
  - どれかが `null` なら、パース失敗と同じく初期設定と `FellBackToDefaults = true` を返す
- #50: `SettingsRepository` に、読めなかった設定ファイルを `settings.json.bak` としてコピーして残すメソッドを追加する
  - `App.OnStartup` は、`fellBackToDefaults` が true なら、起動時の保存より前にこれを呼ぶ
  - 警告文に、元のファイルを残した場所を書き添える
  - 残せなかった場合 (読み取りもできないファイル等) は、これまでの警告文のままにする
- `specs/001-clock-widget/contracts/settings-file.md` の「読み込み契約」に、次の 2 点を足す
  - 一部が `null` の場合もパース不可として扱う
  - パース不可の場合は、元のファイルを `settings.json.bak` として残す

**Alternatives**:

- 読めなかったときは、起動時の保存をしない
  - 起動直後の上書きは防げるが、その後に設定を 1 つでも変えると保存されて同じく失われる
  - なので、元のファイルを別名で残すほうが確実
- `.bak` をリネーム (移動) で作る
  - コピーのほうが、後の保存に失敗しても元のファイルが元の場所に残るので安全

**Files likely to change**:

- `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs`
- `src/OkidokeiWidget.App/App.xaml.cs`
- `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs`
- `specs/001-clock-widget/contracts/settings-file.md`
- `specs/001-clock-widget/tasks.md` (Phase の追記)

**Tests to add or update**:

- `Appearance`・`WindowBehavior`・`Monitors`・モニタの値がそれぞれ `null` のとき、初期設定へフォールバックし通知フラグを立てる
- 読めなかったファイルを `.bak` として残し、その後に初期設定を保存しても `.bak` の中身は元のまま
- 設定ファイルが存在しないときは `.bak` を作らない

## Risks & Considerations

- `.bak` は毎回上書きする。前回の `.bak` は失われるが、壊れたファイルを何度も作ることは通常の操作では起きないので、世代管理はしない (原則 I)
- spec.md の変更は不要
  - FR-018 (読めない設定ファイルでも初期設定で起動し、通知する) の範囲の実装バグ
  - 元のファイルを残すのは、原則 VI の「設定を失わない」を満たすための実装の詳細
- 一方で `contracts/settings-file.md` は読み込み時の動きを定める文書なので、実装と食い違わないように合わせて直す

## Open Questions

- なし
