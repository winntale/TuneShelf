using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using TuneShelf.Interfaces;
using TuneShelf.Models;

namespace TuneShelf.Services;

public sealed class DialogService : IDialogService
{
    public async Task<Album?> ShowAlbumEditorAsync(Album? album)
    {
        var window = new Window
        {
            Title = album is null ? "Создать альбом" : "Редактировать альбом",
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid
        {
            Margin = new Thickness(20)
        };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));


        var titleBox = new TextBox
        {
            Watermark = "Название альбома",
            Text = album?.Title ?? string.Empty,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(titleBox, 0);

        var yearBox = new NumericUpDown
        {
            Watermark = "Год",
            Value = album?.Year ?? DateTime.Now.Year,
            Minimum = 1900,
            Maximum = DateTime.Now.Year + 1,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(yearBox, 1);
        
        var libraryService = new LibraryService();
        var defaultArtistId = await libraryService.GetOrCreateDefaultArtistIdAsync();
        
        var artistIdBox = new TextBox
        {
            Watermark = "ID артиста (Guid, опционально)",
            Text = album?.ArtistId.ToString() ?? defaultArtistId.ToString(),
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(artistIdBox, 2);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        Grid.SetRow(buttonPanel, 4);

        var okButton = new Button { Content = "OK", Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(titleBox);
        grid.Children.Add(yearBox);
        grid.Children.Add(artistIdBox);
        grid.Children.Add(buttonPanel);

        window.Content = grid;

        var tcs = new TaskCompletionSource<Album?>();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text))
                return;

            if (!Guid.TryParse(artistIdBox.Text, out var artistId))
                artistId = defaultArtistId;

            var result = new Album
            {
                Id = album?.Id ?? Guid.NewGuid(),
                Title = titleBox.Text,
                Year = (int)(yearBox.Value ?? DateTime.Now.Year),
                ArtistId = artistId
            };

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(result);

            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);

            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };
        
        var app = Avalonia.Application.Current;
        Window? parentWindow = null;
        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            parentWindow = desktop.MainWindow;
        }

        await window.ShowDialog(parentWindow);
        return await tcs.Task;
    }
    
    public async Task<Artist?> ShowArtistEditorAsync(Artist? artist) 
    {
        var window = new Window
        {
            Title = artist is null ? "Создать артиста" : "Редактировать артиста",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var nameBox = new TextBox
        {
            Watermark = "Имя артиста",
            Text      = artist?.Name ?? string.Empty,
            Margin    = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(nameBox, 0);

        var buttons = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing             = 10
        };
        Grid.SetRow(buttons, 1);

        var okButton     = new Button { Content = "OK",     Width = 80 };
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        grid.Children.Add(nameBox);
        grid.Children.Add(buttons);
        window.Content = grid;

        var tcs = new TaskCompletionSource<Artist?>();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
                return;

            var result = new Artist
            {
                Id   = artist?.Id ?? Guid.NewGuid(),
                Name = nameBox.Text
            };

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(result);

            window.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);

            window.Close();
        };

        window.Closing += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(null);
        };

        Window? parent = null;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            parent = desktop.MainWindow;

        await window.ShowDialog(parent);
        return await tcs.Task;
    }

}

