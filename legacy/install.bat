@echo off
:: Definir codificação UTF-8 para evitar problemas com caracteres
chcp 65001 >nul

:: Verificar se está rodando como administrador
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Iniciando instalação...

:: Encerrar o processo MicMute.exe, se estiver em execução, com erro suprimido
taskkill /IM "MicMute.exe" /F >nul 2>&1

:: Obter o diretório onde o .bat está localizado
set "sourceDir=%~dp0"
:: Remover a barra invertida final, se houver
if "%sourceDir:~-1%"=="\" set "sourceDir=%sourceDir:~0,-1%"

:: Definir a pasta de destino usando o usuário dinâmico
set "targetDir=C:\Users\%USERNAME%\AppData\Local\MicMute"

:: Definir os nomes dos arquivos
set "exeName=MicMute.exe"
set "wav1=on.wav"
set "wav2=off.wav"

:: Criar a pasta de destino, suprimindo mensagem se já existir
mkdir "%targetDir%" 2>nul
if not exist "%targetDir%" (
    echo Erro: Não foi possível criar a pasta %targetDir%
    pause
    exit /b
)

:: Testar permissões de escrita na pasta
echo Teste de escrita > "%targetDir%\test.txt" 2>nul
if not exist "%targetDir%\test.txt" (
    echo Erro: Não foi possível escrever na pasta %targetDir%. Verifique as permissões.
    pause
    exit /b
)
del "%targetDir%\test.txt" >nul 2>&1

:: Copiar os arquivos para a pasta de destino, sobrescrevendo se existirem
copy /Y "%sourceDir%\%exeName%" "%targetDir%\" >nul
if errorlevel 1 (
    echo Erro: Falha ao copiar %exeName%
    pause
    exit /b
)
copy /Y "%sourceDir%\%wav1%" "%targetDir%\" >nul
if errorlevel 1 (
    echo Erro: Falha ao copiar %wav1%
    pause
    exit /b
)
copy /Y "%sourceDir%\%wav2%" "%targetDir%\" >nul
if errorlevel 1 (
    echo Erro: Falha ao copiar %wav2%
    pause
    exit /b
)

:: Criar atalho no Menu Iniciar
set "shortcutDir=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "shortcutName=MicMute.lnk"
set "targetPath=%targetDir%\%exeName%"
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%shortcutDir%\%shortcutName%'); $s.TargetPath = '%targetPath%'; $s.Save()" >nul 2>&1
if errorlevel 1 (
    echo Erro: Falha ao criar o atalho no Menu Iniciar
    pause
    exit /b
)

:: Configurar o atalho para "Executar como administrador" modificando o byte
powershell -Command "$shortcutPath = '%shortcutDir%\%shortcutName%'; $bytes = [System.IO.File]::ReadAllBytes($shortcutPath); $bytes[0x15] = $bytes[0x15] -bor 0x20; [System.IO.File]::WriteAllBytes($shortcutPath, $bytes)" >nul 2>&1
if errorlevel 1 (
    echo Erro: Falha ao configurar o atalho para executar como administrador
    pause
    exit /b
)

:: Criar tarefa agendada para inicialização como administrador
schtasks /create /sc ONLOGON /tn "MicMute" /tr "%targetPath%" /rl HIGHEST /f >nul 2>&1
if errorlevel 1 (
    echo Erro: Falha ao criar a tarefa agendada
    pause
    exit /b
)

:: Criar o uninstall.bat dentro da pasta MicMute, sobrescrevendo se existir
set "uninstallPath=%targetDir%\uninstall.bat"
echo Criando uninstall.bat em %uninstallPath%...

> "%uninstallPath%" (
    echo @echo off
)
>> "%uninstallPath%" echo chcp 65001 ^>nul
>> "%uninstallPath%" echo.
>> "%uninstallPath%" echo net session ^>nul 2^>^&1
>> "%uninstallPath%" echo if %%errorlevel%% neq 0 (
>> "%uninstallPath%" echo     powershell -Command "Start-Process '%%~f0' -Verb RunAs"
>> "%uninstallPath%" echo     exit /b
>> "%uninstallPath%" echo )
>> "%uninstallPath%" echo.
>> "%uninstallPath%" echo taskkill /IM "MicMute.exe" /F ^>nul 2^>^&1
>> "%uninstallPath%" echo.
>> "%uninstallPath%" echo set "targetDir=%targetDir%"
>> "%uninstallPath%" echo set "shortcutDir=%%APPDATA%%\Microsoft\Windows\Start Menu\Programs"
>> "%uninstallPath%" echo set "shortcutName=MicMute.lnk"
>> "%uninstallPath%" echo.
>> "%uninstallPath%" echo del "%%shortcutDir%%\%%shortcutName%%" ^>nul 2^>^&1
>> "%uninstallPath%" echo schtasks /delete /tn "MicMute" /f ^>nul 2^>^&1
>> "%uninstallPath%" echo rmdir /s /q "%%targetDir%%" ^>nul 2^>^&1
>> "%uninstallPath%" echo.
>> "%uninstallPath%" echo echo Desinstalacao concluida!
>> "%uninstallPath%" echo pause

if exist "%uninstallPath%" (
    echo Instalação concluída! O uninstall.bat foi criado em %targetDir%
) else (
    echo Erro: Não foi possível criar o uninstall.bat em %targetDir%
    pause
    exit /b
)

pause
