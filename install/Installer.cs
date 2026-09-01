using Installer;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

const string outputName = "NablaFlow.ArchiWind";
const string projectName = "ArchiWind Revit Add-In";

var versioning = Versioning.CreateFromVersionStringAsync(args[0]);
var project = new Project
{
    OutDir = "output",
    Name = projectName,
    Platform = Platform.x64,
    UI = WUI.WixUI_FeatureTree,
    MajorUpgrade = MajorUpgrade.Default,
    GUID = new Guid("2814AB61-02F9-433C-BFB2-2CAD4B882889"),
    BannerImage = @"install\Resources\Icons\BannerImage.png",
    BackgroundImage = @"install\Resources\Icons\BackgroundImage.png",
    Version = versioning.VersionPrefix,
    ControlPanelInfo =
    {
        Manufacturer = "NablaFlow AS",
        ProductIcon = @"install\Resources\Icons\icon.ico"
    }
};

var wixEntities = Generator.GenerateWixEntities(args[1..]);
project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.CustomizeDlg);

BuildSingleUserMsi();
BuildMultiUserUserMsi();

void BuildSingleUserMsi()
{
    project.Scope = InstallScope.perUser;
    project.OutFileName = $"{outputName}-{versioning.Version}-SingleUser";
    project.Dirs =
    [
        new InstallDir(@"%AppDataFolder%\Autodesk\Revit\Addins\", wixEntities)
    ];
    project.BuildMsi();
}

void BuildMultiUserUserMsi()
{
    project.Scope = InstallScope.perMachine;
    project.OutFileName = $"{outputName}-{versioning.Version}-MultiUser";
    project.Dirs =
    [
        new InstallDir(versioning.VersionPrefix.Major >= 2027 ? @"%ProgramFiles%\Autodesk\Revit\Addins" : @"%CommonAppDataFolder%\Autodesk\Revit\Addins", wixEntities)
    ];
    project.BuildMsi();
}