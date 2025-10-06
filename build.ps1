# build.ps1

param ([string]$Configuration = "Release",
       [string]$SolutionPath = "./QueryBuilder.sln" )

Write-Host "Starting build for $SolutionPath in $Configuration mode..."

Write-Host "Restoring packages..."

Write-Host "Building solution..."
dotnet build $SolutionPath --configuration $Configuration

Write-Host "Runnint Test"
dotnet test $SolutionPath --configuration $Configuration

Write-Host "Build Completed"