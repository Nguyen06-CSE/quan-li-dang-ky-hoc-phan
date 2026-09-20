using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public DashboardView(DashboardViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
