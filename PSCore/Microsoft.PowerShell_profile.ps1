. "~\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1"

function elevate {
    $here = Get-Location
    $args = ("-NoExit","-Command `"pushd '$here'`"")
    Start-Process pwsh.exe -Verb RunAs -ArgumentList $args
}

Import-Module "C:\git\oh-my-posh\oh-my-posh.psd1"
Set-Theme PsCoreSeparated
