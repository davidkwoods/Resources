#requires -Version 2 -Modules posh-git

# This belongs in the $ThemeSettings.MyThemesLocation directory after installing Oh My Posh
# See https://www.hanselman.com/blog/HowToMakeAPrettyPromptInWindowsTerminalWithPowerlineNerdFontsCascadiaCodeWSLAndOhmyposh.aspx
# and https://github.com/JanDeDobbeleer/oh-my-posh
# Maybe try Consolas NF (https://github.com/Znuff/consolas-powerline) instead of Hanselman's recommended Delugia Nerd Font
function Write-Theme {
    param(
        [bool]
        $lastCommandFailed,
        [string]
        $with
    )

    $lastColor = $sl.Colors.PromptBackgroundColor
    $prompt = Write-Prompt -Object $sl.PromptSymbols.StartSymbol -ForegroundColor $sl.Colors.PromptForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor

    #check the last command state and indicate if failed
    If ($lastCommandFailed) {
        $prompt += Write-Prompt -Object "$($sl.PromptSymbols.FailedCommandSymbol) " -ForegroundColor $sl.Colors.CommandFailedIconForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
    }

    #check for elevated prompt
    If (Test-Administrator) {
        $prompt += Write-Prompt -Object "$($sl.PromptSymbols.ElevatedSymbol) " -ForegroundColor $sl.Colors.AdminIconForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
    }

    $user = $sl.CurrentUser
    $computer = [System.Environment]::MachineName
    $path = Get-FullPath -dir $pwd
    if (Test-NotDefaultUser($user)) {
        $prompt += Write-Prompt -Object "$user@$computer " -ForegroundColor $sl.Colors.SessionInfoForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
    }

    if (Test-VirtualEnv) {
        $prompt += Set-VcsSection $sl.Colors.SessionInfoBackgroundColor $sl.Colors.VirtualEnvBackgroundColor -AddSpace
        $prompt += Write-Prompt -Object "$($sl.PromptSymbols.VirtualEnvSymbol) $(Get-VirtualEnvName) " -ForegroundColor $sl.Colors.VirtualEnvForegroundColor -BackgroundColor $sl.Colors.VirtualEnvBackgroundColor
        $prompt += Set-VcsSection $sl.Colors.VirtualEnvBackgroundColor $sl.Colors.PromptBackgroundColor -AddSpace
    }
    else {
        $prompt += Set-VcsSection $sl.Colors.SessionInfoBackgroundColor $sl.Colors.PromptBackgroundColor -AddSpace
    }

    # Writes the drive portion
    $prompt += Write-Prompt -Object "$path " -ForegroundColor $sl.Colors.PromptForegroundColor -BackgroundColor $sl.Colors.PromptBackgroundColor

    $status = Get-VCSStatus
    if ($status) {
        $themeInfo = Get-VcsInfoSeparated -status ($status)
        $lastColor = $themeInfo.BranchBackgroundColor
        $newColor = $lastColor
        $gitForegroundColor = $sl.Colors.GitForegroundColor
        $prompt += Set-VcsSection $sl.Colors.PromptBackgroundColor $lastColor
        $prompt += Write-Prompt -Object " $($themeInfo.BranchText) " -BackgroundColor $lastColor -ForegroundColor $gitForegroundColor
        if ($themeInfo.IndexText) {
            $newColor = $themeInfo.IndexBackgroundColor
            $prompt += Set-VcsSection $lastColor $newColor
            $prompt += Write-Prompt -Object " $($themeInfo.IndexText) " -BackgroundColor $newColor -ForegroundColor $gitForegroundColor
            $lastColor = $newColor
        }
        if ($themeInfo.WorkingText) {
            $newColor = $themeInfo.WorkingBackgroundColor
            if ($themeInfo.IndexText) {
                $prompt += Set-VcsSection $lastColor $newColor $sl.PromptSymbols.SegmentSeparatorForwardSymbol
            }
            else {
                $prompt += Set-VcsSection $lastColor $newColor
            }
            $prompt += Write-Prompt -Object " $($themeInfo.WorkingText) " -BackgroundColor $newColor -ForegroundColor $gitForegroundColor
            $lastColor = $newColor
        }
    }

    # Writes the postfix to the prompt
    $prompt += Write-Prompt -Object $sl.PromptSymbols.SegmentForwardSymbol -ForegroundColor $lastColor

    $timeStamp = Get-Date -UFormat %R
    $timestamp = "[$timeStamp]"

    $prompt += Set-CursorForRightBlockWrite -textLength ($timestamp.Length + 1)
    $prompt += Write-Prompt $timeStamp -ForegroundColor $sl.Colors.PromptForegroundColor

    $prompt += Set-Newline

    if ($with) {
        $prompt += Write-Prompt -Object "$($with.ToUpper()) " -BackgroundColor $sl.Colors.WithBackgroundColor -ForegroundColor $sl.Colors.WithForegroundColor
    }
    $prompt += Write-Prompt -Object ($sl.PromptSymbols.PromptIndicator) -ForegroundColor $sl.Colors.PromptBackgroundColor
    $prompt += ' '
    $prompt
}

function Set-VcsSection {
    param(
        $oldColor,
        $newColor,
        [String] $Demarcation = $sl.PromptSymbols.SegmentForwardSymbol,
        [Switch] $AddSpace
    )
    if ($AddSpace) {
        $Demarcation += " "
    }
    return Write-Prompt -Object $Demarcation -ForegroundColor $oldColor -BackgroundColor $newColor
}

$sl = $global:ThemeSettings #local settings
$sl.PromptSymbols.StartSymbol = ''
$sl.PromptSymbols.PromptIndicator = [char]::ConvertFromUtf32(0x276F)
$sl.PromptSymbols.SegmentForwardSymbol = [char]::ConvertFromUtf32(0xE0B0)
$sl.PromptSymbols.SegmentBackwardSymbol = [char]::ConvertFromUtf32(0xE0B2)
$sl.PromptSymbols.SegmentSeparatorForwardSymbol  = [char]::ConvertFromUtf32(0xE0C6)
$sl.PromptSymbols.SegmentSeparatorBackwardSymbol  = [char]::ConvertFromUtf32(0xE0C7)
$sl.Colors.PromptForegroundColor = [ConsoleColor]::White
$sl.Colors.PromptSymbolColor = [ConsoleColor]::White
$sl.Colors.PromptHighlightColor = [ConsoleColor]::DarkBlue
$sl.Colors.PromptBackgroundColor = [ConsoleColor]::DarkBlue
$sl.Colors.GitForegroundColor = [ConsoleColor]::Black
$sl.Colors.GitDefaultColor = [ConsoleColor]::Blue
$sl.Colors.GitIndexChangesColor = [ConsoleColor]::DarkCyan
$sl.Colors.GitLocalChangesColor = [ConsoleColor]::Cyan
$sl.Colors.GitNoLocalChangesAndAheadColor = [ConsoleColor]::DarkGreen
$sl.Colors.GitNoLocalChangesAndAheadAndBehindColor = [ConsoleColor]::DarkRed
$sl.Colors.GitNoLocalChangesAndBehindColor = [ConsoleColor]::Red
$sl.Colors.WithForegroundColor = [ConsoleColor]::DarkRed
$sl.Colors.WithBackgroundColor = [ConsoleColor]::Magenta
$sl.Colors.VirtualEnvBackgroundColor = [System.ConsoleColor]::Red
$sl.Colors.VirtualEnvForegroundColor = [System.ConsoleColor]::White