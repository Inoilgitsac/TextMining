using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
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
using TextMiningLibrary;

namespace TextMiningWPF
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

        private void btn_searchDocumentPath(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                documentPath.Text = openFileDialog.FileName;
        }

        private void btn_searchLogPath(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                logPath.Text = dialog.SelectedPath;
            }
        }

        private void btn_SearchOnDocument(object sender, RoutedEventArgs e)
        {
            var values = SearchTextInPdf.Search(documentPath.Text, logPath.Text, conditions.Text);
            foreach (var value in values)
                findings.Items.Add(value);
        }
    }
}
