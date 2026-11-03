param(
    [string]$RimWorldPath = $env:RIMWORLD_PATH
)
$ErrorActionPreference = 'Stop'
if (!$RimWorldPath) { throw 'Specify -RimWorldPath or set RIMWORLD_PATH.' }
$managed = Join-Path $RimWorldPath 'RimWorldWin64_Data\Managed'
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.IMGUIModule.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$opcodes = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields()) {
    $opcode = $field.GetValue($null)
    $opcodes[[int]$opcode.Value -band 65535] = $opcode
}
function Show-Method([Reflection.MethodBase]$Method) {
    Write-Output ($Method.DeclaringType.FullName + ' :: ' + $Method)
    $body = $Method.GetMethodBody()
    if ($null -eq $body) { return }
    $bytes = $body.GetILAsByteArray()
    for ($position = 0; $position -lt $bytes.Length;) {
        $offset = $position
        $code = [int]$bytes[$position++]
        if ($code -eq 254) { $code = 65024 + [int]$bytes[$position++] }
        $opcode = $opcodes[$code]
        $operand = ''
        $length = switch ($opcode.OperandType.ToString()) {
            'InlineNone' { 0 }
            'InlineVar' { 2 }
            'ShortInlineVar' { 1 }
            'ShortInlineI' { 1 }
            'ShortInlineBrTarget' { 1 }
            'InlineI8' { 8 }
            'InlineR' { 8 }
            'InlineSwitch' { 4 + 4 * [BitConverter]::ToInt32($bytes, $position) }
            default { 4 }
        }
        if ($opcode.OperandType.ToString() -in @('InlineMethod','InlineField','InlineType','InlineTok')) {
            $operand = $Method.Module.ResolveMember([BitConverter]::ToInt32($bytes, $position))
        } elseif ($opcode.OperandType.ToString() -eq 'InlineString') {
            $operand = $Method.Module.ResolveString([BitConverter]::ToInt32($bytes, $position))
        } elseif ($length -eq 4) {
            $operand = [BitConverter]::ToInt32($bytes, $position)
        } elseif ($length -eq 1) {
            $operand = [int]$bytes[$position]
        }
        '{0:X4} {1,-14} {2}' -f $offset, $opcode.Name, $operand
        $position += $length
    }
}
$flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
foreach ($entry in @(
    @('Verse.Designator','DoExtraGuiControls'),
    @('RimWorld.Designator_ZoneAdd_Growing','CanDesignateCell'),
    @('RimWorld.Designator_ZoneAdd','DesignateMultiCell'),
    @('RimWorld.Designator_ZoneAdd','<DesignateMultiCell>b__13_0'),
    @('Verse.DesignatorManager','DesignationManagerOnGUI'),
    @('Verse.DesignatorManager','ProcessInputEvents'),
    @('Verse.DesignationDragger','EndDrag'),
    @('Verse.DesignationDragger','TryAddDragCell'),
    @('RimWorld.Designator_Plan','DoExtraGuiControls')
)) {
    $type = $assembly.GetType($entry[0])
    if ($null -ne $type) {
        $method = $type.GetMethod($entry[1], $flags)
        if ($null -ne $method) { Show-Method $method }
    }
}
$map = $assembly.GetType('Verse.Map')
foreach ($name in @('fertilityGrid','fogGrid','cellIndices','terrainGrid')) {
    $field = $map.GetField($name)
    $field.ToString()
    $field.FieldType.GetMethods($flags) | Where-Object Name -Match 'FertilityAt|IsFogged|CellToIndex|IndexToCell' | ForEach-Object { $_.ToString() }
}
