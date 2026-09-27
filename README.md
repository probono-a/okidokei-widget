# OkidokeiWidget

[GitHub Spec Kit](https://github.com/github/spec-kit) と [Claude Code](https://claude.com/claude-code) で作った、
Windows 11 向けの常駐型デスクトップ時計ウィジェットです。

このリポジトリは、アプリの公開と同時に、**Spec Kit を使った AI 駆動開発の実例**として、
仕様・実装計画・タスク・バグ修正の記録をそのまま残して公開しています。
要件定義からリリース後の追加要望・バグ修正まで、Spec Kit の成果物がどう積み上がっていったかをファイルから追えます。

| デスクトップ上のウィジェット | 右クリックメニュー | 詳細設定 |
|---|---|---|
| ![ウィジェット本体](docs/screenshots/widget.png) | ![右クリックメニュー](docs/screenshots/context-menu.png) | ![詳細設定ウィンドウ](docs/screenshots/settings.png) |

## 開発の流れ

1. **要件定義**: 作りたいものを [Web の Claude](https://claude.ai/) と相談して、要件定義書にまとめました
2. **原則を決める** (`/speckit-constitution`): 「個人用ツールなので作り込まない」「設定は JSON」
   「誤操作で位置が変わらない」など、判断に迷ったときに立ち返る原則を決めました
3. **仕様を書く** (`/speckit-specify` → `/speckit-clarify`): 要件定義書から仕様を作り、
   あいまいな点を Claude からの質問に答えて詰めました
4. **計画とタスク** (`/speckit-plan` → `/speckit-tasks`): 技術的な設計と、実装タスクへの分解を行いました
5. **実装前の確認** (`/speckit-checklist` → `/speckit-analyze`): 仕様・計画・タスクの抜けや矛盾を確かめました
6. **実装** (`/speckit-implement`): ユーザーストーリー (P1〜P3) ごとに実装し、人間が実機で確認しました
7. **リリース後**: 追加要望とバグ修正を、同じ仕組みの上で続けています
   - 追加要望は `/speckit-specify` からやり直し、仕様の変更として記録しました
   - バグ修正は Spec Kit の bug 拡張で、調査 (assess) → 修正 (fix) → 検証 (test) の順に進めました

> [!TIP]
> 作者は C# をほとんど書いたことがなく、コードはほぼ読まずに Claude Code に任せています。  
> 人間が受け持ったのは、仕様の判断・PR のレビュー (主に仕様や記録の確認)・実機での確認です。  
> コード・テスト・各成果物の作成は Claude Code が行いました。

## 読みどころ

どのファイルも、開発中に Claude Code が作り、人間が確認したものをそのまま置いています。

| ファイル | 何が分かるか |
|---|---|
| [docs/requirements.md](docs/requirements.md) | 最初のインプットにした要件定義書 |
| [.specify/memory/constitution.md](.specify/memory/constitution.md) | プロジェクトの原則。開発中に起きた失敗から足したルールもあります (Development Workflow の 7 など) |
| [specs/001-clock-widget/spec.md](specs/001-clock-widget/spec.md) | 機能仕様。冒頭の Clarifications に、質問と回答・実機確認からの追加要望が日付つきで積み上がっています |
| [specs/001-clock-widget/plan.md](specs/001-clock-widget/plan.md)・[research.md](specs/001-clock-widget/research.md) | 技術的な設計と、その根拠にした調査 |
| [specs/001-clock-widget/tasks.md](specs/001-clock-widget/tasks.md) | 実装タスク。Phase 7 までが最初の実装で、Phase 8 以降はリリース後の追加要望・バグ修正です |
| [specs/001-clock-widget/checklists/](specs/001-clock-widget/checklists/) | 実装前に仕様の品質を確かめたチェックリスト |
| [.specify/bugs/](.specify/bugs/) | バグ修正ごとの調査・修正・検証の記録 |
| [CLAUDE.md](CLAUDE.md) | Claude Code への指示 (進め方・ブランチ運用・文章の書き方) |

> [!IMPORTANT]
> 文中の issue 番号は、開発に使っている非公開のリポジトリのものです。このリポジトリからは参照できません。

## できること

- 時刻・日付・曜日の常時表示
- フォント種類・サイズ・文字色のカスタマイズ
- 背景透過度の調整
- ドラッグでの自由配置、位置ロックによる誤操作防止
  - ドラッグで動かせるのは、表示中のモニタの作業領域 (タスクバーを除いた領域) 内に限ります
- 画面の隅や中央に寄せる配置 (横位置・縦位置・画面端からの余白を右クリックメニューから選択)
  - フォントサイズや表示の拡大率が変わっても、寄せた位置を保ちます
  - 自由配置のときも、余白を選ぶと近くの画面端から離せます
- 常に最前面に表示するかどうかの切り替え
- モニタごとの表示/非表示・表示位置の個別管理 (モニタの接続/切断にも追随)
- 右クリックメニューでのクイック切替 + 詳細設定ウィンドウ
- タスクトレイ常駐 + Windows 起動時の自動起動
  - タスクトレイの右クリックメニューからも、ウィジェット本体と同じ操作ができます
- 設定は JSON ファイルで保存 (レジストリは使用しません)

Microsoft Store にある既存のウィジェットで満足できるものがなかったため、個人用に作りました。

## 使い方

必要環境: Windows 11 / .NET 10 SDK (Windows Desktop ワークロード込み)

```powershell
dotnet build .\src\OkidokeiWidget.App\OkidokeiWidget.App.csproj -c Release
```

でビルドし、生成された `src/OkidokeiWidget.App/bin/Release/net10.0-windows/OkidokeiWidget.App.exe`
を起動します。  
ウィジェットを右クリック →「詳細設定」から、見た目やモニタごとの表示、
自動起動の ON/OFF を設定できます。

## 技術スタック

WPF / C# (.NET 10)。設定は `%APPDATA%` 配下の JSON ファイルに保存します。

## リポジトリ構成

```
docs/
  requirements.md         要件定義書
  screenshots/            README 用のスクリーンショット
.specify/
  memory/constitution.md  プロジェクトの原則 (Spec Kit)
  bugs/                   バグ修正の記録 (Spec Kit の bug 拡張による調査・修正・検証)
specs/
  001-clock-widget/        機能仕様・実装計画・タスク (Spec Kit)
src/
  OkidokeiWidget.App/      WPF アプリ本体
  OkidokeiWidget.Core/     設定・モニタ判定などのロジック
tests/
  OkidokeiWidget.Core.Tests/  OkidokeiWidget.Core の単体テスト
```

## License

[MIT](LICENSE)
