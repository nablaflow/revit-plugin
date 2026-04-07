using ArchiWindRevitAddIn.Tasks;
using ArchiWindRevitAddIn.ViewModels;
using ArchiWindRevitAddIn.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Nice3point.Revit.Toolkit.External;
using System.Windows.Threading;

namespace ArchiWindRevitAddIn.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportStls : ExternalCommand
    {
        public override void Execute()
        {
            var buildingView = Utils.FindView(Application.ActiveUIDocument.Document, Utils.BUILDING_VIEW);
            var surroundingsView = Utils.FindView(Application.ActiveUIDocument.Document, Utils.SURROUNDINGS_VIEW);
            var terrainView = Utils.FindView(Application.ActiveUIDocument.Document, Utils.TERRAIN_VIEW);
            var vegetationView = Utils.FindView(Application.ActiveUIDocument.Document, Utils.VEGETATION_VIEW);

            if (buildingView is null || surroundingsView is null || terrainView is null || vegetationView is null)
            {
                TaskDialog.Show("Error",
                                $"One or more 3D views are missing.\nClick on the \"Preview\" button of the add-in to create them.",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);
                return;
            }

#if REVIT2025_OR_GREATER
            var dialog = new OpenFolderDialog()
            {
                ValidateNames = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() is false)
            {
                TaskDialog.Show("Error",
                                $"You must select a folder to continue.",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);
                return;
            }

            string dir = dialog.FolderName;
#else
            using var dialog = new FolderBrowserDialog()
            {
            };

            if (dialog.ShowDialog() is not DialogResult.OK)
            {
                TaskDialog.Show("Error",
                                $"You must select a folder to continue.",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);
                return;
            }

            string dir = dialog.SelectedPath;
#endif

            ProgressViewModel? progressViewModel = null;
            ProgressView? progressView = null;

            var progressThread = new Thread(() =>
            {
                progressViewModel = new("STLs export progress");
                progressView = new(progressViewModel);

                progressView.Show();
                progressView.Activate();

                Dispatcher.Run();
            });

            progressThread.SetApartmentState(ApartmentState.STA);
            progressThread.IsBackground = true;
            progressThread.Start();

            while (progressView == null || progressViewModel == null)
            {
                Thread.Sleep(10);
            }

            var dispatcher = progressViewModel.Dispatcher;
            var cancellationToken = progressViewModel.CancellationToken;

            try
            {
                CreateSimulationTask.ExportGeometryToStl(progressViewModel, Application.ActiveUIDocument.Document, Utils.BUILDING_VIEW, dir, "building.stl", cancellationToken);
                CreateSimulationTask.ExportGeometryToStl(progressViewModel, Application.ActiveUIDocument.Document, Utils.SURROUNDINGS_VIEW, dir, "surroundings.stl", cancellationToken);
                CreateSimulationTask.ExportGeometryToStl(progressViewModel, Application.ActiveUIDocument.Document, Utils.TERRAIN_VIEW, dir, "terrain.stl", cancellationToken);
                CreateSimulationTask.ExportGeometryToStl(progressViewModel, Application.ActiveUIDocument.Document, Utils.VEGETATION_VIEW, dir, "vegetation.stl", cancellationToken);
            }
            catch (Exception ex)
            {
                _ = dispatcher.BeginInvoke(() => progressViewModel.SetCompleted($"Error: {ex.GetType()}, {ex.Message}"));
                return;
            }

            _ = dispatcher.BeginInvoke(() => progressViewModel.SetCompleted($"Finished."));
        }
    }
}
