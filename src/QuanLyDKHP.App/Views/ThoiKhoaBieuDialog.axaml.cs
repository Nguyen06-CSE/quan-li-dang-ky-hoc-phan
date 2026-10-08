using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class ThoiKhoaBieuDialog : Window
{
    public ThoiKhoaBieuDialog()
    {
        InitializeComponent();
    }

    public ThoiKhoaBieuDialog(ThoiKhoaBieuDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = () => Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
