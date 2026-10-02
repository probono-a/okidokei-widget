# Bug Assessment: 詳細設定の色選択ボタンに、カラーコードが透明度込みの 8 桁で表示される

- **Slug**: color-button-hex
- **Created**: 2026-10-02
- **Source**: desktop-clock-widget (非公開の開発用リポジトリ) の Issue #76 (host: github.com、allowlisted、`gh issue` で作成した本人の Issue)
- **Verdict**: valid
- **Severity**: low

## Report (要約)

issue #76 より (人間の手動操作で発見):

- 詳細設定の「文字色を選択」「背景色を選択」ボタンに、`#FFFFFFFF` のように 8 桁で表示される
- ボタンを押して開く「色を選択」ダイアログでは、同じ色が `#FFFFFF` の 6 桁で表示される

## Symptom

詳細設定の色選択ボタンのカラーコードが `#AARRGGBB` の 8 桁で表示される。
「色を選択」ダイアログと同じ `#RRGGBB` の 6 桁で表示されるのが期待される動作。

## Reproduction

1. ウィジェットの右クリックメニューから「詳細設定」を開く
2. 「文字色を選択」(時刻・日付) と「背景色を選択」のボタンを見ると、8 桁で表示されている
3. ボタンを押して「色を選択」ダイアログを開くと、入力欄には 6 桁で表示されている

## Suspected Code Paths

- `src/OkidokeiWidget.App/SettingsWindow.xaml.cs:433` (`UpdateColorButtonLabel`) — 保存値 (`#AARRGGBB`) を `ColorHexResolver.Resolve` に通しただけで、そのままボタンに表示している
  - 3 つのボタン (`SettingsWindow.xaml.cs:172-174`) と、色を選んだ後の更新 (`:417`) がすべてここを通る
- `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs:80` (`ToRgbHex`) — ダイアログ側は先頭 2 桁 (透明度) を除いた `#RRGGBB` で表示している

## Root Cause Hypothesis

色の保存形式は `#AARRGGBB` (data-model.md) で、「色を選択」ダイアログは表示・入力に `#RRGGBB` を使っている。
その変換 (`ToRgbHex`) がダイアログ内の private メソッドにしかなく、詳細設定のボタンでは変換せずに表示しているため、表記がずれている。確度: high

## Proposed Remediation

**Preferred**: `#AARRGGBB` → `#RRGGBB` の変換を `ColorHexResolver` (Core) に `ToRgbHex` として移す。
パース不可の値はデフォルト色にフォールバックしてから変換する。
`SettingsWindow.UpdateColorButtonLabel` と `ColorPickerWindow` の両方から使い、ダイアログの private な `ToRgbHex` は削除する。

**Alternatives**:
- `SettingsWindow` 側だけで `Substring(3)` する — 変更は最小だが、同じ変換が 2 か所に分かれ、またずれる余地が残る

**Files likely to change**:
- `src/OkidokeiWidget.Core/Settings/ColorHexResolver.cs`
- `src/OkidokeiWidget.App/SettingsWindow.xaml.cs`
- `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs`
- `tests/OkidokeiWidget.Core.Tests/Settings/ColorHexResolverTests.cs`

**Tests to add or update**:
- 有効な `#AARRGGBB` から `#RRGGBB` が返る
- 透明度が `FF` 以外でも、先頭 2 桁を除いた 6 桁が返る
- パース不可の値ではデフォルト色 (`#FFFFFF`) が返る

## Risks & Considerations

- 表示だけの変更で、設定ファイルの保存形式 (`#AARRGGBB`) は変えない
- 背景色の保存値の透明度は使われておらず (FR-030)、ダイアログで選べる色は透明度が常に `FF` なので、6 桁表示で失われる情報はない
- 文字色の保存値を手で編集して透明度を `FF` 以外にした場合も、ボタンには 6 桁しか出なくなる
  - 通常の操作では起きないケースなので、気にしない (constitution の品質の線引き)

## Open Questions

なし
