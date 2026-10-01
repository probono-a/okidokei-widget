# Bug Assessment: 閉じたウィジェットの時計更新タイマーが止まらず、ウィンドウがメモリに残り続ける

- **Slug**: clock-timer-leak
- **Created**: 2026-09-28
- **Source**: desktop-clock-widget (非公開の開発用リポジトリ) の Issue #49
  - host: github.com (allowlisted)。`gh issue view` で本文を取得した
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

issue #49 の要約 (外部の AI による公開リポジトリの静的レビューでの指摘):

- 閉じた `ClockWindow` の時計更新タイマー (`DispatcherTimer`) が止まらず、ウィンドウがメモリに残ったまま毎秒の更新処理を続ける
- モニタの抜き差しや、詳細設定でのモニタの非表示のたびに 1 つずつ溜まっていく
- 修正方針 (案): `ClockWindow` の `Closed` で `_timer.Stop()` を呼ぶ

## Symptom

- 閉じたウィジェットのタイマーが動き続け、ウィンドウも GC されずに残る
- 期待する動作は、ウィジェットを閉じたらタイマーも止まり、ウィンドウが解放されること

## Reproduction

1. アプリを起動する
2. 詳細設定でモニタの表示/非表示を何度か切り替える、またはモニタを抜き差しする
3. 閉じたウィンドウの数だけ、裏でタイマーが動き続ける

- 見た目には何も起きないため、実機で目視での再現はできない
- 実機でのメモリ計測はしていない。コードからの判断

## Suspected Code Paths

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:75-77`: タイマーを作って `Start()` しているが、`Stop()` を呼ぶ箇所がない
  - `Closed` イベントの処理や `OnClosed` の override もない
- `src/OkidokeiWidget.App/App.xaml.cs:97`: モニタの取り外しでウィンドウを閉じる
- `src/OkidokeiWidget.App/App.xaml.cs:112`: 詳細設定で非表示に切り替えたときにウィンドウを閉じる

## Root Cause Hypothesis

- `DispatcherTimer` は動作中、Dispatcher から参照され続ける
- `Tick` のラムダがウィンドウ自身 (`UpdateClockText`) を掴んでいる
- なので、ウィンドウを閉じてもタイマーが止まらず、ウィンドウも GC されない
- 確信度: high (`DispatcherTimer` の既知の性質で、コード上も `Stop()` がない)

## Proposed Remediation

**Preferred**: `ClockWindow` で `OnClosed` を override し、`_timer.Stop()` を呼ぶ。

- `Closed` イベントに購読するより、自分自身のイベントは override で処理するほうが素直
- ほかに `ClockWindow` が購読しているのは自分自身のイベント (`SourceInitialized`・`SizeChanged` 等) だけで、外部のオブジェクトへの購読はない
  - なので、後始末が必要なのはタイマーだけ

**Files likely to change**:

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs`
- `specs/001-clock-widget/tasks.md` (Phase の追記)

**Tests to add or update**:

- なし。WPF のウィンドウとタイマーは Core のテスト (xUnit) の対象外

## Risks & Considerations

- 閉じた後に `Tick` が来ても `UpdateClockText` はテキストを書き換えるだけで、落ちることはない
  - なので、止め忘れがあっても見た目の不具合にはならず、気づきにくい
- 修正後に閉じたウィンドウが実際に GC されることは、メモリ計測をしないと確かめられない

## Open Questions

- なし
