. "~\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1"

# Now that the basic profile is loaded, override with PSCore-specific stuff

function elevate {
    $here = Get-Location
    $args = ("-NoExit","-Command `"pushd '$here'`"")
    Start-Process pwsh.exe -Verb RunAs -ArgumentList $args
}

function New-Password {
    param(
        [Parameter(Mandatory = $false)]
        [ValidateRange(12, 256)]
        [int]
        $length = (Get-Random -Minimum 16 -Maximum 20)
    )
    $symbols = '!@#$%^&*'.ToCharArray()
    $characterList = 'a'..'z' + 'A'..'Z' + '0'..'9' + $symbols
    do {
        $password = ""
        for ($i = 0; $i -lt $length; $i++) {
            $randomIndex = [System.Security.Cryptography.RandomNumberGenerator]::GetInt32(0, $characterList.Length)
            $password += $characterList[$randomIndex]
        }

        [int]$hasLowerChar = $password -cmatch '[a-z]'
        [int]$hasUpperChar = $password -cmatch '[A-Z]'
        [int]$hasDigit = $password -match '[0-9]'
        [int]$hasSymbol = $password.IndexOfAny($symbols) -ne -1

    }
    until (($hasLowerChar + $hasUpperChar + $hasDigit + $hasSymbol) -ge 3)
    $password
}

function Decode-Safelink($url) {
    if (!$url) {
        $fromClipboard = $true
        $url = Get-Clipboard
    }

    $bases = @("https://na01.safelinks.protection.outlook.com/",
               "https://nam06.safelinks.protection.outlook.com/")

    foreach ($base in $bases)
    {
        if ($url.StartsWith($base))
        {
            Add-Type -AssemblyName System.Web
            $url = [Web.HttpUtility]::ParseQueryString(([uri]$url).Query)["url"]
            if ($fromClipboard) { Set-Clipboard -Value $Url }
            break
        }
    }
    $url
}

# From https://github.com/davidkwoods/oh-my-posh.git  (Fork of https://github.com/JanDeDobbeleer/oh-my-posh.git)

$modulePath = "C:\git\oh-my-posh\oh-my-posh.psd1"
if (Test-Path $modulePath) {
    Import-Module $modulePath -Force
    Set-Theme PsCoreSeparated
}
else {
    Set-PoshPrompt -Theme D:\temp\posh-theme2.omp.json
}
