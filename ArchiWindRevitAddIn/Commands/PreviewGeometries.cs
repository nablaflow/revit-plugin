using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using System.Collections.Immutable;

namespace ArchiWindRevitAddIn.Commands
{
    /// <summary>
    ///     External command entry point
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class PreviewGeometries : ExternalCommand
    {
        private const string TRANSACTION_NAME = "ArchiWind 3D views setup";

        private const string BUILDING_VIEW = "ArchiWind - Building View";
        private const string SURROUNDINGS_VIEW = "ArchiWind - Surroundings View";
        private const string TERRAIN_VIEW = "ArchiWind - Terrain View";
        private const string VEGETATION_VIEW = "ArchiWind - Vegetation View";

        public override void Execute()
        {
            Document doc = ActiveView.Document;

            if (ActiveView.ViewType != ViewType.ThreeD)
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

                var buildingView = CreateView(BUILDING_VIEW, doc, ActiveView);
                var surroundingsView = CreateView(SURROUNDINGS_VIEW, doc, ActiveView);
                var vegetationView = CreateView(VEGETATION_VIEW, doc, ActiveView);
                var terrainView = CreateView(TERRAIN_VIEW, doc, ActiveView);

#if REVIT2025_OR_GREATER
                OnlyShowCategories(doc, buildingView, [
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Curtain_Systems,
                    BuiltInCategory.OST_CurtainGrids,
                    BuiltInCategory.OST_CurtainWallMullions,
                    BuiltInCategory.OST_CurtainWallPanels,
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Mass,
                    BuiltInCategory.OST_Railings,
                    BuiltInCategory.OST_Roofs,
                    BuiltInCategory.OST_Stairs,
                    BuiltInCategory.OST_StairsRailing,
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Windows,
                ]);
                OnlyShowCategories(doc, surroundingsView, [
                    BuiltInCategory.OST_Roads,
                    BuiltInCategory.OST_Site,
                ]);
                OnlyShowCategories(doc, vegetationView, [
                    BuiltInCategory.OST_Planting
                ]);
                OnlyShowCategories(doc, terrainView, [
                    BuiltInCategory.OST_Topography,
                    BuiltInCategory.OST_Toposolid,
                ]);
#endif

                t.Commit();
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

            //ProjectLocation loc = currentDocument.ActiveProjectLocation;

            //var terrainView = this.ActiveView.Duplicate(ViewDuplicateOption.Duplicate);
            //new FilteredElementCollector(currentDocument, terrainView).

            //STLExportOptions exportOptions = new STLExportOptions();
            //exportOptions.TargetUnit = ExportUnit.Meter;
            //exportOptions.SetTessellationSettings(ExportResolution.Coarse);
            //exportOptions.ExportBinary = true;

            //CustomExporter customExporter = new CustomExporter(currentDocument, new TerrainExportContext());
            //customExporter.Export(this.ActiveView);

            //currentDocument.Export("C:\\Users\\your mom\\Desktop", "terrain.stl", exportOptions);
        }

#if REVIT2025_OR_GREATER
        private static void OnlyShowCategories(Document doc, View3D view, ImmutableHashSet<BuiltInCategory> showCategories)
        {
            foreach (Category cat in doc.Settings.Categories)
            {
                if (!view.CanCategoryBeHidden(cat.Id))
                {
                    continue;
                }

                view.SetCategoryHidden(cat.Id, !showCategories.Contains(cat.BuiltInCategory));
            }
        }
#endif

        private static void DeleteViewIfExists(string name, Document doc)
        {
            try
            {
                View view = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Views)
                    .WhereElementIsNotElementType()
                    .Cast<View>()
                    .First(x => x.Name == name);

                doc.Delete(view.Id);
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }

        private static View3D CreateView(string name, Document doc, View threeDView)
        {
            DeleteViewIfExists(name, doc);

            var view = DuplicateThreeDView(doc, threeDView);

            view.Name = name;
            view.DetailLevel = ViewDetailLevel.Coarse;
            view.DisplayStyle = DisplayStyle.Shading;

            return view;
        }

        private static View3D DuplicateThreeDView(Document doc, View threeDView)
        {
            var viewId = threeDView.Duplicate(ViewDuplicateOption.Duplicate);

            if (doc.GetElement(viewId) is not View3D view)
            {
                throw new Exception("failed to duplicate 3D View");
            }

            view.ViewTemplateId = ElementId.InvalidElementId;

            return view;
        }
    }
}
