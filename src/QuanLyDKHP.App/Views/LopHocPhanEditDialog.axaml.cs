using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyDKHP.App.ViewModels;

namespace QuanLyDKHP.App.Views;

public partial class LopHocPhanEditDialog : Window
{
    public LopHocPhanEditDialog()
    {
        InitializeComponent();
    }

    public LopHocPhanEditDialog(LopHocPhanEditDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = () => Close();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
