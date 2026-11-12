<#
.SYNOPSIS
  UI Automation smoke test of the Numbered Book Titles window, run through the test host on a
  copy of a project folder: takes a screenshot of each tab, double-clicks the first row (which
  must ask Paratext to go to that book's header), with -Fix changes the first row through the
  window, and closes the window.

  The mouse is never moved and nothing is typed: tick boxes are toggled through UI Automation,
  and clicks are posted as window messages to the test window only, so the test cannot click
  into another window that happens to be in front.

.EXAMPLE
  .\ui_smoke.ps1 -Project ..\..\TestProject\PDT15 -Out C:\temp\nbt -Lang en -Fix
#>
param(
    [Parameter(Mandatory = $true)][string]$Project,
    [Parameter(Mandatory = $true)][string]$Out,
    [string]$Lang = 'en',
    [string]$ExePath = '',
    # Also tick the first row, change it, and report the results box.
    [switch]$Fix
)
$ErrorActionPreference = 'Stop'
if (-not $ExePath) { $ExePath = Join-Path $PSScriptRoot 'TestHost\bin\Release\NumberedBookTitlesTestHost.exe' }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NbtWin {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] public struct Pt { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, int flags);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref Pt p);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    const int WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202, WM_LBUTTONDBLCLK = 0x203, BM_CLICK = 0xF5;
    static IntPtr Lp(IntPtr h, int sx, int sy) {
        Pt p; p.X = sx; p.Y = sy;
        ScreenToClient(h, ref p);
        return (IntPtr)((p.Y << 16) | (p.X & 0xFFFF));
    }
    /// <summary>A click (or double-click) at a screen point, posted to one window only.</summary>
    public static void Click(IntPtr h, int sx, int sy, bool twice) {
        IntPtr lp = Lp(h, sx, sy);
        PostMessage(h, WM_LBUTTONDOWN, (IntPtr)1, lp);
        PostMessage(h, WM_LBUTTONUP, IntPtr.Zero, lp);
        if (twice) {
            PostMessage(h, WM_LBUTTONDBLCLK, (IntPtr)1, lp);
            PostMessage(h, WM_LBUTTONUP, IntPtr.Zero, lp);
        }
    }
    public static void PressButton(IntPtr h) { PostMessage(h, BM_CLICK, IntPtr.Zero, IntPtr.Zero); }
}
'@
# Screen coordinates in real pixels, matching UI Automation's.
[NbtWin]::SetProcessDPIAware() | Out-Null

function Strings_ColBook { if ($Lang -like 'es*') { 'Libro' } elseif ($Lang -like 'fr*') { 'Livre' } else { 'Book' } }
function Strings_ColFix { if ($Lang -like 'es*') { 'Cambiar' } elseif ($Lang -like 'fr*') { 'Modifier' } else { 'Change' } }
function Strings_ConfirmTitle { if ($Lang -like 'es*') { "Cambiar t$([char]0xED)tulos de libros numerados" } elseif ($Lang -like 'fr*') { "Modifier les titres des livres num$([char]0xE9)rot$([char]0xE9)s" } else { 'Change numbered book titles' } }

New-Item -ItemType Directory -Force $Out | Out-Null
$copy = Join-Path $Out 'project'
if (Test-Path $copy) { Remove-Item -Recurse -Force $copy }
New-Item -ItemType Directory -Force $copy | Out-Null
Get-ChildItem -LiteralPath $Project -File | Where-Object { $_.Extension -notin '.bak', '.html' } | Copy-Item -Destination $copy
$bak = Join-Path $Out 'bak'
if (Test-Path $bak) { Remove-Item -Recurse -Force $bak }

$gotoLog = Join-Path $Out 'goto.txt'
if (Test-Path $gotoLog) { Remove-Item $gotoLog }
$psi = New-Object Diagnostics.ProcessStartInfo $ExePath
$psi.Arguments = "form `"$copy`" ALL $Lang `"$bak`""
$psi.EnvironmentVariables['NBT_GOTOLOG'] = $gotoLog
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$proc = [Diagnostics.Process]::Start($psi)

