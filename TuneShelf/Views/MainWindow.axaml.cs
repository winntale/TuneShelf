using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
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
    
    private void MiniPlayerSeek_Start(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.MiniPlayer.BeginUserSeek();
    }

    private async void MiniPlayerSeek_End(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (sender is not Slider slider) return;

        await vm.MiniPlayer.EndUserSeekAsync(slider.Value);
    }
}
