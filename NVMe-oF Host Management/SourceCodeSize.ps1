$files = @(
    Get-ChildItem -Recurse -Include ('*.cs', '*.xaml') `
    | Where-Object { $_.FullName -notlike "*\obj\*" } `
    | Select-Object -ExpandProperty FullName
)

$totalCS = [PSCustomObject] @{
    Type = 'C#'
    Lines = 0
    Characters = 0
}

$totalXAML = [PSCustomObject] @{
    Type = 'XAML'
    Lines = 0
    Characters = 0
}

$perfilestats = $files | ForEach-Object {
    $filestats = Get-Content $_ | Measure-Object -Line -Character
    $path = Resolve-Path -Path $_ -Relative

    if ($_ -like '*.cs') {
        $totalCS.Lines += $filestats.Lines
        $totalCS.Characters += $filestats.Characters
    } elseif ($_ -like '*.xaml') {
        $totalXAML.Lines += $filestats.Lines
        $totalXAML.Characters += $filestats.Characters
    }

    return [PSCustomObject] @{
        Lines = $filestats.Lines
        Characters = $filestats.Characters
        Path = $path
    }
}

$totalSUM = [PSCustomObject] @{
    Type = ''
    Lines = $totalCS.Lines + $totalXAML.Lines
    Characters = $totalCS.Characters + $totalXAML.Characters
}

$perfilestats | Format-Table Lines, Characters, Path | Write-Output | Out-Host

($totalCS, $totalXAML, $totalSUM) | Select-Object Type, Lines, Characters, @{ Name = "KiB"; Expr = { ($_.Characters / 1024).ToString("F2") } } | Write-Output | Out-Host

Write-Output "Press any key to continue..." | Out-Host;
$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') | Out-Null
