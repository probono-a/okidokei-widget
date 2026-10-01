# Specification Quality Checklist: 常駐デスクトップ時計ウィジェット

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-17
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- FR-016(JSON 保存・レジストリ不使用)は「実装詳細」ではなく `docs/requirements.md` で明示されたユーザー側の明確な制約(ビジネス要件)として扱った
- すべての項目が合格し、`/speckit-plan` へ進める状態

### 2026-09-24 追記分の再検証 (FR-034〜FR-038、issue #26)

- 「右クリックメニュー」「タスクトレイ」といった UI 面の言及は、既存の FR-012・FR-023 等と
  同じ扱い(実装詳細ではなくビジネス要件としての UI 面)として踏襲した
- SC-007 は既存の SC-004 と同様、定性的な「〜事象は発生しない」という形式の成功基準とした
- アンカー×モニタ別配置と、タスクトレイからの一括適用という 2 つの設計判断は、いずれも
  Clarifications と Assumptions に Q&A・根拠を明記し、[NEEDS CLARIFICATION] マーカーを残さず
  解消した
- 全項目再チェック済み。`/speckit-plan` へ進める状態

### 2026-09-24 `/speckit-clarify` 実施分

- 既存ユーザーのアップデート時の初期モード(自由配置 vs アンカー指定)という Data
  Model/Lifecycle 上の未決定事項を 1 件検出し、Clarifications・FR-034・Key Entities・
  Assumptions に反映して解消した
- 他のカテゴリ(機能スコープ、UX フロー、非機能要件、外部連携、エッジケース、用語、完了基準)は
  いずれも既存の Clarifications セッションと今回追加した FR-034〜FR-038 の記述で Clear と判断
