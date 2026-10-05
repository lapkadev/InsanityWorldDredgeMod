param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [string]$Parent = ""
)

$emptyTree = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"

Set-Location -LiteralPath $PSScriptRoot

if ($Parent -ne "") {
    $commit = git commit-tree $emptyTree -p $Parent -m $Tag
} else {
    $commit = git commit-tree $emptyTree -m $Tag
}
if ($LASTEXITCODE -ne 0) { exit 1 }

git tag $Tag $commit
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "Tag $Tag created on empty commit $commit"
