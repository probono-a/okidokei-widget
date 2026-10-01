# Bug Verification: 閉じたウィジェットの時計更新タイマーが止まらず、ウィンドウがメモリに残り続ける

- **Slug**: clock-timer-leak
- **Tested**: 2026-09-28
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

- 最初はメモリ計測をせず、閉じたウィンドウが解放されることは未確認として partial で記録した
- 2026-09-28、PR #61 のマージ後に `dotnet-gcdump` で `ClockWindow` の数を数え、修正前と修正後のビルドを比べた
  - 修正前: モニタの表示/非表示を 5 回切り替えると、閉じた 5 個が解放されずに残り、2 個から 7 個に増えた
  - 修正後: 同じ操作をしても 2 個のまま (表示中のモニタの数と同じ) だった
- 実機でモニタの表示/非表示を切り替えても落ちず、回帰は見つかっていない。なので verified とした

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (修正前) | 修正前のビルド (`6a20437`) で、人間が詳細設定から同じモニタの表示を「外す → 付ける」を 5 回くり返し、`dotnet-gcdump` で `ClockWindow` の数を数える | pass | 2 個 → 7 個。バグが起きていることを確認 |
| Reproduction (post-fix) | 修正後のビルド (`main`、PR #61〜#63 のマージ後) で同じ操作をし、同じく数える | pass | 2 個 → 2 個。閉じたウィンドウは解放されている |
| コードの確認 | `ClockWindow.xaml.cs` の読み直し | pass | `OnClosed` で `_timer.Stop()` を呼んでいる。ほかに外部への購読はない |
| 実機での回帰確認 | 詳細設定でモニタの表示/非表示を何度か切り替える | pass | 2026-09-28、PR #61 のレビュー時に人間が確認。落ちなかった |
| New / updated tests | なし | not-run | 自動テストは追加していない (WPF のウィンドウは Core のテストの対象外) |
| Regression suite | `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` | pass | 92 件すべて合格 |
| Lint / type-check | `dotnet build ... -c Release -o <作業用フォルダ>` | pass | 警告 0、エラー 0 |

## Output Excerpts

`dotnet-gcdump collect -p <PID>` の後、`dotnet-gcdump report` の出力から `ClockWindow` の行を抜き出したもの (列はサイズ・個数)。
`dotnet-gcdump` はメモリの中身を取り出すときにガベージコレクションを走らせるため、数はまだ解放されていないものだけになる。

```text
修正前・起動直後      808         2  OkidokeiWidget.App.ClockWindow
修正前・5 回切り替え後 808         7  OkidokeiWidget.App.ClockWindow
修正後・起動直後      808         2  OkidokeiWidget.App.ClockWindow
修正後・5 回切り替え後 808         2  OkidokeiWidget.App.ClockWindow
```

```text
成功!   -失敗:     0、合格:    92、スキップ:     0、合計:    92
```

## Residual Risks

- モニタの抜き差しでウィンドウが閉じる経路は測っていない
  - 詳細設定での非表示と同じ `Close()` を通るので、同じく解放されるはず

## Recommendation

- 修正前後の比較で解放を確認できたので、issue #49 は閉じてよい (PR #61 のマージで閉じた)
