using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TuneShelf.ViewModels;

namespace TuneShelf.Views;

public partial class TrackEditorWindow : Window
{
    public TrackEditorWindow()
    {
        InitializeComponent();

        if (!Design.IsDesignMode)
        {
            if (DataContext is TrackEditorViewModel vm)
            {
                // Подписка на Save/Cancel через закрытие окна – проще сделать так:
                // но у нас Save/Cancel меняют IsConfirmed/ResultTrack.
            }
        }
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}