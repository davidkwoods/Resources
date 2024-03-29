Set-Alias npp "C:\Program Files\Notepad++\notepad++.exe"
Set-Alias mergeit "Merge-GitUpstream"
Set-Alias pushit "Push-GitUpstream"
Set-Alias pushp "Push-Personal"
Set-Alias pushr "Push-Release"
Set-Alias pushh "Push-Hotfix"
Set-Alias pullit "Fetch-All-Prune-Merge"
Set-Alias touch "Touch-File"
Set-Alias gitkk "Gitk-All"
Set-Alias findf "Find-InFiles"
Set-Alias pruneit "Remove-DeadBranches"
Set-Alias cleanit "Clean-RestoreNuget"
Set-Alias diffit "Diff-Commit"
Set-Alias diffitf "Diff-Fork"
Set-Alias blame "Blame-File"
Set-Alias mklink "Make-Link"
Set-Alias wh "where.exe"
Set-Alias vs "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"

$desktop = Get-Item ([Environment]::GetFolderPath("Desktop"))

$env:Path += ";C:\Tools\NuGet\bin;"

function fetchem {
    gci -Directory | %{ Write-Host $_; git -C $_.FullName fapm }
}

function pygen {
    # python2 -m compileall .
    python3 -m compileall .
}

function New-Password
{
    param
    (
        [int]
        $Length = (Get-Random -Minimum 16 -Maximum 20),

        [int]
        $NumSpecialCharacters = (Get-Random -Minimum 4 -Maximum 10)
    )

    Add-Type -AssemblyName System.Web
    [System.Web.Security.Membership]::GeneratePassword($Length, $NumSpecialCharacters)
}

function timeit {
    param(
        [Parameter(Mandatory=$true)]
        [ScriptBlock] $Action,
        [int] $Times = 1
    )
    if ($Times -lt 1) { $Times = 1 }
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    for ($attempt = 1; $attempt -le $Times; $attempt++) {
        $Action.Invoke()
    }
    $stopwatch.Stop()
    Write-Host "avg: $($stopwatch.Elapsed / $Times)"
    $stopwatch
}

Function Touch-File([switch]$LF) {
    $file = $args[0]
    if($null -eq $file) {
        throw "No filename supplied"
    }

    if(Test-Path $file) {
        (Get-ChildItem $file).LastWriteTime = Get-Date
    }
    else {
        if ($LF) {
            Set-Content -NoNewline -Encoding utf8 -Value "`n" $file
        }
        else {
            New-Item -Type File -Path $file
        }
    }
}

Function Diff-Commit([string] $Commit = "HEAD") {
    odd -git ${Commit}^ $Commit
}

Function Diff-Fork([string] $branch) {
    if (!$branch) {
        $branch = (Get-GitStatus).Branch
    }
    odd -git "$branch.fork" $branch
}

Function Blame-File($file) {
    $filename = (gi $file).Name
    $name = "blame_$($filename)_$(New-Guid).txt"
    git blame -- $file > $name
    npp $name
    Start-Sleep -s 2
    del $name
}

Function Merge-GitUpstream {
    $status = Get-GitStatus
    if ($status) {
        if ($status.Upstream) {
            git merge --no-commit $status.Upstream
        }
    }
}

Function Push-GitUpstream ([switch]$Force) {
    $status = Get-GitStatus
    if ($status) {
        if ($status.Upstream) {
            $upstream = $status.Upstream.Split("/")[0]
            $upstreamPath = $status.Upstream.Replace($upstream + "/", '')
            if ($Force) {
                git push $upstream HEAD:$upstreamPath -f
            }
            else {
                git push $upstream HEAD:$upstreamPath
            }
        }
    }
}

Function Push-Personal ([string] $Repo = "origin") {
    Push-Prefix "user/david" $Repo
}

Function Push-Release ([string] $Repo = "origin") {
    Push-Prefix "release" $Repo
}

Function Push-Hotfix ([string] $Repo = "origin") {
    Push-Prefix "hotfix" $Repo
}

Function Push-Prefix([string] $Prefix, [string] $Repo = "origin") {
    $status = Get-GitStatus
    if ($status) {
        $branch = $status.Branch
        $upstreamBranch = "${Prefix}/$branch"
        git push -u $Repo ${branch}:$upstreamBranch
    }
    else {
        Write-Host "Not in a Git repo"
    }
}

