# Exact Fertility Growing Zones

RimWorld 1.6向けの、指定した肥沃度に一致するマスだけに農業ゾーンを作成するModです。

- 導入用フォルダー：`ExactFertilityGrowingZones`
- 配布ZIP：[GitHub Releases](https://github.com/KotaLow/RimWorld-Exact-Fertility-Growing-Zones/releases/latest)
- 詳しい導入方法・操作方法・検証状況：[`ExactFertilityGrowingZones/README.md`](ExactFertilityGrowingZones/README.md)

1.0.1では、ドラッグ中に肥沃度が一致しない各マスで繰り返していたメッセージ翻訳・数値整形をキャッシュしました。1.0.2は公開向けにMod情報とビルド設定を整理した版です。マスごとの設置条件は引き続き毎回確認します。更新時はゲームを終了し、既存の同名Modフォルダーへ上書きしてください。

## ビルド

Windows、.NET SDK、.NET Framework 4.7.2 Developer Pack、RimWorld 1.6、Harmonyが必要です。ゲームのDLLやHarmonyは同梱していません。PowerShellでこのフォルダーを開き、実行します。

```powershell
.\build.ps1 -RimWorldPath 'C:\Games\RimWorld'
```

上のパスは例です。実際のゲームのインストール場所を指定してください。Harmonyが標準のSteam Workshopの位置にない場合は、DLLも指定します。

```powershell
.\build.ps1 -RimWorldPath 'C:\Games\RimWorld' -HarmonyPath 'C:\Mods\Harmony\1.6\Assemblies\0Harmony.dll'
```

`RIMWORLD_PATH`と`HARMONY_DLL_PATH`環境変数でも指定できます。`build.ps1`はReleaseビルド後、`dist`にビルド中間ファイルや参照DLLを含めずにZIPを作成します。ゲームのModsフォルダーや設定ファイルは変更しません。リポジトリにはソースを置き、導入用のビルド済みDLLはReleaseのZIPに含めています。

## 自動検証

肥沃度ロジックとメッセージキャッシュのテストには.NET 10 SDKを使用します。テストには、文字列整形だけを比較するベンチマークも含まれます。ゲームのFPSを測定するものではありません。

```powershell
dotnet run --project .\tests\FertilityTests.csproj -c Release
```

実際のゲームDLLへのHarmonyパッチ適用と、標準の判定を維持する処理を確認するテストは、Windows PowerShell 5.1で実行します。

```powershell
& "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-Harmony.ps1 -RimWorldPath 'C:\Games\RimWorld'
```

ゲームのインストール場所が異なる場合は、このテストにも`-RimWorldPath`と`-HarmonyPath`を渡してください。このテストはテスト用プロセス内のDLLにのみパッチを適用し、ゲームのファイルを変更しません。PowerShell 7はこのHarmony版が使用する.NET Framework APIに対応しないため、このテストには使用しません。
