using ArchiWindRevitAddIn.ViewModels;
using System.Windows;

namespace ArchiWindRevitAddIn.Views
{
    public sealed partial class CreateSimulationView : Window
    {
        public CreateSimulationView(CreateSimulationViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}