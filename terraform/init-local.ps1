Set-Location -Path $PSScriptRoot
Remove-Item -Path "$PSScriptRoot\backend.generated.tf" -ErrorAction SilentlyContinue
terraform init -reconfigure
