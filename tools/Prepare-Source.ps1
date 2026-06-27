param(
    [Parameter(Mandatory=$true)]
    [string]$InputPath,

    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$source = [System.IO.File]::ReadAllText($InputPath)

# Temporary compile fixes for the first source layout.
# These should be removed once src/RhaegarMove.cs is cleaned directly.
$source = $source.Replace('private readonly Timer watchdog;', 'private readonly System.Windows.Forms.Timer watchdog;')
$source = $source.Replace('watchdog = new Timer();', 'watchdog = new System.Windows.Forms.Timer();')
$source = $source.Replace('private enum Operation { None, Move, Resize }', 'private enum OperationKind { None, Move, Resize }')
$source = $source.Replace('Operation.Move', 'OperationKind.Move')
$source = $source.Replace('Operation.Resize', 'OperationKind.Resize')
$source = $source.Replace('Operation.None', 'OperationKind.None')
$source = $source.Replace('public Operation Operation;', 'public OperationKind Kind;')
$source = $source.Replace('state.Operation', 'state.Kind')
$source = $source.Replace('RECT desired = new RECT(left, top, right, bottom);', 'RECT desired = new RECT(); desired.left = left; desired.top = top; desired.right = right; desired.bottom = bottom;')

# Phase 2: route target filtering through the clean-room WindowRules layer.
$oldFilter = 'if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls == "Button" || cls == "#32768")'
$newFilter = 'if (WindowRules.ShouldIgnoreWindow(hwnd, cls))'
$source = $source.Replace($oldFilter, $newFilter)

$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutputPath, $source, $encoding)
