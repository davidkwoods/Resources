. "$env:HOMEPATH\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1"
function elevate {
    $here = Get-Location
    $args = ("-NoExit","-Command `"pushd '$here'`"")
    Start-Process pwsh.exe -Verb RunAs -ArgumentList $args
}
