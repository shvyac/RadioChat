using System.Windows;

namespace RadioChatClient
{
    public partial class ConnectForm : Window
    {
        public ConnectForm()
        {
            InitializeComponent();
        }
        public string HostName
        {
            get
            {
                return hostTextBox.Text;
            }
        }
        public int Port
        {
            get
            {
                return int.Parse(portUpDown.Text);
            }
        }
        public string NickName
        {
            get
            {
                return nameTextBox.Text;
            }
        }
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Initialized(object sender, System.EventArgs e)
        {
            hostTextBox.Text = "192.168.1.6";
            portUpDown.Text = "23";
            nameTextBox.Text = "user001";
        }
    }
}
