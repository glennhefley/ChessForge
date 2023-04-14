﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ImportWebGamesDialog.xaml
    /// </summary>
    public partial class DownloadWebGamesDialog : Window
    {
        /// <summary>
        /// Constructor. Sets up event handler.
        /// </summary>
        public DownloadWebGamesDialog()
        {
            InitializeComponent();
            GameDownload.UserGamesReceived += UserGamesReceived;
            EnableControls(false);

            SetControlValues();

            UiCmbSite.Items.Add(Constants.LichessNameId);
            UiCmbSite.Items.Add(Constants.ChesscomNameId);

            UiCmbSite.SelectedItem = Configuration.WebGamesSite;
            if (UiCmbSite.SelectedItem == null)
            {
                UiCmbSite.SelectedItem = Constants.LichessNameId;
            }

            SetUserName();
        }

        /// <summary>
        /// Invoked when the games download has finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserGamesReceived(object sender, WebAccessEventArgs e)
        {
            try
            {
                if (e.Success)
                {
                    if (string.IsNullOrEmpty(e.TextData))
                    {
                        throw new Exception(Properties.Resources.ErrNoGamesDownloaded);
                    }
                    if (e.TextData.IndexOf("DOCTYPE") > 0 && e.TextData.IndexOf("DOCTYPE") < 10)
                    {
                        throw new Exception(Properties.Resources.ErrGameNotFound);
                    }
                    ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                    int gamesCount = PgnMultiGameParser.ParsePgnMultiGameText(e.TextData, ref games);
                    SelectGames(ref games);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.GameDownloadError + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            EnableControls(false);
        }

        /// <summary>
        /// Set user name per currently selected web site.
        /// </summary>
        private void SetUserName()
        {
            if ((string)UiCmbSite.SelectedItem == Constants.ChesscomNameId)
            {
                UiTbUserName.Text = Configuration.WebGamesChesscomUser;
            }
            else
            {
                UiTbUserName.Text = Configuration.WebGamesLichessUser;
            }
        }

        /// <summary>
        /// Set values in the controls per configuration items.
        /// </summary>
        private void SetControlValues()
        {
            UiTbMaxGames.Text = Math.Max(Configuration.WebGamesMaxCount, 1).ToString();

            EnableDateControls(!Configuration.WebGamesMostRecent);
            UiCbOnlyNew.IsChecked = Configuration.WebGamesMostRecent;

            UiDtStartDate.SelectedDate = Configuration.WebGamesStartDate;
            UiDtEndDate.SelectedDate = Configuration.WebGamesEndDate;
        }

        /// <summary>
        /// Invokes the dialog to select games for import.
        /// </summary>
        /// <param name="games"></param>
        /// <returns></returns>
        private bool SelectGames(ref ObservableCollection<GameData> games)
        {
            for (int i = 0; i < games.Count; i++)
            {
                games[i].OrderNo = (i + 1).ToString();
            }

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, SelectGamesDialog.Mode.DOWNLOAD_WEB_GAMES)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };
            return dlg.ShowDialog() == true;
        }

        /// <summary>
        /// Enables/disables controls depending on whether there is a download in progress.
        /// </summary>
        /// <param name="isDownloading"></param>
        private void EnableControls(bool isDownloading)
        {
            UiLblLoading.Visibility = isDownloading ? Visibility.Visible : Visibility.Collapsed;

            UiBtnDownload.IsEnabled = !isDownloading;
            UiCbOnlyNew.IsEnabled = !isDownloading;
            UiCmbSite.IsEnabled = !isDownloading;
            UiTbMaxGames.IsEnabled = !isDownloading;
            UiTbUserName.IsEnabled = !isDownloading;
            UiDtStartDate.IsEnabled = !isDownloading;
            UiDtEndDate.IsEnabled = !isDownloading;
        }

        /// <summary>
        /// The user clicked the button requesting the download.
        /// This method kicks off the process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UiTbUserName.Text))
            {
                MessageBox.Show(Properties.Resources.ErrEmptyUserName, Properties.Resources.PromptCorrectData, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                UpdateConfiguration();

                EnableControls(true);
                GamesFilter filter = new GamesFilter();
                filter.User = UiTbUserName.Text;

                int gameCount;
                int.TryParse(UiTbMaxGames.Text, out gameCount);
                if (gameCount <= 0)
                {
                    gameCount = WebAccess.GameDownload.DEFAULT_DOWNLOAD_GAME_COUNT;
                }
                filter.MaxGames = gameCount;

                _ = WebAccess.GameDownload.GetLichessUserGames(filter);
            }
        }

        /// <summary>
        /// Update WebGames configuaration items.
        /// </summary>
        private void UpdateConfiguration()
        {
            string site = (string)UiCmbSite.SelectedItem;
            Configuration.WebGamesSite = site;
            if (site == Constants.LichessNameId)
            {
                Configuration.WebGamesLichessUser = UiTbUserName.Text;
            }
            else if (site == Constants.ChesscomNameId)
            {
                Configuration.WebGamesChesscomUser = UiTbUserName.Text;
            }

            int.TryParse(UiTbMaxGames.Text, out Configuration.WebGamesMaxCount);

            Configuration.WebGamesMostRecent = UiCbOnlyNew.IsChecked == true;
            Configuration.WebGamesStartDate = UiDtStartDate.SelectedDate;
            Configuration.WebGamesEndDate = UiDtEndDate.SelectedDate;
        }

        /// <summary>
        /// Enable/disable date controls.
        /// </summary>
        /// <param name="enable"></param>
        private void EnableDateControls(bool enable)
        {
            UiDtStartDate.IsEnabled = enable;
            UiDtEndDate.IsEnabled = enable;
        }

        /// <summary>
        /// Checkbox for "Recent Games only" chnaged.
        /// Disable date controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbOnlyNew_Checked(object sender, RoutedEventArgs e)
        {
            EnableDateControls(false);
        }

        /// <summary>
        /// Checkbox for "Recent Games only" chnaged.
        /// Enable date controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbOnlyNew_Unchecked(object sender, RoutedEventArgs e)
        {
            EnableDateControls(true);
        }

        /// <summary>
        /// Web site selection changed so change the user name accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmbSite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetUserName();
        }

        /// <summary>
        /// Remove handler subscription.
        /// Otherwise, the it will be called twice when the dialog is called again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GameDownload.UserGamesReceived -= UserGamesReceived;
        }
    }
}
