# Bug Verification: 詳細設定で自動起動を切り替えたときに失敗すると、アプリごと落ちる

- **Slug**: autostart-toggle-crash
- **Tested**: 2026-09-28
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

- 失敗の原因になる状況で、修正前の `SetEnabled` は例外を投げ、修正後の `TrySetEnabled` は false を返すことを単体テストで確認した
- 実機でも、詳細設定から切り替えて失敗したときに落ちず、メッセージが出てチェックが外れることを人間が確かめた
  - 最初は partial で記録し、2026-09-28、PR #62 のマージ前の実機確認の結果を追記して verified にした

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| 失敗時の例外 (修正前) | 一時的なテストで `SetEnabled` を呼ぶ | pass | 2 つの状況とも例外を投げることを確認 (確認後にテストは削除) |
| New / updated tests | `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` | pass | 追加した 3 件を含め 95 件すべて合格 |
| Reproduction (post-fix) | 実機で、Startup フォルダに `OkidokeiWidget.lnk` フォルダを作ってから詳細設定で自動起動をオンにする | pass | 2026-09-28、PR #62 のレビュー時に人間が確認。落ちずにメッセージが出て、チェックが外れた。フォルダを消した後は普通にオンになった |
| Regression suite | 同上の `dotnet test` | pass | 既存の自動起動のテスト 2 件も合格 |
| Lint / type-check | `dotnet build ... -c Release -o <作業用フォルダ>` | pass | 警告 0、エラー 0 |

## Output Excerpts

```text
A: System.UnauthorizedAccessException: アクセスが拒否されました。 (0x80070005 (E_ACCESSDENIED))
B: System.IO.IOException: Cannot create '...\Startup' because a file or directory with the same name already exists.
成功!   -失敗:     0、合格:    95、スキップ:     0、合計:    95
```

## Residual Risks

- 受け止める例外は種類を絞っている。想定外の種類の例外なら、これまでどおり落ちる

## Recommendation

- 実機での確認 (手順は tasks.md の Phase 21 の Checkpoint のとおり) で問題がなかったので、issue #18 は閉じてよい (PR #62 のマージで閉じた)
