$exclude = @(".vs", "deploy", "build.ps1", ".gitignore", ".terra*", "*.tf", ".vscode")

New-Item -ItemType Directory -Force -Path deploy

# the release is part of the assets archived for use with terraform,
# so the .zip file is pushed into the terraform directory

Push-Location .. 
& dotnet publish azureddns.csproj --sc -c Release -o build
Get-ChildItem -Path ./build -Exclude $exclude | Compress-Archive -DestinationPath terraform/deploy/functionapp.zip -Force
Pop-Location
