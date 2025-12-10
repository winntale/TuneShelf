using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using TuneShelf.ViewModels;
using Avalonia.Markup.Xaml;

namespace TuneShelf.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void AddTrackButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm is null) return;

        var editorVm = new TrackEditorViewModel();
        var editorWindow = new TrackEditorWindow
        {
            DataContext = editorVm
        };

        var result = await editorWindow.ShowDialog<bool>(this);

        if (editorVm.IsConfirmed && editorVm.ResultTrack is not null)
        {
            await vm.CreateTrackFromDialogAsync(editorVm.ResultTrack);
        }
    }

    private async void EditTrackButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm?.SelectedTrack is null) return;

        var editorVm = new TrackEditorViewModel(track: vm.SelectedTrack);
        var editorWindow = new TrackEditorWindow
        {
            DataContext = editorVm
        };

        var result = await editorWindow.ShowDialog<bool>(this);

        if (editorVm.IsConfirmed && editorVm.ResultTrack is not null)
        {
            await vm.UpdateTrackFromDialogAsync(editorVm.ResultTrack);
        }
    }
    
    
    
    private async Task OpenCreateTrackDialogAsync()
    {
        var mainVm = (MainWindowViewModel)DataContext!;
        var albums = await mainVm.GetAllAlbumsAsync(); // проброс к LibraryService

        var editorVm = new TrackEditorViewModel();
        foreach (var album in albums)
            editorVm.Albums.Add(album);

        // По умолчанию — первый альбом (или дефолтный, если хочешь найти по названию)
        editorVm.SelectedAlbum = editorVm.Albums.FirstOrDefault();

        var editorWindow = new TrackEditorWindow { DataContext = editorVm };
        await editorWindow.ShowDialog(this);

        if (editorVm.IsConfirmed && editorVm.ResultTrack is not null)
        {
            await mainVm.CreateTrackFromDialogAsync(editorVm.ResultTrack);
        }
    }

    private async Task OpenEditTrackDialogAsync()
    {
        var mainVm = (MainWindowViewModel)DataContext!;
        if (mainVm.SelectedTrack is null) return;

        var albums = await mainVm.GetAllAlbumsAsync();

        var editorVm = new TrackEditorViewModel(mainVm.SelectedTrack);
        foreach (var album in albums)
            editorVm.Albums.Add(album);

        // Выставляем SelectedAlbum по Id трека
        editorVm.SelectedAlbum = editorVm.Albums
                                     .FirstOrDefault(a => a.Id == mainVm.SelectedTrack.AlbumId)
                                 ?? editorVm.Albums.FirstOrDefault();

        var editorWindow = new TrackEditorWindow { DataContext = editorVm };
        await editorWindow.ShowDialog(this);

        if (editorVm.IsConfirmed && editorVm.ResultTrack is not null)
        {
            await mainVm.UpdateTrackFromDialogAsync(editorVm.ResultTrack);
        }
    }
}