using Avalonia.Controls;
using BattleshipsLan.UI.ViewModels;

namespace BattleshipsLan.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new GameViewModel();
    }
}