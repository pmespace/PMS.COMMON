using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using COMMON;
using COMMON.WIN32;

namespace WpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			efBox.Text = "empty";
			IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
			Win32.PostMessage(hWnd, Win32.WM_USER, 0, 0);
			efBox.Text = "pressed";
			press.IsEnabled = false;
			reset.IsEnabled = !press.IsEnabled;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			efBox.Text = "empty";
			press.IsEnabled = true;
			reset.IsEnabled = !press.IsEnabled;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			reset.IsEnabled = false;
		}
	}
}