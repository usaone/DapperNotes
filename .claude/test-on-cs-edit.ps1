$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path

# Only react to files that can change the build or test outcome.
# -like and -notmatch are case-insensitive, matching how Windows treats file names.
$patterns = '*.cs', '*.csproj', '*.slnx', '*.sln', '*.props', '*.targets',
            'appsettings*.json', 'global.json', 'nuget.config', '.editorconfig'

$name = [IO.Path]::GetFileName($path)
$relevant = $path -and
            $path -notmatch '[\\/](bin|obj)[\\/]' -and
            ($patterns | Where-Object { $name -like $_ })

if (-not $relevant) { exit 0 }

$output = dotnet test "$env:CLAUDE_PROJECT_DIR" --nologo -v q 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine($output)
    exit 2  # exit 2 sends stderr back to Claude as feedback
}
exit 0
