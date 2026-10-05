# Builds win64, linux64 and osx-arm64 builds

import os

os.system('dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o ./dist/windows')
os.system('dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -o ./dist/linux')
os.system('dotnet publish -c Release -r osx-arm64 -p:PublishSingleFile=true --self-contained true -o ./dist/mac-arm')