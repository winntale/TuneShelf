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
}
