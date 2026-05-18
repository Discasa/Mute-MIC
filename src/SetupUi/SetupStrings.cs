using System.Globalization;

namespace MuteMIC.SetupUi;

internal static class SetupStrings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Values = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["InstallerTitleBar"] = "Mute MIC Installer",
            ["UninstallerTitleBar"] = "Mute MIC Uninstaller",
            ["InstallTitle"] = "Install Mute MIC",
            ["InstallBody"] = "This setup will install Mute MIC and configure startup, Start Menu shortcuts, and Windows Installed Apps integration.",
            ["InstallLocation"] = "Install location",
            ["ReadyToInstall"] = "Ready to install.",
            ["InstallButton"] = "Install",
            ["Cancel"] = "Cancel",
            ["InstallingTitle"] = "Installing Mute MIC",
            ["InstallingBody"] = "Please wait while setup installs the application.",
            ["InstalledTitle"] = "Mute MIC installed",
            ["InstalledBody"] = "The application is installed and ready to use.",
            ["InstalledStatus"] = "Mute MIC was installed successfully.",
            ["InstallFailedTitle"] = "Installation failed",
            ["SetupCouldNotComplete"] = "Setup could not complete.",
            ["Finish"] = "Finish",
            ["Close"] = "Close",
            ["PreparingInstall"] = "Preparing installation...",
            ["RemovingStartup"] = "Removing old startup entries...",
            ["CreatingFolder"] = "Creating installation folder...",
            ["CopyingFiles"] = "Copying application files...",
            ["AppNotCopied"] = "Application executable was not copied.",
            ["CreatingShortcuts"] = "Creating Start Menu shortcuts...",
            ["RegisteringInstalledApps"] = "Registering Windows Installed Apps entry...",
            ["ConfiguringStartup"] = "Configuring startup entry...",
            ["SkippingStartup"] = "Skipping startup entry for this test run...",
            ["StartingApp"] = "Starting Mute MIC...",
            ["UninstallTitle"] = "Uninstall Mute MIC",
            ["UninstallBody"] = "This will remove Mute MIC, startup entries, Start Menu shortcuts, Windows Installed Apps integration, and settings.",
            ["InstalledLocation"] = "Installed location",
            ["ReadyToUninstall"] = "Ready to uninstall.",
            ["UninstallButton"] = "Uninstall",
            ["UninstallingTitle"] = "Uninstalling Mute MIC",
            ["UninstallingBody"] = "Please wait while setup removes the application.",
            ["UninstalledTitle"] = "Mute MIC uninstalled",
            ["AlreadyUninstalledTitle"] = "Mute MIC already uninstalled",
            ["UninstalledStatus"] = "Mute MIC was uninstalled successfully.",
            ["AlreadyUninstalledStatus"] = "Mute MIC was already uninstalled. Remaining entries were cleaned up.",
            ["UninstallFailedTitle"] = "Uninstall failed",
            ["StoppingApp"] = "Stopping Mute MIC...",
            ["RemovingShortcuts"] = "Removing Start Menu shortcuts...",
            ["RemovingRegistry"] = "Removing Windows Installed Apps entry and settings...",
            ["SchedulingRemoval"] = "Scheduling application file removal...",
            ["FinishingCleanup"] = "Finishing cleanup..."
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["InstallerTitleBar"] = "Instalador do Mute MIC",
            ["UninstallerTitleBar"] = "Desinstalador do Mute MIC",
            ["InstallTitle"] = "Instalar Mute MIC",
            ["InstallBody"] = "Este instalador vai instalar o Mute MIC e configurar inicialização, atalhos do Menu Iniciar e integração com Aplicativos instalados do Windows.",
            ["InstallLocation"] = "Local de instalação",
            ["ReadyToInstall"] = "Pronto para instalar.",
            ["InstallButton"] = "Instalar",
            ["Cancel"] = "Cancelar",
            ["InstallingTitle"] = "Instalando Mute MIC",
            ["InstallingBody"] = "Aguarde enquanto o instalador instala o aplicativo.",
            ["InstalledTitle"] = "Mute MIC instalado",
            ["InstalledBody"] = "O aplicativo foi instalado e está pronto para uso.",
            ["InstalledStatus"] = "O Mute MIC foi instalado com sucesso.",
            ["InstallFailedTitle"] = "Falha na instalação",
            ["SetupCouldNotComplete"] = "A instalação não pôde ser concluída.",
            ["Finish"] = "Finalizar",
            ["Close"] = "Fechar",
            ["PreparingInstall"] = "Preparando a instalação...",
            ["RemovingStartup"] = "Removendo entradas antigas de inicialização...",
            ["CreatingFolder"] = "Criando pasta de instalação...",
            ["CopyingFiles"] = "Copiando arquivos do aplicativo...",
            ["AppNotCopied"] = "O executável do aplicativo não foi copiado.",
            ["CreatingShortcuts"] = "Criando atalhos no Menu Iniciar...",
            ["RegisteringInstalledApps"] = "Registrando entrada em Aplicativos instalados do Windows...",
            ["ConfiguringStartup"] = "Configurando inicialização automática...",
            ["SkippingStartup"] = "Ignorando inicialização automática neste teste...",
            ["StartingApp"] = "Iniciando Mute MIC...",
            ["UninstallTitle"] = "Desinstalar Mute MIC",
            ["UninstallBody"] = "Isto vai remover o Mute MIC, entradas de inicialização, atalhos do Menu Iniciar, integração com Aplicativos instalados do Windows e configurações.",
            ["InstalledLocation"] = "Local instalado",
            ["ReadyToUninstall"] = "Pronto para desinstalar.",
            ["UninstallButton"] = "Desinstalar",
            ["UninstallingTitle"] = "Desinstalando Mute MIC",
            ["UninstallingBody"] = "Aguarde enquanto o desinstalador remove o aplicativo.",
            ["UninstalledTitle"] = "Mute MIC desinstalado",
            ["AlreadyUninstalledTitle"] = "Mute MIC já estava desinstalado",
            ["UninstalledStatus"] = "O Mute MIC foi desinstalado com sucesso.",
            ["AlreadyUninstalledStatus"] = "O Mute MIC já estava desinstalado. As entradas restantes foram limpas.",
            ["UninstallFailedTitle"] = "Falha na desinstalação",
            ["StoppingApp"] = "Encerrando Mute MIC...",
            ["RemovingShortcuts"] = "Removendo atalhos do Menu Iniciar...",
            ["RemovingRegistry"] = "Removendo entrada em Aplicativos instalados do Windows e configurações...",
            ["SchedulingRemoval"] = "Agendando remoção dos arquivos do aplicativo...",
            ["FinishingCleanup"] = "Finalizando limpeza..."
        }
    };

    public static string DetectLanguage()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("pt", StringComparison.OrdinalIgnoreCase)
            ? "pt-BR"
            : "en";
    }

    public static string Get(string language, string key)
    {
        if (!Values.TryGetValue(language, out Dictionary<string, string>? languageValues))
        {
            languageValues = Values["en"];
        }

        return languageValues.TryGetValue(key, out string? value) ? value : Values["en"][key];
    }
}
