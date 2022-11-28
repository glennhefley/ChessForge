﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for AboutBoxDialog.xaml
    /// </summary>
    public partial class AboutBoxDialog : Window
    {
        /// <summary>
        /// Instantiates the dialog and initializes the controls. 
        /// </summary>
        public AboutBoxDialog()
        {
            InitializeComponent();

            this.WindowStyle = WindowStyle.None;  

            Paragraph dummy = new Paragraph();
            dummy.Margin = new Thickness(0, 0, 0, 0);
            dummy.Inlines.Add(new Run(""));

            _rtbAboutBox.Document.Blocks.Add(dummy);

            Paragraph title = CreateTitleParagraph();
            _rtbAboutBox.Document.Blocks.Add(title);

            Paragraph info = CreateInfoParagraph();
            _rtbAboutBox.Document.Blocks.Add(info);
        }

        /// <summary>
        /// Creates the Title text.
        /// </summary>
        /// <returns></returns>
        private Paragraph CreateTitleParagraph()
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(10, 0, 0, 20);
            para.FontSize = 22;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Left;
            para.Foreground = Brushes.Teal;

            Run run = new Run("CHESS FORGE beta");
            para.Inlines.Add(run);

            return para;
        }

        /// <summary>
        /// Gathers all data in one paragraph.
        /// </summary>
        /// <returns></returns>
        private Paragraph CreateInfoParagraph()
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(50, 0, 0, 0);
            para.FontSize = 14;
            para.FontWeight = FontWeights.Normal;
            para.TextAlignment = TextAlignment.Left;
            para.Foreground = Brushes.Black;

            Version ver = AppStateManager.GetAssemblyVersion();

            para.Inlines.Add(new Run("Free and Open Source Software (FOSS)\n"));

            para.Inlines.Add(new Run("\nVersion: "));
            Run rVer = new Run(ver.ToString());
            rVer.FontWeight = FontWeights.Bold;
            para.Inlines.Add(rVer);

            para.Inlines.Add(new Run("\nChess Engine: "));
            Run rEng = new Run(AppStateManager.EngineName);
            rEng.FontWeight = FontWeights.Bold;
            para.Inlines.Add(rEng);

            para.Inlines.Add(new Run("\nOpenings/Tablebases: "));
            Run rLichess = new Run("lichess.org");
            rLichess.FontWeight = FontWeights.Bold;
            para.Inlines.Add(rLichess);

            return para;
        }

        /// <summary>
        /// Closes and  exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Allows the window to be moved despite WindowsStyle set to none.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
