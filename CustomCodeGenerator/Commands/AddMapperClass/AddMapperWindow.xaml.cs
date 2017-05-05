using System.Windows;

namespace CustomCodeGenerator.Commands.AddMapperClass
{
    public partial class AddMapperWindow : Window
    {
        public AddMapperWindow(AddMapperViewModel model)
        {
            InitializeComponent();

            DataContext = model;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}