$AE = [Windows.Automation.AutomationElement]
$Scope = [Windows.Automation.TreeScope]
$root = $AE::RootElement
$byProcess = New-Object Windows.Automation.PropertyCondition ($AE::ProcessIdProperty, $proc.Id)
$win = $null
for ($i = 0; $i -lt 100 -and -not $win; $i++) {
    Start-Sleep -Milliseconds 200
    $win = $root.FindFirst($Scope::Children, $byProcess)
}
if (-not $win) { throw 'window did not appear' }
Start-Sleep -Milliseconds 2000

# PrintWindow has the window draw itself, so the shot is right even when the window is behind
# another one, or on a monitor scaled differently from the main one.
function Shot($name) {
    $h = [IntPtr]$win.Current.NativeWindowHandle
    $r = New-Object NbtWin+Rect
    [NbtWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [NbtWin]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $bmp.Save((Join-Path $Out $name), [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    "SHOT $name"
}

function FindByName($name, $type) {
    $c1 = New-Object Windows.Automation.PropertyCondition ($AE::NameProperty, $name)
    $c2 = New-Object Windows.Automation.PropertyCondition ($AE::ControlTypeProperty, $type)
    $win.FindFirst($Scope::Descendants, (New-Object Windows.Automation.AndCondition $c1, $c2))
}

function FindByText($pattern) {
    @($win.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition)) |
        Where-Object { $_.Current.Name -match $pattern -and $_.Current.Name -notmatch ' Row ' } | Select-Object -First 1
}

# A click on an element, posted to the window that owns it.
function PostClick($el, $hwndOwner, [bool]$twice, $dx = $null, $dy = $null) {
    $r = $el.Current.BoundingRectangle
    $x = if ($null -ne $dx) { $r.X + $dx } else { $r.X + $r.Width / 2 }
    $y = if ($null -ne $dy) { $r.Y + $dy } else { $r.Y + $r.Height / 2 }
    [NbtWin]::Click([IntPtr]$hwndOwner, [int]$x, [int]$y, $twice)
}

Shot 'differences.png'
$summary = @($win.FindAll($Scope::Descendants,
    (New-Object Windows.Automation.PropertyCondition ($AE::ControlTypeProperty, [Windows.Automation.ControlType]::Text)))) |
    ForEach-Object { $_.Current.Name } | Where-Object { $_ -match "^\d+ (numbered book|libro\(s\) numerado|livre\(s\) num$([char]0xE9)rot)" }
"SUMMARY $summary"

$grid = $win.FindFirst($Scope::Descendants,
    (New-Object Windows.Automation.PropertyCondition ($AE::ControlTypeProperty, [Windows.Automation.ControlType]::Table)))
$gridHwnd = $grid.Current.NativeWindowHandle

# Double-click the first row's Book cell: the test host logs the GoTo. A posted double-click
# can be missed (it has no real timing), so it is sent again until one is logged.
$cell = FindByName ((Strings_ColBook) + ' Row 0') ([Windows.Automation.ControlType]::Edit)
if (-not $cell) { throw 'first row not found' }
"FIRSTROW " + $cell.GetCurrentPropertyValue([Windows.Automation.ValuePattern]::ValueProperty)
for ($i = 0; $i -lt 5 -and -not (Test-Path $gotoLog); $i++) {
    PostClick $cell $gridHwnd $true
    Start-Sleep -Milliseconds 700
}
"GOTO (double-click) " + $(if (Test-Path $gotoLog) { (Get-Content $gotoLog | Select-Object -First 1) } else { '(none: posted double-clicks are not always seen)' })

# Select the second row and press "Go to in Paratext": must log that row's book.
$cell2 = FindByName ((Strings_ColBook) + ' Row 1') ([Windows.Automation.ControlType]::Edit)
$book2 = $cell2.GetCurrentPropertyValue([Windows.Automation.ValuePattern]::ValueProperty)
$before = if (Test-Path $gotoLog) { @(Get-Content $gotoLog).Count } else { 0 }
PostClick $cell2 $gridHwnd $false
Start-Sleep -Milliseconds 400
$goBtn = FindByText $(if ($Lang -like 'es*') { '^Ir en Paratext$' } elseif ($Lang -like 'fr*') { '^Aller dans Paratext$' } else { '^Go to in Paratext$' })
[NbtWin]::PressButton([IntPtr]$goBtn.Current.NativeWindowHandle)
Start-Sleep -Milliseconds 600
$after = if (Test-Path $gotoLog) { @(Get-Content $gotoLog) } else { @() }
$got = if ($after.Count -gt $before) { $after[$after.Count - 1] } else { '(none)' }
"GOTO (button) $got " + $(if ($got -eq "$book2 1:0") { 'OK' } else { "FAIL (expected $book2 1:0)" })

if ($Fix) {
    # Tick the first row through UI Automation, then press Change.
    $tick = FindByName ((Strings_ColFix) + ' Row 0') ([Windows.Automation.ControlType]::CheckBox)
    $tick.GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern).Toggle()
    Start-Sleep -Milliseconds 500
    # WinForms buttons here are exposed as panes, so the button is found by its text.
    $fixBtn = FindByText '\(1\)$'
    if (-not $fixBtn) { Shot 'tick_failed.png'; throw 'ticking the first row did not enable the Change button' }
    "FIXBUTTON " + $fixBtn.Current.Name
    [NbtWin]::PressButton([IntPtr]$fixBtn.Current.NativeWindowHandle)

    $byTitle = New-Object Windows.Automation.PropertyCondition ($AE::NameProperty, (Strings_ConfirmTitle))
    $dialog = $null
    for ($i = 0; $i -lt 30 -and -not $dialog; $i++) {
        Start-Sleep -Milliseconds 200
        $dialog = $root.FindFirst($Scope::Children, (New-Object Windows.Automation.AndCondition $byProcess, $byTitle))
        if (-not $dialog) { $dialog = $win.FindFirst($Scope::Children, $byTitle) }
    }
    if (-not $dialog) { throw 'confirmation dialog did not appear' }
    "DIALOG " + $dialog.Current.Name
    $yes = @($dialog.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition)) |
        Where-Object { $_.Current.Name -match "^&?(Yes|S$([char]0xED)|Oui)$" } | Select-Object -First 1
    if (-not $yes) { throw 'Yes button not found' }
    [NbtWin]::PressButton([IntPtr]$yes.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 2000

    # The results box gives UI Automation no text, so the change is checked in the files
    # (and the results box is in the screenshot).
    $book = Get-ChildItem $copy -File | Where-Object { (Get-Content -LiteralPath $_.FullName -TotalCount 1) -match '^\\id 1TH' } | Select-Object -First 1
    "CHANGED " + ((Get-Content -LiteralPath $book.FullName -Encoding UTF8 | Where-Object { $_ -match '^\\h ' }) -join ' ')
    "BACKUPS " + ((Get-ChildItem $bak -Filter *.bak -ErrorAction SilentlyContinue | ForEach-Object { $_.Name.Substring(0, 11) }) -join ', ')
    Shot 'after_fix.png'
}

# Second tab. WinForms exposes no tab items to UI Automation, so a click is posted to the tab
# control at the second header, which sits just above the grid, about 150 px in.
$walker = [Windows.Automation.TreeWalker]::ControlViewWalker
$tabs = $walker.GetParent($walker.GetParent($grid))
PostClick $grid $tabs.Current.NativeWindowHandle $false 150 -14
Start-Sleep -Milliseconds 800
Shot 'allbooks.png'

$win.GetCurrentPattern([Windows.Automation.WindowPattern]::Pattern).Close()
$proc.WaitForExit(10000) | Out-Null
$proc.StandardOutput.ReadToEnd() | Out-Null
