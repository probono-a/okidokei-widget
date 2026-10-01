# Contract: 設定ファイル (`settings.json`)

**Feature**: `001-clock-widget`

本アプリの唯一の永続化された外部インターフェースである設定ファイルの構造を定義する。
アプリの再起動をまたいで状態を復元する際 (FR-017) の契約であり、[`data-model.md`](../data-model.md)
のモデルに対応する。

## 保存場所

`%APPDATA%\OkidokeiWidget\settings.json`

(`ClockWidget` や `DesktopClockWidget` は一般的すぎて他アプリと衝突する可能性があるため、
「置き時計」に由来する造語 `Okidokei` を使ったアプリ名 `OkidokeiWidget` を採用した。個人名を
含むベンダーフォルダ(`<作者名>\ClockWidget` 案)は、実名がアプリ内部のパスに埋め込まれて
しまうため不採用)

## スキーマ (例)

2026-10-01 (issue #53) から、見た目 (`Appearance`) とウィンドウ挙動 (`WindowBehavior`) をモニタごとに持つ。

```json
{
  "Monitors": {
    "\\\\?\\DISPLAY#GSM123B#5\u00263b7d6ecd\u00260\u0026UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}": {
      "IsVisible": true,
      "X": 1600,
      "Y": 20,
      "Anchor": "TopRight",
      "AnchorMargin": "Narrow",
      "Appearance": {
        "ShowDate": true,
        "ShowDayOfWeek": true,
        "ShowSeconds": true,
        "FontFamily": "Yu Gothic UI",
        "TimeFontSize": 24,
        "TimeFontColor": "#FFFFFFFF",
        "DateFontSize": 14,
        "DateFontColor": "#FFFFFFFF",
        "DateSeparator": "/",
        "DayOfWeekFormat": "LongKanji",
        "DateDayOfWeekPosition": "Below",
        "BackgroundColor": "#FF000000",
        "BackgroundOpacity": 30
      },
      "WindowBehavior": {
        "TopMost": true,
        "PositionLocked": false
      }
    }
  },
  "AutoStartEnabled": true
}
```

以前のバージョン (2026-10-01 より前) の設定ファイルは、`Appearance`・`WindowBehavior` をルートに 1 つずつ持ち、
`Monitors` の各エントリには持たない。読み込み方は下の「読み込み契約」を参照。

```json
{
  "Appearance": { "ShowDate": true, "...": "..." },
  "WindowBehavior": { "TopMost": true, "PositionLocked": false },
  "Monitors": {
    "\\\\?\\DISPLAY#GSM123B#...": { "IsVisible": true, "X": 1600, "Y": 20, "Anchor": "TopRight", "AnchorMargin": "Narrow" }
  },
  "AutoStartEnabled": true
}
```

## スキーマに関する補足

Phase 7 (T038) で、実際に書き出された `settings.json` と上記の例を突き合わせて確認した内容:

- `Monitors` のキーは、`EnumDisplayDevices` に `EDD_GET_DEVICE_INTERFACE_NAME` を指定して
  取得するデバイスインターフェース名 (`\\?\DISPLAY#<EDID 由来のモデル ID>#<インスタンス ID>#{GUID}`)
  である (research.md #2、`MonitorIdentifier`)
  - 2026-09-28 追記 (issue #52): `<EDID 由来のモデル ID>` はメーカーと型番 (以降「型番」と呼ぶ)。
    `<インスタンス ID>` はモニタではなく、つないでいる出力先 (端子) ごとの値だった。キーの形は変えず、読み込み時の探し方で端子のつなぎ直しに対応する (読み込み契約、
    research.md #20)
- キーに含まれる `\` は JSON の文字列エスケープにより `\\` として、`&` は `System.Text.Json` の
  既定のエンコーダにより `\u0026` として書き出される
- `TimeFontSize`・`DateFontSize`・`BackgroundOpacity` は `double` だが、値が整数のときは
  `24.0` ではなく `24` として書き出される
- 上記以外のキー名・階層構造・型は、実ファイルと本契約とで一致している

2026-09-24 に追加したフィールド (FR-034, FR-035、data-model.md の `MonitorPlacement`):

- `Anchor`: `AnchorPosition` の enum 名 (`TopLeft`〜`BottomRight` の 9 値) または `null`。
  `null` は自由配置を表し、`X`/`Y` を使う
- `AnchorMargin`: `Narrow` または `Wide`
- アンカー指定中も `X`/`Y` は書き出される (値は最後に自由配置していたときの位置)

2026-09-28 に改訂したフィールド (FR-026、issue #60):

- `DateSeparator`: 詳細設定画面から書き出されるのは `/`・`-`・`.` のどれか。ファイルを直接書き換えた場合は、
  任意の文字列 (空欄を含む) をそのまま読み込み、日付の区切りに使う
  - 項目がない場合は `/` として読み込む。`null` の場合は `/` として表示し、ファイルには `null` のまま書き出す
  - 選択肢にない値は、詳細設定画面で選び直さない限り、同じ値のまま書き出される
  - 英数字以外の文字は、他のフィールドと同じく `System.Text.Json` の既定のエンコーダにより `\uXXXX` の形で
    書き出されることがある (例: `"🍣"` → `"\uD83C\uDF63"`)。読み込めば同じ文字に戻る (research.md #21)
  - アプリは起動するたびに設定ファイルを書き出すため、手で書き換えた後にアプリを起動しただけで、この形になる

2026-10-01 に変えた構造 (FR-041、issue #53、data-model.md の `MonitorPlacement`、research.md #22):

- `Monitors` の各エントリに `Appearance`・`WindowBehavior` を足した。中の項目は、以前のルートのものと同じ
- ルートの `Appearance`・`WindowBehavior` は書き出さない。以前のバージョンのファイルを読むためだけに使う

## 読み込み契約

- ファイルが **存在しない** 場合: 全フィールドをデフォルト値としたオブジェクトを生成し、
  そのまま起動を継続する (FR-018)。この時点ではファイルへの書き込みは行わない
- ファイルが **存在するがパース不可**(不正な JSON、必須フィールドの型不一致等)な場合:
  ファイル全体を無視し、全フィールドをデフォルト値として起動する(部分的な復旧は行わない)。
  アプリはクラッシュしてはならない。この場合、デフォルト設定にフォールバックしたことを
  ユーザーに通知しなければならない(FR-018。通知の具体的な UI 表現は実装時に決めてよい)。
  ファイルが単に存在しない場合(初回起動等)は通知しない
  - JSON としては正しくても、`Monitors`、`Monitors` の各モニタの値、または各モニタの
    `Appearance`・`WindowBehavior` が `null` の場合は、パース不可として扱う (issue #51)
    - 2026-10-01 改訂: ルートの `Appearance`・`WindowBehavior` は、以前のバージョンのファイルを読むときにしか
      使わないため、`null` やないときはパース不可にせず、「以前のバージョンのファイルではない」として読む
  - 元のファイルは、デフォルト設定で上書きする前に `settings.json.bak` として同じフォルダへ
    コピーして残し、通知にその場所を書き添える。ユーザーが元のファイルを手で直せるようにするため
    (issue #50)。`.bak` は毎回上書きし、世代管理はしない
- `Monitors` に含まれるキーのうち、現在接続されているモニタの識別子と一致しないものは
  読み込み時にそのまま保持し(削除しない)、表示ロジックの対象からのみ除外する。ただし、次の項目の
  付け替えで今のキーへ移したエントリは、古いキーからは消える (FR-040)
- `Monitors` に、現在接続されているが対応するキーが存在しないモニタがある場合、キーの 2 つ目の
  部分 (型番。上の書式の `<EDID 由来のモデル ID>`) が同じエントリのうち、キーが接続中の
  どのモニタとも一致しないものを、キーの順に 1 対 1 で割り当て、今のキーへ移す (FR-040、
  research.md #20)。割り当てるエントリがなければ、デフォルト値でエントリを補完する
  - 以前のバージョンの設定ファイルもキーの形が同じなので、移行の処理なしでそのまま読める
  - 2026-10-01 改訂 (FR-041): 補完するエントリは、表示しない (`IsVisible = false`)、見た目・ウィンドウ挙動は
    既定値のエントリにする。ただし、`Monitors` が空のとき (ファイルがない、壊れていた、モニタの設定が空) は、
    プライマリモニタのエントリだけ `IsVisible = true` にする
- ルートに `Appearance` または `WindowBehavior` がある場合 (以前のバージョンのファイル) は、上の付け替えの後、
  デフォルト値のエントリを補完する前に、次のとおり各モニタへ引き継ぐ (FR-041、research.md #22)
  - 補完の前に行うので、新しいモニタのエントリには、以前の見た目ではなく既定値が入る (`/speckit-analyze` の指摘 L1)
  - `Monitors` のすべてのエントリ (接続していないモニタのものも含む) に、ルートの値の複製を入れる
  - キーが接続中のどのモニタとも一致しないエントリは、`IsVisible` を `false` にする。接続中のモニタのエントリは変えない
  - ルートの 2 つは、次の保存から書き出さない。なので、この引き継ぎはアップデートして初めて起動したときに 1 回だけ起きる

## 書き込み契約

- 設定変更(見た目・配置・モニタ設定・自動起動 ON/OFF)が確定するたびに、`WidgetSettings`
  全体を都度ファイルへ上書き保存する
- ルートの `Appearance`・`WindowBehavior` は、値が `null` のときは書き出さない (2026-10-01 追加。
  引き継ぎを終えた後は常に `null`)
- 書き込みは、まず一時ファイルへ書き出してから置き換える等、書き込み途中のクラッシュで
  ファイルが破損しにくい方法で行う
- `BackgroundOpacity` は書き込み前に `0〜100` の範囲へクランプする。2026-10-01 からは、各モニタの `Appearance` の値に対して行う
  (ルートの `Appearance` は、null でないときだけ。`/speckit-analyze` の指摘 C1)
- 書き込み先ディレクトリへの権限不足等により書き込みに失敗した場合でも、アプリはクラッシュ
  せず動作を継続しなければならない(FR-022)。この場合、その変更内容はそのセッション中は
  永続化されない(次回、書き込みに成功した時点で最新の状態が保存される)

## 後方互換性

- 現バージョンでは単一スキーマのみを対象とし、バージョン番号フィールドは持たない
  (YAGNI: 将来のマイグレーション要件は現時点で存在しない)
- 未知の追加フィールドがファイル内に存在してもエラーにはせず無視する
  (デシリアライザの既定動作に従う)
- `Anchor`・`AnchorMargin` を持たない (2026-09-24 より前の) 設定ファイルは、エラーにせず
  そのまま読み込む。`Anchor` = null (自由配置)、`AnchorMargin` = `Narrow` として扱うため、
  既存ユーザーの表示位置は変わらない (FR-034)。この互換性は単体テストで保証する
- `Anchor`・`AnchorMargin` に未知の文字列が入っている場合は、パース不可として扱う
  (上記「読み込み契約」のとおり全体をデフォルトへフォールバックし、ユーザーに通知する)
- 2026-10-01 より前の設定ファイル (ルートに `Appearance`・`WindowBehavior` があるもの) は、エラーにせず読み込み、
  各モニタへ引き継ぐ (上記「読み込み契約」)。バージョン番号のフィールドは引き続き持たない
  - 引き継いだ後の見た目・位置ロック・最前面表示・位置はアップデート前と同じ。表示/非表示だけは、
    そのときつながっていないモニタが非表示になる (spec.md の Clarifications)。この互換性は単体テストで保証する
- 2026-10-01 以降の設定ファイルを以前のバージョンで読んだ場合の扱いは保証しない (spec.md の Assumptions)
  - 参考: 以前のバージョンは、ルートの `Appearance`・`WindowBehavior` がないので既定値で起動する。
    各モニタの `Appearance`・`WindowBehavior` は未知のフィールドとして無視する
