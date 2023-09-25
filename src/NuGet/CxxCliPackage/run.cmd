setlocal
set BINDIR=%~dp0..\..\..\bin\
call "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat" -startdir=none -arch=x64 -host_arch=x64
echo on

MSBuild CxxCliClassLibrary.sln /nologo /p:Configuration=Release /p:Platform=x64 /v:minimal
MSBuild CxxCliClassLibrary.sln /nologo /p:Configuration=Release /p:Platform=x86 /v:minimal

rem NoWarn=NU5131: https://github.com/NuGet/Home/issues/8684 
nuget pack -BasePath %BINDIR% Asmichi.CxxCliPackage.nuspec -Properties NoWarn=NU5131

nuget add -Source %BINDIR%NuGetFeed Asmichi.cxxCliClassLibrary.0.0.0-alpha.4.nupkg

dotnet run --no-self-contained -r win-x64 -f net472 --project CxxCliConsumerApp\CxxCliConsumerApp.csproj
dotnet run --no-self-contained -r win-x86 -f net472 --project CxxCliConsumerApp\CxxCliConsumerApp.csproj
dotnet run --no-self-contained -r win-x64 -f net6.0 --project CxxCliConsumerApp\CxxCliConsumerApp.csproj
dotnet run --no-self-contained -r win-x86 -f net6.0 --project CxxCliConsumerApp\CxxCliConsumerApp.csproj

MSBuild CxxCliConsumerAppOldStyle.sln /nologo /p:Configuration=Release /p:Platform=x86 /v:minimal
%BINDIR%\CxxCliConsumerAppOldStyle\x86\CxxCliConsumerAppOldStyle.exe

MSBuild CxxCliConsumerAppOldStyle.sln /nologo /p:Configuration=Release /p:Platform=x64 /v:minimal
%BINDIR%\CxxCliConsumerAppOldStyle\x64\CxxCliConsumerAppOldStyle.exe

endlocal