Function Remove-DeadBranches() {
    if (Get-GitStatus) {
        git branch -vv | ForEach-Object{if ($_ -match "^[\+\*]? +(.*?) +.+?(?:\[.+?: gone\])") {$matches[1]}} | ForEach-Object{git branch -D $_}
        
        # Run a second time for fork branches that may now be missing their main branch.
        git branch -vv | ForEach-Object{if ($_ -match "^[\+\*]? +(.*?) +.+?(?:\[.+?: gone\])") {$matches[1]}} | ForEach-Object{git branch -D $_}
        
        #$remainingBranches = git branch | ForEach-Object{$_.Split(' ')[-1]}
        #$remainingBranches | Where-Object{$_.Contains(".fork") -and -not $remainingBranches.Contains($_.Replace(".fork", ""))} | ForEach-Object{git branch -D $_}
    }
}

Function Fetch-All-Prune-Merge ([switch]$All) {
    if ($All) {
        git fetch --all --prune
    }
    else {
        git fetch --prune
    }
    
    Merge-GitUpstream
}

Function Gitk-All {
    gitk --all
}

Function Clean-RestoreNuget([switch] $Scorch) {
    if ($Scorch) {
        git scorch
    }
    else {
        git clean -dxf
    }
    
    Get-ChildItem *.sln | %{nuget restore $_}
}

Function Find-InFiles ([string[]]$Patterns, [Parameter(ValueFromPipeline=$true)]$Files, [switch]$Regex) {
    Begin {
        $Patterns = @($Patterns)
        if (!$Regex) {
            for($i = 0; $i -lt $Patterns.Count; $i++) {
                $Patterns[$i] = [regex]::Escape($Patterns[$i])
            }
        }
        
        $filler = ')|('
        $Pattern = "($($Patterns -join $filler))"
    }
    
    Process {
        if ($Files) {
            $Files | Select-String $Pattern -List | Select-Object -ExpandProperty Path
        }
        else {
            Get-ChildItem -Recurse | Select-String $Pattern -List | Select-Object -ExpandProperty Path
        }
    }
}

function Test-IsAdmin() 
{
    # Get the current ID and its security principal
    $windowsID = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object System.Security.Principal.WindowsPrincipal($windowsID)
    
    # Get the Admin role security principal
    $adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
    
    # Are we an admin role?
    $windowsPrincipal.IsInRole($adminRole)
}

Function Make-Link {
    param(
        [ValidateScript({Test-Path $_})]
        $Source,

        [ValidateScript({!(Test-Path $_)})]
        $Dest,
        
        [switch]$Hard
        )
    $sourcePath = (Get-Item $Source).FullName
    if ($Hard) {
        [void](New-Item -Type HardLink -Path $Dest -Value $sourcePath)
    }
    elseif (Test-IsAdmin) {
        [void](New-Item -Type SymbolicLink -Path $Dest -Value $sourcePath | Out-Null)
    }
    else {
        [string[]]$argList = ('-NoLogo', '-NoProfile', '-ExecutionPolicy Bypass', "-Command `"& {cd $pwd; New-Item -Type SymbolicLink -Path $Dest -Value $sourcePath}`"")
        Start-Process PowerShell.exe -Verb Runas -WindowStyle Hidden -Wait -ArgumentList $argList
    }
    
    Get-Item $Dest
}

function elevate {
    $here = Get-Location
    $myArgs = ("-NoExit","-Command `"pushd '$here'`"")
    Start-Process PowerShell.exe -Verb RunAs -ArgumentList $myArgs
}

function Decode-Safelink($url) {
    if (!$url) {
        $fromClipboard = $true
        $url = Get-Clipboard -TextFormatType Text
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

# Load posh-git example profile
# Import-Module "C:\Git\posh-git\src\posh-git.psd1"

# Must first call `winget install JanDeDobbeleer.OhMyPosh` to allow this
# Plus `choco install cascadia-code-nerd-font` for the nice glyphs
$env:POSH_GIT_ENABLED = $true
oh-my-posh --init --shell pwsh --config "C:\Users\dkwoo\OneDrive\Documents\PowerShell\PoshThemes\posh-segmented-theme.omp.json" | Invoke-Expression
if (!$?) {
    Write-Host "oh-my-posh command failed.  May need to run 'winget install JanDeDobbeleer.OhMyPosh' first."
}

# Chocolatey profile
$ChocolateyProfile = "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
if (Test-Path($ChocolateyProfile)) {
Import-Module "$ChocolateyProfile"
}
