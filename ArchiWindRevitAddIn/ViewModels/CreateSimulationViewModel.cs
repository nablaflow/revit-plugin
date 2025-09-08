using ArchiwindRevitAddIn.Api.Models;
using ArchiWindRevitAddIn.Models;
using ArchiWindRevitAddIn.Models.Forms;
using ArchiWindRevitAddIn.Models.Validators;
using ArchiWindRevitAddIn.Services;
using ArchiWindRevitAddIn.Tasks;
using ArchiWindRevitAddIn.Views;
using Autodesk.Revit.UI;
using FluentValidation;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Cursors = System.Windows.Input.Cursors;

namespace ArchiWindRevitAddIn.ViewModels
{
    public sealed partial class CreateSimulationViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private readonly CreateSimulationFormValidator validator = new();
        private readonly CreateSimulationForm simParams = new();

        private readonly Dictionary<string, List<string>> errors = [];

        [ObservableProperty]
        private ProjectV1? selectedProject;

        [ObservableProperty]
        private bool isProjectSelectionEnabled = false;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private bool isDraftQuality = true;

        [ObservableProperty]
        private bool isDetailedQuality = false;

        [ObservableProperty]
        private string latitude = string.Empty;

        [ObservableProperty]
        private string longitude = string.Empty;

        [ObservableProperty]
        private int? selectedRefSystem;

        [ObservableProperty]
        private bool hasBuilding = true;

        [ObservableProperty]
        private bool hasSurroundings = true;

        [ObservableProperty]
        private bool hasTerrain = true;

        [ObservableProperty]
        private bool hasVegetation = true;

        [ObservableProperty]
        public bool areViewGeometriesLoaded = false;

        [ObservableProperty]
        private bool isBuildingEnabled;

        [ObservableProperty]
        private bool areSurroundingsEnabled;

        [ObservableProperty]
        private bool isTerrainEnabled;

        [ObservableProperty]
        private bool isVegetationEnabled;

        [ObservableProperty]
        private string buildingViewStatus = string.Empty;

        [ObservableProperty]
        private string surroundingsViewStatus = string.Empty;

        [ObservableProperty]
        private string terrainViewStatus = string.Empty;

        [ObservableProperty]
        private string vegetationViewStatus = string.Empty;

        public ObservableCollection<ProjectV1> Projects { get; } = [];

        public ObservableCollection<int> RefSystems { get; } = [];

        public AsyncRelayCommand CreateCommand { get; private set; }
        public RelayCommand LoadCoordinatesFromDocument { get; private set; }
        public RelayCommand ClearRefSystem { get; private set; }
        public RelayCommand DoUpdateGeometriesControls { get; private set; }
        public AsyncRelayCommand LoadProjects { get; private set; }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private Document Document { get; set; }

        public CreateSimulationViewModel(Document document)
        {
            CreateCommand = new(Create, CanCreate);
            LoadCoordinatesFromDocument = new(PerformLoadCoordinatesFromDocument);
            ClearRefSystem = new(PerformClearRefSystem);
            LoadProjects = new(PerformLoadProjects);
            DoUpdateGeometriesControls = new(UpdateGeometriesControls);

            Document = document;
            Name = document.Title;

            RefSystems = new(Epsg.Values);
        }

        private bool CanCreate()
        {
            return !HasErrors;
        }

        private async Task Create()
        {
            ValidateAllProperties();

            if (CanCreate() == false)
            {
                return;
            }

            ProgressViewModel? progressViewModel = null;
            ProgressView? progressView = null;

            var progressThread = new Thread(() =>
            {
                progressViewModel = new("Simulation creation progress");
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

            try
            {
                var createdSimulation = await CreateSimulationTask.Run(progressViewModel, Document, simParams);

                if (createdSimulation is null)
                {
                    await dispatcher.BeginInvoke(() => progressViewModel.SetCompleted($"Error: no simulation was returned"));

                    return;
                }

                await dispatcher.BeginInvoke(() => progressViewModel.SetCompleted("Simulation created."));

                OpenSimulationInBrowser(createdSimulation);

                await dispatcher.BeginInvoke(() => progressView.Close());
            }
            catch (JsonErrorResponse ex)
            {
                await dispatcher.BeginInvoke(() => progressViewModel.SetCompleted($"Server error: {ex.Message}"));
            }
            catch (OperationCanceledException)
            {
                await dispatcher.BeginInvoke(() => progressViewModel.SetCompleted("Operation was cancelled."));
            }
            catch (Exception ex)
            {
                await dispatcher.BeginInvoke(() => progressViewModel.SetCompleted($"Error: {ex.GetType()}, {ex.Message}"));
            }
        }

        private async Task PerformLoadProjects()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ServiceLocator.ApiClient.V1.Projects.GetAsProjectsGetResponseAsync();

                Projects.Clear();

                if (response?.Items != null)
                {
                    foreach (var project in response.Items)
                    {
                        Projects.Add(project);
                    }
                }

                SelectedProject = Projects.Count > 0 ? Projects.First() : null;

                if (SelectedProject != null)
                {
                    IsProjectSelectionEnabled = true;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error",
                                $"An error occured, please report to the developer: \n\n{ex.GetType()}\n{ex.Message}",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void PerformClearRefSystem()
        {
            SelectedRefSystem = null;
        }

        public bool HasErrors => errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName is null)
            {
                return Enumerable.Empty<string>();
            }

            return errors.TryGetValue(propertyName, out var errorsList) ? errorsList : Enumerable.Empty<string>();
        }

        partial void OnSelectedProjectChanged(ProjectV1? value)
        {
            if (value == null) { return; }

            simParams.ProjectId = value.Id!.Value;

            ValidateProperty("ProjectId");
        }

        partial void OnNameChanged(string value)
        {
            simParams.Name = value;

            ValidateProperty(nameof(Name));
        }

        partial void OnLatitudeChanged(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedValue))
            {
                return;
            }

