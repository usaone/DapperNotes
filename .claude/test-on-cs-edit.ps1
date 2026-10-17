$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path

# Only react to C# source edits
if (-not $path -or -not $path.EndsWith('.cs')) { exit 0}

$output = dotnet test "$env:CLAUDE_PROJECT_DIR" --nologo -v q 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine($output)
    exit 2  # exit 2 sends stderr back to Claude as feedback
}
exit 0
