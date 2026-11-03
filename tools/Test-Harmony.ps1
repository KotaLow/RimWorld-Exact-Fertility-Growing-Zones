param(
    [string]$RimWorldPath = $env:RIMWORLD_PATH,
    [string]$HarmonyPath = $env:HARMONY_DLL_PATH
)
# Run with Windows PowerShell 5.1 (.NET Framework), matching the mod's framework.
$ErrorActionPreference = 'Stop'
if (!$RimWorldPath) { throw 'Specify -RimWorldPath or set RIMWORLD_PATH.' }
if (!$HarmonyPath) {
    $HarmonyPath = [IO.Path]::GetFullPath((Join-Path $RimWorldPath '..\..\workshop\content\294100\2009463077\1.6\Assemblies\0Harmony.dll'))
}
$managed = Join-Path $RimWorldPath 'RimWorldWin64_Data\Managed'
foreach ($dll in @('UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','Assembly-CSharp.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $managed $dll))
}
$harmonyAssembly = [Reflection.Assembly]::LoadFrom($HarmonyPath)
$modPath = Join-Path $PSScriptRoot '..\ExactFertilityGrowingZones\Assemblies\ExactFertilityGrowingZones.dll'
$mod = [Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($modPath))
$harmonyType = $harmonyAssembly.GetType('HarmonyLib.Harmony')
$owner = 'kotalow.exactfertilitygrowingzones'
$harmony = [Activator]::CreateInstance($harmonyType, @($owner))
try {
    $harmony.PatchAll($mod)
    $game = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
    foreach ($entry in @(
        @('RimWorld.Designator_ZoneAdd_Growing','CanDesignateCell'),
        @('Verse.Designator','DoExtraGuiControls')
    )) {
        $method = $game.GetType($entry[0]).GetMethod($entry[1])
        $info = $harmonyType.GetMethod('GetPatchInfo').Invoke($null, @($method))
        if ($null -eq $info -or $info.Owners -notcontains $owner -or $info.Postfixes.Count -ne 1) {
            throw ('Patch was not applied: ' + $method)
        }
        Write-Output ('PASS: Harmony postfix applied to ' + $entry[0] + '.' + $entry[1])
    }

    # Exercise the postfix's early returns against the real AcceptanceReport type.
    $flags = [Reflection.BindingFlags]'NonPublic,Static'
    $state = $mod.GetType('ExactFertilityGrowingZones.FilterState')
    $enabled = $state.GetField('Enabled', $flags)
    $postfix = $mod.GetType('ExactFertilityGrowingZones.GrowingCellPatch').GetMethod('Postfix', $flags)
    $reportType = $game.GetType('Verse.AcceptanceReport')
    $toReport = $reportType.GetMethods() | Where-Object {
        $_.Name -eq 'op_Implicit' -and $_.GetParameters()[0].ParameterType -eq [bool]
    }
    $cell = [Activator]::CreateInstance($game.GetType('Verse.IntVec3'))
    $enabled.SetValue($null, $false)
    $accepted = $toReport.Invoke($null, @($true))
    $arguments = [object[]]@($null, $cell, $accepted)
    [void]$postfix.Invoke($null, $arguments)
    if (!$arguments[2].Accepted) { throw 'Disabled filter changed acceptance.' }
    Write-Output 'PASS: Disabled filter preserves vanilla acceptance.'

    $enabled.SetValue($null, $true)
    $rejected = [Activator]::CreateInstance($reportType, @('original rejection'))
    $arguments = [object[]]@($null, $cell, $rejected)
    [void]$postfix.Invoke($null, $arguments)
    if ($arguments[2].Accepted -or $arguments[2].Reason -ne 'original rejection') {
        throw 'Enabled filter changed a vanilla rejection.'
    }
    Write-Output 'PASS: Enabled filter preserves vanilla rejection and its reason.'
} finally {
    if ($null -ne $enabled) { $enabled.SetValue($null, $false) }
    $harmony.UnpatchAll($owner)
}
