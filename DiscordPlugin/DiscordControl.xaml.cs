using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch;

namespace DiscordPlugin
{
    /// <summary>
    /// Interaction logic for DiscordControl.xaml
    /// </summary>
    public partial class DiscordControl : UserControl
    {
        private IBotControl _botControl;
        public DiscordControl(IBotControl botControl)
        {
            _botControl = botControl;
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            TorchBase.Instance.InvokeAsync(() => _botControl.Start());
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            TorchBase.Instance.InvokeAsync(() => _botControl.Stop());
        }
    }
}
