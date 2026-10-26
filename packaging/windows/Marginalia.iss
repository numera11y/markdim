#define AppName "Marginalia"
#define AppPublisher "Numera11y"

#ifndef AppVersion
  #error AppVersion must be passed to ISCC.
#endif
#ifndef AppSource
  #error AppSource must be passed to ISCC.
#endif
#ifndef OutputDir
  #error OutputDir must be passed to ISCC.
#endif

[Setup]
AppId=Marginalia-{#AppVersion}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (C) Numera11y
DefaultDirName={localappdata}\Programs\Marginalia\v{#AppVersion}\Marginalia
DefaultGroupName=Marginalia {#AppVersion}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName=Marginalia {#AppVersion}
UninstallDisplayIcon={app}\Marginalia.exe
SetupIconFile=..\..\MarkdownDesk\Assets\marginalia.ico
LicenseFile=..\..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=Marginalia-Setup-v{#AppVersion}-windows-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le bureau"; GroupDescription: "Raccourcis supplémentaires :"; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\packaging\windows\register-file-association.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\packaging\windows\unregister-file-association.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Marginalia {#AppVersion}"; Filename: "{app}\Marginalia.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Marginalia {#AppVersion}"; Filename: "{app}\Marginalia.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\register-file-association.ps1"" -ApplicationPath ""{app}\Marginalia.exe"""; Flags: runhidden waituntilterminated
Filename: "{app}\Marginalia.exe"; Description: "Lancer Marginalia après l’installation"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\unregister-file-association.ps1"" -ApplicationPath ""{app}\Marginalia.exe"""; Flags: runhidden waituntilterminated
