param(
    [string]$RimWorldPath = $env:RIMWORLD_PATH,
    [string]$HarmonyPath = $env:HARMONY_DLL_PATH
)
$ErrorActionPreference = 'Stop'
if (!$RimWorldPath) { throw 'Specify -RimWorldPath or set RIMWORLD_PATH to the RimWorld 1.6 installation directory.' }
if (!$HarmonyPath) {
    $HarmonyPath = [IO.Path]::GetFullPath((Join-Path $RimWorldPath '..\..\workshop\content\294100\2009463077\1.6\Assemblies\0Harmony.dll'))
}
$project = Join-Path $PSScriptRoot 'ExactFertilityGrowingZones\Source\ExactFertilityGrowingZones.csproj'
dotnet build $project -c Release --nologo --verbosity minimal "-p:RimWorldPath=$RimWorldPath" "-p:HarmonyPath=$HarmonyPath"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$modRoot = Join-Path $PSScriptRoot 'ExactFertilityGrowingZones'
$dist = Join-Path $PSScriptRoot 'dist'
[void][IO.Directory]::CreateDirectory($dist)
$version = ([xml](Get-Content -LiteralPath (Join-Path $modRoot 'About\About.xml') -Raw)).ModMetaData.modVersion
$zipPath = Join-Path $dist "ExactFertilityGrowingZones-$version-RimWorld1.6.zip"
$files = @(
    Get-Item -LiteralPath (Join-Path $modRoot 'README.md')
    Get-Item -LiteralPath (Join-Path $modRoot 'Assemblies\ExactFertilityGrowingZones.dll')
    Get-ChildItem -LiteralPath (Join-Path $modRoot 'About') -Recurse -File
    Get-ChildItem -LiteralPath (Join-Path $modRoot 'Languages') -Recurse -File
    Get-ChildItem -LiteralPath (Join-Path $modRoot 'Source') -File | Where-Object Extension -In '.cs','.csproj'
)
$stream = [IO.File]::Open($zipPath, [IO.FileMode]::Create)
try {
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($modRoot.Length + 1).Replace('\','/')
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $file.FullName, "ExactFertilityGrowingZones/$relative", [IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $archive.Dispose() }
} finally { $stream.Dispose() }
Write-Output "Package: $zipPath"
