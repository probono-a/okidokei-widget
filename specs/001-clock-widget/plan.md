# Implementation Plan: 常駐デスクトップ時計ウィジェット

**Branch**: `001-clock-widget` | **Date**: 2026-09-17 (2026-09-24 更新: アンカー指定・右クリックメニュー統一、issue #26。2026-09-26 更新: ドラッグ範囲の制限、issue #39。同日更新: 自由配置のときの余白、issue #43。2026-09-28 更新: モニタの見分け方、issue #52。同日更新: 日付の区切り文字、issue #60。2026-10-01 更新: モニタごとの設定、issue #53・#17) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-clock-widget/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Windows 11 上で常駐する WPF デスクトップ時計ウィジェット。時刻/日付/曜日を表示し、フォント・
文字色・背景透過度をカスタマイズ可能、ドラッグ配置と位置ロック・最前面表示 ON/OFF、モニタごと
の表示/非表示・位置の独立管理を行う。設定は `%APPDATA%\OkidokeiWidget\settings.json` に保存し
(レジストリ不使用)、Windows 起動時にスタートアップフォルダのショートカット経由で自動起動する。
見た目・配置に関するロジックはテスト可能な `OkidokeiWidget.Core` ライブラリに切り出し、WPF 本体
(`OkidokeiWidget.App`) から利用する構成とする(詳細は research.md 参照)。

2026-09-24 の更新では、実装済みのアプリに対して以下を追加する (FR-034〜FR-038、FR-010 等の改訂)。

- モニタごとに、9 つの配置 (右上など) と余白 (狭め/広め) でウィジェットの位置を指定できる
  「アンカー指定」を追加する
- アンカー指定中は、フォントサイズや DPI が変わってウィジェットのサイズが変わっても、隅からの
  位置関係を保つ
- 本体とタスクトレイの右クリックメニューを同じ構成に揃える。トレイ側は、配置を選ぶ前に
  モニタを選ぶ階層が 1 段入る
- 位置ロック中は、メニューからの配置の変更もグレーアウトで禁止する

設計の詳細は research.md #15〜#17、data-model.md の `MonitorPlacement`、
contracts/context-menus.md を参照。

2026-09-26 の更新では、FR-009 の改訂 (issue #39) に合わせて、ドラッグで動かせる範囲を
表示中のモニタの作業領域内に限る。

- ドラッグ中の位置を物理ピクセルで計算し、作業領域内に収めてから動かす
- ウィンドウが別のモニタへ出なくなるので、`DpiChanged` による引き戻しと揺れが起きなくなる
- 設定ファイルの形式、画面、メニューは変えない (constitution v1.5.0 の小さな変更として、
  specify から implement までを 1 ブランチ・1PR で進める)

設計の詳細は research.md #18 を参照。

同じく 2026-09-26 の更新で、FR-035 の改訂と FR-039 の新設 (issue #43) に合わせて、自由配置の
ときの余白の扱いを変える。

- 自由配置中は、右クリックメニューの余白にチェックを付けない
- 自由配置中に余白を選ぶと、範囲内の縁から余白ぶん内側へ動かす。範囲内の縁がない余白は
  グレーアウトする。動かした後も自由配置のまま
- グレーアウトの判定と移動先は、Core の 1 つの関数で求めて食い違わないようにする
- 設定ファイルの形式と画面は変えない (issue #39 と同じく、小さな変更として 1 ブランチ・1PR で進める)

設計の詳細は research.md #19 を参照。

2026-09-28 の更新では、FR-026 の改訂 (issue #60) に合わせて、日付の区切り文字を広げる。

- 詳細設定画面の選択肢に「.」を足す。設定ファイルでは任意の文字列を使えるようにする
- 日付の文字列は、書式文字列に区切り文字を埋め込まず、数字と区切り文字をつなぎ合わせて作る
  (`y`・`M`・`d` などを区切り文字にしても、書式の記号として解釈されないようにするため)
- 設定の区切り文字が選択肢にないときは、詳細設定画面の欄を空欄にし、選び直さない限り書き換えない
- 設定ファイルの形式は変えない (小さな変更として 1 ブランチ・1PR で進める)

設計の詳細は research.md #21 を参照。

2026-10-01 の更新では、FR-041・FR-042 の新設と FR-038 などの改訂 (issue #53・#17) に合わせて、
見た目・位置ロック・最前面表示をモニタごとに持つ。

- 設定ファイルでは、見た目とウィンドウ挙動を `Monitors` の各エントリの中に入れる。ルートには書き出さない
- 以前のバージョンの設定ファイル (ルートに見た目がある) は、起動時の `Reconcile` で各モニタへ複製して引き継ぐ
  - そのとき、つながっていないモニタは非表示にする
- 新しいモニタは非表示で始める。モニタの設定が 1 つもないときだけ、プライマリモニタに表示する
- 詳細設定画面
  - 上に「編集するモニター」のドロップダウンを置き、選んだモニタの値を各タブに入れ直す
  - 開いたままモニタを抜き差ししたら、選択肢を作り直す (#17)
- タスクトレイの右クリックメニューは「詳細設定...」「自動起動」「終了」だけにする
- 設定ファイルの形が変わるので、小さな変更ではない。plan・tasks・implement を別々の PR で進める

設計の詳細は research.md #22〜#24、data-model.md の `MonitorPlacement`、contracts/settings-file.md・
contracts/context-menus.md を参照。画面とメニューの形は ui-per-monitor-settings.md にある。

## Technical Context

**Language/Version**: C# (最新言語バージョン) / .NET 10 (LTS)、`net10.0-windows` ターゲット

**Primary Dependencies**: WPF (Microsoft.WindowsDesktop.App)、`System.Text.Json` (BCL)、
`System.Windows.Forms.Screen`(モニタ列挙)、Win32 `EnumDisplayDevices`(P/Invoke、モニタの
安定した識別子取得用。2026-09-28 訂正: 取れる ID は端子ごとの値を含み、端子を変えると変わる。
research.md #20)。`OkidokeiWidget.Core` / `OkidokeiWidget.App` の本体コードには外部 NuGet
パッケージを導入しない(テストプロジェクトの xUnit 関連パッケージは対象外。research.md #10 参照)

**Storage**: ローカル JSON ファイル 1 つ (`%APPDATA%\OkidokeiWidget\settings.json`)。DB は使用しない

**Testing**: xUnit による `OkidokeiWidget.Core` の単体テスト(設定の読み書き・デフォルトへの
フォールバック・モニタ識別子マッチング・アンカー指定の位置計算・既存設定ファイルとの互換性)。
UI/視覚的な確認は `quickstart.md` の手動シナリオで行い、UI 自動化フレームワークは導入しない
(research.md #7)

**Target Platform**: Windows 11 デスクトップ

**Project Type**: デスクトップアプリ(単一 WPF 実行ファイル + テスト可能なコアライブラリ)

**Performance Goals**: ログインからウィジェット表示まで 5 秒以内 (SC-001)。表示更新は 1 秒
周期で十分であり、過剰なポーリングは行わない (Core Principle II)

**Constraints**: アイドル時 CPU 使用率 1%未満・メモリ使用量 100MB 未満 (SC-006)。設定は
JSON のみで保存しレジストリを使用しない (Core Principle III)。設定ファイルが存在しない/
不正でもクラッシュせずデフォルト設定で起動する (FR-018)

**Scale/Scope**: 個人利用(1 PC・1 ユーザー)、4 ユーザーストーリー・40 の機能要件
(2026-09-24 の追加分 FR-034〜FR-038 はすべて User Story 3「配置とロック」に属する。
2026-09-26 の FR-009 の改訂と SC-008 の新設、FR-035 の改訂と FR-039 の新設も User Story 3 に属する)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | `docs/requirements.md` にない機能(トレイアイコン、クリックスルー、設定の多重フォーマット対応等)は導入しない。依存パッケージも BCL/WPF 標準の範囲に留める (research.md #6, #8) |
| II. 軽量な常駐動作 | PASS | 表示更新は 1 秒周期のタイマーのみ。自動起動はショートカット配置のみで起動時の重い処理を行わない |
| III. 設定は JSON・非破壊 | PASS | レジストリ不使用。設定ファイル欠損/不正時はデフォルトにフォールバックしクラッシュしない (data-model.md, contracts/settings-file.md) |
| IV. 誤操作防止 | PASS | 位置ロック中はドラッグ操作を無効化 (FR-010)。破壊的操作(終了等)は右クリックメニューの通常導線に置くのみで、誤操作しやすい配置にはしない |
| V. マルチモニタ・DPI 対応 | PASS | EDID 由来の安定したモニタ識別子で配置を保持し、再接続構成の変化にも対応 (research.md #2)。Per-Monitor V2 DPI 宣言でモニタ間 DPI 差異に対応 (research.md #5) |

**結果**: 違反なし。Complexity Tracking への記載は不要

補足 (2026-09-28、issue #52): 原則 V の根拠の「EDID 由来の安定したモニタ識別子」は誤りだった。
ID は端子ごとの値を含み、端子を変えると変わる。見分け方は research.md #20 で決め直した

### 再チェック (2026-09-24、アンカー指定・右クリックメニュー統一の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 追加する設定は spec で要求された `Anchor`・`AnchorMargin` の 2 つのみ。余白は 2 段階に留め、px 指定などの細かい設定は設けない。新しい依存パッケージは追加しない。メニューの共通化は、本体とトレイの 2 か所で実際に使うために行う (research.md #17) |
| II. 軽量な常駐動作 | PASS | 再配置はサイズ変更・DPI 変更・メニュー操作の時だけ行い、定期的な監視はしない |
| III. 設定は JSON・非破壊 | PASS | 追加フィールドがない既存の設定ファイルもそのまま読める。未知の値は既存と同じくデフォルトへフォールバックする (contracts/settings-file.md) |
| IV. 誤操作防止 | PASS | 位置ロック中はドラッグに加えて、メニューからの配置変更もグレーアウトで禁止する (FR-010) |
| V. マルチモニタ・DPI 対応 | PASS | アンカー指定はモニタごとに保持する。余白は DIP で定義してモニタの DPI で物理ピクセルに換算するため、拡大率が変わっても見た目が保たれる (research.md #15) |

**結果**: 違反なし。Complexity Tracking への記載は不要

補足: 初回チェックの原則 I の根拠にある「トレイアイコンは導入しない」は、FR-031 (2026-09-17) の
時点で見直し済み (research.md #13)。今回はさらにトレイのメニューを本体と揃える (research.md #17)

### 再チェック (2026-09-26、ドラッグ範囲の制限の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | ドラッグでモニタを移す機能は作らない (spec.md の Clarifications)。作業領域内へ収める計算は既存の処理を共通化して使い、新しい設定・依存パッケージは追加しない |
| II. 軽量な常駐動作 | PASS | 計算はドラッグ中の `MouseMove` の時だけ行う。常駐中の処理は増えない |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形式は変えない。保存するのは今と同じ、作業領域の左上からの相対座標 |
| IV. 誤操作防止 | PASS | ドラッグでウィジェットが作業領域の外やタスクバーの下へ出なくなり、見失う・操作できなくなることがない。位置ロック中の扱いは変えない |
| V. マルチモニタ・DPI 対応 | PASS | ドラッグ中の位置を物理ピクセルで計算し、拡大率の違うモニタの境界でも単位がずれない (research.md #18) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にした 4 つの挙動を最小のアプリで確かめ、方法と結果を research.md #18 に記録した。未確認の前提は残っていない |

**結果**: 違反なし。Complexity Tracking への記載は不要

### 再チェック (2026-09-26、自由配置のときの余白の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 新しい設定・メニュー項目・依存パッケージは追加しない。向かい合う縁が両方とも範囲内になるケースは作り込まず、spec で「定めない」とした (issue #45) |
| II. 軽量な常駐動作 | PASS | 判定はメニューを開いたときと余白を選んだときだけ行う。常駐中の処理は増えない |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形式は変えない。保存するのは今と同じ `X`/`Y` と `AnchorMargin` |
| IV. 誤操作防止 | PASS | 位置ロック中は今までどおりすべてグレーアウトする (FR-010)。範囲内の縁がない余白もグレーアウトし、選んでも何も起きない項目を残さない |
| V. マルチモニタ・DPI 対応 | PASS | 縁までの距離と余白は、アンカー指定と同じ換算で物理ピクセルにそろえて比べる (research.md #19) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にしている挙動 3 つを research.md #19 に列挙した。これまで確かめていなかった「有効・無効が混ざったサブメニューの表示とクリック」は、`/speckit-analyze` の指摘 C1 を受けて最小のアプリで確かめ、方法と結果を記録した。未確認の前提は残っていない |

**結果**: 違反なし。Complexity Tracking への記載は不要

### 再チェック (2026-09-28、モニタの見分け方の設計後、issue #52)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 設定ファイルの形を変えず、移行の処理も書かない。探し方に「キーが一致しないモニタに、使われていない同じ型番の設定を端子の番号順に割り当てる」を 1 段足すだけで、spec の場合分けをすべて満たす (research.md #20)。画面の並びを保存して見分ける案は採らない。EDID のシリアル番号の読み取りは作らない |
| II. 軽量な常駐動作 | PASS | 探し方の判定は、起動時・モニタ構成が変わったとき・DPI が変わったときの `Reconcile` の中だけで行う。常駐中の処理は増えない |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形は変えない。以前のバージョンの設定もそのまま読める。付け替えは設定を移すだけで、消したり初期値で上書きしたりしない |
| IV. 誤操作防止 | PASS | 操作や画面は変えない。ケーブルをつなぎ直しただけで位置が初期値に戻る、という意図しない変化がなくなる |
| V. マルチモニタ・DPI 対応 | PASS | research.md #2 の誤った前提 (ID が物理モニタに紐づく) を正し、端子のつなぎ直しでも設定を引き継ぐ (research.md #20)。同じ型番のモニタが複数ある場合も、端子が変われば使われていない設定を割り当て、初期値には戻さない。どれが前の設定かは保証しない (spec.md の SC-005・Edge Cases) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | 逸脱あり (Complexity Tracking) | 前提にしている Win32 の挙動 4 つを research.md #20 に列挙した。ID の形は開発機の設定ファイルで確かめた。「端子を変えると ID の端子ごとの部分が変わる」ことは、当初レジストリの記録からの推測だけで PASS としていたが、`/speckit-analyze` の指摘 K1 を受け、実装の前に実機でケーブルをつなぎ替えて確かめた (DELL の ID の端子ごとの部分が `UID4353` から `UID4355` に変わった。research.md #20)。同じ型番のモニタ・EDID を読めないモニタについての 2 つは、手元に該当するモニタがなく確かめられないため、Complexity Tracking に理由を書いた |

**結果**: Development Workflow 7 の未確認の前提 2 つを、理由を付けて Complexity Tracking に記載した。それ以外の違反はない

### 再チェック (2026-09-28、日付の区切り文字の設計後、issue #60)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 画面には選択肢を 1 つ足すだけで、任意の文字を入力する欄は作らない。長さの上限や書記素単位の数え方も作らない (spec.md の Clarifications)。新しい設定・依存パッケージは追加しない |
| II. 軽量な常駐動作 | PASS | 日付の文字列の作り方を変えるだけで、更新の頻度は変わらない (1 秒周期のまま) |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形式は変えない。選択肢にない区切り文字は、画面で選び直さない限り書き換えない。以前のバージョンの設定ファイルもそのまま読める |
| IV. 誤操作防止 | PASS | 詳細設定画面を開いただけ・ほかの項目を変えただけで、設定ファイルに書いた区切り文字が消えることはない |
| V. マルチモニタ・DPI 対応 | PASS | 影響なし (区切り文字はアプリ全体で 1 つのまま。モニタごとに持つのは issue #53 で扱う) |
| VI. 品質の線引き | PASS | 守る側: 手で書き換えた区切り文字 (`null`・空欄・絵文字を含む) を読み込んでも落ちず、保存し直しても値を失わない (T127 のテストで押さえる)。気にしない側: 制御文字や極端に長い文字列の見た目、アプリの起動や保存で書き出したときに `\uXXXX` の形になる見た目 (spec.md の Clarifications・Assumptions)。どちらも設定ファイルを直接書き換えたときにしか起きない (`/speckit-analyze` の指摘 D1) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にしている `ComboBox` と `TextBlock` の挙動 4 つを research.md #21 に列挙し、最小のアプリで確かめて方法と結果を記録した。選択肢にない値を `SelectedItem` に入れても空欄にならないことが分かり、`null` を入れる設計にした。前提 3 のうち「マウスで選んだ場合」だけが未確認として残り、T130 の実機での確認で確かめる (research.md #21。今までの 2 択でも同じハンドラがマウスで動いており、外れても設定は失わない。2 回目の `/speckit-analyze` の指摘 D1) |

**結果**: 違反なし。未確認の前提 1 つは、実装後の T130 で確かめる (2026-09-28 に確認済み。tasks.md の Phase 25 の確認結果)

### 再チェック (2026-10-01、モニタごとの設定の設計後、issue #53・#17)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 見た目とウィンドウ挙動のクラスは中身を変えず、`MonitorPlacement` の中へ置き場所を移すだけにした。設定ファイルのバージョン番号、新しいモニタのひな形、全モニタへの一括適用は作らない。選択肢の「(非表示)」は、変更通知を付けずに選択肢を作り直して変える (research.md #22・#23) |
| II. 軽量な常駐動作 | PASS | 引き継ぎと新しいモニタの判定は、今までどおり起動時とモニタ構成の変化時の `Reconcile` の中だけで行う。常駐中の処理は増えない。詳細設定画面の選択肢の作り直しも、画面を開いている間だけ |
| III. 設定は JSON・非破壊 | PASS | 引き続き JSON に保存する。以前のバージョンの見た目・位置ロック・最前面表示・位置は、各モニタへ複製して引き継ぎ、失わない。変わるのは、つながっていないモニタの表示/非表示だけで、これは spec.md で人間が決めた (Clarifications)。非表示にしたモニタの位置・見た目も残るので、表示に戻せば元どおりになる |
| IV. 誤操作防止 | PASS | 右クリックメニューで表示/非表示を切り替えられないことは変えない (FR-012)。非表示のモニタは、タスクトレイから詳細設定画面を開いて戻せる。位置ロックがモニタごとになっても、ロックしたモニタのウィジェットは動かない (SC-004) |
| V. マルチモニタ・DPI 対応 | PASS | 見た目・位置ロック・最前面表示をモニタごとに持つことで、モニタごとに違う大きさ・拡大率に合わせられる。FR-040 の付け替えの後に引き継ぐので、別の端子につなぎ直したモニタも「つながっている」と扱う (research.md #22) |
| VI. 品質の線引き | PASS | 守る側: 以前のバージョンの設定ファイルを読み、見た目・位置などを失わないこと、開いたままモニタを抜き差ししても詳細設定画面が落ちないこと (単体テストと quickstart.md で確かめる)。気にしない側: 起動中に別の端子につなぎ直して `Identifier` が変わったときに、詳細設定画面の選択がプライマリモニタに戻ること、開いている間にプライマリモニタが変わったときの扱い (research.md #23、spec.md の Assumptions) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にしている `ComboBox` と `CheckBox` の挙動 2 つを research.md #23 に列挙し、最小のアプリで確かめた。`ItemsSource` を替えても、値の等しい要素があると選択が残る、と分かったため、選択肢は参照で比べるクラスにし、作り直した後に必ず選び直す設計にした。開いたままモニタを抜き差しする操作は、実装後に quickstart.md で実機で確かめる |

**結果**: 違反なし

## Project Structure

### Documentation (this feature)

```text
specs/001-clock-widget/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/
│   ├── settings-file.md  # Phase 1 output: 設定ファイルのスキーマ契約
│   └── context-menus.md  # Phase 1 output: 右クリックメニューの項目構成 (2026-09-24 追加)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── OkidokeiWidget.Core/          # WPF に依存しない、テスト可能なロジック
│   ├── Settings/                # WidgetSettings, AppearanceSettings,
│   │                             # WindowBehaviorSettings, MonitorPlacement
│   ├── Persistence/              # 設定ファイルの読み書き・デフォルトへのフォールバック
│   └── Monitors/                  # モニタ列挙・モニタ識別子の取得
│
└── OkidokeiWidget.App/           # WPF 実行ファイル
    ├── App.xaml(.cs)              # エントリポイント、多重起動防止、自動起動連携
    ├── ClockWindow.xaml(.cs)       # 時計本体ウィンドウ(ドラッグ・右クリックメニュー・最前面表示)
    └── SettingsWindow.xaml(.cs)    # 詳細設定ウィンドウ

tests/
└── OkidokeiWidget.Core.Tests/    # xUnit: 設定の読み書き・モニタ識別ロジックの単体テスト
```

**Structure Decision**: Single-project 系のシンプルな構成を採用しつつ、WPF に依存しない設定・
モニタロジックのみ `OkidokeiWidget.Core` として分離した 2 プロジェクト構成とする。UI 全体を含む
単一プロジェクトにしなかった理由は、FR-018(壊れた設定ファイルでもクラッシュしない)等の
ロジックを WPF の UI テストハーネストなしに単体テストできるようにするためであり、将来の
仮説的な拡張のためではない(Core Principle I への準拠)。Web/モバイル向けのテンプレート構成
(Option 2/3)は対象外(バックエンド/フロントエンド分離やモバイルプラットフォームが存在しない
ネイティブデスクトップアプリのため)

## 既存実装に対する変更計画 (2026-09-24)

実装済みのコードに対して、どのファイルをどう変えるかの一覧。作業の順序と粒度は `/speckit-tasks`
で決める。

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Settings/AnchorPosition.cs` | 新規 | 9 つの配置を表す enum (data-model.md) |
| `Settings/AnchorMargin.cs` | 新規 | 余白 `Narrow`/`Wide` の enum |
| `Settings/MonitorPlacement.cs` | 変更 | `Anchor` (null 許容)・`AnchorMargin` を追加 |
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | アンカー指定の位置計算を追加。DPI スケールを引数で受け取り、余白の DIP 値を物理ピクセルへ換算する (research.md #15)。自由配置の計算は変更しない |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `PlacementMenuBuilder.cs` | 新規 | 1 つのモニタ分の「配置」サブメニュー (9 項目 + 余白 2 項目) を作る。本体とトレイの両方から使う (research.md #17) |
| `ClockWindow.xaml` | 変更 | 右クリックメニューに「配置」を追加する (中身は開くたびに `PlacementMenuBuilder` で作る) |
| `ClockWindow.xaml.cs` | 変更 | `ApplyPlacement` でアンカーと DPI スケールを計算に渡す。`SizeChanged` で再配置する。ドラッグ終了時に `Anchor` を null に戻す (FR-036)。メニューを開く時にロック状態でグレーアウトを切り替える |
| `TrayIconManager.cs` | 変更 | 右クリック時に、`App` から受け取ったメニューを表示するだけにする。「終了」だけを持つ現在のメニュー組み立ては削除する |
| `App.xaml.cs` | 変更 | トレイのメニュー (詳細設定・位置ロック・最前面表示・モニタ一覧付きの配置・終了) を組み立てる。配置の変更を該当モニタのウィンドウへ反映して保存する |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 9 つの配置 × 余白 × DPI スケールの位置計算、作業領域より大きいときの収め方 |
| `Persistence/SettingsRepositoryTests.cs` | 変更 | `Anchor` のない既存の設定ファイルを読むと自由配置になること、`Anchor` を含む設定の保存・読み込み |

### 変更しないもの

- 詳細設定画面 (`SettingsWindow`): アンカー指定は右クリックメニューからのみ操作する
  (spec.md の Clarifications)
- `MonitorSettingsReconciler`: 新規モニタの既定値は従来どおり自由配置 (中央寄せ) のまま
- `DisplayChangeNotifier` と issue #25 の `DpiChanged` 対応: そのまま使い、アンカー指定の
  再配置もこの経路で行う

## 既存実装に対する変更計画 (2026-09-26、issue #39)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | 作業領域内へ収める計算を、ドラッグからも呼べる公開メソッドとして切り出す。`ToAbsolutePosition` もこれを使うようにし、結果は変えない |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `WindowPositionHelper.cs` | 変更 | マウスの画面座標を物理ピクセルで取る処理 (`GetCursorPos`) を追加する |
| `ClockWindow.xaml.cs` | 変更 | ドラッグ開始時に、つかんだ位置 (マウスとウィンドウ左上の差、物理ピクセル) を覚える。`MouseMove` では `Left`/`Top` を使わず、作業領域内に収めた位置へ `WindowPositionHelper.MoveTo` で動かす。ドラッグ終了時の処理は変えない |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 作業領域内へ収める計算の単体テスト (内側ならそのまま、4 辺の外なら端に止まる、負の座標のモニタ、作業領域より大きいとき) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`) と右クリックメニュー (`contracts/context-menus.md`)
- `DpiChanged` の後回し処理 (issue #41 の修正): ドラッグ以外の経路 (拡大率の変更、モニタの取り外し) で
  引き続き必要
- ドラッグ終了時の保存とアンカー指定の解除 (FR-036)

## 既存実装に対する変更計画 (2026-09-26、issue #43)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | 自由配置中に余白を選んだときの移動先を求める関数を追加する。範囲内の縁がなければ null を返す (research.md #19)。余白を物理ピクセルに換算する処理は、アンカー指定の計算と共通にする |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `PlacementMenuBuilder.cs` | 変更 | 余白のチェックは、アンカー指定中だけ付ける。余白の項目ごとに選べるかどうかを、呼び出し側から受け取って反映する (位置ロック中は今までどおりすべて無効) |
| `ClockWindow.xaml.cs` | 変更 | 余白の項目が選べるかを返すメソッドを追加する (アンカー指定中は常に選べる、自由配置中は Core の関数で判定)。`SetAnchorMargin` は、自由配置中なら Core の関数で求めた位置を `X`/`Y` に保存してから配置し直す。移動先がなければ何もしない |
| `App.xaml.cs` | 変更 | トレイのメニューを作るときに、各モニタのウィジェットの「余白が選べるか」を `PlacementMenuBuilder` へ渡す |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 追加する関数の単体テスト (縁に接している、距離 = 余白、余白 + 1 px で null、隅で両方の縁から離れる、1 つの縁だけで他の軸は動かない、拡大率 150% での換算、負の座標のモニタ) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`)
- アンカー指定中の余白の動き、ドラッグ、横位置・縦位置の選び方
- 位置ロック中のグレーアウト (FR-010)

## 既存実装に対する変更計画 (2026-09-28、issue #52)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/MonitorIdentifier.cs` | 変更 | ID から型番 (`#` で区切った 2 つ目の部分) を取り出す関数を追加する。区切った結果が 3 つ未満の ID と、2 つ目が空の ID には null を返す (research.md #20)。`GetStableIdsByAdapterDeviceName` のコメントの「EDID 由来の安定した ID」を、型番と端子ごとの値からなる ID である、という実態に合わせて直す |
| `Monitors/ConnectedMonitor.cs` | 変更 | `Identifier` のコメントを、上と同じく実態に合わせて直す |
| `Persistence/MonitorSettingsReconciler.cs` | 変更 | キーが一致しなかったモニタに、同じ型番の「使われていない設定」(キーが接続中のどのモニタとも一致しないもの) を、キーの順に 1 対 1 で割り当てる段を足す。割り当てた `MonitorPlacement` は新しいキーへ移す (同じインスタンスを移し、古いキーは消す)。割り当てる設定がなかったモニタには、今までどおりデフォルト値を足す (research.md #20) |

### OkidokeiWidget.App

変更しない。起動時とモニタ構成の変化時 (`OnDisplaySettingsChanged`) は、どちらも `Reconcile` の後に
`SettingsRepository.Save` しているので、付け替えた結果はそのまま保存される。

- 実行中にケーブルをつなぎ直した場合、`SyncClockWindows` は古いキーのウィンドウを閉じ、新しいキーで
  ウィンドウを作り直す
- `ClockWindow` は設定を辞書のキーではなく `MonitorPlacement` のインスタンスで持っている
  - `Reconcile` で同じインスタンスを移すので、閉じる側のウィンドウが古いキーの設定を作り直すことはない

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/MonitorIdentifierTests.cs` | 追加 | 型番の取り出し (実際の ID の形、`#` で区切れない `\\.\DISPLAY1`) |
| `Persistence/MonitorSettingsReconcilerTests.cs` | 変更 | 別の端子につなぎ直すと前の設定が付け替わり古いキーが消える、型番の違う 2 台の端子の入れ替え、同じ型番 2 台の端子ごとの設定、1 台から 2 台に増えたとき、2 台から 1 台に減り別の端子につながったとき、同じ型番の 2 台をどちらも別の端子へつなぎ直したとき、型番が取れない ID、の各場合 (research.md #20 の Rationale の場合分け) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`)。キーの形も今までと同じ
- 詳細設定画面のモニタの一覧、右クリックメニュー
  - 詳細設定画面を開いたままケーブルをつなぎ直すと、画面の一覧は古い ID のままなので、そのモニタの
    チェックを切り替えても何も起きない (落ちはしない)。開き直せば直る。今までも、開いたままモニタを
    つなぎ直すと一覧が古いままだった。通常の操作ではまず起きないため、対応しない (`/speckit-analyze` の指摘 A2)
- モニタを取り外したときに設定を残す扱い (Edge Cases)

## 既存実装に対する変更計画 (2026-09-28、issue #60)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Settings/AppearanceSettings.cs` | 変更 | `DateSeparator` の型を `string?` にする。設定ファイルから `null` が入り、そのまま保存し直されるため (`/speckit-analyze` の指摘 I1)。既定値は今までどおり「/」 |
| `Settings/DateSeparatorResolver.cs` | 変更 | `Resolve` の引数を `string?` にし、`null` のときだけ `DefaultSeparator` (「/」) を返す。それ以外はそのまま返す。許可リストと XML コメントの「`/` または `-` 以外はフォールバック」を、FR-026 の改訂に合わせて直す (research.md #21) |
| `Settings/DateTextFormatter.cs` | 追加 | 日時と区切り文字から日付の文字列 (`年{区切り}月{区切り}日`。年は 4 桁、月・日は 2 桁) を作る。区切り文字は `DateSeparatorResolver.Resolve` を通してから、書式文字列に埋め込まずにつなぎ合わせる (research.md #21) |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `ClockWindow.xaml.cs` | 変更 | `UpdateClockText` の日付の表示を `DateTextFormatter` で作る |
| `SettingsWindow.xaml.cs` | 変更 | 区切り文字の選択肢に「.」を足す。設定の区切り文字 (`Resolve` を通した値) が選択肢にあればそれを、なければ `null` を `SelectedItem` に入れる (research.md #21 の前提 2)。`SelectionChanged` の処理は変えない |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Settings/DateSeparatorResolverTests.cs` | 変更 | 「/」「-」「.」、空欄、選択肢にない文字列 (`//`・`🍣` など) はそのまま返す。`null` は「/」を返す。今の「許可されていない値はデフォルトを返す」テストは、FR-026 の改訂で成り立たなくなるので置き換える |
| `Settings/DateTextFormatterTests.cs` | 追加 | 「/」「.」で `2026/09/28`・`2026.09.28` になる。月・日が 1 桁の日付でも 2 桁で出る。空欄で `20260928`、`null` で `2026/09/28` になる。`🍣` のような絵文字、書式の記号になる `d`・`M`・`y`・`'`・`\`・`%`・`:` がそのまま出る |
| `Persistence/SettingsRepositoryTests.cs` | 変更 | `"DateSeparator": null` のファイルが、壊れたファイル扱いにならずに読める。項目がないファイルは「/」になる。`null`・空欄・`🍣` が保存して読み直しても同じ値になる (`/speckit-analyze` の指摘 C1) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`)。`DateSeparator` は今までどおり文字列 1 つ
- `SettingsRepository` の読み込み。`DateSeparator` が `null` のファイルは、今までどおり読み込み、表示の前に `Resolve` で「/」にする
- `SettingsRepository` の書き出し方。英数字以外の文字の一部 (日本語・絵文字・`'`・`&` など) は、今までどおり `\uXXXX` の形で書き出す (research.md #21、人間が決定)
- 区切り文字をアプリ全体で 1 つ持つこと (モニタごとに持つのは issue #53)
- 利用者向けの説明 (README)。隠し機能とする (spec.md の Clarifications)

## 既存実装に対する変更計画 (2026-10-01、issue #53・#17)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Settings/MonitorPlacement.cs` | 変更 | `Appearance` (`AppearanceSettings`、初期値 `new()`) と `WindowBehavior` (`WindowBehaviorSettings`、初期値 `new()`) を足す (research.md #22) |
| `Settings/WidgetSettings.cs` | 変更 | `Appearance`・`WindowBehavior` を null を許す型にし、初期値を null にする。`JsonIgnore(Condition = WhenWritingNull)` を付け、null のときは書き出さない。以前のバージョンの設定ファイルを読むためだけに使うことを XML コメントに書く |
| `Settings/AppearanceSettings.cs`・`Settings/WindowBehaviorSettings.cs` | 変更 | 複製を返す `Clone()` を足す (`MemberwiseClone`) |
| `Persistence/MonitorSettingsReconciler.cs` | 変更 | 最初に `Monitors` が空かどうかを覚えておく。FR-040 の付け替えの後に、ルートの `Appearance`・`WindowBehavior` があれば、全エントリへ複製を入れ、接続していないモニタのエントリを非表示にし、ルートの 2 つを null にする。補完するエントリは `IsVisible = false` にし、最初に空だったときだけプライマリモニタ (なければ先頭のモニタ) を `IsVisible = true` にする (research.md #22) |
| `Persistence/SettingsRepository.cs` | 変更 | `HasNullSection` から、ルートの `Appearance`・`WindowBehavior` の null の判定を外し、各モニタの `Appearance`・`WindowBehavior` の null の判定を足す (contracts/settings-file.md の読み込み契約)。`Save` の `BackgroundOpacity` のクランプを、ルートではなく各モニタの `Appearance` に対して行う (ルートは null でないときだけ)。ルートが null になった後の起動時の `Save` で落ちないようにするため (`/speckit-analyze` の指摘 C1、書き込み契約) |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `ClockWindow.xaml.cs` | 変更 | 見た目・位置ロック・最前面表示を、`_settings` ではなく `_placement.Appearance`・`_placement.WindowBehavior` から読む。本体のメニューの位置ロック・最前面表示は、自分の `WindowBehavior` を切り替えて自分にだけ反映し、保存する (全ウィジェットへ反映するコールバックはなくす)。「詳細設定...」は、自分のモニタの `Identifier` を付けて App に知らせる |
| `App.xaml.cs` | 変更 | `OpenSettingsWindow` にモニタの `Identifier` (タスクトレイからは null) を受け取らせる。開いていなければそのモニタ (null ならプライマリ) を選んで開き、開いていれば `Identifier` があるときだけそのモニタに切り替えて前に出す。見た目が変わったときは、そのモニタのウィジェットにだけ反映して保存する。`OnDisplaySettingsChanged` で、開いている詳細設定画面に接続中のモニタの一覧を渡す (#17)。タスクトレイのメニューを「詳細設定...」「自動起動」「終了」にし、「自動起動」は `AutoStartEnabled` を反転して `OnAutoStartChanged` を呼ぶ (research.md #24) |
| `SettingsWindow.xaml` | 変更 | `TabControl` の上に「編集するモニター」の `ComboBox` を置く。「モニター・起動」タブをなくし、「表示」タブの先頭に「このモニターに表示する」の `CheckBox` を置く |
| `PlacementMenuBuilder.cs` | 変更 | トレイのメニューのモニタの項目を作る `Build` を消す。トレイのメニューから配置をなくすと呼ぶ所がなくなるため (原則 I、`/speckit-analyze` の指摘 M3)。本体のメニューが使う `Populate` は残す |
| `SettingsWindow.xaml.cs` | 変更 | コンストラクタで最初に選ぶモニタの `Identifier` を受け取る。選んだモニタの `MonitorPlacement` の値を全コントロールに入れ直す処理を作り、モニタを選んだとき・選択がプライマリに戻ったときに呼ぶ。各コントロールの変更は、選んだモニタの `MonitorPlacement` に書き込み、`Identifier` を付けて App に知らせる。選択肢を作り直す処理 (接続中のモニタの一覧が変わったとき、表示/非表示を切り替えたとき) と、外からモニタを選ばせる処理を足す。自動起動の処理はなくす (research.md #23) |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Persistence/MonitorSettingsReconcilerTests.cs` | 変更 | 以前のバージョンの設定の引き継ぎ (接続中・接続していないモニタの両方に複製が入る、接続していないモニタだけ非表示になる、複製が別のインスタンスである、ルートの 2 つが null になる、2 回目の `Reconcile` では何も変わらない、FR-040 で付け替えたモニタは接続中として扱う)、新しいモニタが非表示・既定値で足される、`Monitors` が空のときはプライマリモニタだけ表示される、プライマリモニタが見つからないときは先頭のモニタが表示される。今の「新しいモニタは表示する」前提のテストは、FR-041 で成り立たなくなるので置き換える |
| `Persistence/SettingsRepositoryTests.cs` | 変更 | 以前のバージョンの形のファイル (ルートに `Appearance`・`WindowBehavior`) が壊れたファイル扱いにならずに読め、ルートの値が残る。新しい形のファイルを保存して読み直すと、各モニタの値が同じになり、ルートの 2 つは書き出されない。各モニタの `Appearance`・`WindowBehavior` が `null` のファイルは壊れたファイルとして扱う。ルートの `Appearance`・`WindowBehavior` が `null` のファイルは壊れたファイルとして扱わない (issue #51 のテストを置き換える) |
| `Settings/SettingsCloneTests.cs` | 追加 | `AppearanceSettings.Clone()`・`WindowBehaviorSettings.Clone()` が、すべての項目を写し、別のインスタンスを返す (片方を変えても、もう片方は変わらない) |

### 変更しないもの

- `AppearanceSettings`・`WindowBehaviorSettings` の項目と、それぞれの値の扱い (フォールバックなど)
- `PlacementMenuBuilder.Populate` (位置ロックの値を、そのウィジェットのものに変えて渡すだけ)
- 自動起動のショートカットを書き換える処理 (`AutoStartManager`) と、書き換えるのは切り替えたときだけという制約 (issue #20)
- タスクトレイの左クリックで全ウィジェットを前に出す動き (FR-031)
- モニタの見分け方と付け替え (FR-040、research.md #20)

### 利用者向けの説明

- 公開リポジトリの README に、新しいモニタと、アップデートのときにつないでいなかったモニタには時計が
  表示されないこと、表示するには詳細設定画面でそのモニタを選ぶことを書く (spec.md の Assumptions)
- タスクトレイのメニューと詳細設定画面の構成が変わるので、README の操作の説明も合わせて直す
- 実装の PR では、このリポジトリの README だけを直す。公開リポジトリの README は、公開リポジトリへ反映する
  ときに直す (tasks の PR で人間が決定。T144。`/speckit-analyze` の指摘 M1)

## Complexity Tracking

*2026-09-27 までの Constitution Check は、すべて PASS で記載すべき違反はなかった。*

*2026-10-01 (issue #53・#17) は、constitution の違反ではないが、Development Workflow 4 の「1 ユーザーストーリー = 1 PR」から外れて
implement を 1 つの PR で行うため、下の表に理由を書く (`/speckit-analyze` の指摘 M2)。*

| 逸脱 | 必要な理由 | より単純な代替案を採らなかった理由 |
|---|---|---|
| Development Workflow 7 の未確認の前提が 2 つ残る (issue #52、research.md #20): 同じ型番のモニタ 2 台の ID が別々になること、EDID を読めないモニタの ID の形 | 手元に同じ型番のモニタ 2 台も、EDID を読めないモニタもなく、確かめる手段がない | 前提が外れても、今までと同じ動き (2 台が同じキーになり片方のウィジェットしか表示されない、完全一致だけで探す) になり、設定を失わない。確かめるためだけにモニタを用意するのは、個人用ツールとして釣り合わない (原則 I)。US4 のシナリオ 5・7 は単体テストでのみ確かめ、実機では未確認であることを quickstart.md に書いた |
| Development Workflow 4 の「1 ユーザーストーリー = 1 PR」から外れ、Phase 26 (issue #53・#17) の implement を 1 つの PR で行う。US4 のほか、US3 のシナリオ 13・14 (タスクトレイのメニュー) と US1 のシナリオ 4 (自動起動) の変更を含む | 位置ロック・最前面表示をモニタごとにすると、今のタスクトレイのメニュー (アプリ全体の位置ロック・最前面表示を切り替える) が成り立たなくなる。US3 のタスクトレイの変更 (T141) を、US4 の変更と別の PR に分けて出せない | US3 の PR を先に出す案は、位置ロックがまだアプリ全体で 1 つの間にトレイから外すことになり、トレイから操作できなくなるだけの中間の状態ができる。US4 を先に出す案は、トレイのメニューが壊れた状態を経由する。どちらも 1 つの PR にするより手間が増えるだけなので採らない (人間が決定。tasks.md の Phase 26 の Purpose) |
