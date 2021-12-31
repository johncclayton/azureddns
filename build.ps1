$exclude = @(".vs", "deploy", "build.ps1", ".gitignore", ".terra*", "*.tf", ".vscode")
New-Item -ItemType Directory -Force -Path deploy
& dotnet publish --sc -c Release -o build
Get-ChildItem -Path ./build -Exclude $exclude | Compress-Archive -DestinationPath deploy/functionapp.zip -Force