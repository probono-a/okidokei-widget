# Bug Verification: 詳細設定の色選択ボタンに、カラーコードが透明度込みの 8 桁で表示される

- **Slug**: color-button-hex
- **Tested**: 2026-10-02
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

詳細設定の文字色 (時刻・日付) と背景色のボタンが、色の選択画面と同じ `#RRGGBB` の 6 桁で表示される
ようになった。色を選び直した後も 6 桁のままで、色の選択画面の表示も今までどおりだった。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | 下記の手順 (人間が実施) | pass | 3 つのボタンとも 6 桁で表示された |
| New / updated tests | `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` | pass | `ToRgbHex` のテスト 2 件 (4 ケース) を含め、169 件すべて合格 (fix 時に実行、以降 Core 層は未変更) |
| Build | `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release` | pass | 警告 0、エラー 0 (ウィジェットを終了してから再ビルド) |
| Lint / type-check | なし | not-run | 本プロジェクトでは使っていない |

実機での確認手順 (Release ビルド、2026-10-02 に人間が実施):

1. 詳細設定を開き、「文字色を選択」(時刻・日付) と「背景色を選択」のボタンが 6 桁で表示されることを確認
2. 色を選び直し、ボタンの表示が 6 桁のままであることを確認
3. 「色を選択」ダイアログの入力欄が、今までどおり 6 桁で表示されることを確認

## Output Excerpts

```text
    0 個の警告
    0 エラー
成功!   -失敗:     0、合格:   169、スキップ:     0、合計:   169
```

## Residual Risks

- 設定ファイルを手で編集して文字色の透明度を `FF` 以外にした場合、ボタンには透明度が出ない
  - 通常の操作では起きないケースのため、気にしない (assessment.md)

## Recommendation

Close the bug — 実機で 3 つのボタンが 6 桁で表示されることを確認した。
