$files = @(
    Get-ChildItem -Recurse -Filter "*.cs" `
    | Where-Object { $_.FullName -notlike "*\obj\*" } `
    | Select-Object -ExpandProperty FullName
)

$total = [PSCustomObject] @{
    Lines = 0
    Characters = 0
}

$perfilestats = $files | ForEach-Object {
    $filestats = Get-Content $_ | Measure-Object -Line -Character
    $path = Resolve-Path -Path $_ -Relative

    $total.Lines += $filestats.Lines
    $total.Characters += $filestats.Characters

    return [PSCustomObject] @{
        Lines = $filestats.Lines
        Characters = $filestats.Characters
        Path = $path
    }
}

$perfilestats | Format-Table Lines, Characters, Path | Write-Output | Out-Host

$total | Select-Object Lines, Characters, @{ Name = "KiB"; Expr = { ($_.Characters / 1024).ToString("F2") } } | Write-Output | Out-Host

Write-Output "Press any key to continue..." | Out-Host;
$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') | Out-Null
