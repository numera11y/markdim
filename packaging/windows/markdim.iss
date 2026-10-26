#define AppName "markdim"
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
AppId=markdim-{#AppVersion}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (C) Numera11y
DefaultDirName={localappdata}\Programs\markdim\v{#AppVersion}\markdim
DefaultGroupName=markdim {#AppVersion}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName=markdim {#AppVersion}
UninstallDisplayIcon={app}\markdim.exe
SetupIconFile=..\..\MarkdownDesk\Assets\markdim.ico
LicenseFile=..\..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=markdim-Setup-v{#AppVersion}-windows-x64
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
Name: "{autoprograms}\markdim {#AppVersion}"; Filename: "{app}\markdim.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\markdim {#AppVersion}"; Filename: "{app}\markdim.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\register-file-association.ps1"" -ApplicationPath ""{app}\markdim.exe"""; Flags: runhidden waituntilterminated
Filename: "{app}\markdim.exe"; Description: "Lancer markdim après l’installation"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\unregister-file-association.ps1"" -ApplicationPath ""{app}\markdim.exe"""; Flags: runhidden waituntilterminated
