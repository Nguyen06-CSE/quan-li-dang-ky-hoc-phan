using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace QuanLyDKHP.App.Views;

public partial class NguoiDungView : UserControl
{
    public NguoiDungView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
