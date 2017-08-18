# Test if admin
function Test-IsAdmin() 
{
    # Get the current ID and its security principal
    $windowsID = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object System.Security.Principal.WindowsPrincipal($windowsID)
    
    # Get the Admin role security principal
    $adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
    
    # Are we an admin role?
    if ($windowsPrincipal.IsInRole($adminRole))
    {
        $true
    }
    else
    {
        $false
    }
}

# Relaunch the script if not admin
function Invoke-RequireAdmin
{
    Param(
    [Parameter(Position=0, Mandatory=$true, ValueFromPipeline=$true)]
    [System.Management.Automation.InvocationInfo]
    $MyInvocation,
    [Parameter(Position=1, Mandatory=$false)]
    $arguments)

    if (-not (Test-IsAdmin))
    {
        Write-Host "Elevating"
        # Build base arguments for powershell.exe
        [string[]]$argList = ('-NoLogo', '-NoProfile', '-NoExit', '-ExecutionPolicy Bypass', "-File $PSCommandPath", "-Command `"& {New-SymLink $arguments}")

        # Add 
        #$argList += $MyInvocation.BoundParameters.GetEnumerator() | Foreach {"-$($_.Key)", "$($_.Value)"}
        #$argList += $MyInvocation.UnboundArguments
        #$argList += $arguments

        Write-Host "Using -ArgumentList $argList"
        try
        {
            $process = Start-Process PowerShell.exe -PassThru -Verb Runas -Wait -WorkingDirectory $pwd -ArgumentList $argList
            exit $process.ExitCode
            Write-Host "done"
        }
        catch {
            Write-Host "Exception"
            Write-Host $error[0]
        }

        # Generic failure code
        Write-Host "General failure"
        exit 1 
    }
}

Function New-SymLink {
    <#
        .SYNOPSIS
            Creates a Symbolic link to a file or directory

        .DESCRIPTION
            Creates a Symbolic link to a file or directory as an alternative to mklink.exe

        .PARAMETER Path
            Name of the path that you will reference with a symbolic link.

        .PARAMETER SymName
            Name of the symbolic link to create. Can be a full path/unc or just the name.
            If only a name is given, the symbolic link will be created on the current directory that the
            function is being run on.

        .PARAMETER File
            Create a file symbolic link

        .PARAMETER Directory
            Create a directory symbolic link

        .NOTES
            Name: New-SymLink
            Author: Boe Prox
            Created: 15 Jul 2013


        .EXAMPLE
            New-SymLink -Path "C:\users\admin\downloads" -SymName "C:\users\admin\desktop\downloads" -Directory

            SymLink                          Target                   Type
            -------                          ------                   ----
            C:\Users\admin\Desktop\Downloads C:\Users\admin\Downloads Directory

            Description
            -----------
            Creates a symbolic link to downloads folder that resides on C:\users\admin\desktop.

        .EXAMPLE
            New-SymLink -Path "C:\users\admin\downloads\document.txt" -SymName "SomeDocument" -File

            SymLink                             Target                                Type
            -------                             ------                                ----
            C:\users\admin\desktop\SomeDocument C:\users\admin\downloads\document.txt File

            Description
            -----------
            Creates a symbolic link to document.txt file under the current directory called SomeDocument.
    #>
    [cmdletbinding(
        DefaultParameterSetName = 'Directory',
        SupportsShouldProcess=$True
    )]
    Param (
        [parameter(Position=0,ParameterSetName='Directory',ValueFromPipeline=$True,
            ValueFromPipelineByPropertyName=$True,Mandatory=$True)]
        [parameter(Position=0,ParameterSetName='File',ValueFromPipeline=$True,
            ValueFromPipelineByPropertyName=$True,Mandatory=$True)]
        [ValidateScript({
            If (Test-Path $_) {$True} Else {
                Throw "`'$_`' doesn't exist!"
            }
        })]
        [string]$Path,
        [parameter(Position=1,ParameterSetName='Directory')]
        [parameter(Position=1,ParameterSetName='File')]
        [string]$SymName,
        [parameter(Position=2,ParameterSetName='File')]
        [switch]$File,
        [parameter(Position=2,ParameterSetName='Directory')]
        [switch]$Directory
    )
    Begin {
        Try {
            $null = [mklink.symlink]
        } Catch {
            Add-Type @"
            using System;
            using System.Runtime.InteropServices;
 
            namespace mklink
            {
                public class symlink
                {
                    [DllImport("kernel32.dll")]
                    public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
                }
            }
"@
        }
    }
    Process {
        Write-Host "Executing with: -Path:$Path -SymName:$SymName -File:$File -Directory:$Directory"
        $item = gi $Path
        $arguments = ('-Path', $item.FullName, '-SymName', "$($item.Parent.FullName)\$SymName", "-$($PScmdlet.ParameterSetName)")
        Invoke-RequireAdmin $script:MyInvocation $arguments

            Write-Host "Test-Admin thinks we're in admin mode"

        #Assume target Symlink is on current directory if not giving full path or UNC
        If ($SymName -notmatch "^(?:[a-z]:\\)|(?:\\\\\w+\\[a-z]\$)") {
            $SymName = "{0}\{1}" -f $pwd,$SymName
        }
        $Flag = @{
            File = 0
            Directory = 1
        }
        If ($PScmdlet.ShouldProcess($Path,'Create Symbolic Link')) {
            Try {
                $return = [mklink.symlink]::CreateSymbolicLink($SymName,$Path,$Flag[$PScmdlet.ParameterSetName])
                If ($return) {
                    $object = New-Object PSObject -Property @{
                        SymLink = $SymName
                        Target = $Path
                        Type = $PScmdlet.ParameterSetName
                    }
                    $object.pstypenames.insert(0,'System.File.SymbolicLink')
                    $object
                } Else {
                    Throw "Unable to create symbolic link!"
                }
            } Catch {
                Write-warning ("{0}: {1}" -f $path,$_.Exception.Message)
            }
        }
    }
 }