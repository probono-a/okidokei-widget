# Bug Verification: 壊れた設定ファイルが警告の前に上書きされる・一部が null だと起動直後に落ちる

- **Slug**: broken-settings-file
- **Tested**: 2026-09-28
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

- 読み込みと `.bak` の処理は、修正前に失敗し修正後に合格する単体テストで確認した
- 実機でも、2 つの再現手順で落ちずに警告が出て、`.bak` に元の内容が残ることを人間が確かめた
  - 最初は partial で記録し、2026-09-28、PR #63 のマージ前の実機確認の結果を追記して verified にした

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| 新しいテスト (修正前) | 修正前のコードに対して追加したテストを実行 | pass | 期待どおり 5 件が失敗した (バグを捉えている) |
| New / updated tests | `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` | pass | 追加した 6 件を含め 101 件すべて合格 |
| Reproduction (post-fix, #50) | 実機で `settings.json` の末尾の `}` を消して起動する | pass | 2026-09-28、PR #63 のレビュー時に人間が確認。落ちずに `.bak` の場所つきの警告が出て、`.bak` に元の内容が残っていた |
| Reproduction (post-fix, #51) | 実機で `"Appearance": null` にして起動する | pass | 同上。落ちずに警告が出た |
| Regression suite | 同上の `dotnet test` | pass | 既存の読み込み・保存のテストもすべて合格 |
| Lint / type-check | `dotnet build ... -c Release -o <作業用フォルダ>` | pass | 警告 0、エラー 0 |

## Output Excerpts

修正前:

```text
失敗!   -失敗:     5、合格:    96、スキップ:     0、合計:   101
```

修正後:

```text
成功!   -失敗:     0、合格:   101、スキップ:     0、合計:   101
```

## Residual Risks

- 設定ファイルを読み取れない場合 (ほかのプロセスがロックしている等) は `.bak` も作れず、これまでどおり上書きされる
  - 通常の操作では起きないため、原則 VI に従い対応しない

## Recommendation

- 実機での確認 (手順は tasks.md の Phase 22 の Checkpoint のとおり) で問題がなかったので、issue #50・#51 は閉じてよい (PR #63 のマージで閉じた)
