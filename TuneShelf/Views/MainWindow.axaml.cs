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
        => await OpenCreateTrackDialogAsync();

    private async void EditTrackButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await OpenEditTrackDialogAsync();

    private async Task OpenCreateTrackDialogAsync()
    {
        var mainVm = (MainWindowViewModel)DataContext!;
        var albums = await mainVm.GetAllAlbumsAsync();

        var editorVm = new TrackEditorViewModel();
        foreach (var album in albums)
            editorVm.Albums.Add(album);

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
