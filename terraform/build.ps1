$exclude = @(".vs", "deploy", "build.ps1", ".gitignore", ".terra*", "*.tf", ".vscode")

Push-Location ..

# changes must of course be made in the terraform folder :-)
# Remove-Item -Force -Recurse -Path deploy
# Remove-Item -Force -Recurse -Path bin/Release
# Remove-Item -Force -Recurse -Path obj/Release

New-Item -ItemType Directory -Force -Path deploy
& dotnet publish --sc -c Release -o build
Get-ChildItem -Path ./build -Exclude $exclude | Compress-Archive -DestinationPath deploy/functionapp.zip -Force

Pop-Location