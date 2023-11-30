﻿using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;

namespace ChessForge
{
    /// <summary>
    /// Monitors and manages moving between tabs and articles.
    /// </summary>
    public class WorkbookLocationNavigator
    {
        // The list of location history in this workbook session
        private static List<WorkbookLocation> _locations = new List<WorkbookLocation>();

        // index of the current location in the _locations list
        private static int _currentLocationIndex = -1;

        // whether the location changes are being tracked
        private static bool _isNavigationTrackingOn;

        /// <summary>
        /// Clears the locations cache
        /// </summary>
        public static void Reset()
        {
            _currentLocationIndex = -1;
            _locations.Clear();
        }

        /// <summary>
        /// Whether the location changes are being tracked.
        /// For example, we don't want to track them if we are navigating in response
        /// to the user asking for previous/next location. Doing so would cause 
        /// dupes/confusion in navigation tracking.
        /// </summary>
        public static bool IsNavigationTrackingOn
        {
            get => _isNavigationTrackingOn;
            set => _isNavigationTrackingOn = value;
        }

        /// <summary>
        /// Returns true if there is no newer location after the current one
        /// </summary>
        public static bool IsLastLocation
        {
            get { return _currentLocationIndex == _locations.Count - 1; }
        }

        /// <summary>
        /// Returns true if there is no older location before the current one
        /// </summary>
        public static bool IsFirstLocation
        {
            get { return _currentLocationIndex <= 0; }
        }

        /// <summary>
        /// Opens a view for the specified chapter and specified content (tab) type.
        /// The active article of the requested type will be open if exists.
        /// If the type is Intro and there the Intro tab is not visible in the target
        /// chapter, the Study view will open.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="tabType"></param>
        public static void GotoArticle(int chapterIndex, TabViewType tabType)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
            if (chapter != null)
            {
                if (tabType == TabViewType.INTRO && !chapter.ShowIntro)
                {
                    tabType = TabViewType.STUDY;
                }

                WorkbookManager.SessionWorkbook.ActiveChapter = chapter;
                //AppState.MainWin.HighlightActiveChapterHeader();
                GameData.ContentType contentType = GameData.ContentType.NONE;
                int articleIndex = -1;
                switch (tabType)
                {
                    case TabViewType.STUDY:
                        contentType = GameData.ContentType.STUDY_TREE;
                        WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                        break;
                    case TabViewType.INTRO:
                        // Intro is a special case where we need to save the current one
                        AppState.MainWin.SaveIntro();
                        contentType = GameData.ContentType.INTRO;
                        AppState.MainWin.SetupGuiForIntro(true);
                        break;
                    case TabViewType.MODEL_GAME:
                        contentType = GameData.ContentType.MODEL_GAME;
                        articleIndex = chapter.ActiveModelGameIndex;
                        AppState.MainWin.SelectModelGame(articleIndex, true);
                        break;
                    case TabViewType.EXERCISE:
                        contentType = GameData.ContentType.EXERCISE;
                        articleIndex = chapter.ActiveExerciseIndex;
                        AppState.MainWin.SelectExercise(articleIndex, true);
                        break;
                }

                SaveNewLocation(chapter, contentType, articleIndex);
                AppState.EnableNavigationArrows();
            }
        }

