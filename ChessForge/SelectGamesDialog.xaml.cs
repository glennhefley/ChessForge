﻿using ChessPosition.GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectGamesDialog.xaml
    /// </summary>
    public partial class SelectGamesDialog : Window
    {
        /// <summary>
        /// Exit result of this dialog.
        /// </summary>
        public bool ExitOK = false;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<GameData> _gameList;

        /// <summary>
        /// Creates the dialog object. Sets ItemsSource for the ListView
        /// to GamesHeaders list.
        /// </summary>
        public SelectGamesDialog(ref ObservableCollection<GameData> gameList, string infoText)
        {
            _gameList = gameList;
            InitializeComponent();
            UiLvGames.ItemsSource = gameList;
            UiLblInstruct.Content = infoText;
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// SelectAll box was unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gameList)
            {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = true;
            Close();
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }
    }
}