- 全項目再チェック済み、状態に変化なし(引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-24 追記分 (タスクトレイのアンカーメニュー設計の見直し)

- 人間から「タスクトレイのメニューを階層化してモニタごとに選べるようにしては」という指摘があり、
  FR-038 の「全モニタへ一括適用」という設計を、「モニタを選んでからそのモニタの設定を選ぶ
  階層メニュー」に置き換えた(FR-038、Acceptance Scenario、Edge Cases、Assumptions を修正)
- この変更により、タスクトレイ側でもモニタごとの現在値をチェック表示でき、ウィジェット本体の
  右クリックメニューとの表示上の矛盾が解消された
- 全項目再チェック済み、状態に変化なし(引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-24 `/speckit-clarify` 再実施分

- 本体の右クリックメニューは階層化しない(FR-037)、位置ロック中はアンカーの項目をグレーアウト
  する(FR-010、SC-004)の 2 点を確認し、Clarifications・FR・Acceptance Scenario に反映した
- 全項目再チェック済み、状態に変化なし(引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-26 追記分の再検証 (FR-009 の改訂・SC-008 の新設、issue #39)

- FR-009 の「作業領域 (タスクバーを除いた領域)」は、既存の FR-020 や Edge Cases と同じく、
  ユーザーから見える画面上の範囲を指す言葉として扱った (実装詳細ではない)
- SC-008 は既存の SC-004・SC-007 と同様、定性的な「〜事象は発生しない」という形式の成功基準とした
- 別のモニタへドラッグで移せるようにするかという判断は、Clarifications に Q&A と根拠 (モニタごとに
  ウィジェットがあり、使い道がない) を明記し、[NEEDS CLARIFICATION] マーカーを残さず解消した
- 端で止まった後の細かい追従の仕方と、ドラッグ中のモニタ構成の変化は、Assumptions で範囲を区切った
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-26 `/speckit-clarify` 実施分

- 端で止まって位置が変わらなかったドラッグでアンカー指定を解除するか、という FR-036 との境目を
  1 件検出した。「解除する」と決め、Clarifications・FR-036・Edge Cases に反映して解消した
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-26 追記分の再検証 (FR-035 の改訂・FR-039 の新設、issue #43)

- 「チェック表示」「グレーアウト」は、既存の FR-010・FR-038 と同じく、ユーザーから見える右クリック
  メニューの状態を指す言葉として扱った (実装詳細ではない)
- FR-039 の距離と余白の比較は「DPI スケールを考慮した実際の画面上の大きさ」とだけ書き、具体的な単位や
  計算方法は plan に任せた
- 動かした後にアンカー指定にするか、範囲内の縁が 2 つのときにどうするか、という 2 つの判断は、
  Clarifications に Q&A と不採用案の理由を明記し、[NEEDS CLARIFICATION] マーカーを残さず解消した
- 新しい成功基準は設けなかった。FR-039 は受け入れシナリオ 17・18 で確かめられ、既存の SC-004
  (位置ロック中は位置が変わらない) もそのまま当てはまるため
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-26 `/speckit-clarify` 実施分 (issue #43)

- 向かい合う縁が両方とも余白の範囲内になる場合に、FR-039 の「範囲内の縁すべてから離す」が
  実現できないという矛盾を 1 件検出した
- 起きるのはウィジェットが作業領域とほぼ同じ大きさのときだけで、大きさに上限がないこと自体を
  見直すべきと判断した。issue #45 に切り出し、本 issue では Assumptions で範囲外と明記して解消した
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-28 追記分の再検証 (FR-040 の新設・SC-005 の改訂、issue #52)

- 「メーカーと型番」「つないでいる端子」は、利用者から見える言葉として書いた。モニタから何をどう
  読み取るかは plan に任せた
- それまでの Assumptions の「同一モニタとして認識できる範囲で」というあいまいな書き方をやめた
  - 同じモニタとみなす範囲を FR-040 で決め、Assumptions からはそこを参照するようにした
- 同じ型番のモニタの台数が変わる場合は、Clarifications に判断を書き、Edge Cases に反映した
  - 通常の操作ではまず起きないため、constitution の品質の線引きに従い Claude が決めた
  - [NEEDS CLARIFICATION] マーカーは残していない
- SC-005 は、端子のつなぎ直しとアップデートを加える形で改訂した。新しい成功基準は設けなかった
  - FR-040 は、User Story 4 の受け入れシナリオ 4〜6 で確かめられる
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態
- speckit-clarify で、以前のバージョンへ戻したときの扱いを 1 件検出した。「設定が初期値に戻ってよい」と決め、
  Clarifications・Assumptions に反映して解消した
  - あわせて、同じ型番の 2 台が 1 台に減り、残りが別の端子につながった場合の扱いを Edge Cases に足した
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-09-28 追記分の再検証 (FR-026 の改訂、issue #60)

- FR-026 は、選択肢に「.」を足し、設定ファイルでは任意の文字列を使えるように改訂した。新しい FR・成功基準は設けなかった
  - FR-026 の改訂は、User Story 2 の受け入れシナリオ 6 で確かめられる
- 設定ファイルに選択肢以外の文字があるときの画面の表示は、issue #60 のコメントでの人間の決定を Clarifications に写した
- 空欄・長さの上限は、Clarifications に判断を書いた
  - 設定ファイルを直接書き換えたときにしか起きないため、constitution の品質の線引きに従い Claude が決めた
  - [NEEDS CLARIFICATION] マーカーは残していない
- 絵文字が単色になること、制御文字や極端に長い文字列の見た目を定めないことは、Assumptions に書いた
- 設定ファイルの `null` は、#51 の修正で扱いを決めた言葉として、利用者向けの言い換えを添えて使った
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態
- speckit-clarify で、Issue の「README 等に書くか」を spec で決めていなかったことを 1 件検出した
  - 「書かない隠し機能とする」と決め、Clarifications・Assumptions に反映して解消した
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態

### 2026-10-01 追記分の再検証 (FR-041・FR-042 の新設ほか、issue #53・#17)

- 見た目・位置ロック・最前面表示をモニタごとに持つ FR-041 と、詳細設定画面で編集するモニタを選ぶ FR-042 を新設した
  - あわせて FR-010〜FR-014・FR-025・FR-037〜FR-040 を改訂し、SC-005 を改訂、SC-009・SC-010 を新設した
- タスクトレイの右クリックメニューを「詳細設定」「自動起動」「終了」だけにしたため、タスクトレイから配置を変える
  受け入れシナリオ (User Story 3 の 13・14) は、番号を変えずに新しいメニューを確かめる内容に書き換えた
- specify の時点の [NEEDS CLARIFICATION] 3 件 (編集するモニタの選ばせ方、新しいモニタの初期値、全モニタへの一括適用) は、
  図 (ui-per-monitor-settings.md) を見ながら人間が決め、Clarifications に反映して解消した
  - 位置ロック・最前面表示をモニタごとにすることと、モニタごとの表示/非表示・自動起動の置き場所も、同じ流れで人間が決めた
- 「新しいモニタは表示しない」から出てくる、保存済みの設定が 1 つもないときの扱い (プライマリモニタに表示する) は、
  US1 を満たすために Claude が決め、FR-041・User Story 4 の受け入れシナリオ 15 に書いた
- User Story 2 の受け入れシナリオ 1・4 は、どのモニタのウィジェットに反映されるかを書いていないが、単一モニタでの
  確認を想定したものとして変えていない。複数モニタの場合は User Story 4 の受け入れシナリオ 8 で確かめられる
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-clarify` または `/speckit-plan` へ進める状態
- speckit-clarify で 2 問を人間に聞き、Clarifications・FR-041 に反映した
  - 初めて起動したときはプライマリモニタだけに表示する (今の動きから変わることも明記した)
  - FR-040 で割り当てる設定があるモニタは「新しいモニタ」ではなく、その設定をそのまま使う
- 文脈を持たないサブエージェントの見直しで、矛盾・古い範囲のままの記述・足りない受け入れシナリオを検出し、解消した
  - 品質の線引きで Claude が決めた 3 点 (判定は起動時だけ、非表示のモニタの位置ロック、プライマリの変更) は Clarifications に書いた
- plan で扱うこと (spec には書かない)
  - plan 側の成果物 (contracts/context-menus.md、research.md #17 のタスクトレイの階層化など) が古くなっているので、`/speckit-plan` で直す
  - 自動起動をタスクトレイに移しても、ショートカットを書き換えるのはオン/オフを切り替えたときだけ、という issue #20 の制約を引き継ぐ
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態
- 2 回目の speckit-clarify で、文脈を持たないサブエージェントの見直しを再度行った
  - 1 回目の修正に誤りや新しい矛盾はなかった。細かい曖昧さ (タスクトレイから開き直したときの選択、抜き差し時の選択など) を解消した
  - 人間に 1 問聞き、アップデートのときにつながっていないモニタは非表示にすると決めた
  - これに合わせて FR-040・FR-041・SC-005・US4-6・US4-10・Assumptions を改訂した
- 全項目再チェック済み、状態に変化なし (引き続き全項目合格)。`/speckit-plan` へ進める状態