        /// <summary>
        /// Opens a view for the specified article.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void GotoArticle(int chapterIndex, GameData.ContentType contentType, int articleIndex, bool save = true)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(chapterIndex, contentType, articleIndex, save);
                if (chapter != null)
                {
                    if (contentType == GameData.ContentType.MODEL_GAME)
                    {
                        // TODO: should SelectModelGame/Exercise be part of SetActiveChapterTreeByIndex above?
                        if (articleIndex < 0)
                        {
                            AppState.MainWin.UiTabModelGames.Focus();
                        }
                        else
                        {
                            AppState.MainWin.SelectModelGame(articleIndex, true);
                        }
                    }
                    else if (contentType == GameData.ContentType.EXERCISE)
                    {
                        if (articleIndex < 0)
                        {
                            AppState.MainWin.UiTabExercises.Focus();
                        }
                        else
                        {
                            AppState.MainWin.SelectExercise(articleIndex, true);
                        }
                    }
                    else if (contentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    else if (contentType == GameData.ContentType.INTRO)
                    {
                        AppState.MainWin.SetupGuiForIntro(true);
                    }
                }

                AppState.EnableNavigationArrows();
            }
            catch { }
        }

        /// <summary>
        /// Moves to the next location in the list if there is one.
        /// </summary>
        public static void MoveToNextLocation()
        {
            if (_currentLocationIndex < _locations.Count - 1)
            {
                _currentLocationIndex++;
                GotoLocation(_locations[_currentLocationIndex]);
                AppState.EnableNavigationArrows();
            }
        }

        /// <summary>
        /// Moves to the previous location if there is one.
        /// </summary>
        public static void MoveToPreviousLocation()
        {
            if (_currentLocationIndex > 0)
            {
                _currentLocationIndex--;
                GotoLocation(_locations[_currentLocationIndex]);
                AppState.EnableNavigationArrows();
            }
        }

        /// <summary>
        /// Creates a new location object and saves it to the list of locations.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void SaveNewLocation(Chapter chapter, GameData.ContentType contentType, int articleIndex = -1)
        {
            if (_isNavigationTrackingOn)
            {
                try
                {
                    if (chapter != null)
                    {
                        TabViewType tab = TabViewType.NONE;

                        string articleGuid = null;

                        switch (contentType)
                        {
                            case GameData.ContentType.INTRO:
                                tab = TabViewType.INTRO;
                                break;
                            case GameData.ContentType.STUDY_TREE:
                                tab = TabViewType.STUDY;
                                break;
                            case GameData.ContentType.MODEL_GAME:
                                tab = TabViewType.MODEL_GAME;
                                if (articleIndex >= 0)
                                {
                                    articleGuid = chapter.ModelGames[articleIndex].Guid;
                                }
                                break;
                            case GameData.ContentType.EXERCISE:
                                tab = TabViewType.EXERCISE;
                                if (articleIndex >= 0)
                                {
                                    articleGuid = chapter.Exercises[articleIndex].Guid;
                                }
                                break;
                        }

                        WorkbookLocation location = new WorkbookLocation(chapter.Guid, tab, articleGuid, articleIndex);
                        VerifyNewLocation(location);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Overloaded for convenience.
        /// </summary>
        /// <param name="tabType"></param>
        public static void SaveNewLocation(TabViewType tabType)
        {
            if (_isNavigationTrackingOn)
            {
                WorkbookLocation location = new WorkbookLocation(null, tabType, null, -1);
                VerifyNewLocation(location);
            }
        }

        /// <summary>
        /// Checks if the location needs to be added
        /// and if so appends it.
        /// </summary>
        /// <param name="location"></param>
        private static void VerifyNewLocation(WorkbookLocation location)
        {
            // if different than last location, append to the list
            WorkbookLocation lastLocation = null;

            if (_currentLocationIndex >= 0)
            {
                lastLocation = _locations[_currentLocationIndex];
            }

            if (lastLocation == null
                || lastLocation.ChapterGuid != location.ChapterGuid
                || lastLocation.ViewType != location.ViewType
                || lastLocation.ArticleGuid != location.ArticleGuid
                || lastLocation.ArticleIndex != location.ArticleIndex)
            {
                AppendLocation(location);
            }

            AppState.EnableNavigationArrows();
        }

        /// <summary>
        /// Adds a new location after the current location. 
        /// Removes all the later locations.
        /// </summary>
        /// <param name="location"></param>
        private static void AppendLocation(WorkbookLocation location)
        {
            RemoveForwardLocations();
            _locations.Add(location);
            _currentLocationIndex = _locations.Count - 1;
        }

        /// <summary>
        /// Go to the location encapsulated in the passed object.
        /// </summary>
        /// <param name="location"></param>
        private static void GotoLocation(WorkbookLocation location)
        {
            try
            {
                if (location.ViewType == TabViewType.CHAPTERS)
                {
                    AppState.MainWin.UiTabChapters.Focus();
                }
                else
                {
                    // if the tab type is a Tree holding tab identify the chapter by guid
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByGuid(location.ChapterGuid, out int chapterIndex);
                    if (chapter != null)
                    {
                        GameData.ContentType contentType = GameData.ContentType.NONE;
                        switch (location.ViewType)
                        {
                            case TabViewType.INTRO:
                                contentType = GameData.ContentType.INTRO;
                                GotoArticle(chapterIndex, contentType, -1, false);
                                break;
                            case TabViewType.STUDY:
                                contentType = GameData.ContentType.STUDY_TREE;
                                GotoArticle(chapterIndex, contentType, -1, false);
                                break;
                            case TabViewType.MODEL_GAME:
                                contentType = GameData.ContentType.MODEL_GAME;
                                GotoArticle(chapterIndex, contentType, location.ArticleIndex, false);
                                break;
                            case TabViewType.EXERCISE:
                                contentType = GameData.ContentType.EXERCISE;
                                GotoArticle(chapterIndex, contentType, location.ArticleIndex, false);
                                break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Removes all locations after the current one
        /// </summary>
        private static void RemoveForwardLocations()
        {
            if (_currentLocationIndex < _locations.Count - 1)
            {
                _locations.RemoveRange(_currentLocationIndex + 1, (_locations.Count - 1) - _currentLocationIndex);
            }
        }
    }
}
