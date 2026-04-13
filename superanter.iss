[Setup]
AppId={{B5E53A49-6F07-4F75-BF80-43F4C8EAC0E1}
AppName=Super Anter
AppVersion=1.0
AppPublisher=Ahmed Fahmy
DefaultDirName={autopf}\Super Anter
DefaultGroupName=Super Anter
OutputDir=C:\Users\ahmed\Desktop\Super-Anter\InstallerOutput
OutputBaseFilename=SuperAnterSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "C:\Users\ahmed\Desktop\Super-Anter-exe\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Output\*;Super-Anter_BurstDebugInformation_DoNotShip\*;*.pdb"

[Icons]
Name: "{group}\Super Anter"; Filename: "{app}\Super-Anter.exe"
Name: "{autodesktop}\Super Anter"; Filename: "{app}\Super-Anter.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Super-Anter.exe"; Description: "Launch Super Anter"; Flags: nowait postinstall skipifsilent