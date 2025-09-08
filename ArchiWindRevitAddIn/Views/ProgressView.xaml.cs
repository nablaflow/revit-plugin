using ArchiWindRevitAddIn.ViewModels;
using System.ComponentModel;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace ArchiWindRevitAddIn.Views
{
    public sealed partial class ProgressView : Window
    {
        public ProgressViewModel ViewModel { get; private set; }

        public ProgressView(ProgressViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();

            viewModel.CloseCommand = new RelayCommand(() =>
            {
                DialogResult = viewModel.IsCompleted;
                Close();
            }, () => viewModel.IsCompleted);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!ViewModel.IsCompleted && ViewModel.CanCancel)
            {
                var result = MessageBox.Show(
                    "The process is still in progress. Do you want to cancel it?",
                    "Confirm Close",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ViewModel.CancelCommand.Execute(null);
                }
                else
                {
                    e.Cancel = true;
                }
            }

            base.OnClosing(e);
        }
    }
}
