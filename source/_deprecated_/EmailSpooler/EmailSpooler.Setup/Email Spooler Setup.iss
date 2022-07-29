; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Email Spooler Service"
#define MyAppVersion "1"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{E5583BDA-5B7D-40D2-A00B-4350B09138FC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=Setup-EmailSpoolerService
Compression=lzma2
SolidCompression=yes
OutputDir=.
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\*.exe"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\EmailSpooler.Win32Service.exe.config"; DestDir: "{app}"
Source: "bin\appSettings.config"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "bin\ConnectionStrings.config"; DestDir: "{app}"; Flags: onlyifdoesntexist
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\EmailSpooler.Win32Service.exe"; Parameters: "-i -s"; WorkingDir: "{app}"; Flags: waituntilterminated postinstall runascurrentuser runhidden; Description: "Install Service"

[UninstallRun]
Filename: "{app}\EmailSpooler.Win32Service.exe"; Parameters: "-u"; Flags: waituntilterminated skipifdoesntexist runhidden runascurrentuser;