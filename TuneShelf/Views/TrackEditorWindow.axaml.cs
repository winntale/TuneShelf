using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using TuneShelf.Models;
using TuneShelf.ViewModels;

namespace TuneShelf.Views;

public partial class TrackEditorWindow : Window
{
    public TrackEditorWindow()
    {
        InitializeComponent();

        if (!Design.IsDesignMode)
        {
            DataContextChanged += OnDataContextChanged;
        }
    }
    
    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TrackEditorViewModel vm)
        {
            vm.CloseRequested += (_, _) => Close();
        }
    }
    
    private void AlbumComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is TrackEditorViewModel vm && sender is ComboBox comboBox)
        {
            vm.SelectedAlbum = comboBox.SelectedItem as Album;
        }
    }
    
    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackEditorViewModel vm && vm.SaveCommand.CanExecute(null))
        {
            vm.SaveCommand.Execute(null);
        }
    }
    
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackEditorViewModel vm && vm.CancelCommand.CanExecute(null))
        {
            vm.CancelCommand.Execute(null);
        }
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}