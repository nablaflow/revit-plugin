using ArchiWindRevitAddIn.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace ArchiWindRevitAddIn.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class PreviewGeometries : ExternalCommand
    {
        private const string TRANSACTION_NAME = "ArchiWind 3D views setup";

        private List<string> viewNames = [
            Utils.BUILDING_VIEW,
            Utils.SURROUNDINGS_VIEW,
            Utils.VEGETATION_VIEW,
            Utils.TERRAIN_VIEW,
        ];

        public override void Execute()
        {
            var activeView = Application.ActiveUIDocument.ActiveView;
            Document doc = activeView.Document;

            if (activeView.ViewType != ViewType.ThreeD || viewNames.Contains(activeView.Name))
            {
                TaskDialog.Show("Error",
                                $"Please select the document's 3D view.",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);

                Result = Result.Failed;

                return;
            }

            using var t = new Transaction(doc, TRANSACTION_NAME);

            try
            {
                t.Start();

                var buildingView = Utils.CreateView(doc, activeView, Utils.BUILDING_VIEW);
                var surroundingsView = Utils.CreateView(doc, activeView, Utils.SURROUNDINGS_VIEW);
                var vegetationView = Utils.CreateView(doc, activeView, Utils.VEGETATION_VIEW);
                var terrainView = Utils.CreateView(doc, activeView, Utils.TERRAIN_VIEW);

                Utils.OnlyShowCategories(doc, buildingView, Models.Categories.DefaultBuildingCategories);
                Utils.OnlyShowCategories(doc, surroundingsView, Models.Categories.DefaultSurroundingsCategories);
                Utils.OnlyShowCategories(doc, vegetationView, Models.Categories.DefaultVegetationCategories);
                Utils.OnlyShowCategories(doc, terrainView, Models.Categories.DefaultTerrainCategories);

                t.Commit();

                Application.ActiveUIDocument.RequestViewChange(buildingView);
                Application.ActiveUIDocument.RequestViewChange(surroundingsView);
                Application.ActiveUIDocument.RequestViewChange(vegetationView);
                Application.ActiveUIDocument.RequestViewChange(terrainView);

                TaskDialog.Show("Success",
                    $"Views created.\nYou can now customise them with what should be visibile and be used for export.",
                    TaskDialogCommonButtons.Ok,
                    TaskDialogResult.Ok);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error",
                                $"An error occured while building previews, please report to the developer: \n\n{ex.GetType()}\n{ex.Message}",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);

                if (t.GetStatus() == TransactionStatus.Started)
                {
                    t.RollBack();
                }

                Result = Result.Failed;

                return;
            }
        }
    }
}
