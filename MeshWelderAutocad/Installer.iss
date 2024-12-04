; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "MeshWelderAutocad"
#define MyAppVersion "1.0"
#define MyAppPublisher "����� ������"
#define MyAppURL "https://vk.com/scripts_revit"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{5f68513c-3cf8-47a2-8437-0daeb57f9e01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName=C:\ProgramData\Autodesk\ApplicationPlugins
DisableDirPage=yes
;DefaultGroupName={#MyAppName}
AllowNoIcons=yes
PrivilegesRequired=lowest
OutputDir=C:\Users\Acer\Desktop\Work\Projects\01_Revit\03_DNS\06_MW\MeshWelderAutocad\MeshWelderAutocad
OutputBaseFilename=DNSAutocad_2024-12-04
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "C:\Users\Acer\Desktop\Work\Projects\01_Revit\03_DNS\06_MW\MeshWelderAutocad\MeshWelderAutocad\ForInstaller\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
; Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

