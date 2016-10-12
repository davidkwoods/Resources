Set-Alias npp "C:\Program Files (x86)\Notepad++\notepad++.exe"
Set-Alias stp "C:\tools\StackParser\bin\StackParser.exe"
Set-Alias mergeit "Merge-GitUpstream"
Set-Alias pushit "Push-GitUpstream"
Set-Alias pushp "Push-Personal"
Set-Alias pullit "Fetch-All-Prune-Merge"
Set-Alias touch "Touch-File"
Set-Alias gitkk "Gitk-All"
Set-Alias findf "Find-InFiles"

$desktop = Get-Item ([Environment]::GetFolderPath("Desktop"))

Function Touch-File
{
    $file = $args[0]
    if($file -eq $null) {
        throw "No filename supplied"
    }

    if(Test-Path $file)
    {
        (Get-ChildItem $file).LastWriteTime = Get-Date
    }
    else
    {
        New-Item -Type File -Name $file
    }
}

Function Merge-GitUpstream
{
    $status = Get-GitStatus
    if ($status)
    {
        if ($status.Upstream)
        {
            git merge --no-commit $status.Upstream
        }
    }
}

Function Push-GitUpstream ([switch]$Force)
{
    $status = Get-GitStatus
    if ($status)
    {
        if ($status.Upstream)
        {
            if ($Force)
            {
                git push origin HEAD:$($status.Upstream.Replace('origin/', '')) -f
            }
            else
            {
                git push origin HEAD:$($status.Upstream.Replace('origin/', ''))
            }
        }
    }
}

Function Push-Personal
{
    $status = Get-GitStatus
    if ($status)
    {
        $branch = $status.Branch
        $upstreamBranch = "personal/dwoo/$branch"
        git push -u origin ${branch}:$upstreamBranch
    }
}

Function Fetch-All-Prune-Merge ([switch]$All)
{
    if ($All)
    {
        git fetch --all --prune
    }
    else
    {
        git fetch --prune
    }
    Merge-GitUpstream
}

Function Gitk-All
{
	gitk --all
}

Function Find-InFiles([string]$Pattern)
{
    Get-ChildItem -recurse | Select-String $Pattern -List | select -ExpandProperty Path
}

# Load posh-git example profile
. 'C:\Git\posh-git\profile.example.ps1'


# Chocolatey profile
$ChocolateyProfile = "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
if (Test-Path($ChocolateyProfile)) {
  Import-Module "$ChocolateyProfile"
}
