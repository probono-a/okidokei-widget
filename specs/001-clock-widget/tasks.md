---

description: "Task list template for feature implementation"
---

# Tasks: 常駐デスクトップ時計ウィジェット

**Input**: `/specs/001-clock-widget/` 配下の設計ドキュメント (plan.md, spec.md, research.md, data-model.md, contracts/settings-file.md, contracts/context-menus.md, quickstart.md)

**Prerequisites**: plan.md、spec.md (必須)、research.md、data-model.md、contracts/settings-file.md、contracts/context-menus.md (Phase 14 以降)、quickstart.md

**Tests**: plan.md の Testing 方針 (research.md #7) により、`OkidokeiWidget.Core` の設定読み書き・
デフォルトへのフォールバック・モニタ識別子マッチングのロジックは xUnit 単体テストで検証する。
UI・視覚的な確認は自動テスト化せず、quickstart.md の手動シナリオで行う (YAGNI: UI 自動化
フレームワークは導入しない)

**Organization**: タスクはユーザーストーリー (spec.md の P1〜P3) 単位でグループ化し、各ストーリーを
独立して実装・検証できるようにする

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 並行実行可能 (別ファイル・未完了タスクへの依存なし)
- **[Story]**: 対応するユーザーストーリー (US1〜US4)
- 各タスクの説明には具体的なファイルパスを含める

## Path Conventions

plan.md の Project Structure に従う:

- `src/OkidokeiWidget.Core/` — WPF に依存しない設定・永続化・モニタロジック
- `src/OkidokeiWidget.App/` — WPF 実行ファイル
- `tests/OkidokeiWidget.Core.Tests/` — xUnit 単体テスト

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: ソリューション・プロジェクトの初期構成

- [X] T001 リポジトリルートに `OkidokeiWidget.sln` を作成し、`src/OkidokeiWidget.Core/`、
      `src/OkidokeiWidget.App/`、`tests/OkidokeiWidget.Core.Tests/` の各プロジェクトを参照させる
      (plan.md の Project Structure に準拠)
- [X] T002 [P] `src/OkidokeiWidget.Core/OkidokeiWidget.Core.csproj` を作成する。ターゲットは
      `net10.0`、外部 NuGet パッケージ参照は追加しない (research.md #1, #10)
- [X] T003 [P] `src/OkidokeiWidget.App/OkidokeiWidget.App.csproj` を作成する。ターゲットは
      `net10.0-windows`、出力形式は WPF 実行ファイル (`UseWPF=true`)、
      `OkidokeiWidget.Core` プロジェクトを参照する。あわせて `app.manifest` で
      Per-Monitor V2 DPI 認識を宣言する (research.md #5)
- [X] T004 [P] `tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` を作成する。
      `OkidokeiWidget.Core` を参照し、xUnit のテストランナーパッケージ (`xunit`,
      `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) を追加する前に、それぞれのライセンス
      (MIT 等) を確認し、個人利用の範囲で問題ないことを確かめる (research.md #10、
      CLAUDE.md の OSS ライセンス確認ルール)

**Checkpoint**: `dotnet build OkidokeiWidget.sln` が成功する状態

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 全ユーザーストーリーが依存する設定モデル・永続化・モニタ識別ロジック

**⚠️ CRITICAL**: このフェーズが完了するまで、いずれのユーザーストーリーの実装にも着手できない

- [X] T005 [P] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` に `AppearanceSettings`
      モデルを実装する。フィールドは `ShowDate`(bool)、`ShowDayOfWeek`(bool)、
      `FontFamily`(string、data-model.md のバリデーション「空文字/未インストールフォントの場合は
      システムデフォルトにフォールバック」を満たす)、`FontSize`(double、data-model.md の
      バリデーション「`> 0`。範囲外・不正値はデフォルト値にフォールバック」)、
      `FontColor`(string、`#AARRGGBB` 形式、data-model.md の「パース不可の場合はデフォルト色に
      フォールバック」)、`BackgroundOpacity`(double、data-model.md の「`0〜100` の範囲に
      クランプ (FR-008)」)
- [X] T006 [P] `src/OkidokeiWidget.Core/Settings/WindowBehaviorSettings.cs` に
      `WindowBehaviorSettings` モデルを実装する。フィールドは `TopMost`(bool)、
      `PositionLocked`(bool)
- [X] T007 [P] `src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs` に `MonitorPlacement`
      モデルを実装する。フィールドは `IsVisible`(bool)、`X`(int)、`Y`(int)
- [X] T008 `src/OkidokeiWidget.Core/Settings/WidgetSettings.cs` にルート集約 `WidgetSettings`
      を実装する。フィールドは `Appearance`(AppearanceSettings)、
      `WindowBehavior`(WindowBehaviorSettings)、
      `Monitors`(`Dictionary<string, MonitorPlacement>`、モニタ識別子をキーとする)、
      `AutoStartEnabled`(bool)。全フィールドのデフォルト値を持つコンストラクタ/ファクトリも
      用意する (T005, T006, T007 に依存)
- [X] T009 [P] `src/OkidokeiWidget.Core/Monitors/MonitorIdentifier.cs` に、Win32
      `EnumDisplayDevices` を P/Invoke して EDID 由来の安定したモニタデバイス ID を取得する
      ロジックを実装する (research.md #2)
- [X] T010 `src/OkidokeiWidget.Core/Monitors/MonitorEnumerationService.cs` に、
      `System.Windows.Forms.Screen` から取得したモニタの作業領域情報と、T009 の
      `MonitorIdentifier` による EDID 由来 ID を組み合わせて、接続中モニタの一覧
      (識別子・作業領域・プライマリかどうか) を返すサービスを実装する (T009 に依存)
- [X] T011 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` に設定ファイルの
      読み書きを実装する。`Load()` はファイルが存在しない場合、または JSON のパースに失敗
      した場合 (不正な JSON・必須フィールドの型不一致等) に、部分的な復旧を行わず
      `WidgetSettings` 全体をデフォルト値で構築して返す (FR-018、
      contracts/settings-file.md の読み込み契約)。`Save()` は一時ファイルへ書き出してから
      置き換える方式で `%APPDATA%\OkidokeiWidget\settings.json` へ書き込み、書き込み前に
      `BackgroundOpacity` を `0〜100` の範囲へクランプする (contracts/settings-file.md の
      書き込み契約) (T008 に依存)
- [X] T012 `src/OkidokeiWidget.Core/Persistence/MonitorSettingsReconciler.cs` に、読み込んだ
      `WidgetSettings.Monitors` と T010 の現在接続中モニタ一覧を突き合わせる処理を実装する。
      現在接続されていないモニタのキーはそのまま保持し (削除しない)、対応するキーが存在しない
      新規接続モニタにはデフォルト値 (`IsVisible = true`、作業領域中央寄せの位置) のエントリを
      追加する (data-model.md の状態遷移・関係性、Edge Case: モニタ取り外し時の設定保持)
      (T008, T010 に依存)
- [X] T013 `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に
      `SettingsRepository` の単体テストを実装する: (1) ファイルが存在しない場合に
      デフォルト値の `WidgetSettings` を返すこと、(2) 不正な JSON の場合も同様にデフォルト値
      へフォールバックし例外を投げないこと (FR-018)、(3) `Save()` が `BackgroundOpacity` を
      `0〜100` にクランプして書き込むこと、を検証する (T011 に依存)
- [X] T014 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/MonitorSettingsReconcilerTests.cs`
      に `MonitorSettingsReconciler` の単体テストを実装する: 現在接続されていないモニタの
      エントリが削除されずに保持されること、新規接続モニタにデフォルト値のエントリが
      補完されることを検証する (T012 に依存)

**Checkpoint**: `dotnet test tests/OkidokeiWidget.Core.Tests` が全件成功する状態。ここから
各ユーザーストーリーの実装に着手できる

---

## Phase 3: User Story 1 - デスクトップに時計を常時表示する (Priority: P1) 🎯 MVP

**Goal**: ログイン後、操作なしにデスクトップへ時計ウィジェットが表示され、時刻が 1 秒単位で
更新され続ける

**Independent Test**: アプリをインストールし PC を再起動するだけで、ログイン後にデスクトップに
時計が表示され、時刻が 1 秒単位で正しく更新されることを確認できる

### Implementation for User Story 1

- [X] T015 [P] [US1] `src/OkidokeiWidget.App/App.xaml.cs` の `OnStartup` に、名前付き Mutex
      による多重起動チェックを実装する。2 つ目のプロセスは UI を作らずそのまま終了する
      (research.md #6、Edge Case: 二重起動時は何もしない)
- [X] T016 [US1] `src/OkidokeiWidget.App/App.xaml.cs` に起動処理を実装する:
      `SettingsRepository.Load()` で設定を読み込み、`MonitorSettingsReconciler` で現在の
      モニタ構成と突き合わせたうえで、`IsVisible = true` の各モニタについて `ClockWindow` を
      1 つずつ生成する (T015、Foundational T011/T012 に依存)
- [X] T017 [P] [US1] `src/OkidokeiWidget.App/ClockWindow.xaml` にレイアウトを実装する:
      枠なし・内容にサイズが合うウィンドウとし、時刻・日付・曜日を表示する `TextBlock` を配置する
- [X] T018 [US1] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` に 1 秒周期の `DispatcherTimer`
      による時刻更新ループを実装する (FR-002)。`AppearanceSettings.ShowDate` /
      `ShowDayOfWeek` に応じて日付・曜日表示の有無を切り替える (FR-003, FR-004) (T017 に依存)
- [X] T019 [US1] 起動時、各 `ClockWindow` をそのモニタに対応する `MonitorPlacement.X` /
      `Y` の位置に (エントリがなければ作業領域中央寄せの位置に) 配置する (T016, T018 に依存)
- [X] T020 [US1] `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs` に、
      `shell:startup` フォルダへのショートカット (`.lnk`) の作成・削除で自動起動の ON/OFF を
      切り替える処理を実装する (レジストリの Run キーは使用しない)。`App.xaml.cs` の起動処理
      から `AutoStartEnabled` の値に応じて呼び出す (FR-001, FR-019、research.md #3)
- [X] T021 [P] [US1] `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs`
      に `AutoStartManager` の単体テストを実装する: 有効化時にスタートアップフォルダへ
      ショートカットが作成され、無効化時に削除されることを検証する (T020 に依存)

**Checkpoint**: この時点で User Story 1 は単独で完全に機能し、テスト可能な状態

---

## Phase 4: User Story 2 - 見た目を自分好みにカスタマイズする (Priority: P2)

**Goal**: 詳細設定画面でフォント・サイズ・色・背景透過度を変更すると、即座にウィジェットへ
反映され、再起動後も保持される

**Independent Test**: 詳細設定画面でフォント・サイズ・色・透過度を変更し、即座にウィジェットの
見た目に反映されることを確認できる

### Implementation for User Story 2

- [X] T022 [P] [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` にレイアウトを実装する:
      フォント種類選択 (インストール済みフォント一覧)、フォントサイズ入力、文字色選択、
      背景透過度スライダー (0〜100)、日付表示・曜日表示のチェックボックスを配置する
- [X] T023 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` に、各コントロールの変更を
      共有の `AppearanceSettings` インスタンスへ反映し、表示中のすべての `ClockWindow` へ
      即座に反映するロジックを実装する (FR-005〜FR-008、spec.md の Acceptance Scenario
      US2-1, US2-2) (T018, T022 に依存)
- [X] T024 [US2] `SettingsWindow` での変更確定のたびに `SettingsRepository.Save()` を呼び出し、
      `WidgetSettings` 全体を上書き保存する (contracts/settings-file.md の書き込み契約、
      spec.md の Acceptance Scenario US2-3: 再起動後も見た目が保持される) (T023、
      Foundational T011 に依存)
- [X] T025 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の右クリック `ContextMenu` に
      「詳細設定」項目を追加し、`SettingsWindow` を開けるようにする (T022 に依存)
- [X] T026 [P] [US2] `FontFamily` が空文字、またはシステムに未インストールのフォント名を
      指す場合に、システムデフォルトフォントへフォールバックする処理を、`ClockWindow` が
      フォントを適用する箇所に実装する (data-model.md の `FontFamily` バリデーション)
      (T018 に依存)

**Checkpoint**: この時点で User Story 1 と 2 がともに独立して動作する状態

---

## Phase 5: User Story 3 - 配置を自由に決め、誤操作から保護する (Priority: P2)

**Goal**: ウィジェットをドラッグして配置でき、ロック後は位置がずれず、最前面表示を右クリック
メニューから切り替えられる

**Independent Test**: ウィジェットをドラッグして移動できること、ロック後はドラッグしても位置が
変わらないこと、右クリックメニューから最前面表示を ON/OFF できることを個別に確認できる

### Implementation for User Story 3

- [X] T027 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` にマウスドラッグによる移動を
      実装する (`PreviewMouseLeftButtonDown` / `MouseMove` でウィンドウ位置を更新する) (FR-009)
      (T018 に依存)
- [X] T028 [US3] `WindowBehaviorSettings.PositionLocked` が `true` の間はドラッグ処理を
      無効化する (FR-010、spec.md の Acceptance Scenario US3-2) (T027 に依存)
- [X] T029 [P] [US3] `src/OkidokeiWidget.App/ClockWindow.xaml(.cs)` の右クリック
      `ContextMenu` に「位置ロック」「最前面表示」の切替項目と「終了」項目を追加し、
      `WindowBehaviorSettings` と連動させる (FR-011, FR-012、research.md #8: トレイアイコンを
      追加せず本体への右クリックメニューとして実装する)。「終了」項目の選択時は
      `Application.Current.Shutdown()` 等でアプリケーションを終了する (FR-023)
- [X] T030 [US3] ドラッグ終了時に、そのウィンドウが表示されているモニタに対応する
      `MonitorPlacement.X` / `Y` を更新し、`SettingsRepository.Save()` で永続化する (FR-015)
      (T027、Foundational T011 に依存)
- [X] T031 [US3] `ClockWindow.Topmost` を `WindowBehaviorSettings.TopMost` にバインドし、
      右クリックメニューからの切替時に `SettingsRepository.Save()` で永続化する (FR-011)。
      同様に「位置ロック」切替時も `WindowBehaviorSettings.PositionLocked` を
      `SettingsRepository.Save()` で永続化する (FR-010、spec.md の Acceptance Scenario
      US3-5: 再起動後も位置ロック・最前面表示の設定が保持される) (T029 に依存)

**Checkpoint**: この時点で User Story 1・2・3 がともに独立して動作する状態

---

## Phase 6: User Story 4 - マルチモニタ環境でモニタごとに管理する (Priority: P3)

**Goal**: モニタごとに表示/非表示を切り替え、モニタごとに異なる表示位置を独立して保持・復元する

**Independent Test**: 2 台以上のモニタ環境で、モニタ A のみ表示・モニタ B は非表示に設定し、
モニタ A での表示位置を移動した後、アプリを再起動してもモニタごとの表示/非表示と位置が
それぞれ保持されていることを確認できる

### Implementation for User Story 4

- [X] T032 [US4] `SettingsWindow.xaml(.cs)` に、`MonitorEnumerationService` から取得した
      接続中モニタの一覧を表示し、モニタごとの表示/非表示チェックボックスを配置する (FR-014)
      (Foundational T010、T022 に依存)
- [X] T033 [US4] `SettingsWindow` でモニタの表示/非表示チェックボックスが切り替えられたとき、
      対応する `ClockWindow` を実行時に生成/破棄する処理を `App.xaml.cs` (または専用の
      ウィンドウ管理ヘルパー) に実装する (FR-014、spec.md の Acceptance Scenario US4-1)
      (T016, T032 に依存)
- [X] T034 [US4] T027 のドラッグ移動と T030 の位置永続化が、各 `ClockWindow` ごとに
      それぞれのモニタ識別子をキーとした `WidgetSettings.Monitors` のエントリへ独立して
      書き込まれ、他モニタのエントリに影響しないことを実装・確認する (FR-015、spec.md の
      Acceptance Scenario US4-2) (T030, T033 に依存)
- [X] T035 [P] [US4] `SettingsWindow` に自動起動 ON/OFF のチェックボックスを追加し、
      `AutoStartManager` と `WidgetSettings.AutoStartEnabled` に連動させる (FR-019, FR-013)
      (T020, T022 に依存)

**Checkpoint**: すべてのユーザーストーリーが独立して機能する状態

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: 全ストーリー共通の最終確認

- [X] T036 [P] quickstart.md の US1〜US4 の手動シナリオと DPI 混在環境の確認をすべて実施し、
      期待結果と一致することを確認する (FR-020)
      (2026-09-17 実施。プライマリ 1920x1080 /100% + セカンダリ 3840x2160 /225% の DPI 混在
      環境で確認。結果は下記「Phase 7 の確認結果」を参照)
- [X] T037 [P] 詳細設定画面を閉じている間のアイドル時 CPU 使用率が 1%未満、メモリ使用量が
      100MB 未満であることをタスクマネージャー等で確認する (SC-006、詳細設定画面を開いている
      間は対象外)。超過する場合はタイマー周期・描画処理を見直す
      (60 秒平均で CPU 0.007%、プライベートワーキングセット 36.1MB。SC-006 を満たす)
- [X] T038 [P] 実際に書き出された `settings.json` が contracts/settings-file.md のスキーマ例
      と一致すること (キー名・型・`Monitors` のキー形式) を確認する
      (キー名・階層・型は一致。`Monitors` のキー例が実装と異なっていたため
      `contracts/settings-file.md` の例と補足を実装に合わせて修正した)

### Phase 7 の確認結果 (2026-09-17)

**確認できたこと**

- `dotnet test` 27 件すべて成功
- US1: 両モニターにウィジェットが表示され、秒表示 ON で 1 秒ごとに時刻が更新される
- US1: 起動中に 2 つ目のプロセスを起動すると、新しいウィンドウを作らず終了コード 0 で終了する
- US2: 秒表示・時刻フォントサイズ・背景透過度・日付/曜日の表示位置の変更が即座に反映される
  - 変更は `settings.json` にも即座に保存され、再起動後も同じ見た目・位置で復元される
- US3: 位置ロック ON ではドラッグしても位置が変わらず、OFF にするとドラッグで移動・保存される
- US3: 最前面表示 OFF で背後に回ったウィジェットが、タスクトレイアイコンのクリックで最前面に戻る
- US3: タスクトレイアイコンの右クリックメニュー「終了」でアプリが終了する
- US3: 実行ファイル・ウィンドウ・タスクトレイのアイコンが専用の時計アイコンになっている
- US4: モニターごとの表示/非表示の切り替えが他方のモニターに影響せず、位置もモニターごとに
  独立して保存・復元される
- DPI 混在: 100% のプライマリと 225% のセカンダリの双方で、フォント・レイアウトが崩れずに
  それぞれの拡大率で表示される (FR-020)

**発見した挙動 (対応は保留)**

- 背景透過度を 100 にすると、文字が描画されているピクセル以外はクリックが下のウィンドウへ抜ける
  - 原因は、`AllowsTransparency` のレイヤードウィンドウではアルファ 0 の領域が
    ヒットテスト対象外になること
  - 文字の上を正確に右クリックすればメニューは出る
  - quickstart.md の「既知の制約の確認」にある「透過度 100% でもドラッグ・右クリックが
    できること」とは食い違うため、記録として残す (対応方針は別途判断)

**未確認**

- モニターの物理的な取り外し/再接続後の設定保持 (quickstart.md 上も任意項目)
- DirectX 排他フルスクリーンでの挙動 (既知の制約であり、不具合ではない)

---

## Phase 8: /speckit-analyze 指摘の反映 (2026-09-17)

**Purpose**: tasks.md 作成後の `/speckit-clarify` で spec.md に追加された FR-018(通知)・
FR-022(書き込み失敗耐性)・Edge Case(稼働中のモニタ切断)に対応するタスクを追加する
(`/speckit-analyze` の指摘 C1・C2・C4 への対応。C3・I1・U1 は該当タスク本文を直接修正済み)

- [X] T039 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `Load()` を変更し、
      設定ファイルの内容が不正でデフォルト値へフォールバックしたかどうかを呼び出し元へ
      伝える戻り値(例: `(WidgetSettings Settings, bool FellBackToDefaults)` のタプル)を
      追加する (FR-018) (T011 の変更)
- [X] T040 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、
      不正な JSON 読み込み時に T039 のフォールバックフラグが `true` になることを検証する
      テストケースを追加する (T039 に依存)
- [X] T041 [US1] `src/OkidokeiWidget.App/App.xaml.cs` の起動処理で、T039 のフラグが `true`
      の場合に、設定が壊れていたためデフォルト設定で起動したことをユーザーに通知する
      (例: 起動直後に簡易なメッセージボックスを表示する) (FR-018) (T016, T039 に依存)
- [X] T042 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `Save()` に、
      書き込み時の例外(`UnauthorizedAccessException`、`IOException` 等)を捕捉し、
      クラッシュせず処理を継続する(保存はできないが呼び出し元には正常終了として返す)
      処理を追加する (FR-022) (T011 の変更)
- [X] T043 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、
      書き込み失敗時(例: 書き込み不可なパスを指定)でも `Save()` が例外を外部に伝播させ
      ないことを検証するテストを追加する (T042 に依存)
- [X] T044 `src/OkidokeiWidget.Core/Monitors/MonitorEnumerationService.cs` (または新規の
      ヘルパークラス)に、`SystemEvents.DisplaySettingsChanged` を購読してアプリ稼働中の
      モニタ構成変化を検知する仕組みを実装する (T010 に依存)
      (実装は `src/OkidokeiWidget.App/DisplayChangeNotifier.cs` に、TrayIconManager と同様の
      HwndSource + WM_DISPLAYCHANGE フックとして追加した。`Microsoft.Win32.SystemEvents` は
      追加の NuGet 参照が必要になるため、既存の P/Invoke パターンに合わせて置き換えた)
- [X] T045 [US4] `src/OkidokeiWidget.App/App.xaml.cs` (または専用のウィンドウ管理ヘルパー)で、
      T044 の変化通知を受けて、切断されたモニタに対応する `ClockWindow` を非表示にし、
      再接続時に同じ設定(表示/非表示・位置)で表示を再開する処理を実装する (Edge Case:
      稼働中のモニタ切断時は自動非表示、設定は保持) (T016, T033, T044 に依存)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、`quickstart.md` の既知の制約確認
セクションで DPI 混在環境の手動確認も行える状態

---

## Phase 9: User Story 2 実機確認による仕様追加の反映 (2026-09-17)

**Purpose**: User Story 2 実装後、実際にウィジェットを表示して確認した結果判明した仕様漏れ
(FR-024〜FR-029、spec.md の Clarifications 参照) に対応する

- [X] T046 [P] [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` を変更し、
      `ShowSeconds`(bool)、`DateSeparator`(string)、`DayOfWeekFormat`(string)、
      `TimeAndDateOnSameLine`(bool) を追加する。既存の `FontSize`/`FontColor` は
      `TimeFontSize`/`TimeFontColor` に改名し、新たに `DateFontSize`/`DateFontColor` を
      追加する(data-model.md 参照。設定ファイルは現バージョンのみが対象のため、旧フィールド名
      からの移行処理は行わない)
- [X] T047 [P] [US2] `src/OkidokeiWidget.Core/Settings/DayOfWeekFormat.cs` に、
      `ShortKanjiParen`・`LongKanji`・`ShortEnglish`・`LongEnglish` の 4 値を持つ enum を
      定義する (FR-027)
- [X] T048 [US2] `src/OkidokeiWidget.Core/Settings/DayOfWeekFormatter.cs` に、
      `DayOfWeek` と `DayOfWeekFormat` から表示文字列 (例:「(月)」「月曜日」「Mon.」「Monday」)
      を返すロジックを実装する (T047 に依存)
- [X] T049 [P] [US2] `src/OkidokeiWidget.Core/Settings/DateSeparatorResolver.cs` に、
      `DateSeparator` が `/` または `-` 以外の場合にデフォルト (`/`) へフォールバックする
      ロジックを実装する (FR-026、data-model.md のバリデーション)
- [X] T050 [P] [US2] `tests/OkidokeiWidget.Core.Tests/Settings/DayOfWeekFormatterTests.cs`、
      `tests/OkidokeiWidget.Core.Tests/Settings/DateSeparatorResolverTests.cs` に単体テストを
      実装する (T048, T049 に依存)
- [X] T051 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml(.cs)` のレイアウトを変更する:
      日付・曜日を常に同一行(改行なし)に表示し (FR-028)、`TimeAndDateOnSameLine` に応じて
      時刻行と日付/曜日行を同一行/別行に切り替える (FR-029)。`ShowSeconds` に応じて秒の
      表示有無を切り替え (FR-024)、時刻表示と日付/曜日表示にそれぞれ
      `TimeFontSize`/`TimeFontColor`・`DateFontSize`/`DateFontColor` を適用し (FR-025)、
      日付表示に `DateSeparator` を、曜日表示に `DayOfWeekFormatter` の結果を反映する
      (T046, T048, T049 に依存)
- [X] T052 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` に、秒表示 ON/OFF、
      時刻表示/日付・曜日表示それぞれのフォントサイズスライダー・文字色選択ボタン、
      日付区切り文字選択、曜日表示形式選択、時刻と日付/曜日を同一行に表示するかの
      チェックボックスを追加し、変更を即座に反映・保存する (T046, T048, T049 に依存)
- [X] T053 [P] [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` の見た目を整理する
      (「見た目が古い」との指摘への対応): `GroupBox` 等でセクション分け
      (表示内容・時刻・日付/曜日・レイアウト・背景) し、余白・配置を統一する。機能追加は伴わない
      見た目の整理のみ

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から秒表示・
時刻/日付/曜日それぞれのフォントサイズ・色・日付区切り文字・曜日形式・改行有無を変更すると
即座にウィジェットへ反映され、再起動後も保持されることを確認できる状態

---

## Phase 10: 背景色の追加・詳細設定画面のレイアウト再編 (2026-09-17)

**Purpose**: 実機確認で追加された FR-030(背景色)への対応と、設定項目の増加に伴い縦に伸び
続けている詳細設定画面のレイアウトをタブ化して整理する

- [X] T054 [P] [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` に
      `BackgroundColor`(string、`#AARRGGBB` 形式、デフォルト黒)を追加する (FR-030、
      data-model.md 参照。アルファ成分は無視し、実際の透過度は `BackgroundOpacity` から
      都度算出する)
- [X] T055 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `ApplyAppearance()` で、
      `BackgroundColor` の RGB 成分と `BackgroundOpacity` から算出したアルファ値を組み合わせて
      `BackgroundBorder.Background` に反映する (T054 に依存)
- [X] T056 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` の「背景」セクションに、
      既存の `ColorPickerWindow` を再利用した背景色選択ボタンを追加する (T054、既存の
      `PickColor` ヘルパーに依存)
- [X] T057 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` を `TabControl` で再編し、
      既存の 5 セクション(表示内容・フォント・日付/曜日の表示形式・レイアウト・背景)を
      「表示」「フォント」「レイアウト・背景」の 3 タブにまとめ、ウィンドウの縦方向の
      伸びを抑える。今後 US3/US4 で増える設定項目(位置ロック・最前面表示・モニタ別表示・
      自動起動)も同様にタブへ追加していく方針とする(機能追加は伴わない見た目の整理)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から背景色を
変更すると即座にウィジェットへ反映され、再起動後も保持されることを確認できる状態。
詳細設定画面がタブ化され、ウィンドウの高さが Phase 9 時点より抑えられていることを確認できる状態

---

## Phase 11: User Story 3 実装後の追加要望への対応 (2026-09-17)

**Purpose**: User Story 3 の実機確認後に追加された FR-029(改訂)・FR-031〜FR-033
(日付/曜日の 4 方向配置、タスクトレイ常駐アイコンによる前面復帰・終了、アプリアイコン)
に対応する

- [X] T058 [P] [US2] `src/OkidokeiWidget.Core/Settings/RelativePosition.cs` に、
      `Above`・`Below`・`Left`・`Right` の 4 値を持つ enum `RelativePosition` を定義する
      (FR-029)
- [X] T059 [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` の
      `TimeAndDateOnSameLine`(bool)を `DateDayOfWeekPosition`(`RelativePosition`、
      デフォルト `Below`)に置き換える(T058 に依存。設定ファイルは現バージョンのみが対象の
      ため、旧フィールド名からの移行処理は行わない)
- [X] T060 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml` の `RootPanel` を
      `StackPanel` から `DockPanel` に変更する。`ClockWindow.xaml.cs` の
      `ApplyAppearance()` で `DateDayOfWeekPosition` に応じて
      `DockPanel.SetDock(DateRow, ...)` と `DateRow.Margin` を設定し、時刻表示に対して
      上/下/左/右のいずれかに日付/曜日表示を配置する(T059 に依存)
- [X] T061 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` の
      `TimeAndDateOnSameLineCheckBox` を、4 方向から選択する `ComboBox` に置き換える
      (`DayOfWeekFormatCombo` と同様の Value+Label パターン)(T059 に依存)
- [X] T062 [P] `src/OkidokeiWidget.App/app.ico` を新規作成する(時計をイメージした専用の
      アイコン、複数解像度を含む)。`OkidokeiWidget.App.csproj` の `ApplicationIcon` に
      指定し、実行ファイルのアイコンとする (FR-033、research.md #14)
- [X] T063 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml`・`SettingsWindow.xaml` に
      `Icon="app.ico"` を設定する(T062 に依存)
- [X] T064 [US3] `src/OkidokeiWidget.App/TrayIconManager.cs` に、`Shell_NotifyIcon`
      (shell32.dll)への P/Invoke と非表示の `HwndSource` を用いたタスクトレイ常駐アイコンを
      実装する。クリックで全 `ClockWindow` を最前面に呼び戻し(`Topmost` の一時的な
      true→元の値への切替)、右クリックで「終了」のみを持つ `ContextMenu` を表示する
      (FR-031, FR-032、research.md #13)(T062 に依存)
- [X] T065 [US3] `src/OkidokeiWidget.App/App.xaml.cs` の起動処理で `TrayIconManager` を
      初期化し、終了処理(`OnExit`)でアイコンを破棄する(T064 に依存)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から日付/曜日の
表示位置(上下左右)を変更すると即座にウィジェットへ反映され、再起動後も保持されること、
タスクトレイの常駐アイコンをクリックすると最前面表示 OFF でもウィジェットが前面に戻ること、
右クリックの「終了」でアプリが終了すること、実行ファイル・ウィンドウ・タスクトレイアイコンに
専用アイコンが表示されることを確認できる状態

---

## Phase 12: 実機確認で発覚したバグの修正 (2026-09-17)

**Purpose**: User Story 4 の実機確認 (モニタの物理的な取り外し/再接続) と Phase 7 以降の
利用で判明した 2 件のバグを修正する。いずれも既存 FR の実装バグであり、仕様の追加・変更は
伴わない

- issue #10: モニタ構成が変わるとウィジェットが画面外に配置され表示されない
  (FR-014, FR-015、Constitution 原則 V「マルチモニタ・DPI 対応を前提とした設計」に反する状態)
- issue #11: 背景の透過度を 100% にするとウィジェットのほとんどの領域をクリックできない
  (FR-008, FR-030)

- [X] T066 [P] [US2] `src/OkidokeiWidget.Core/Settings/BackgroundAlphaResolver.cs` を追加し、
      背景の透過度からアルファ値を算出する処理を切り出す。アルファ値の下限を 1 とし、透過度
      100% でも 0 にしない (issue #11)
- [X] T067 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `ApplyAppearance()` の
      アルファ値計算を `BackgroundAlphaResolver.Resolve()` に置き換える (T066 に依存)
- [X] T068 [P] [US4] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` を追加し、
      `MonitorPlacement` (作業領域基準の相対座標) と仮想デスクトップ上の絶対座標を物理ピクセルで
      相互変換する。作業領域から外れる座標は作業領域内へ収める (issue #10)
- [X] T069 [P] [US4] `src/OkidokeiWidget.App/WindowPositionHelper.cs` を追加し、
      `SetWindowPos` / `GetWindowRect` でウィンドウ位置を物理ピクセルとして扱えるようにする。
      WPF の `Window.Left/Top` は論理単位 (DIP) のため、拡大率 100% 以外のモニタでは物理座標と
      ずれる (issue #10)
- [X] T070 [US4] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` のウィンドウ配置を
      `SourceInitialized` / `ContentRendered` での `ApplyPlacement()` に移し、ドラッグ終了時の
      位置保存も物理ピクセルで行うようにする (T068、T069 に依存)
- [X] T071 [US4] `src/OkidokeiWidget.App/App.xaml.cs` の `SyncClockWindows()` で、表示中の
      `ClockWindow` にも最新の `ConnectedMonitor` を渡して配置し直す (`UpdateMonitor()`)。
      モニタの取り外し・再接続で作業領域の原点が動くため (T070 に依存)
- [X] T072 [P] `tests/OkidokeiWidget.Core.Tests/Settings/BackgroundAlphaResolverTests.cs`・
      `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` に単体テストを
      追加する (T066、T068 に依存)

**Checkpoint**: `dotnet test` が全件成功し、背景の透過度 100% でもウィジェットの矩形全体で
右クリック・ドラッグができること、モニタを取り外して再接続してもウィジェットが保存された位置に
表示されることを確認できる状態

**補足**: 修正前の `settings.json` の座標は論理単位と物理ピクセルが混ざった値のため、拡大率が
100% でないモニタではウィジェットの位置が一度だけずれる。そのモニタで一度ドラッグし直せば、
以後は正しい値が保存される

---

## Phase 13: DPI 変更時に位置ロックがずれるバグの修正 (2026-09-24)

**Purpose**: issue #25 の修正の記録。既存 FR (FR-010, FR-020) の実装バグであり、仕様の追加・
変更は伴わない。修正は `bug` 拡張のフローで PR #27 として実施済み (経緯・検証結果は
`.specify/bugs/dpi-position-drift/` を参照)。本 Phase は、CLAUDE.md の「既存 FR の実装バグは
`tasks.md` に新しい Phase として追記する」ルールに従い、修正後に追記したもの

- issue #25: 位置ロック中でも、DPI スケールだけが変わるとウィジェットの位置がずれる。
  `WM_DISPLAYCHANGE` は解像度変更時にしか届かず、WPF が Windows の提案する位置へウィンドウを
  動かしたまま、保存済みの位置へ戻していなかった

- [X] T073 [US4] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` のコンストラクタに
      `Action onDpiChanged` を追加し、`Window.DpiChanged` で呼び出す (issue #25)
- [X] T074 [US4] `src/OkidokeiWidget.App/App.xaml.cs` の `CreateClockWindow()` で、既存の
      `OnDisplaySettingsChanged` を `onDpiChanged` として渡す。モニタ情報の再取得と全
      ウィンドウの再配置を、解像度変更時と同じ経路で行うため (T073 に依存)

**Checkpoint**: `dotnet build`・`dotnet test` が成功し、位置ロック中に表示スケールを変更すると
ウィジェットが保存済みの位置へ配置し直されること (人間が実機で確認済み)

---

## Phase 14: アンカー指定と右クリックメニューの統一 (2026-09-24)

**Purpose**: issue #26 を受けて spec.md に追加した FR-034〜FR-038 と、FR-010・FR-012・SC-004・
SC-007 の改訂に対応する。すべて User Story 3「配置を自由に決め、誤操作から保護する」に属する。
設計は plan.md の「既存実装に対する変更計画 (2026-09-24)」、research.md #15〜#17、
data-model.md の `MonitorPlacement`、contracts/context-menus.md に従う

**Independent Test**: ウィジェットを右クリック→「配置」→「右上」でウィジェットが作業領域の
右上へ移動し、フォントサイズや拡大率を変えても右上に留まること。位置ロック中は「配置」の
中の項目がグレーアウトすること (サブメニュー自体は開ける)。タスクトレイの右クリックメニューが本体と同じ項目を持ち、モニタを選んで
から配置を変えられること

### Core: 設定モデルと位置計算

- [X] T075 [P] [US3] `src/OkidokeiWidget.Core/Settings/AnchorPosition.cs` に、`TopLeft`・`Top`・
      `TopRight`・`Left`・`Center`・`Right`・`BottomLeft`・`Bottom`・`BottomRight` の 9 値を持つ
      enum `AnchorPosition` を定義する (FR-034、research.md #15)
- [X] T076 [P] [US3] `src/OkidokeiWidget.Core/Settings/AnchorMargin.cs` に、`Narrow` (狭め)・
      `Wide` (広め) の 2 値を持つ enum `AnchorMargin` を定義する (FR-035)
- [X] T077 [US3] `src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs` に
      `AnchorPosition? Anchor` (既定値 null) と `AnchorMargin AnchorMargin` (既定値 `Narrow`) を
      追加する。data-model.md の制約: 「null なら自由配置で `X`/`Y` を使う。値があれば `X`/`Y` を
      無視してアンカーから位置を計算する」「未知の enum 値は JSON 全体のパース失敗として扱い、
      `WidgetSettings` 全体をデフォルトへフォールバックする」。`SettingsRepository` は既存の
      `JsonStringEnumConverter` のまま変更しない (T075、T076 に依存)
- [X] T078 [US3] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` の
      `ToAbsolutePosition()` に引数 `double dpiScale` を追加し、`placement.Anchor` が null でない
      場合はアンカーから位置を計算する (research.md #15)。T077 に依存
      - 余白は定数 `NarrowMarginDip = 8`・`WideMarginDip = 24` に `dpiScale` を掛け、四捨五入して
        物理ピクセルにする
      - 横位置: 左 = `作業領域の左端 + 余白`、中央 = `作業領域の左端 + (作業領域の幅 - ウィジェット幅) / 2`
        (余白は使わない)、右 = `作業領域の右端 - ウィジェット幅 - 余白`。縦位置も同様
      - 計算結果は既存の `Clamp` で作業領域内へ収める。`Anchor` が null の場合の計算は変更しない
      - 既存の呼び出し元 (`ClockWindow.xaml.cs`) とテストは、この時点では `dpiScale: 1.0` を渡す
        形で修正してビルドを通す
- [X] T079 [P] [US3] `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs`
      に単体テストを追加する (T078 に依存)
      - 9 つの配置それぞれについて、`Narrow`・`dpiScale` 1.0 で期待どおりの座標になる
      - `Wide`・`dpiScale` 1.5 で余白が 36 px になる (例: `TopRight`)
      - 中央の軸では余白が使われない
      - ウィジェットが作業領域より大きい場合は左上に合わせて収まる
      - `Anchor` が null の場合は既存テストと同じ結果になる
- [X] T080 [P] [US3] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に
      単体テストを追加する (T077 に依存)
      - `Anchor`・`AnchorMargin` を含まない既存形式の JSON を読むと、`Anchor` = null、
        `AnchorMargin` = `Narrow` になり、`FellBackToDefaults` は false (FR-034)
      - `Anchor` = `TopRight`、`AnchorMargin` = `Wide` を保存して読み戻すと値が一致する
      - `Anchor` に未知の文字列 (例: `"Middle"`) が入っていると、デフォルト設定に
        フォールバックし `FellBackToDefaults` が true になる

### App: ウィジェット本体

- [X] T081 [P] [US3] `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` を新規作成し、1 モニタ分の
      配置サブメニューを作る静的メソッドを実装する (research.md #17、contracts/context-menus.md)。
      T077 に依存
      - 引数: メニューの見出し (`"配置"` やモニタ名)、対象の `MonitorPlacement`、位置ロック中か
        どうか (`bool isLocked`)、配置を選んだときのコールバック (`Action<AnchorPosition>`)、
        余白を選んだときのコールバック (`Action<AnchorMargin>`)。戻り値は子項目を持つ `MenuItem`
      - 子項目: 「左上」「上」「右上」「左」「中央」「右」「左下」「下」「右下」、区切り線、
        「余白: 狭め」「余白: 広め」
      - チェック: 9 項目は `placement.Anchor` と一致する 1 項目のみ。null ならどれも付けない。
        余白は `placement.AnchorMargin` 側に付ける
      - グレーアウト: `isLocked` が true なら、9 項目と余白 2 項目の `IsEnabled` を false にする。
        戻り値の親 `MenuItem` 自体は有効のままにする。WPF では無効な親のサブメニューは開けず、
        spec の「項目は表示されたままグレーアウト」(Acceptance Scenario 10) を満たせないため
        (FR-010、contracts/context-menus.md)。チェック状態はロック中も通常どおり付ける
- [X] T082 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の配置処理を更新する (T078 に依存)
      - `ApplyPlacement()` で `VisualTreeHelper.GetDpi(this).DpiScaleX` を `dpiScale` として渡す
      - `SizeChanged` でも `ApplyPlacement()` を呼ぶ。フォントサイズ変更や日付/曜日表示の切り替えで
        サイズが変わっても、アンカーからの位置を保つため (SC-007)
      - ドラッグ終了時 (`BackgroundBorder_MouseLeftButtonUp`) の保存前に `_placement.Anchor = null`
        にする (FR-036)
- [X] T083 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` に public メソッド
      `SetAnchor(AnchorPosition)`・`SetAnchorMargin(AnchorMargin)` を追加する。どちらも
      `_placement` を更新し、`ApplyPlacement()` で再配置してから `SettingsRepository.Save()` する。
      位置ロック中は何もしない (FR-010)。本体とトレイの両方からこのメソッドを使い、配置変更の経路を
      1 つにする (T082 に依存)
- [X] T084 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml` の右クリックメニューで、「最前面表示」の
      下に `x:Name="PlacementMenuItem"` の「配置」を追加する。`ClockWindow.xaml.cs` で
      右クリックメニューを開く直前 (メニューが付いている `BackgroundBorder` の
      `ContextMenuOpening`) に、`PlacementMenuBuilder` で作った子項目で `PlacementMenuItem` の
      中身を置き換える。`isLocked` には `PositionLocked` を渡し、`PlacementMenuItem` 自体は
      無効にしない (T081 のグレーアウトの規則)。コールバックは T083 の `SetAnchor`・
      `SetAnchorMargin` を呼ぶ (FR-010, FR-037)。T081、T083 に依存

### App: タスクトレイ

- [X] T085 [US3] `src/OkidokeiWidget.App/TrayIconManager.cs` の右クリックメニューの組み立てを
      外に出す。コンストラクタで `Func<ContextMenu>` を受け取り、右クリック時にそれが返す
      メニューを表示するだけにする。「終了」だけを持つ現在のメニュー組み立てと `ExitRequested`
      イベントは削除する。左クリックの `ActivateAllRequested` は変更しない (research.md #17)。
      `src/OkidokeiWidget.App/App.xaml.cs` の `TrayIconManager` の生成箇所と `ExitRequested` の
      購読も合わせて直し、ビルドが通る状態にする。この時点では「終了」だけのメニューを返す
      `Func<ContextMenu>` を渡しておき、T086 で本来のメニューに置き換える
- [X] T086 [US3] `src/OkidokeiWidget.App/App.xaml.cs` に、トレイの右クリックメニューを組み立てる
      メソッドを追加し、`TrayIconManager` に渡す (FR-038、contracts/context-menus.md)。
      T084、T085 に依存
      - 項目: 「詳細設定...」(`OpenSettingsWindow`)、区切り線、「位置ロック」「最前面表示」
        (チェック付き。クリックで値を反転して既存の `OnWindowBehaviorChanged` を呼ぶ)、「配置」、
        区切り線、「終了」(`Shutdown`)
      - 「配置」の下には、ウィジェットを表示中のモニタ (`_clockWindowsByMonitor` にあるもの) ごとに
        `PlacementMenuBuilder` で作ったサブメニューを並べる。見出しは `SettingsWindow.xaml.cs` と
        同じ「モニター N」「モニター N (プライマリ)」形式。コールバックは該当モニタの
        `ClockWindow.SetAnchor`・`SetAnchorMargin` を呼ぶ
      - 位置ロック中は、各モニタの `PlacementMenuBuilder` に `isLocked: true` を渡して中の項目を
        グレーアウトする。「配置」とモニタ名の項目は無効にしない (FR-010、T081 の規則)
      - モニタが 1 台だけでも、モニタ一覧の階層は省略しない

### 実機確認中の変更: 配置メニューを軸ごとに選ぶ形へ

T087 の実機確認で、9 つの配置を 1 列に並べたメニューは「縦にずらずら並んで直感的に分かり
にくい」との指摘があった。メニューを「横位置 ▶ 左/中央/右」「縦位置 ▶ 上/中央/下」「余白 ▶
狭め/広め」に変更する。保存形式 (9 値の `Anchor`) は変えない。T081・T083 の記述のうち、9 項目を
並べる部分と `SetAnchor(AnchorPosition)` を public にする部分は、以下のタスクで置き換えた
(research.md #15 の追記、contracts/context-menus.md)

- [X] T088 [P] [US3] `src/OkidokeiWidget.Core/Settings/AnchorAxes.cs` を追加する。enum
      `AnchorHorizontal` (`Left`/`Center`/`Right`)・`AnchorVertical` (`Top`/`Center`/`Bottom`) と、
      9 値の `AnchorPosition` との相互変換 (`HorizontalOf`・`VerticalOf`・`Compose`) を持つ
- [X] T089 [US3] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` に
      `WithHorizontal()`・`WithVertical()` を追加する。片方の軸を選んだときのもう片方は、アンカー
      指定中なら今の値のまま、自由配置中なら作業領域を 3 等分してウィジェットの中心がある所にする。
      `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` に単体テストを
      追加する (T088 に依存)
- [X] T090 [US3] `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` のサブメニューを「横位置」「縦位置」
      「余白」の 3 階層に変更する。位置ロック中に無効にするのは末端の選択肢のみ。
      `ClockWindow.xaml.cs` の public メソッドを `SetAnchorHorizontal()`・`SetAnchorVertical()`・
      `SetAnchorMargin()` にし (`SetAnchor()` は private に)、`App.xaml.cs` のトレイのメニューも
      これを使う (T089 に依存)

### 確認

- [X] T087 [US3] `dotnet build`・`dotnet test` が成功することを確認し、`quickstart.md` の
      「US3: 配置とロック」→「アンカー指定」の手動シナリオを人間に実施してもらう。結果と、
      余白の値 (8 / 24 DIP) を調整した場合はその値を本 Phase の末尾に記録する (T079〜T086、
      T088〜T090 に依存)

**Checkpoint**: `dotnet test` が全件成功し、quickstart.md の「アンカー指定」のシナリオが
すべて期待どおりに動作する状態。既存の `settings.json` のまま起動しても位置が変わらないこと

### Phase 14 の確認結果 (2026-09-24)

- `dotnet build` (Debug・Release とも警告 0、エラー 0)、`dotnet test` (70 件すべて合格。
  Phase 14 で 31 件を追加)
- 人間が Debug 版を実機で起動して確認し、問題なし。確認の途中で配置メニューの構成を変更した
  (T088〜T090)
- 余白の値は当初の 8 / 24 DIP のまま変更なし
- (2026-09-24 追記) この確認では、右クリックメニューのチェックマークが表示されていないことを
  見落としていた。Phase 15 (issue #34) で修正

---

## Phase 15: 右クリックメニューのチェックマークが表示されないバグの修正 (2026-09-24)

**Purpose**: issue #34 の修正の記録。既存 FR (FR-037, FR-038) の実装バグであり、仕様の追加・
変更は伴わない。修正は `bug` 拡張のフローで行った (経緯・検証結果は
`.specify/bugs/menu-check-marks/` を参照)

- issue #34: コードで組み立てている右クリックメニューの項目 (配置サブメニューと、タスクトレイの
  「位置ロック」「最前面表示」) で、チェックマークが表示されない。Fluent テーマの `MenuItem` は、
  `IsCheckable` が true の項目にしかチェックの枠を表示しないため。公開リポジトリの README 用に
  スクリーンショットを撮った際に見つかった

- [X] T091 [US3] `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` の配置サブメニューの選択肢に
      `IsCheckable = true` を付ける (issue #34)
- [X] T092 [US3] `src/OkidokeiWidget.App/App.xaml.cs` の `BuildTrayContextMenu()` の
      「位置ロック」「最前面表示」に `IsCheckable = true` を付ける (issue #34)

**Checkpoint**: `dotnet build`・`dotnet test` が成功し、本体の「配置 ▶ 横位置」で今の配置に、
タスクトレイの「位置ロック」に、チェックが表示されること (実機で撮影して確認済み)

---

## Phase 16: ウィジェットのサイズが変わるとアンカー指定の位置がずれるバグの修正 (2026-09-25)

**Purpose**: issue #36 の修正の記録。既存の成功基準 (SC-007) を満たせていなかった実装バグであり、
仕様の追加・変更は伴わない。修正は `bug` 拡張のフローで行った (経緯・検証結果は
`.specify/bugs/anchor-width-change/` を参照)

- issue #36: 右上アンカーのウィジェットで、表示文字列の変化 (曜日の変化、秒表示の ON/OFF 等) により
  幅が変わると、変化分だけ位置がずれる。`SizeToContent` のウィンドウでは、`SizeChanged` の時点では
  Win32 側のウィンドウがまだ変化前のサイズで、`GetWindowRect` が古いサイズを返すため
  - 位置ロック中にずれたことで見つかった

- [X] T093 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `SizeChanged` での再配置を、
      `Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyPlacement)` 経由にして、
      リサイズが終わってから配置し直す (issue #36)

**Checkpoint**: `dotnet build`・`dotnet test` が成功し、右上アンカーのまま秒表示を ON/OFF しても
右端の余白が変わらないこと (修正前に実機で再現し、修正後に解消したことを確認済み)

---

## Phase 17: 表示中のモニタとの HDMI 接続が切れるとアプリが落ちるバグの修正 (2026-09-26)

**Purpose**: issue #41 の修正の記録。既存の Edge Case (稼働中のモニタ切断時は自動非表示、設定は
保持) を満たせていなかった実装バグであり、仕様の追加・変更は伴わない。修正は `bug` 拡張のフローで
行った (経緯・検証結果は `.specify/bugs/hdmi-disconnect-crash/` を参照)

- issue #41: 拡大率の異なるモニタとの HDMI 接続が切れると、再接続時に `UCEERR_RENDERTHREADFAILURE`
  で落ちる
  - 取り外しで Windows がウィンドウを別の DPI のモニタへ移すと、`DpiChanged` が起きる
  - その中で同期的にモニタを列挙し直すため、処理中のウィンドウ自身を閉じてしまい、描画スレッドが
    異常終了していた
  - 画面ロック中に TV (Android OS 搭載) 側で接続が切れて見つかった
- 最初はソフトウェア描画への切り替えで直ると見たが、効果がなかった
  - 診断用ビルドで呼び出し履歴を記録して原因を特定し直した

- [X] T094 [US4] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `DpiChanged` での
      `onDpiChanged` の呼び出しを、`Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ...)` 経由に
      して、`WM_DPICHANGED` の処理が終わってから実行する (issue #41、T073 の修正)

**Checkpoint**: `dotnet build`・`dotnet test` が成功し、次のことを実機で確認済み
- TV の Android OS を再起動しても落ちず、再接続後に TV 側のウィジェットが元の位置に再表示される
  (修正前に実機で再現し、修正後に解消したことを確認)
- 位置ロック中に拡大率を変えても位置がずれない (issue #25 の回帰なし)

---

## Phase 18: ドラッグで動かせる範囲をモニタの作業領域内に限る (2026-09-26)

**Purpose**: issue #39 を受けた FR-009 の改訂・SC-008 の新設と、FR-036 の明確化に対応する。
すべて User Story 3「配置を自由に決め、誤操作から保護する」に属する。constitution v1.5.0 の
小さな変更として、specify から implement までを 1 ブランチ (`feature/drag-within-monitor`)・
1PR で進める。設計は plan.md の「既存実装に対する変更計画 (2026-09-26、issue #39)」と
research.md #18 に従う

- issue #39: 拡大率の異なるモニタの境界をドラッグでまたぐと落ちていた。PR #42 (issue #41) の後は
  落ちないが、揺れてマウスを離すと元の位置へ戻る
- ドラッグ中の位置を物理ピクセルで計算し、作業領域内に収めてから動かすことで、ウィンドウが別の
  モニタへ出なくなり、`DpiChanged` による引き戻しが起きなくなる

**Independent Test**: 位置ロックを OFF にしてウィジェットを隣のモニタやタスクバーの方へドラッグし
続けると、作業領域の端で止まり、揺れず、マウスを離すと止まった位置に留まること

### Core: 作業領域内へ収める計算

- [X] T095 [US3] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` に、絶対座標
      (物理ピクセル) をモニタの作業領域内へ収める公開メソッド
      `ClampToWorkArea(ConnectedMonitor monitor, int x, int y, int widgetWidth, int widgetHeight)` を
      追加する。中身は `ToAbsolutePosition` の末尾にある `Clamp` 2 回分をそのまま移したもので、
      ウィジェットが作業領域より大きい場合は左上を優先する既存の規則を保つ。`ToAbsolutePosition` は
      サイズ未確定の 0 を最低 1 px にしてから、このメソッドを呼ぶ形にする (結果は変えない。
      research.md #18)
- [X] T096 [US3] `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` に
      `ClampToWorkArea` のテストを追加する。作業領域の内側ならそのまま返す、左・右・上・下それぞれ
      の外なら端に止まる、作業領域の原点が負 (プライマリの左や上にあるモニタ) でも正しく収める、
      ウィジェットが作業領域より大きいときは左上に揃える、の各ケース。既存の `ToAbsolutePosition`
      のテストがすべて通ることも確認する (T095 に依存)

### App: ドラッグ

- [X] T097 [P] [US3] `src/OkidokeiWidget.App/WindowPositionHelper.cs` に、マウスの画面座標を
      物理ピクセルで返す `TryGetCursorPosition()` を追加する。`user32.dll` の `GetCursorPos` を
      P/Invoke し、失敗した場合は `null` を返す。既存の `RECT` と同じく、`POINT` 構造体を
      クラス内に private で定義する
- [X] T098 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` のドラッグ処理を、物理ピクセルで
      計算する形に書き換える (T095・T097 に依存)
      - フィールド `_dragLastPointerPosition` (`Point`、DIP) を、つかんだ位置 `_dragGrabOffset`
        (`(int X, int Y)`、物理ピクセル) に置き換える
      - `BackgroundBorder_PreviewMouseLeftButtonDown`: 位置ロック中は今までどおり何もしない。
        `WindowPositionHelper.TryGetCursorPosition()` と `TryGetBounds(this)` の差を
        `_dragGrabOffset` に入れてから `_isDragging = true` と `CaptureMouse()` を行う。どちらかが
        取れない場合はドラッグを始めない
      - `BackgroundBorder_MouseMove`: `Left`/`Top` は使わない。`マウスの画面座標 - _dragGrabOffset`
        を `WidgetPlacementCalculator.ClampToWorkArea(_monitor, ...)` で作業領域内に収め、
        `WindowPositionHelper.MoveTo` で動かす。ウィジェットのサイズは `TryGetBounds(this)` の
        幅・高さを使う
      - `ApplyPlacement`: ドラッグ中 (`_isDragging` が true) は何もせずに戻る。ドラッグ中に
        ウィジェットのサイズが変わると (等幅でない数字のフォントで秒表示が ON のとき等)、
        `SizeChanged` から保存済みの位置 (まだドラッグ前の位置) やアンカーの位置へ引き戻され、
        揺れの原因になるため (SC-008、`/speckit-analyze` の指摘 C1)。マウスを離した時点で
        今の位置が保存されるので、ドラッグ後に配置し直す必要はない
      - `BackgroundBorder_MouseLeftButtonUp`: 変えない。位置が変わったかどうかに関係なく、
        今の位置を保存して `Anchor` を null に戻す (FR-036、spec.md の Clarifications)
      - コメントには、境界をまたがせないことで `DpiChanged` による引き戻しを避けている理由
        (issue #39) と、DIP ではなく物理ピクセルで計算する理由 (issue #10) を残す

### 確認

- [X] T099 [US3] `dotnet build`・`dotnet test` が成功することを確認し、`quickstart.md` の
      「US3: 配置とロック」→「ドラッグで動かせる範囲」の手動シナリオを人間に実施してもらう。
      あわせて既存の「アンカー指定」のシナリオのうち、ドラッグに関わるもの (FR-036) と、位置ロック中は
      ドラッグで動かないこと (SC-004) を確認する。結果を本 Phase の末尾に記録する (T095〜T098 に依存)

**Checkpoint**: `dotnet test` が全件成功し、quickstart.md の「ドラッグで動かせる範囲」のシナリオが
すべて期待どおりに動作する状態。issue #39 の再現手順で、落ちない・揺れない・元の位置へ戻らないこと

### Phase 18 の確認結果 (2026-09-26)

- `dotnet build` (Debug、警告 0、エラー 0)、`dotnet test` (78 件すべて合格。Phase 18 で 8 件を追加)
- 人間が Debug 版を実機で起動し、quickstart.md の「ドラッグで動かせる範囲」のシナリオを確認した。
  余白に関する 2 点を除き、問題なし
  - 端で止まる・端に沿って動く・内側へ戻すと再び追従する
  - 225% のモニタとの境界の手前で止まり、揺れず、落ちず、離した位置に留まる (issue #39 の解消)
  - 再起動後の復元、位置ロック中はドラッグで動かないこと (SC-004)
- 次の 2 点に違和感があったが、いずれも issue #26 で決めた仕様どおりの動き (research.md #16) で、
  本 Phase の不具合ではない。FR-035・FR-036 の見直しとして issue #43 に切り出した
  - ドラッグで自由配置になっても、「余白」のチェックが外れない
  - 自由配置のまま端に付けて「余白」を選んでも、余白が入らない

---

## Phase 19: 自由配置のときも余白で縁から離せるようにする (2026-09-26)

**Purpose**: issue #43 を受けた FR-035 の改訂と FR-039 の新設に対応する。すべて User Story 3
「配置を自由に決め、誤操作から保護する」に属する。constitution v1.5.0 の小さな変更として、
specify から implement までを 1 ブランチ (`feature/margin-in-free-placement`)・1PR で進める。
設計は plan.md の「既存実装に対する変更計画 (2026-09-26、issue #43)」と research.md #19 に従う

- 自由配置中は、余白のチェックを付けない (横位置・縦位置と同じ扱い)
- 自由配置中に余白を選ぶと、縁までの距離がその余白以下の縁から、余白ぶん内側へ動かす。
  動かした後も自由配置のまま。範囲内の縁がない余白はグレーアウトする

**Independent Test**: 自由配置のウィジェットを右の縁に付けて「余白」→「狭め」を選ぶと、右の縁から
少し内側へ動き、縦の位置は変わらず、チェックも付かないこと。画面の中ほどでは余白の項目が
どちらもグレーアウトしていること

### Core: 余白で縁から離した位置の計算

- [X] T100 [US3] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` に、自由配置中に
      余白を選んだときの移動先を返す公開メソッド
      `ApplyMarginToNearEdges(ConnectedMonitor monitor, int x, int y, int widgetWidth, int widgetHeight, AnchorMargin margin, double dpiScale)`
      を追加する。戻り値は `(int X, int Y)?` (仮想デスクトップ上の絶対座標、物理ピクセル) (research.md #19)
      - 余白の物理ピクセルは、`ToAnchoredPosition` と同じ `余白 (DIP) × 拡大率` の四捨五入で求める。
        この換算は private メソッドに切り出し、`ToAnchoredPosition` からも使う (結果は変えない)
      - 横方向: 左の縁までの距離 (`x - WorkAreaX`) が余白以下なら `WorkAreaX + 余白` にする。
        そうでなく右の縁までの距離 (`WorkAreaX + WorkAreaWidth - (x + widgetWidth)`) が余白以下なら
        `WorkAreaX + WorkAreaWidth - widgetWidth - 余白` にする。どちらでもなければ `x` のまま
      - 縦方向も同じ (上、下の順)
      - 横・縦のどちらも範囲内の縁がなければ null を返す。それ以外は `ClampToWorkArea` で収めて返す
      - XML コメントに、メニューのグレーアウトの判定にも同じ結果を使うこと、向かい合う縁が両方とも
        範囲内の場合は仕様で定めていないこと (issue #45) を書く
- [X] T101 [US3] `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` に
      `ApplyMarginToNearEdges` のテストを追加する (T100 に依存)
      - 右の縁に接している (距離 0) → 右の縁から余白ぶん内側へ。縦の位置は変わらない
      - 距離がちょうど余白と同じ → 位置は変わらず、null ではない
      - 距離が余白 + 1 px → null
      - 右下の隅に接している → 右と下の両方から余白ぶん離れる
      - 左・上の縁でも同じように動く
      - 拡大率 1.5 で、狭め = 12 px・広め = 36 px として判定・移動する
      - 作業領域の原点が負 (プライマリの左や上にあるモニタ) でも正しく計算する

### App: メニューと余白の選択

- [X] T102 [P] [US3] `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` を変更する
      - `Build`・`Populate` に、余白の項目ごとに選べるかを返す `Func<AnchorMargin, bool> canSelectMargin`
        を追加する。余白の項目の `IsEnabled` は `!isLocked && canSelectMargin(value)` にする。横位置・
        縦位置の項目は今までどおり `!isLocked`
      - 余白のチェックは、`placement.Anchor` が null (自由配置中) ならどちらにも付けない。アンカー
        指定中は今までどおり `AnchorMargin` の側に付ける (FR-035)
      - クラスと `Populate` の XML コメントを、上の 2 点に合わせて更新する
- [X] T103 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` を変更する (T100・T102 に依存)
      - 余白が選べるかを返す `public bool CanSelectAnchorMargin(AnchorMargin margin)` を追加する。
        アンカー指定中は true。自由配置中は `TryGetBounds(this)` と `VisualTreeHelper.GetDpi(this)` で
        今の位置・サイズ・拡大率を取り、`ApplyMarginToNearEdges` が null でなければ true。位置が
        取れなければ false (位置ロックは `PlacementMenuBuilder` 側で扱うので、ここでは見ない)
      - `SetAnchorMargin`: 位置ロック中は今までどおり何もしない。アンカー指定中は今までどおり
        `AnchorMargin` を更新して `ApplyPlacement` し、保存する。自由配置中は `ApplyMarginToNearEdges`
        で移動先を求める。位置が取れない場合と、移動先が null の場合は何もしない。null でなければ
        `AnchorMargin` を更新し、移動先を `ToRelativePosition` で `X`/`Y` に入れてから
        `ApplyPlacement` し、保存する (`Anchor` は null のまま)
      - `SetAnchorMargin` の XML コメントの「自由配置中は値を保存するだけで、位置は変わらない
        (research.md #16)」を、今の動き (FR-039、research.md #19) に書き換える
      - `BackgroundBorder_ContextMenuOpening` で `PlacementMenuBuilder.Populate` に
        `CanSelectAnchorMargin` を渡す
- [X] T104 [US3] `src/OkidokeiWidget.App/App.xaml.cs` のトレイのメニューで、各モニタの
      `PlacementMenuBuilder.Build` に `window.CanSelectAnchorMargin` を渡す (T102・T103 に依存)

### 確認

- [X] T105 [US3] `dotnet build`・`dotnet test` が成功することを確認し、`quickstart.md` の
      「US3: 配置とロック」→「自由配置のときの余白」の手動シナリオを人間に実施してもらう。
      あわせて既存の「アンカー指定」のシナリオのうち、余白に関わるもの (アンカー指定中の余白の
      切り替え、ドラッグ後のチェック) と、位置ロック中は余白もグレーアウトすること (FR-010) を
      確認する。結果を本 Phase の末尾に記録する (T100〜T104 に依存)

**Checkpoint**: `dotnet test` が全件成功し、quickstart.md の「自由配置のときの余白」のシナリオが
すべて期待どおりに動作する状態。issue #43 の 2 つの違和感 (ドラッグ後もチェックが残る、端に
付けて余白を選んでも動かない) が解消していること

### Phase 19 の確認結果 (2026-09-26)

- `dotnet build` (Debug、警告 0、エラー 0)、`dotnet test` (92 件すべて合格。Phase 19 で 14 件を追加)
- 人間が Debug 版を実機で起動し、quickstart.md の「自由配置のときの余白」のシナリオを確認した。
  すべて問題なし
  - ドラッグ後は余白のチェックも外れる (issue #43 の 1 つ目の違和感の解消)
  - 端に付けて余白を選ぶと縁から離れ、隅では両方の縁から離れる (issue #43 の 2 つ目の違和感の解消)
  - 範囲内の縁がない余白のグレーアウト、本体とタスクトレイの表示の一致、次のアンカー指定での
    余白の引き継ぎ、再起動後の復元
- あわせて、アンカー指定中の余白の切り替えと、位置ロック中は余白もグレーアウトすること (FR-010) を
  確認し、問題なし

---

## Phase 20: 閉じたウィジェットの時計更新タイマーが止まらないバグの修正 (2026-09-28)

**Purpose**: issue #49 の修正の記録。constitution 原則 VI の「作ったリソース (タイマー、イベントの
購読など) は後始末する」を満たせていなかった実装バグであり、仕様の追加・変更は伴わない。修正は
`bug` 拡張のフローで行った (経緯・検証結果は `.specify/bugs/clock-timer-leak/` を参照)

- issue #49: 閉じた `ClockWindow` の `DispatcherTimer` が止まらず、ウィンドウが解放されずに毎秒の
  更新を続ける
  - モニタの取り外しや、詳細設定でのモニタの非表示のたびに 1 つずつ溜まる

- [X] T106 [US1] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` で `OnClosed` を override し、
      時計更新タイマーを止める (issue #49、T018 の修正)

**Checkpoint**: `dotnet build`・`dotnet test` が成功すること。見た目には変化がないため、実機では
モニタの表示/非表示の切り替えで落ちないことだけを確認する

---

## Phase 21: 詳細設定で自動起動を切り替えたときに失敗するとアプリが落ちるバグの修正 (2026-09-28)

**Purpose**: issue #18 の修正の記録。FR-019 (自動起動の ON/OFF) の実装で、失敗しても落ちない
(constitution 原則 VI) を満たせていなかった実装バグであり、仕様の追加・変更は伴わない。修正は
`bug` 拡張のフローで行った (経緯・検証結果は `.specify/bugs/autostart-toggle-crash/` を参照)

- issue #18: Startup フォルダへの書き込み失敗等でショートカットの作成・削除に失敗すると、未処理例外で
  アプリごと落ちる
  - 起動時の呼び出しは issue #20 の修正で削除済みで、詳細設定での切り替え時だけが残っていた

- [X] T107 [US1] `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs` に、失敗したら例外を
      投げずに false を返す `TrySetEnabled` を追加する (issue #18、T020 の修正)
- [X] T108 [P] [US1] `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs` に、
      ショートカットを保存できない場合・Startup フォルダを作れない場合に `TrySetEnabled` が false を
      返すテストを追加する (T107 に依存)
- [X] T109 [US4] `src/OkidokeiWidget.App/App.xaml.cs` の `OnAutoStartChanged` を `TrySetEnabled` に
      切り替え、失敗したら設定値を戻して MessageBox で知らせる。`SettingsWindow` はチェックの表示を
      設定値に合わせ直す (issue #18、T035 の修正) (T107 に依存)

**Checkpoint**: `dotnet build`・`dotnet test` が成功すること。実機では、Startup フォルダに
`OkidokeiWidget.lnk` という名前のフォルダを作った状態で自動起動をオンにし、落ちずにメッセージが
出てチェックが外れることを確認する

---

## Phase 22: 壊れた設定ファイルの上書き・一部が null の設定ファイルで落ちるバグの修正 (2026-09-28)

**Purpose**: issue #50・#51 の修正の記録。FR-018 (読めない設定ファイルでもデフォルト設定で起動し、
通知する) の実装で、constitution 原則 VI の「失敗しても落ちない・設定を失わない」を満たせていなかった
実装バグであり、spec.md の変更は伴わない。読み込み時の動きを定める `contracts/settings-file.md` は
実装に合わせて直した。修正は `bug` 拡張のフローで行った (経緯・検証結果は
`.specify/bugs/broken-settings-file/` を参照)

- issue #50: 読めない設定ファイルを、警告を出す前にデフォルト設定で上書きしてしまい、元の設定が失われる
- issue #51: 設定の一部が `null` だと、起動直後の保存で NullReferenceException になりアプリが落ちる
  - #51 だけ直すと「落ちる」が「設定が消える」(#50 の症状) に変わるだけなので、一緒に直した

- [X] T110 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `Load` で、
      `Appearance`・`WindowBehavior`・`Monitors`・各モニタの値の `null` をパース不可として扱う
      (issue #51、T011 の修正)
- [X] T111 [US1] `SettingsRepository` に、読めなかった設定ファイルを `settings.json.bak` として
      残す `BackupBrokenFile` を追加し、`App.OnStartup` で起動時の保存より前に呼ぶ。警告文に
      残した場所を書き添える (issue #50、T016 の修正) (T110 に依存)
- [X] T112 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、
      `null` を含む設定ファイルのフォールバックと、`.bak` が後の保存で上書きされないことのテストを
      追加する (T110, T111 に依存)
- [X] T113 [P] `specs/001-clock-widget/contracts/settings-file.md` の「読み込み契約」に、`null` の
      扱いと `.bak` を残すことを追記する

**Checkpoint**: `dotnet build`・`dotnet test` が成功すること。実機では、`settings.json` の末尾の
`}` を消した場合と `"Appearance": null` にした場合のそれぞれで、落ちずに警告が出て、
`settings.json.bak` に元の内容が残っていることを確認する

---

## Phase 23: 別の端子につなぎ直したモニタの設定を引き継ぐ (2026-09-28)

**Purpose**: issue #52 を受けた FR-040 の新設と SC-005 の改訂に対応する。すべて User Story 4
「マルチモニタ環境でモニタごとに管理する」に属する。constitution v1.6.0 の小さな変更として、
specify から implement までを 1 ブランチ (`feature/monitor-identity`)・1PR で進める。
設計は plan.md の「既存実装に対する変更計画 (2026-09-28、issue #52)」と research.md #20 に従う

- 設定ファイルのキーの形は変えない (端子ごとの値を含むデバイスインターフェース名のまま)
- キーが一致する設定がないモニタには、同じ型番の使われていない設定を、キーの順に 1 対 1 で
  割り当て、今のキーへ移す

**Independent Test**: 型番の違うモニタのケーブルを別の端子につなぎ直してアプリを起動すると、
つなぎ直す前と同じ位置にウィジェットが出ること。`settings.json` に古いキーが残っていないこと

### Core: 型番の取り出しと、同じ型番の設定の付け替え

- [X] T114 [P] [US4] `src/OkidokeiWidget.Core/Monitors/MonitorIdentifier.cs` に、ID から型番を
      取り出す公開メソッド `string? GetModel(string identifier)` を追加する (research.md #20)
      - ID を `#` で区切った 2 つ目の部分を返す。区切った結果が 3 つ未満 (`\\.\DISPLAY1` など)、
        または 2 つ目が空なら null を返す
      - `GetStableIdsByAdapterDeviceName` の XML コメントの「EDID 由来の安定したモニタ ID」を、
        「型番 (EDID 由来) と端子ごとの値からなる ID。端子を変えると変わる」という実態に合わせて直し、
        research.md #2 の訂正と #20 を参照する
- [X] T115 [P] [US4] `src/OkidokeiWidget.Core/Monitors/ConnectedMonitor.cs` の XML コメントの
      「`Identifier` は EDID 由来の安定した ID」を、T114 と同じく実態に合わせて直す
- [X] T116 [US4] `src/OkidokeiWidget.Core/Persistence/MonitorSettingsReconciler.cs` の `Reconcile` を、
      research.md #20 の 3 段で割り当てるように変える (T114 に依存)
      - 1: 接続中のモニタのうち、キーが `settings.Monitors` にあるものはそのまま使う
      - 2: 残ったモニタのうち `MonitorIdentifier.GetModel` が null でないものを、型番ごとに分ける
        (大文字・小文字も区別して比べる)。型番ごとに、同じ型番の保存済みの設定のうち、キーが接続中の
        どのモニタのキーとも一致しないものを集める。モニタ・設定をどちらもキーの順
        (`StringComparer.Ordinal`) に並べ、先頭から 1 対 1 に組み合わせる
      - 残ったモニタは、キーの重複を除いてから並べる (同じキーのモニタが複数あっても、組み合わせるのは
        1 回だけ。research.md #20 の 2 の補足、`/speckit-analyze` の 4 回目の指摘 H1)
      - 組み合わせた `MonitorPlacement` のインスタンスは今のキーへ移し、古いキーを消す。複製はしない
        (`ClockWindow` が同じインスタンスを持っているため。plan.md の変更計画)
      - 3: 組み合わせる設定がなかったモニタと、型番が null のモニタには、今までどおりデフォルト値を足す
      - クラスの XML コメントに、この探し方と FR-040・research.md #20 への参照を足す
- [X] T117 [P] [US4] `tests/OkidokeiWidget.Core.Tests/Monitors/MonitorIdentifierTests.cs` を追加し、
      `GetModel` のテストを書く (T114 に依存)
      - 実際の形の ID (`\\?\DISPLAY#SNYAE04#5&3b7d6ecd&0&UID4352#{e6f07b5f-...}`) → `SNYAE04`
      - `\\.\DISPLAY1` (ID が取れなかったとき) → null
      - 2 つ目が空の ID → null
      - `#` で 2 つにしか区切れない ID (`\\?\DISPLAY#SNYAE04`) → null
      - 末尾が `#` で終わる ID (`\\?\DISPLAY#SNYAE04#`) → 3 つに区切れるので `SNYAE04`
- [X] T118 [US4] `tests/OkidokeiWidget.Core.Tests/Persistence/MonitorSettingsReconcilerTests.cs` に、
      research.md #20 の場合分けのテストを追加する (T116 に依存)
      - 型番の違うモニタを別の端子につなぎ直した → 前の設定 (位置・表示/非表示・アンカー) が
        新しいキーで使われ、古いキーは消える。移した後もインスタンスが同じである
      - 型番の違う 2 台の端子を入れ替えた → それぞれの設定が入れ替わった先のキーへ移る
      - 同じ型番の 2 台が端子ごとに設定を持っている → どちらもそのまま使われる
      - 1 台だけだった型番に、同じ型番がもう 1 台増えた → 同じ端子の方は前の設定、もう 1 台はデフォルト値。
        接続中のモニタの並び順に左右されないことを確かめるため、新しい方を先に並べた場合も試す
      - 1 台だけだった型番のモニタを別の端子へ移し、同時に同じ型番をもう 1 台つないだ (どちらも前の
        端子ではない) → キーの順で先のモニタが前の設定を使い、もう 1 台はデフォルト値
      - 同じ型番の 2 台の設定があり、1 台を外して、残りを外した方が使っていた端子へつないだ → その端子の
        設定が使われる (spec.md の Edge Cases)
      - 同じ型番の 2 台の設定があり、1 台だけが別の端子 (どちらの設定もない端子) につながった → 2 つの
        設定のうちキーの順で先の方が移される。もう 1 つは古いキーのまま残る
      - 同じ型番の 2 台を、2 台とも別の端子につなぎ直した → 2 つの設定がキーの順に 1 つずつ移され、
        デフォルト値は足されない (spec.md の US4 シナリオ 7)
      - 型番の違う 2 台が、それぞれの型番の中で組み合わされ、別の型番の設定を使わない
      - 同じ型番の 2 台の設定 (K1・K2) があり、C は K1 のまま、D だけ設定のない端子 (K3) へ移した →
        C は K1 をそのまま使い、D には K2 が移される (使われている K1 を横取りしない。`/speckit-analyze`
        の 4 回目の指摘 M1)
      - 同じ型番の設定が 2 つ (K1・K2) あり、接続中の 2 台が同じキー K3 になっている (research.md #20 の
        未確認の前提 3 が外れた場合) → K3 には K1 だけが移され、K2 は古いキーのまま残る。どの設定も
        失われない (指摘 H1)
      - 型番が取れない ID (`\\.\DISPLAY1`) → 今までどおりデフォルト値
      - 以前のバージョンの設定ファイルと同じキーのまま接続されている → 何も変わらない (既存の
        「既存エントリのあるモニタは上書きしない」テストで押さえられていることを確かめる)

### 記録と確認

- [X] T119 [P] [US4] `specs/001-clock-widget/checklists/` 以外で、モニタの ID を「EDID 由来の安定した
      ID」と書いているところが残っていないかを `grep` で確かめ、見つかれば直すか訂正を注記する。
      `tasks.md` の完了済みタスクと、research.md #2・plan.md の最初の Constitution Check と
      Primary Dependencies は当時の記録として本文を直さない (訂正を注記済み。`/speckit-analyze` の指摘 S1)
- [X] T120 [US4] `dotnet build`・`dotnet test` が成功することを確認し、`quickstart.md` の
      「US4: マルチモニタでの独立管理」のうち、ケーブルのつなぎ直しと以前のバージョンの設定ファイルの
      シナリオを人間に実施してもらう。結果を本 Phase の末尾に記録する (T114〜T119 に依存)
      - 始める前に、`settings.json` の `Monitors` に、型番ごとの設定が 1 つずつしかないことを確かめる。
        同じ型番の設定が 2 つあると、Edge Cases のとおり本来とは別の設定が割り当てられることがあり、実装の不具合と見分けがつかない
      - 開発機では、K1 の確認で DELL を別の端子 (`UID4355`) に挿し替えた後、アプリを起動していない。
        `settings.json` の DELL の設定は前の端子 (`UID4353`) のキーのままなので、新しい版を起動する
        だけで、別の端子へのつなぎ直しを確かめられる。自動起動などで v1.2 が起動してしまい、DELL の設定が 2 つ (`UID4353` と `UID4355`) になっていたら、`UID4355` の方を消してから始める
      - research.md #20 の 2 つ目の前提 (端子を変えると ID の端子ごとの部分が変わる) は、
        `/speckit-analyze` の指摘 K1 を受けて実装の前に確かめた。ここでは、新しい版で古いキーが
        新しいキーへ移ることを `settings.json` で確かめる
      - 同じ型番のモニタ 2 台の場合 (US4 のシナリオ 5・7) は、T118 の単体テストでのみ確かめる。
        実機では未確認のまま、と記録する

**Checkpoint**: `dotnet test` が全件成功し、型番の違うモニタを別の端子につなぎ直しても、前の
表示/非表示・位置が引き継がれる状態。以前のバージョンの `settings.json` のままアップデートしても、
各モニタの位置が変わらないこと

### Phase 23 の確認結果 (2026-09-28)

- `dotnet build` (Debug、警告 0、エラー 0)、`dotnet test` (119 件すべて合格。Phase 23 で 18 件を追加)
- 始める前に、`settings.json` の `Monitors` が型番ごとに 1 つずつ (SONY `UID4352`・DELL `UID4353`) で
  あることを確かめた。DELL は K1 の確認で別の端子 (`UID4355`) につないだまま、アプリは起動していなかった
- 人間が Debug 版を実機で起動した。SONY・DELL とも、今までどおりの位置にウィジェットが出た
  - DELL の設定 (X=737, Y=9, 右上のアンカー) は、同じ値のまま `UID4355` のキーへ移り、`UID4353` の
    キーは残っていなかった (US4 のシナリオ 4)
  - SONY は、以前のバージョンの設定のまま、今までどおりの位置に出た (US4 のシナリオ 6)
- アプリを起動したまま、人間が DELL のケーブルを元の端子に戻した。ウィジェットの位置は変わらず、
  設定は `UID4353` のキーへ戻り、`UID4355` のキーは残っていなかった (起動中のつなぎ直し)
- 同じ型番のモニタ 2 台の場合 (US4 のシナリオ 5・7) は、T118 の単体テストでのみ確かめた。実機では未確認

---

## Phase 24: コードで作る色のブラシを Freeze していない問題の修正 (2026-09-28)

**Purpose**: issue #15 の修正の記録。FR-005〜FR-008 (見た目のカスタマイズ) の実装で、作った後に
変更しないブラシを変更可能なまま使っていた。見た目の変化はなく、仕様の追加・変更は伴わない。修正は
`bug` 拡張のフローで行った (経緯・検証結果は `.specify/bugs/freeze-brushes/` を参照)

- issue #15: `ClockWindow.ApplyAppearance()` で作る文字色・背景色の `SolidColorBrush` に `Freeze()` していない
  - 同じ作りの `ColorPickerWindow` のパレットとプレビューのブラシも、あわせて直した

- [X] T121 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `ApplyAppearance()` で、文字色 2 つと
      背景色のブラシを Freeze してから使う (issue #15、T023・T055 の修正)
- [X] T122 [P] [US2] `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs` で、パレットとプレビューの
      ブラシを Freeze してから使う (issue #15)

**Checkpoint**: `dotnet build`・`dotnet test` が成功すること。見た目には変化がないため、実機では
文字色・背景色・背景透過度の変更と、色の選択画面の表示が今までどおり動くことだけを確認する

---

## Phase 25: 日付の区切り文字に「.」と任意の文字を使えるようにする (2026-09-28)

**Purpose**: issue #60 を受けた FR-026 の改訂に対応する。User Story 2「見た目を自分好みにカスタマイズする」に
属する。小さな変更として、specify から implement までを 1 ブランチ (`feature/date-separator`)・1PR で進める。
設計は plan.md の「既存実装に対する変更計画 (2026-09-28、issue #60)」と research.md #21 に従う

- 設定ファイルの形は変えない (`DateSeparator` は今までどおり文字列 1 つ)
- T049 (`DateSeparatorResolver` の許可リスト) は当時の記録として本文を直さない。本 Phase で置き換える

**Independent Test**: 詳細設定画面で「.」を選ぶと `2026.09.28` になること。アプリを終了して
`settings.json` の `DateSeparator` を `"🍣"` にすると `2026🍣09🍣28` と表示され、詳細設定画面の区切り文字の
欄が空欄になり、選び直さない限り `"🍣"` のまま残ること

### Core: 区切り文字の扱いと日付の文字列

- [X] T123 [US2] `src/OkidokeiWidget.Core/Settings/DateSeparatorResolver.cs` の `Resolve` と、
      `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` の `DateSeparator` を、FR-026 の改訂に合わせて変える
      (research.md #21)
      - `AppearanceSettings.DateSeparator` の型を `string?` にする。既定値は今までどおり `"/"`
        (設定ファイルから `null` が入り、そのまま保存し直されるため。`/speckit-analyze` の指摘 I1)
      - `Resolve` の引数を `string?` にし、`null` のときだけ `DefaultSeparator` (「/」) を返す。それ以外 (空欄・選択肢に
        ない文字列を含む) はそのまま返す
      - 許可リスト `AllowedSeparators` と `using System.Linq;` を消す (Core は `ImplicitUsings` が有効なので、using はもともと不要)
      - XML コメントを data-model.md の「詳細設定画面の選択肢は `/`・`-`・`.`。設定ファイルでは任意の文字列
        (空欄を含む) を使える。`null` のときだけデフォルト (`/`) にフォールバック」に合わせて直す
- [X] T124 [US2] `src/OkidokeiWidget.Core/Settings/DateTextFormatter.cs` を追加し、`DateTime` と区切り文字
      (`string?`) から日付の文字列を作る `Format` を実装する (T123 に依存)
      - 区切り文字は `DateSeparatorResolver.Resolve` を通す
      - 年は 4 桁、月・日は 2 桁の数字にし、`年{区切り}月{区切り}日` の順につなぎ合わせる。書式文字列に
        区切り文字を埋め込まない (`y`・`M`・`d` などが書式の記号として解釈されないようにするため)
      - 数字は地域設定によらず 0〜9 で出す (`CultureInfo.InvariantCulture` を使う)
      - クラスの XML コメントに、つなぎ合わせる理由と FR-026・research.md #21 への参照を書く
- [X] T125 [P] [US2] `tests/OkidokeiWidget.Core.Tests/Settings/DateSeparatorResolverTests.cs` を書き直す
      (T123 に依存)
      - 「/」「-」「.」、空欄、`//`・`🍣` はそのまま返す
      - `null` は `DefaultSeparator` を返す (`[InlineData(null)]` は Nullable の警告 xUnit1012 が出るので、別の `[Fact]` にする)
      - 今の「許可されていない値はデフォルトを返す」テストは消す (FR-026 の改訂で成り立たなくなるため)
- [X] T126 [P] [US2] `tests/OkidokeiWidget.Core.Tests/Settings/DateTextFormatterTests.cs` を追加する (T124 に依存)
      - 2026-09-28 を「/」で `2026/09/28`、「.」で `2026.09.28`、「-」で `2026-09-28` にする
      - 月・日が 1 桁の日付 (2026-01-05) を「/」で `2026/01/05` にする
      - 空欄で `20260928`、`null` で `2026/09/28` にする
      - `🍣` で `2026🍣09🍣28` にする
      - 書式の記号になる `d`・`M`・`y`・`'`・`\`・`%`・`:` と、複数文字の `年` や `--` が、そのまま区切りとして出る
- [X] T127 [P] [US2] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、区切り文字の
      読み書きのテストを追加する (T123 に依存。`/speckit-analyze` の指摘 C1)
      - `"DateSeparator": null` のファイルを読むと、`FellBackToDefaults` が false で、`DateSeparator` が `null` になる
      - `DateSeparator` の項目がないファイルを読むと、`DateSeparator` が `"/"` になる
      - `DateSeparator` が `null`・空欄・`🍣` の設定を `Save` して `Load` し直すと、同じ値になる
        (`🍣` は `\uXXXX` の形で書き出されてよい。contracts/settings-file.md)

### App: 表示と詳細設定画面

- [X] T128 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `UpdateClockText` で、日付の表示を
      `DateTextFormatter.Format(now, appearance.DateSeparator)` で作る (T124 に依存)
      - 今ある `DateSeparatorResolver.Resolve` の呼び出しと、`now.ToString($"yyyy{separator}MM{separator}dd")` による
        組み立ては消す (`Resolve` は `Format` の中で通すため。`/speckit-analyze` の指摘 U1)
- [X] T129 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` の区切り文字の欄を変える (T123 に依存)
      - `DateSeparatorOptions` に「.」を足し、「/」「-」「.」の 3 つにする
      - 初期化時、`DateSeparatorResolver.Resolve(appearance.DateSeparator)` が選択肢にあればそれを、なければ
        `null` を `DateSeparatorCombo.SelectedItem` に入れる (選択肢にない値をそのまま入れると前の選択が残る。
        research.md #21 の前提 2)。`null` や項目がない設定は `Resolve` で「/」になるので、「/」を選んだ状態になる
      - `DateSeparatorCombo_SelectionChanged` は変えない (初期化中は `_isInitializing` で無視し、利用者が
        選んだときだけ保存する)

### 確認

- [X] T130 [US2] `dotnet build`・`dotnet test` が成功することを確認し、`quickstart.md` の「US2: 見た目の
      カスタマイズ」の「日付の区切り文字」のシナリオを人間に実施してもらう。結果を本 Phase の末尾に記録する
      (T123〜T129 に依存)

**Checkpoint**: `dotnet test` が全件成功し、「.」と、設定ファイルに書いた任意の区切り文字で日付が表示される状態。
詳細設定画面を開いたり、ほかの項目を変えたりしても、設定ファイルに書いた区切り文字が消えないこと

### Phase 25 の確認結果 (2026-09-28)

- `dotnet build` (Debug、警告 0、エラー 0)、`dotnet test` (142 件すべて合格。Phase 25 で 23 件を追加)
- 確認の前に、開発機の `settings.json` の控えを scratchpad に取った。確認の後、区切り文字は元の「-」に戻した
- 人間が Debug 版を実機で操作した。quickstart.md の US2「日付の区切り文字」のシナリオはすべて期待どおりだった
  - 詳細設定画面で「.」を選ぶと、日付が `2026.09.28` の形になった
  - `DateSeparator` を `"🤣"` に書き換えて起動すると (🍣 の代わりに使った)、日付が `2026🤣09🤣28` と表示された
    - 起動した時点で、`settings.json` の値は `"\uD83E\uDD23"` の形に書き換わった (contracts/settings-file.md のとおり)
    - 詳細設定画面の区切り文字の欄は空欄だった。ほかの項目を変えて閉じても、値は `"\uD83E\uDD23"` のままだった
    - 区切り文字の欄で「/」をマウスで選ぶと、日付が `2026/09/28` になり、`settings.json` も `"/"` になった。
      research.md #21 の前提 3 のうち未確認だった「マウスで選んだ場合」も、これで確かめられた
  - `DateSeparator` を `null` にして起動すると、日付は `2026/09/28` で、詳細設定画面の欄は「/」を選んだ状態だった。
    起動後も、ほかの項目を変えて閉じた後も、`settings.json` は `null` のままだった
- 最初の確認では、アプリを起動したまま `settings.json` を書き換えていたため、アプリを終了してから起動し直した
  (設定ファイルはアプリを終了してから書き換える前提。spec.md の Assumptions)
---

## Phase 26: 見た目・位置ロック・最前面表示をモニタごとに持つ (2026-10-01)

**Purpose**: issue #53 (見た目の設定をモニタごとに持つ) と issue #17 (詳細設定画面を開いたままモニタを
抜き差ししても一覧が更新されない) を受けた FR-041・FR-042 の新設と、FR-010〜FR-014・FR-038・FR-040 などの
改訂に対応する。設計は plan.md の「既存実装に対する変更計画 (2026-10-01、issue #53・#17)」と research.md #22〜#24 に従う

- 設定ファイルの形が変わるため、constitution の小さな変更には当たらない。specify・plan・tasks は別々の PR で進めた
- タスクは主に User Story 4 (マルチモニタ) に属する
  - US3 のシナリオ 13・14 (タスクトレイのメニュー) と US1 のシナリオ 4 (自動起動) の変更も含む
  - 位置ロック・最前面表示をモニタごとにすると、今のタスクトレイのメニュー (全体の位置ロックを切り替える) が成り立たなくなる。
    なので、US4 と切り離して実装できない
  - implement は、この Phase 全体を 1 ブランチ・1PR で進める (US4 の PR として扱う)

**Independent Test**: 2 台のモニタで、モニタ A のウィジェットの見た目・位置ロック・最前面表示を変えても
モニタ B が変わらず、再起動後もそれぞれ復元されること。以前のバージョンの `settings.json` のまま起動すると、
つながっているモニタはアップデート前と同じ見た目・表示で出ること。タスクトレイのメニューが
「詳細設定...」「自動起動」「終了」だけであること

### Core: 設定の形と引き継ぎ

- [X] T131 [P] [US4] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` と
      `src/OkidokeiWidget.Core/Settings/WindowBehaviorSettings.cs` に、複製を返す
      `public AppearanceSettings Clone()`・`public WindowBehaviorSettings Clone()` を足す (research.md #22)
      - 中身は値型と文字列だけなので、`(AppearanceSettings)MemberwiseClone()` で足りる
      - XML コメントに、以前のバージョンの設定を各モニタへ引き継ぐときに、モニタごとに別のインスタンスにする
        ためのもの (SC-009) と書く
- [X] T132 [P] [US4] `src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs` に、次の 2 つを足す (data-model.md の `MonitorPlacement`)
      - `public AppearanceSettings Appearance { get; set; } = new();`
        - data-model.md: 「このモニタの表示設定 (FR-041、2026-10-01 追加)。項目がないときは既定値。明示的な `null` は壊れたファイルとして扱う (issue #51 と同じ)」
      - `public WindowBehaviorSettings WindowBehavior { get; set; } = new();`
        - data-model.md: 「このモニタのウィンドウ挙動設定 (FR-041、2026-10-01 追加)。項目がないとき・`null` のときの扱いは `Appearance` と同じ」
      - `IsVisible` の初期値 (`true`) は変えない。以前のバージョンの設定ファイルで `IsVisible` を書いていないエントリはないが、
        今の読み込みの挙動を変えないため。新しいモニタを非表示にするのは T135 の `Reconcile` で行う
- [X] T133 [US4] `src/OkidokeiWidget.Core/Settings/WidgetSettings.cs` の `Appearance`・`WindowBehavior` を、以前のバージョンの
      設定ファイルを読むためだけのものに変える (research.md #22、data-model.md の `WidgetSettings`)
      - 型を `AppearanceSettings?`・`WindowBehaviorSettings?` にし、初期値を null にする (`= new()` を外す)
      - 両方に `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` を付ける。JSON の名前は今と同じ (`Appearance`・`WindowBehavior`)
      - XML コメントに、data-model.md の説明「以前のバージョン (2026-10-01 より前) の設定ファイルにある、アプリ全体で共通の
        表示設定。読み込んだ後、`Reconcile` で各モニタへ引き継いでから null にする。null のときは書き出さない」を書く
      - `using System.Text.Json.Serialization;` を足す
      - この変更の後も、`OkidokeiWidget.App` の `_settings.Appearance`・`_settings.WindowBehavior` を使うところはビルドが通る。
        null 参照の警告 (CS8602) が出るだけで、エラーにはならない (`TreatWarningsAsErrors` を指定していないため)。
        直し漏れはビルドでは止まらず実行時に落ちるので、T134・T139〜T141・T143 で直したうえで、T146 で警告 0 を確かめる
        (`/speckit-analyze` の指摘 H1)
- [X] T134 [US4] `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `HasNullSection` を、contracts/settings-file.md の
      読み込み契約に合わせて変える (T132・T133 に依存)
      - ルートの `settings.Appearance is null`・`settings.WindowBehavior is null` の判定を外す
        (ルートの 2 つは、以前のバージョンのファイルでなければ null なのが正しいため)
      - 各モニタの値について、`placement is null` に加えて `placement.Appearance is null`・`placement.WindowBehavior is null` を壊れたファイルとして扱う
      - XML コメントの issue #51 の説明に、2026-10-01 の改訂 (ルートの 2 つは null を許す) を足す
      - `Save` の先頭の `settings.Appearance.BackgroundOpacity` のクランプを、各モニタの `placement.Appearance.BackgroundOpacity`
        に対して行うように変える。ルートの `settings.Appearance` は、null でないときだけクランプする
        (今のままだと、ルートが null になった後の起動時の `Save` で落ちる。`/speckit-analyze` の指摘 C1、contracts/settings-file.md の書き込み契約)
- [X] T135 [US4] `src/OkidokeiWidget.Core/Persistence/MonitorSettingsReconciler.cs` の `Reconcile` を、research.md #22 の手順に
      変える (T131〜T133 に依存)
      - 最初に、`settings.Monitors.Count == 0` かどうかを覚えておく (`wasEmpty`)
      - 1・2: キーの一致と FR-040 の付け替えは今のまま
      - 引き継ぎ: `settings.Appearance` または `settings.WindowBehavior` が null でなければ (以前のバージョンのファイル)、
        `settings.Monitors` のすべてのエントリについて次を行う
        - `settings.Appearance` が null でなければ `placement.Appearance = settings.Appearance.Clone()`。`WindowBehavior` も同じ
        - キーが接続中のどのモニタの `Identifier` とも一致しないエントリは `placement.IsVisible = false`。一致するエントリは変えない
        - 終わったら `settings.Appearance = null`・`settings.WindowBehavior = null`
        - この時点の `Monitors` には、1・2 で今のキーへ移したエントリも入っている。そのため、別の端子につなぎ直したモニタは「つながっている」と扱われる
      - 3: 割り当てるエントリがなかったモニタには、`IsVisible = false`、位置は今と同じ中央寄せ、見た目・ウィンドウ挙動は既定値 (`new()`) のエントリを足す
      - `wasEmpty` なら、足したエントリのうち、`ConnectedMonitor.IsPrimary` のモニタのものだけ `IsVisible = true` にする。
        `IsPrimary` のモニタがなければ、`connectedMonitors` の先頭のモニタのものを `IsVisible = true` にする
      - クラスと `Reconcile` の XML コメントを、引き継ぎ・新しいモニタを非表示で足すこと・`wasEmpty` の扱い
        (`Monitors` のエントリは消さないので、空なのは起動時だけ。research.md #22) に合わせて直す

### Core: テスト

- [X] T136 [P] [US4] `tests/OkidokeiWidget.Core.Tests/Settings/SettingsCloneTests.cs` を追加する (T131 に依存)
      - `AppearanceSettings` のすべての項目を既定値と違う値にしてから `Clone()` し、すべての項目が同じ値で、別のインスタンスであることを確かめる
      - 複製の `TimeFontSize`・`DateSeparator` などを変えても、元が変わらないことを確かめる
      - `WindowBehaviorSettings` も同じ (`TopMost`・`PositionLocked`)
- [X] T137 [US4] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` を、contracts/settings-file.md の
      読み込み契約・書き込み契約・後方互換性に合わせて変える (T132〜T134 に依存)
      - 以前のバージョンの形のファイル (ルートに `Appearance`・`WindowBehavior`、各モニタにはない) が、壊れたファイル扱いに
        ならずに読める。ルートの値が `settings.Appearance`・`settings.WindowBehavior` に入り、各モニタの値は既定値である
      - 新しい形のファイルを保存して読み直すと、各モニタの `Appearance`・`WindowBehavior` が同じ値になる。
        保存したファイルの JSON のルートに `Appearance`・`WindowBehavior` のキーがない
      - 各モニタの `"Appearance": null`・`"WindowBehavior": null` のファイルは、壊れたファイルとして扱い、通知のフラグを立てる
      - 今の `Load_設定の一部がnullの場合はデフォルト値へフォールバックし通知フラグを立てる` の `{ "Appearance": null }`・
        `{ "WindowBehavior": null }` のケースは、FR-041 の改訂で成り立たなくなるので外す。代わりに、ルートの 2 つが `null` の
        ファイルは壊れたファイル扱いにならないテストを足す
      - 今の `Appearance` を使うテスト (`DateSeparator` の `null` など) は、ルートの `Appearance` を読むテストとして残すか、
        各モニタの `Appearance` を読むテストに書き換える (どちらでも、`null`・空欄・絵文字を失わないことを確かめる。T127 の意図を保つ)
      - ルートの `Appearance` の初期値が null になるため、次のテストも壊れる。各モニタの値を見る形に書き換える (`/speckit-analyze` の指摘 H2)
        - `Load_ファイルが存在しない場合は…`・`Load_不正なJSONの場合は…` の `settings.Appearance.ShowDate`: 既定値ではルートが null なので、
          `settings.Appearance` が null で `Monitors` が空であることを確かめる形にする
        - `Save_BackgroundOpacityを0から100の範囲にクランプして書き込む`・`Save_区切り文字は保存して読み戻しても同じ値になる`:
          `CreateDefault().Appearance` が null。モニタのエントリを 1 つ作り、その `Appearance` に値を入れて保存・読み直す形にする
          (クランプは、T134 で各モニタに当てるようにしたことを確かめるテストになる)
        - `Load_設定の一部がnullの場合は…` の残す InlineData の `Assert.NotNull(settings.Appearance)`・`Assert.NotNull(settings.WindowBehavior)`:
          既定値ではルートが null なので、`Assert.Null` にする
- [X] T138 [US4] `tests/OkidokeiWidget.Core.Tests/Persistence/MonitorSettingsReconcilerTests.cs` に、research.md #22 の場合分けの
      テストを足す (T135 に依存)
      - 以前のバージョンの設定 (ルートに値あり、モニタ A・B のエントリ、A だけ接続) → A・B どちらにもルートの値の複製が入る。
        A は `IsVisible` がそのまま、B は false になる。ルートの 2 つは null になる
      - 引き継いだ複製は、エントリごとに別のインスタンスで、ルートの元のインスタンスとも別である
      - 引き継いだ後にもう一度 `Reconcile` を呼び、B をつないでも、B は false のまま (引き継ぎは 1 回だけ)
      - 以前のバージョンの設定で、A を別の端子につなぎ直した (FR-040 で付け替わる) → A は接続中として扱われ、`IsVisible` はそのまま
      - 保存済みの設定があるときに新しいモニタがつながった → `IsVisible = false`、見た目・ウィンドウ挙動は既定値 (`TopMost = true`・`PositionLocked = false`)
      - `Monitors` が空 (初めて起動した) で、プライマリとそうでないモニタがつながっている → プライマリだけ `IsVisible = true`。
        プライマリを接続中の一覧の後ろに並べた場合も同じ
      - `Monitors` が空で、`IsPrimary` のモニタがない → 一覧の先頭のモニタだけ `IsVisible = true`
      - 以前のバージョンの形で `Monitors` が空 (手で編集したとき) → プライマリだけ表示し、見た目は既定値。ルートの 2 つは null になる
        (spec.md の Clarifications)
      - 今の `Reconcile_新規接続モニタにはデフォルト値のエントリを補完する` は、`Monitors` が空でプライマリのモニタなので、新しい規則でも
        `IsVisible = true` のまま通る。テスト名を「初めて起動したときはプライマリモニタを表示する」の意味に直す
        (今の前提で `IsVisible` を確かめているのはこのテストだけで、Phase 23 の T118 のテストは `IsVisible` を見ていない。`/speckit-analyze` の指摘 L2)
      - `Monitors` が空でないときの新しいモニタが非表示になることは、上の「保存済みの設定があるときに新しいモニタがつながった」で確かめる

### App: ウィジェット・タスクトレイ・詳細設定画面

- [X] T139 [US4] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` を、そのモニタの設定を使うように変える (T132・T133 に依存)
      - `_settings.Appearance` を `_placement.Appearance` に、`_settings.WindowBehavior` を `_placement.WindowBehavior` に置き換える
        (見た目の反映、`ApplyWindowBehavior`、配置・余白・ドラッグの位置ロックの判定、メニューのチェック)
      - 本体のメニューの「位置ロック」「最前面表示」は、`_placement.WindowBehavior` を切り替えて、自分の `ApplyWindowBehavior()` を呼び、
        `SettingsRepository.Save(_settings)` で保存する。全ウィジェットへ反映するコールバック (`onWindowBehaviorChanged`) はなくす (FR-012)
      - 「詳細設定...」は、コンストラクタで受け取る `Action<string> openSettingsWindow` に、自分のモニタの `_monitor.Identifier` を渡して呼ぶ (FR-042)
      - `PlacementMenuBuilder.Populate` に渡す位置ロックの値を、`_placement.WindowBehavior.PositionLocked` にする
        (本体のメニューが使うのは `Populate`。`Build` は T145 で消す。2 回目の指摘 I1)
- [X] T140 [US4] `src/OkidokeiWidget.App/App.xaml.cs` の、ウィジェットと詳細設定画面のつなぎを変える (T139・T143 に依存)
      - `OpenSettingsWindow(string? identifier)` にする (research.md #23)
        - 開いていなければ、`identifier` (null ならプライマリモニタ、それもなければ先頭のモニタの `Identifier`) を最初に選ぶモニタとして `SettingsWindow` を作る
        - 開いていれば、`identifier` が null でないときだけ `_settingsWindow.SelectMonitor(identifier)` を呼び、`Activate()` する
      - `CreateClockWindow` で `ClockWindow` に `OpenSettingsWindow` を渡す (ウィジェットからは `Identifier` 付きで呼ばれる)
      - 見た目が変わったとき (`OnAppearanceChanged(string identifier)`) は、`_clockWindowsByMonitor` にそのモニタのウィジェットがあれば
        `ApplyAppearance()` を呼び、保存する。ウィジェットがなければ (非表示のモニタ) 保存だけ
      - `OnWindowBehaviorChanged` (全ウィジェットへの反映) をなくす
      - `OnDisplaySettingsChanged` で、`SyncClockWindows` の後に、`_settingsWindow?.UpdateConnectedMonitors(_connectedMonitors)` を呼ぶ (issue #17)
      - `OnMonitorVisibilityChanged` は今のまま (`SyncClockWindows` と保存)
- [X] T141 [US3] `src/OkidokeiWidget.App/App.xaml.cs` の `BuildTrayContextMenu` を、contracts/context-menus.md の
      タスクトレイの右クリックメニューに変える (T140 と同じファイルのため、T140 の後に行う)
      - 「詳細設定...」(`OpenSettingsWindow(null)`)、区切り線、「自動起動」、区切り線、「終了」だけにする
      - 「自動起動」は `IsCheckable = true`・`IsChecked = _settings.AutoStartEnabled` にする (issue #34 と同じく、Fluent テーマでチェックを描かせるため)
      - クリックで `_settings.AutoStartEnabled` を反転し、`OnAutoStartChanged()` を呼ぶ。失敗時に値を戻してメッセージを出す処理 (issue #18) はそのまま
      - ショートカットを書き換えるのは、このクリックのときだけ (issue #20)。起動時の処理 (`OnStartup` のコメントの箇所) は変えない
      - 位置ロック・最前面表示・配置の項目と、`PlacementMenuBuilder` の呼び出しをなくす
      - XML コメントと `OnStartup` のタスクトレイのコメント (「右クリックでウィジェット本体と同じ項目のメニューを出す」) を、FR-038 の改訂に合わせて直す
- [X] T142 [US4] `src/OkidokeiWidget.App/SettingsWindow.xaml` の画面の構成を変える (ui-per-monitor-settings.md の詳細設定画面)
      - `TabControl` の上に、「編集するモニター」の `TextBlock` と `ComboBox` (`x:Name="MonitorCombo"`、`DisplayMemberPath="Label"`、
        `SelectionChanged="MonitorCombo_SelectionChanged"`) を置く
      - 「モニター・起動」タブ (`MonitorsPanel`・`AutoStartCheckBox`) をなくす
      - 「表示」タブの先頭に、「このモニターに表示する」の `CheckBox` (`x:Name="MonitorVisibleCheckBox"`、`Checked`・`Unchecked` を
        `MonitorVisibleCheckBox_Changed` に) を置く
      - 画面の幅 (`Width="560"`)・`SizeToContent="Height"`・タブの見た目のスタイルは変えない
- [X] T143 [US4] `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` を、選んだモニタの設定を編集するように変える (research.md #23、T142 に依存)
      - 選択肢のクラス `private sealed class MonitorOption` (`Identifier`・`Label`) を作る。`record` にはしない
        (値の等しい要素があると `ItemsSource` を替えても選択が残るため。research.md #23 の前提 1)
      - コンストラクタの引数を `(WidgetSettings settings, IReadOnlyList<ConnectedMonitor> connectedMonitors, string initialIdentifier,
        Action<string> onAppearanceChanged, Action onMonitorVisibilityChanged)` にする。自動起動の引数と `AutoStartCheckBox_Changed` はなくす
      - 選択肢を作る処理 `RebuildMonitorOptions(string? preferredIdentifier)`
        - 接続中のモニタを `DisplayNumber` の順に並べ、ラベルを「モニター N」にする。プライマリなら「 (プライマリ)」、
          非表示なら「 (非表示)」、両方なら「 (プライマリ、非表示)」を付ける
        - `_isInitializing` を立ててから `ItemsSource` を替え、`preferredIdentifier` の選択肢があればそれを、なければプライマリ
          (なければ先頭) を `SelectedItem` に入れる。選んだモニタが前と違えば `LoadSelectedMonitor()` を呼ぶ
      - 選んだモニタの値を全コントロールに入れ直す処理 `LoadSelectedMonitor()`
        - 今のコンストラクタの初期化 (フォント・サイズ・透過度・表示の ON/OFF・位置・区切り文字・曜日・色のボタンのラベル) を、
          選んだモニタの `MonitorPlacement.Appearance` から行うように移す。`MonitorVisibleCheckBox` には `IsVisible` を入れる
        - 入れ直している間は `_isInitializing` を立てる (research.md #23 の前提 2)
        - 区切り文字が選択肢にないときに `SelectedItem` へ null を入れる扱い (research.md #21) はそのまま
        - フォントの `FontFamilyCombo` も、設定のフォント名が選択肢にない (既定値の `""` を含む) ときは `SelectedItem` に null を入れる
          - 選択肢にない値を入れても前の選択が残るため (research.md #21 の前提 2)
          - 入れ直すと、前に選んでいたモニタのフォントが見えてしまう。新しいモニタは必ず既定値なので、普段の操作で起きる (`/speckit-analyze` の指摘 H3)
          - 空欄のまま閉じたり、ほかの項目を変えたりしても、フォント名は書き換えない (区切り文字と同じ)
      - `MonitorCombo_SelectionChanged`: `_isInitializing` なら何もしない。それ以外は `LoadSelectedMonitor()` を呼ぶ
      - コードから `SelectedItem` を変えるとき (`RebuildMonitorOptions`・`SelectMonitor`) は、`_isInitializing` を立ててから変え、
        `LoadSelectedMonitor()` は最後に 1 回だけ呼ぶ (`SelectionChanged` から二重に呼ばれないようにする。`/speckit-analyze` の指摘 L3)
      - 色の選択画面 (`PickColor`) は、開く前に選んでいるモニタの `MonitorPlacement` を変数に取っておき、閉じた後はそこへ書き込む
        - 色の選択画面を開いている間も、モニタの抜き差しの通知は届き、選択がプライマリに変わることがあるため (指摘 L3)
      - 各コントロールの変更イベントは、`_settings.Appearance` ではなく、選んだモニタの `MonitorPlacement.Appearance` に書き込み、
        `_onAppearanceChanged(選んだモニタの Identifier)` を呼ぶ
      - `MonitorVisibleCheckBox_Changed`: `_isInitializing` なら何もしない (`LoadSelectedMonitor` が `IsChecked` を入れると
        `Checked`/`Unchecked` が起き、読み込みの途中で選択肢を作り直してフラグが下りてしまうため。`/speckit-analyze` の 2 回目の指摘 U1)。
        それ以外は、選んだモニタの `IsVisible` を書き換え、`_onMonitorVisibilityChanged()` を呼び、
        `RebuildMonitorOptions(選んだモニタの Identifier)` でラベルの「(非表示)」を直す (FR-014)
      - 外から呼ぶ `public void SelectMonitor(string identifier)`: その選択肢があれば選び、`LoadSelectedMonitor()` を呼ぶ (FR-042)
      - 外から呼ぶ `public void UpdateConnectedMonitors(IReadOnlyList<ConnectedMonitor> connectedMonitors)`: 一覧を差し替え、
        `RebuildMonitorOptions(選んでいたモニタの Identifier)` を呼ぶ。選んでいたモニタがなくなっていれば、プライマリの値が入る (issue #17)
      - `BuildMonitorCheckBoxes`・`MonitorVisibilityCheckBox_Changed` はなくす

### 記録と確認

- [X] T144 [P] [US4] `README.md` の「OkidokeiWidget の概要」の「モニタごとの表示/非表示・表示位置の管理」を、見た目・位置ロック・
      最前面表示もモニタごとに持てる、という内容に直す
      - 公開リポジトリ (`probono-a/okidokei-widget`) の README は、反映のときに書き換える運用なので、ここでは直さない (README の「関連リポジトリ」)
      - 代わりに、反映のときに使う文案を本 Phase の末尾に書いておく
        - 「できること」: モニタごとに見た目・位置ロック・最前面表示も持てること。タスクトレイの右クリックメニューは
          「詳細設定」「自動起動」「終了」だけになったこと (「ウィジェット本体と同じ操作ができます」を消す)
        - 「ビルドと起動」: 自動起動の ON/OFF はタスクトレイの右クリックメニューで切り替えること
        - 新しくつないだモニタと、アップデートのときにつないでいなかったモニタには時計が表示されないので、
          詳細設定画面の「編集するモニター」でそのモニタを選び、「このモニターに表示する」をオンにすること (spec.md の Assumptions)
- [X] T145 [US4] `src/` の中で、見た目・位置ロック・最前面表示を「アプリ全体で共通」「全ウィジェットに反映」と説明している
      コメントや、タスクトレイのメニューを「本体と同じ項目」と説明しているコメントが残っていないかを `grep` で確かめ、見つかれば直す
      - `/speckit-analyze` で見つかった次の箇所は、上の言い回しでは拾えないので、必ず直す (指摘 M3)
        - `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` のクラスの summary の「ウィジェット本体とタスクトレイの両方の右クリックメニューから使い」
        - `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `SetAnchorHorizontal` の summary の「本体とタスクトレイのどちらのメニューからも」
        - `src/OkidokeiWidget.App/App.xaml.cs` の `OnStartup` の「詳細設定での ON/OFF トグル時」(タスクトレイの「自動起動」に直す)
        - `src/OkidokeiWidget.App/App.xaml.cs` の `OnAutoStartChanged` の「詳細設定のチェックも SettingsWindow 側で設定値に合わせ直す」
          (タスクトレイのメニューは開くたびに作り直すので、合わせ直す処理は要らない、に直す)
      - `PlacementMenuBuilder.Build` (トレイのメニューのモニタの項目を作るメソッド) は、T141 で呼ぶ所がなくなるので消す。
        本体のメニューが使う `Populate` は残す (原則 I。plan.md の変更計画)
- [X] T146 [US4] `dotnet build` が、App (`src/OkidokeiWidget.App/OkidokeiWidget.App.csproj`) とテスト
      (`tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj`) のどちらでも警告 0・エラー 0 で (2 回目の指摘 A1)、`dotnet test` が全件成功することを確認し (警告 0 は、T133 の null 参照の直し漏れを
      見つけるため。`/speckit-analyze` の指摘 H1)、`quickstart.md` の「モニタごとの設定 (FR-038・FR-041・FR-042、
      2026-10-01 追加、issue #53・#17)」のシナリオを人間に実施してもらう。結果を本 Phase の末尾に記録する (T131〜T145 に依存)
      - 始める前に、今の `settings.json` (v1.2.x の形) を別名でコピーしておく。以前のバージョンからの引き継ぎを確かめた後は、
        ルートの `Appearance` がなくなり、同じ確認をもう一度できないため
      - 以前のバージョンからの引き継ぎで「つながっていないモニタが非表示になる」ことは、`settings.json` に、つながっていないモニタの
        エントリがあるときだけ確かめられる。なければ、起動する前に手で 1 つ足しておく (quickstart.md の手順)
      - US2 の区切り文字の手順は、ルートではなく各モニタの `Appearance.DateSeparator` を書き換える形に直した quickstart.md の手順で行う
        (ルートに `Appearance` を足すと、以前のバージョンのファイルとして引き継ぎが走るため。2 回目の指摘 C1)
      - 新しいモニタの確認は、一度もつないだことのないモニタがなければ、`settings.json` からそのモニタのエントリを消してから
        アプリを起動して確かめる (同じ型番の使われていない設定がないことも確かめる。FR-040 で割り当てられると、新しいモニタにならない)

**Checkpoint**: `dotnet test` が全件成功し、2 台のモニタで見た目・位置ロック・最前面表示を別々に設定・復元できる状態。
以前のバージョンの `settings.json` のまま起動しても、つながっているモニタの見た目・位置・表示が変わらないこと。
詳細設定画面を開いたままモニタを抜き差ししても、落ちずに選択肢が追随すること

### Phase 26 の確認結果 (2026-10-02)

- `dotnet build` (App・テストの両方で警告 0、エラー 0)、`dotnet test` (165 件すべて合格。Phase 26 で 23 件を追加)
- 実機は SONY (モニター 2)・DELL (モニター 1、プライマリ) の 2 台。開発機の `settings.json` (v1.2.x の形) のまま Debug 版を起動した
  - 引き継ぎ (US4-6・US4-10): ルートの `Appearance`・`WindowBehavior` がなくなり、2 台に複製が入った。2 台とも表示のままで、位置も変わらなかった
  - 画面の構成 (人間が確認): 「編集するモニター」が開いた場所のモニタ (DELL のウィジェットからは DELL) を選んだ状態で、タブは「表示」「フォント」「レイアウト・背景」の 3 つ。「表示」タブの先頭に「このモニターに表示する」がある (US4-13)
  - モニタごとの見た目 (人間が確認、US4-8、SC-009): SONY の見た目だけを変えても DELL は変わらなかった
  - フォントの欄 (analyze の H3): SONY のフォント名を `""` に書き換えて起動すると、SONY を選んだときだけ欄が空欄になり、DELL では `Franklin Gothic Book`、SONY に戻すとまた空欄になった。SONY の文字色だけを変えて閉じても、`settings.json` のフォント名は `""` のままだった
    - 最初の確認 (SONY でフォントを変えてから DELL に切り替える) は、DELL にもフォントが保存されていたため、空欄にならなかった。確認の手順の間違いで、不具合ではない
  - 位置ロック・最前面表示 (US4-12・US4-17、SC-009): UI Automation で SONY のウィジェットの右クリックメニューを操作し、SONY だけが切り替わって DELL は変わらないことを、`settings.json` で確かめた
  - 再起動後の復元 (US4-9・US4-18、SC-005): SONY はロック OFF・最前面 ON、DELL はロック ON・最前面 OFF の状態で「終了」メニューから終了して起動し直すと、それぞれのウィジェットのメニューのチェックが同じ状態で復元された
  - 開き直したときの切り替え (人間が確認、FR-042): 詳細設定を開いたまま別のモニタのウィジェットから開き直すとそのモニタに切り替わり、タスクトレイから開き直すと選択は変わらなかった。タスクトレイのメニューは「詳細設定...」「自動起動」「終了」だけだった (US3-13・US3-14)
  - 表示/非表示 (人間が確認、US4-16): 「このモニターに表示する」を OFF にすると、そのウィジェットが消えて選択肢が「(非表示)」の表記になり、詳細設定画面は開いたままだった。ON に戻すと元の位置・見た目で表示された
  - 新しいモニタ (US4-14): DELL のエントリを消して起動すると、DELL は非表示・見た目は既定値・位置ロック OFF・最前面表示 ON で足され、画面のウィジェットは SONY の 1 つだけだった
  - 初めての起動 (US4-15): `settings.json` を退避して起動すると、プライマリの DELL だけが表示され、SONY は非表示で足された
  - アップデートでつながっていないモニタ (US4-6): v1.2.x の形に、つながっていないモニタ (会議室のプロジェクター) のエントリを足して起動すると、そのエントリだけが非表示になり、位置は保存されたままだった。SONY・DELL は表示のままだった
  - モニタの抜き差し (人間が確認、US4-11・US4-16、issue #17)
    - SONY のケーブルを抜くと、DELL を選んでいた詳細設定画面の選択は DELL のままで、選択肢から SONY が消えた。落ちなかった
    - ケーブルを戻すと、選択肢に SONY が戻った。SONY を選んだ状態でもう一度抜くと、プライマリの DELL に切り替わった
    - 手順の途中で 1 回だけ、「モニター 2 を選んでいたのに、抜いた後も表示が『モニター 2』のままだった」という報告があった。同じ手順を 2 回やり直したが再現せず、原因は特定できなかった (最小の WPF のアプリで `ItemsSource` の差し替えと選択を確かめたが、ドロップダウンを開閉した後でも選択は正しく切り替わった)。設定を失ったり落ちたりする現象ではないので、再現したらあらためて issue にする
- 自動起動の切り替え (タスクトレイの「自動起動」で、スタートアップフォルダのショートカットが作られる・消えること) は、まだ確かめていない
  - いまのショートカットは Release 版ではなく Debug 版の exe を指していた (CLAUDE.md には「自動起動は Release ビルドの exe を対象にしている」とある。実物とのずれとして、人間に報告済み)
  - 切り替えの処理 (`AutoStartManager`) は今回触っていない。呼び出し元だけが詳細設定画面からタスクトレイに変わった
  - PR のマージ後に Release 版を入れてから、タスクトレイで切り替えて確かめる (人間が決定)
  - 2026-10-02 追記: PR #73 のマージ後に、Release 版で確かめた (issue #53 のコメント)
    - Release 版を起動すると、v1.2.x の形の `settings.json` が各モニタへ引き継がれた (2 台ともつながっていて、表示・位置・見た目・位置ロックは元どおり)
    - タスクトレイの「自動起動」を OFF にすると、ショートカットが消え、`AutoStartEnabled` が `false` になった
    - 開き直したメニューではチェックが外れていて、ON にすると、ショートカットが Release 版の exe を指して作り直され、`AutoStartEnabled` が `true` に戻った
    - これで、ショートカットが Debug 版を指していたずれも解消し、CLAUDE.md の記述と一致した

---

## Phase 27: 色の選択ボタンのカラーコードが 8 桁で表示される問題の修正 (2026-10-02)

**Purpose**: issue #76 の修正の記録。FR-025・FR-030 (文字色・背景色) の実装で、詳細設定のボタンに
保存値の `#AARRGGBB` をそのまま表示しており、色の選択画面の `#RRGGBB` と表記がずれていた。仕様の
追加・変更は伴わない。修正は `bug` 拡張のフローで行った (経緯・検証結果は
`.specify/bugs/color-button-hex/` を参照)

- [X] T147 [US2] `src/OkidokeiWidget.Core/Settings/ColorHexResolver.cs` に、`#AARRGGBB` から
      `#RRGGBB` を返す `ToRgbHex` を追加し、`ColorHexResolverTests.cs` にテストを足す (issue #76)
- [X] T148 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` の文字色・背景色のボタンの表示と、
      `ColorPickerWindow.xaml.cs` の入力欄の表示で `ToRgbHex` を使う (issue #76、T052・T056 の修正、
      T147 に依存)

**Checkpoint**: `dotnet build`・`dotnet test` が成功すること。実機で、文字色 (時刻・日付) と背景色の
ボタンが 6 桁で表示され、色を選び直した後も 6 桁のままであることを確認する

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 依存なし。即座に着手可能
- **Foundational (Phase 2)**: Setup 完了に依存。すべてのユーザーストーリーをブロックする
- **User Stories (Phase 3+)**: すべて Foundational フェーズの完了に依存
  - 各ストーリーはその後、優先度順 (P1 → P2 → P3) に進めるか、要員がいれば並行して進められる
- **Polish (最終フェーズ)**: 実施対象のユーザーストーリーすべての完了に依存
- **Phase 8 (/speckit-analyze 指摘の反映)**: T039・T042・T044 は Foundational (T011, T010) の
  後であれば着手可能。T041 は US1 (T016) に、T045 は US4 (T016, T033) に依存するため、
  それぞれ対応するユーザーストーリーの実装後が自然
- **Phase 13 (issue #25)**: 実施済み (PR #27)
- **Phase 14 (アンカー指定)**: Phase 12 (`WidgetPlacementCalculator`・物理ピクセルでの配置) と
  Phase 13 (`DpiChanged` での再配置) の上に積む。Phase 内の順序は以下のとおり
  - T075・T076 (enum) → T077 (`MonitorPlacement`) → T078 (位置計算)
  - T079・T080 (テスト) と T081 (`PlacementMenuBuilder`) は、T077/T078 の後に並行して着手できる
  - T082 → T083 → T084 (本体のメニュー) の順
  - T085 (`TrayIconManager`) は T077 以降いつでも着手できる。T086 (トレイのメニュー) は
    T084・T085 の後
  - T087 (確認) は最後
- **Phase 18 (ドラッグ範囲の制限、issue #39)**: Phase 14 の `WidgetPlacementCalculator` と、
  Phase 17 (issue #41) の `DpiChanged` の後回し処理の上に積む。Phase 内の順序は以下のとおり
  - T095 (Core の計算) → T096 (テスト)
  - T097 (`GetCursorPos`) は T095 と並行して着手できる
  - T098 (ドラッグ処理) は T095・T097 の後
  - T099 (確認) は最後
- **Phase 19 (自由配置のときの余白、issue #43)**: Phase 18 の `ClampToWorkArea` の上に積む。
  Phase 内の順序は以下のとおり
  - T100 (Core の計算) → T101 (テスト)
  - T102 (`PlacementMenuBuilder`) は T100 と並行して着手できる
  - T103 (`ClockWindow`) は T100・T102 の後、T104 (トレイのメニュー) は T103 の後
  - T105 (確認) は最後
- **Phase 23 (モニタの見分け方、issue #52)**: Foundational の `MonitorIdentifier`・
  `MonitorSettingsReconciler` の上に積む。他の Phase の変更とは重ならない。Phase 内の順序は以下のとおり
  - T114 (型番の取り出し)・T115 (コメント) は並行して着手できる
  - T116 (付け替え) は T114 の後。T117 (`GetModel` のテスト) は T114 の後、T118 は T116 の後
  - T119 (記述の確認) はいつでも着手できる。T120 (確認) は最後
- **Phase 25 (日付の区切り文字、issue #60)**: Phase 9 の `DateSeparatorResolver` (T049)・日付表示 (T051)・区切り文字の
  選択欄 (T052) の上に積む。他の Phase の変更とは重ならない。Phase 内の順序は以下のとおり
  - T123 (`Resolve`) → T124 (`DateTextFormatter`)
  - T125・T127 は T123 の後、T126 は T124 の後。T125〜T127 は並行して着手できる
  - T128 (`ClockWindow`) は T124 の後、T129 (`SettingsWindow`) は T123 の後
  - T130 (確認) は最後
- **Phase 26 (モニタごとの設定、issue #53・#17)**: Phase 23 の `MonitorSettingsReconciler` (T116)、Phase 22 の
  `HasNullSection` (issue #51)、Phase 25 の詳細設定画面の区切り文字 (T129) の上に積む。Phase 内の順序は以下のとおり
  - T131 (`Clone`)・T132 (`MonitorPlacement`) は並行して着手できる。T133 (`WidgetSettings`) は T132 の後
  - T134 (`HasNullSection`) は T132・T133 の後、T135 (`Reconcile`) は T131〜T133 の後
  - T136 は T131 の後、T137 は T134 の後、T138 は T135 の後
  - T139 (`ClockWindow`) は T133 の後。T142 (XAML) → T143 (`SettingsWindow`)
  - T140 (App のつなぎ) は T139・T143 の後、T141 (トレイのメニュー) は T140 の後 (同じファイル)
  - T133 の後もビルドは通るが、T134・T139〜T141・T143 が終わるまで、起動すると null 参照で落ちる。App の変更はまとめて行う
  - T144 (README) はいつでも着手できる。T145 (コメントの確認と `Build` の削除) は T141 の後。T146 (確認) は最後

### User Story Dependencies

- **User Story 1 (P1)**: Foundational (Phase 2) の後に着手可能。他ストーリーへの依存なし
- **User Story 2 (P2)**: Foundational の後に着手可能。`ClockWindow` の時刻表示ロジック (T018)
  を前提として見た目を反映するため、実装順としては US1 の後が自然
- **User Story 3 (P2)**: Foundational の後に着手可能。`ClockWindow` (T018) を前提とするため、
  実装順としては US1 の後が自然
- **User Story 4 (P3)**: Foundational の後に着手可能。モニタごとの `ClockWindow` 生成/破棄
  (T016) とドラッグ移動・位置永続化 (T027, T030) を前提とするため、実装順としては
  US1・US3 の後が自然

### Within Each User Story

- モデル/サービスの実装を先に行い、その後 UI・イベントハンドラを実装する
- 単体テストが対象とするロジック (Foundational の SettingsRepository・
  MonitorSettingsReconciler・AutoStartManager) は、実装直後にそのテストを追加する
- 各ストーリーの実装が完了してから次の優先度のストーリーに進む (または並行して着手する)

### Parallel Opportunities

- Setup の [P] マーク付きタスク (T002〜T004) は並行実行できる
- Foundational の [P] マーク付きタスク (T005〜T007、T009、T014) は並行実行できる
- Foundational 完了後は、要員がいれば各ユーザーストーリーを並行して着手できる
- 同一ストーリー内の [P] マーク付きタスクは並行実行できる

---

## Parallel Example: Foundational Phase

```bash
# データモデルを並行実装する:
Task: "AppearanceSettings モデルを src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs に実装"
Task: "WindowBehaviorSettings モデルを src/OkidokeiWidget.Core/Settings/WindowBehaviorSettings.cs に実装"
Task: "MonitorPlacement モデルを src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs に実装"
```

## Parallel Example: User Story 1

```bash
# 多重起動チェックと画面レイアウトは別ファイルのため並行実装できる:
Task: "App.xaml.cs に名前付き Mutex による多重起動チェックを実装"
Task: "ClockWindow.xaml に時刻/日付/曜日表示のレイアウトを実装"
```

---

## Implementation Strategy

### MVP First (User Story 1 のみ)

1. Phase 1: Setup を完了する
2. Phase 2: Foundational を完了する (すべてのストーリーをブロックするため必須)
3. Phase 3: User Story 1 を完了する
4. **一度立ち止まって検証する**: User Story 1 を独立して手動テストする (quickstart.md US1)
5. 準備ができればここでリリース/デモしてもよい (MVP)

### Incremental Delivery

1. Setup + Foundational を完了する → 基盤が整う
2. User Story 1 を追加 → 独立してテスト → デモ (MVP)
3. User Story 2 を追加 → 独立してテスト → デモ
4. User Story 3 を追加 → 独立してテスト → デモ
5. User Story 4 を追加 → 独立してテスト → デモ
6. 各ストーリーは、それ以前のストーリーを壊さずに価値を追加する

---

## Notes

- [P] タスク = 別ファイルかつ未完了タスクへの依存がない
- [Story] ラベルはタスクを対応するユーザーストーリーに紐づけ、追跡可能にする
- 各ユーザーストーリーは独立して完了・テスト可能であるべき
- Foundational フェーズの単体テストは、対象ロジックの実装直後に追加し、実装が正しいことを
  確認する
- 論理的な単位ごとにコミットする
- 各チェックポイントで、そのストーリーが独立して動作することを検証してから次に進む
- 避けるべきこと: 曖昧なタスク、同一ファイルへの並行編集競合、ストーリー間の独立性を壊す
  依存関係
