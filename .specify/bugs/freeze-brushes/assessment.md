# Bug Assessment: コードで作る SolidColorBrush に Freeze() していない

- **Slug**: freeze-brushes
- **Created**: 2026-09-28
- **Source**: desktop-clock-widget (非公開の開発用リポジトリ) の Issue #15 (host: github.com、allowlisted、`gh issue` で作成した本人の Issue)
- **Verdict**: valid
- **Severity**: low

## Report (要約)

issue #15 より (外部の AI (Grok) によるコードレビューでの指摘):

- 文字色・背景色が変わるたびに `ClockWindow.ApplyAppearance()` が新しい `SolidColorBrush` を作っているが、
  `Freeze()` を呼んでいない
- 変更しない `Freezable` は `Freeze()` するのが WPF のベストプラクティス (変更通知の監視が不要になり、
  スレッドをまたいで使える)
- 単一ウィンドウ・色の変更時のみの更新なので、実害はほぼない

## Symptom

目に見える不具合はない。コードで作ったブラシが、作った後に変更しないにもかかわらず変更可能なまま
残っており、WPF が変更の監視を続けている。

## Reproduction

見た目の変化がないため、再現手順はない。コード上で確認できる (下記の Suspected Code Paths)。

## Suspected Code Paths

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:255-256` — 時刻・日付の文字色のブラシ
- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:273-274` — 背景色のブラシ
- `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs:39` — 色の選択画面のパレットのブラシ
  - Issue には書かれていないが、同じ作り方
  - `static readonly` の配列に入っており、色の選択画面を開くたびに使い回している
- `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs:57` — 色の選択画面のプレビューのブラシ
  - Issue には書かれていないが、同じ作り方

## Root Cause Hypothesis

**確信度: 高** (コードで確認済み)

ブラシを作った後に `Freeze()` を呼ぶ処理がない。どのブラシも作った後に色を変えることはなく、
色を変えるときは新しいブラシを作り直している。

## Proposed Remediation

**Preferred**: 上の 4 箇所で、作ったブラシに `Freeze()` を呼んでから使う。

- `ClockWindow` と `ColorPickerWindow` の両方で同じことをするため、`SolidColorBrush` を作って
  `Freeze()` まで行う小さなヘルパーを各クラスに置くか、その場で呼ぶ
  - 呼ぶ箇所が少ないので、共通クラスは作らない (YAGNI)
- Issue の範囲 (`ClockWindow`) に加えて `ColorPickerWindow` も直す
  - 同じ指摘がそのまま当てはまり、特に static で使い回すパレットのブラシは Freeze の効果が一番大きい
  - 片方だけ直すと、次に読む人が「なぜこちらは Freeze していないのか」と迷う

**Files likely to change**:
- `src/OkidokeiWidget.App/ClockWindow.xaml.cs`
- `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs`

**Tests to add or update**:
- App 層に自動テストはない (research.md #7)
- ビルドが通ること、既存のテストが通ることを確認する
- 実機で、文字色・背景色・背景透過度の変更がこれまでどおり反映されること、色の選択画面のパレットと
  プレビューが表示されることを確認する

## Risks & Considerations

- Freeze したブラシを後から変更すると例外になる
  - 4 箇所とも作った後に変更するコードはない。色を変えるときは新しいブラシを作り直している
  - アニメーションで色を変える処理もない
- 見た目の変化はないので、実機確認は「今までどおり動くこと」の確認になる

## Open Questions

- なし
