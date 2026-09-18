using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class SinhVienEditDialog : Window
{
    public SinhVienEditDialog()
    {
        InitializeComponent();
    }

    public SinhVienEditDialog(SinhVienEditDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = () => Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
