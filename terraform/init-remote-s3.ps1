param(
    [Parameter(Mandatory = $true)]
    [string]$Bucket,

    [Parameter(Mandatory = $true)]
    [string]$Region,

    [string]$Key = "joby/dev/terraform.tfstate",
    [string]$DynamoDbTable = ""
)

Set-Location -Path $PSScriptRoot
Copy-Item -Path "$PSScriptRoot\backend.generated.tf.example" -Destination "$PSScriptRoot\backend.generated.tf" -Force

$backendConfig = @(
    "-backend-config=bucket=$Bucket",
    "-backend-config=key=$Key",
    "-backend-config=region=$Region"
)

if ($DynamoDbTable -ne "") {
    $backendConfig += "-backend-config=dynamodb_table=$DynamoDbTable"
}

terraform init -reconfigure @backendConfig
