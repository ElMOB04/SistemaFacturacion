@echo off
REM ============================================================
REM  Compila el Sistema de Facturacion sin necesidad de abrir
REM  Visual Studio, usando el compilador de C# (Roslyn) que
REM  viene con Build Tools y el .NET Framework instalado.
REM ============================================================
setlocal

set "PROY=%~dp0"
set "SALIDA=%PROY%bin\Debug"
set "CSC=C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
set "FW=C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if not exist "%CSC%" (
    echo No se encontro el compilador csc.exe en:
    echo   %CSC%
    echo Ajuste la variable CSC en este archivo segun su instalacion.
    pause
    exit /b 1
)

if not exist "%SALIDA%" mkdir "%SALIDA%"

echo Compilando...
REM Se generan las referencias y la lista de archivos en un fichero de respuesta.
set "RSP=%TEMP%\build_sistemafacturacion.rsp"
> "%RSP%" echo /target:winexe
>> "%RSP%" echo /out:"%SALIDA%\SistemaFacturacion.exe"
>> "%RSP%" echo /nologo
>> "%RSP%" echo /win32manifest:"%PROY%app.manifest"
>> "%RSP%" echo /reference:"%FW%\System.dll"
>> "%RSP%" echo /reference:"%FW%\System.Core.dll"
>> "%RSP%" echo /reference:"%FW%\System.Data.dll"
>> "%RSP%" echo /reference:"%FW%\System.Drawing.dll"
>> "%RSP%" echo /reference:"%FW%\System.Windows.Forms.dll"
>> "%RSP%" echo /reference:"%FW%\System.Configuration.dll"
>> "%RSP%" echo /reference:"%FW%\System.Xml.dll"
for /r "%PROY%" %%f in (*.cs) do >> "%RSP%" echo "%%f"

"%CSC%" @"%RSP%"
if errorlevel 1 (
    echo.
    echo *** Ocurrieron errores de compilacion ***
    pause
    exit /b 1
)

REM Copiar el archivo de configuracion junto al ejecutable
copy /Y "%PROY%App.config" "%SALIDA%\SistemaFacturacion.exe.config" >nul

echo.
echo Compilacion exitosa.
echo Ejecutable: %SALIDA%\SistemaFacturacion.exe
echo.
choice /C SN /M "Desea ejecutar la aplicacion ahora"
if errorlevel 2 goto fin
start "" "%SALIDA%\SistemaFacturacion.exe"
:fin
endlocal
