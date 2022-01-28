$exclude = @(".vs", "deploy", "build.ps1", ".gitignore", ".terra*", "*.tf", ".vscode")
Push-Location .. 
New-Item -ItemType Directory -Force -Path deploy
& dotnet publish --sc -c Release -o build
Get-ChildItem -Path ./build -Exclude $exclude | Compress-Archive -DestinationPath deploy/functionapp.zip -Force
Pop-Location