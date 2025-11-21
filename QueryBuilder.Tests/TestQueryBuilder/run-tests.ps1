# run-tests.ps1
# PowerShell script to run tests in Release mode without rebuilding

# Navigate to your test project folder
Set-Location "C:\Users\nithy\OneDrive\Documentos\Nithya\CoreTraining\QueryBuilder\QueryBuilder.Tests\TestQueryBuilder"

# Run tests
dotnet test --configuration Release --no-build `
    --logger "console;verbosity=detailed" `
    --logger "trx;LogFileName=test_results.trx"

Write-Host "Tests executed. Results saved to test_results.trx"