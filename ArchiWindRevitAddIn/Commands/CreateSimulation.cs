using ArchiWindRevitAddIn.ViewModels;
using ArchiWindRevitAddIn.Views;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;

namespace ArchiWindRevitAddIn.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CreateSimulation : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new CreateSimulationViewModel(Application.ActiveUIDocument.Document);
            var view = new CreateSimulationView(viewModel);
            view.ShowDialog();
        }
    }
}