            simParams.Latitude = parsedValue;

            ValidateProperty(nameof(Latitude));
        }
        partial void OnLongitudeChanged(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedValue))
            {
                return;
            }

            simParams.Longitude = parsedValue;

            ValidateProperty(nameof(Longitude));
        }

        partial void OnSelectedRefSystemChanged(int? value)
        {
            if (value == null)
            {
                return;
            }

            simParams.RefSystem = value.Value;
            ValidateProperty(nameof(simParams.RefSystem));
        }

        partial void OnHasSurroundingsChanged(bool value)
        {
            simParams.HasSurroundings = value;

            errors.Remove(nameof(HasBuilding));
            errors.Remove(nameof(HasTerrain));
            ValidateProperty(nameof(HasSurroundings));
        }

        partial void OnHasBuildingChanged(bool value)
        {
            simParams.HasBuilding = value;

            errors.Remove(nameof(HasSurroundings));
            errors.Remove(nameof(HasTerrain));
            ValidateProperty(nameof(HasBuilding));
        }

        partial void OnHasTerrainChanged(bool value)
        {
            simParams.HasTerrain = value;

            errors.Remove(nameof(HasSurroundings));
            errors.Remove(nameof(HasBuilding));
            ValidateProperty(nameof(HasTerrain));
        }

        partial void OnHasVegetationChanged(bool value)
        {
            simParams.HasVegetation = value;

            ValidateProperty(nameof(HasVegetation));
        }

        private void ValidateProperty(string propertyName)
        {
            var results = validator.Validate(simParams, options => options.IncludeProperties(propertyName));

            if (results.IsValid)
            {
                errors.Remove(propertyName);
            }
            else
            {
                errors[propertyName] = [.. results.Errors.Select(e => e.ErrorMessage)];
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

            CreateCommand.NotifyCanExecuteChanged();
        }

        private void ValidateAllProperties()
        {
            errors.Clear();

            var results = validator.Validate(simParams);

            foreach (var error in results.Errors)
            {
                if (!errors.TryGetValue(error.PropertyName, out List<string>? value))
                {
                    value = [];

                    errors[error.PropertyName] = value;
                }

                value.Add(error.ErrorMessage);

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(error.PropertyName));
            }

            CreateCommand.NotifyCanExecuteChanged();
        }

        private void PerformLoadCoordinatesFromDocument()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var coordinates = WSG84.FromDocument(Document);

                if (coordinates == null)
                {
                    return;
                }

                Latitude = coordinates.Latitude.ToString("F6", CultureInfo.CurrentCulture);
                Longitude = coordinates.Longitude.ToString("F6", CultureInfo.CurrentCulture);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void UpdateGeometriesControls()
        {
            AreViewGeometriesLoaded = false;

            UpdateGeometryControl(
                Utils.BUILDING_VIEW,
                flag => { HasBuilding = flag; OnHasBuildingChanged(flag); },
                flag => IsBuildingEnabled = flag,
                s => BuildingViewStatus = s
            );

            UpdateGeometryControl(
                 Utils.SURROUNDINGS_VIEW,
                 flag => { HasSurroundings = flag; OnHasSurroundingsChanged(flag); },
                 flag => AreSurroundingsEnabled = flag,
                 s => SurroundingsViewStatus = s
            );

            UpdateGeometryControl(
                 Utils.TERRAIN_VIEW,
                 flag => { HasTerrain = flag; OnHasTerrainChanged(flag); },
                 flag => IsTerrainEnabled = flag,
                 s => TerrainViewStatus = s
            );

            UpdateGeometryControl(
                 Utils.VEGETATION_VIEW,
                 flag => { HasVegetation = flag; OnHasVegetationChanged(flag); },
                 flag => IsVegetationEnabled = flag,
                 s => VegetationViewStatus = s
            );

            if (!(HasBuilding || HasSurroundings || HasTerrain))
            {
                TaskDialog.Show("Error",
                                "At least one of building, surroundings or terrain has to be present.",
                                TaskDialogCommonButtons.Ok,
                                TaskDialogResult.Ok);
            }

            AreViewGeometriesLoaded = true;
        }

        private void UpdateGeometryControl(string viewName, Action<bool> hasGeometry, Action<bool> isEnabled, Action<string> status)
        {
            if (Utils.FindView(Document, viewName) is not View3D view)
            {
                isEnabled(false);
                hasGeometry(false);
                status("View is missing");
                return;
            }

            var elementsInView = Utils.ShownElementsCount(Document, view);

            if (elementsInView == 0)
            {
                isEnabled(false);
                hasGeometry(false);
                status("View is empty");
            }

            isEnabled(true);
            hasGeometry(true);
            status($"{elementsInView} element{(elementsInView > 1 ? "s" : "")}");
        }

        private static void OpenSimulationInBrowser(SimulationV1 sim)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = sim.BrowserUrl!,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Cannot open URL to simulation: {ex.Message}", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);
            }
        }
    }
}
