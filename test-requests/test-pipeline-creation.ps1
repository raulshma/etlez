# PowerShell script to test pipeline creation with various scenarios
# This script sends HTTP requests to the ETL Framework API to test pipeline creation

param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$TestType = "all"
)

# Function to send POST request and handle response
function Test-PipelineCreation {
    param(
        [string]$Url,
        [object]$RequestBody,
        [string]$TestName
    )
    
    Write-Host "Testing: $TestName" -ForegroundColor Cyan
    
    try {
        $jsonBody = $RequestBody | ConvertTo-Json -Depth 10
        Write-Host "Request Body:" -ForegroundColor Yellow
        Write-Host $jsonBody
        
        $response = Invoke-RestMethod -Uri $Url -Method POST -Body $jsonBody -ContentType "application/json" -SkipCertificateCheck
        
        Write-Host "✅ SUCCESS: Pipeline created successfully" -ForegroundColor Green
        Write-Host "Pipeline ID: $($response.id)" -ForegroundColor Green
        Write-Host "Pipeline Name: $($response.name)" -ForegroundColor Green
        Write-Host ""
        
        return $response
    }
    catch {
        Write-Host "❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error Details: $errorContent" -ForegroundColor Red
        }
        Write-Host ""
        return $null
    }
}

# Function to test pipeline execution
function Test-PipelineExecution {
    param(
        [string]$BaseUrl,
        [string]$PipelineId,
        [string]$TestName
    )
    
    Write-Host "Testing execution for: $TestName" -ForegroundColor Cyan
    
    try {
        $executeUrl = "$BaseUrl/api/pipelines/$PipelineId/execute"
        $executeBody = @{
            parameters = @{
                testMode = $true
                dryRun = $true
            }
        }
        
        $jsonBody = $executeBody | ConvertTo-Json -Depth 5
        $response = Invoke-RestMethod -Uri $executeUrl -Method POST -Body $jsonBody -ContentType "application/json" -SkipCertificateCheck
        
        Write-Host "✅ EXECUTION SUCCESS: Pipeline executed successfully" -ForegroundColor Green
        Write-Host "Execution ID: $($response.executionId)" -ForegroundColor Green
        Write-Host "Status: $($response.status)" -ForegroundColor Green
        Write-Host ""
        
        return $response
    }
    catch {
        Write-Host "❌ EXECUTION FAILED: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $null
    }
}

# Main test execution
$apiUrl = "$BaseUrl/api/pipelines"
$testResults = @()

Write-Host "Starting Pipeline Creation Tests" -ForegroundColor Magenta
Write-Host "API URL: $apiUrl" -ForegroundColor Magenta
Write-Host "Test Type: $TestType" -ForegroundColor Magenta
Write-Host "=" * 50

# Test basic pipeline creation requests
if ($TestType -eq "all" -or $TestType -eq "basic") {
    Write-Host "Running Basic Pipeline Tests..." -ForegroundColor Yellow
    
    $basicRequests = Get-Content "pipeline-creation-test-requests.json" | ConvertFrom-Json
    
    foreach ($request in $basicRequests) {
        $result = Test-PipelineCreation -Url $apiUrl -RequestBody $request -TestName $request.name
        
        $testResults += @{
            TestName = $request.name
            TestType = "Basic"
            Success = ($result -ne $null)
            PipelineId = if ($result) { $result.id } else { $null }
            Response = $result
        }
        
        # Test execution if pipeline was created successfully
        if ($result -and $result.id) {
            $execResult = Test-PipelineExecution -BaseUrl $BaseUrl -PipelineId $result.id -TestName $request.name
        }
        
        Start-Sleep -Seconds 1
    }
}

# Test advanced pipeline creation requests
if ($TestType -eq "all" -or $TestType -eq "advanced") {
    Write-Host "Running Advanced Pipeline Tests..." -ForegroundColor Yellow
    
    $advancedRequests = Get-Content "advanced-pipeline-test-requests.json" | ConvertFrom-Json
    
    foreach ($request in $advancedRequests) {
        $result = Test-PipelineCreation -Url $apiUrl -RequestBody $request -TestName $request.name
        
        $testResults += @{
            TestName = $request.name
            TestType = "Advanced"
            Success = ($result -ne $null)
            PipelineId = if ($result) { $result.id } else { $null }
            Response = $result
        }
        
        Start-Sleep -Seconds 1
    }
}

# Test edge case and error scenarios
if ($TestType -eq "all" -or $TestType -eq "edge") {
    Write-Host "Running Edge Case Tests..." -ForegroundColor Yellow
    
    $edgeCaseRequests = Get-Content "edge-case-pipeline-test-requests.json" | ConvertFrom-Json
    
    foreach ($request in $edgeCaseRequests) {
        $result = Test-PipelineCreation -Url $apiUrl -RequestBody $request -TestName $request.name
        
        $testResults += @{
            TestName = $request.name
            TestType = "EdgeCase"
            Success = ($result -ne $null)
            PipelineId = if ($result) { $result.id } else { $null }
            Response = $result
            ExpectedToFail = $true  # Most edge cases are expected to fail
        }
        
        Start-Sleep -Seconds 1
    }
}

# Generate test summary
Write-Host "=" * 50
Write-Host "TEST SUMMARY" -ForegroundColor Magenta
Write-Host "=" * 50

$totalTests = $testResults.Count
$successfulTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $successfulTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Successful: $successfulTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($successfulTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

# Group results by test type
$groupedResults = $testResults | Group-Object TestType

foreach ($group in $groupedResults) {
    Write-Host ""
    Write-Host "$($group.Name) Tests:" -ForegroundColor Cyan
    
    foreach ($test in $group.Group) {
        $status = if ($test.Success) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($test.Success) { "Green" } else { "Red" }
        
        Write-Host "  $status $($test.TestName)" -ForegroundColor $color
        
        if ($test.PipelineId) {
            Write-Host "    Pipeline ID: $($test.PipelineId)" -ForegroundColor Gray
        }
    }
}

# Save detailed results to file
$testResults | ConvertTo-Json -Depth 10 | Out-File "test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"

Write-Host ""
Write-Host "Detailed test results saved to test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json" -ForegroundColor Magenta
