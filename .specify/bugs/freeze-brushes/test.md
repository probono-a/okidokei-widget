# Bug Verification: コードで作る SolidColorBrush に Freeze() していない

- **Slug**: freeze-brushes
- **Tested**: 2026-09-28
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

4 箇所のブラシを Freeze した後も、文字色・背景色・背景透過度の変更と、色の選択画面の表示は
これまでどおり動いた。Freeze したブラシを変更して例外になる箇所はなかった。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | コード上の確認 | pass | 4 箇所とも `CreateFrozenBrush` を通すようになった。見た目の症状はないため、再現手順はない (assessment.md) |
| 実機での動作確認 | 下記の手順 (人間が実施) | pass | 異常なし |
| Build | `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release` | pass | 警告 0、エラー 0 (ウィジェットを終了してから再ビルド) |
| Regression suite | `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` | pass | 119 件すべて合格 (fix 時に実行、以降 Core 層は未変更) |
| Lint / type-check | なし | not-run | 本プロジェクトでは使っていない |

実機での確認手順 (Release ビルド、2026-09-28 に人間が実施):

1. 詳細設定で時刻の文字色を色の選択画面で変える
   - パレットの表示、プレビューの色の追随、ウィジェットへの反映を確認
2. 日付の文字色と背景色を変え、ウィジェットに反映されることを確認
3. 背景透過度のスライダーを動かし、背景の透け具合が変わることを確認
4. 色の選択画面をもう一度開き、使い回しているパレットのブラシが正しく表示されることを確認
5. 色を元に戻す

## Output Excerpts

```text
    0 個の警告
    0 エラー
成功!   -失敗:     0、合格:   119、スキップ:     0、合計:   119
```

## Residual Risks

- Freeze による性能面の改善は計測していない。もともと実害はほぼない指摘 (issue #15) のため

## Recommendation

Close the bug — 実機で今までどおり動くことを確認した。
