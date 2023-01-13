﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ChessForge
{
    /// <summary>
    /// A chapter within a Workbook.
    /// A chapter comprises one or more VariationTrees. 
    /// </summary>
    public class Chapter
    {
        /// <summary>
        /// The Study Tree of the chapter. There is exactly one
        /// Study Tree in a chapter.
        /// </summary>
        public Article StudyTree = new Article(GameData.ContentType.STUDY_TREE);

        /// <summary>
        /// The list of Model Games Trees
        /// </summary>
        public List<Article> ModelGames = new List<Article>();

        /// <summary>
        /// The list of Exercises Tress.
        /// </summary>
        public List<Article> Exercises = new List<Article>();

        // number of this chapter
        private int _id;

        // title of this chapter
        private string _title;

        // VariationTree to be used when this chapter becomes active.
        private VariationTree _activeTree;

        // Article to be used when this chapter becomes active.
        private Article _activeArticle;

        // index of the currently shown game in the Model Games list
        private int _activeModelGameIndex = -1;

        // index of the currently shown exercise in the Exercises list
        private int _activeExerciseIndex = -1;

        // whether the chapter is expanded in the ChaptersView
        private bool _isViewExpanded = true;

        // whether the Model Games list is expanded in the ChaptersView
        private bool _isModelGamesListExpanded;

        // whether the Exercises list is expanded in the ChaptersView
        private bool _isExercisesListExpanded;

        // associated OperationsManager
        private WorkbookOperationsManager _opsManager;

        /// <summary>
        /// Creates the object. Initializes Operations Manager
        /// </summary>
        public Chapter()
        {
            _opsManager = new WorkbookOperationsManager(this);
        }

        /// <summary>
        /// Returns Model Game stored at a given index.
        /// Null if invalid index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetModelGameAtIndex(int index)
        {
            if (index >= 0 && index < ModelGames.Count)
            {
                return ModelGames[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns Exercise stored at a given index.
        /// Null if invalid index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetExerciseAtIndex(int index)
        {
            if (index >= 0 && index < Exercises.Count)
            {
                return Exercises[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        // Index of the currently shown Game in the Model Games list
        /// </summary>
        public int ActiveModelGameIndex
        {
            get
            {
                if (_activeModelGameIndex < 0 && ModelGames.Count > 0)
                {
                    _activeModelGameIndex = 0;
                }
                return _activeModelGameIndex;
            }
            set => _activeModelGameIndex = value;
        }


        /// <summary>
        // Index of the currently shown Exercise in the Exercises list
        /// </summary>
        public int ActiveExerciseIndex
        {
            get
            {
                if (_activeExerciseIndex < 0 && Exercises.Count > 0)
                {
                    _activeExerciseIndex = 0;
                }
                return _activeExerciseIndex;
            }
            set => _activeExerciseIndex = value;
        }


        /// <summary>
        /// Returns Tree "active" in this chapter.
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (_activeTree != null && _activeTree.IsAssociatedTreeActive && _activeTree.AssociatedSecondary != null)
                {

                    return _activeTree.AssociatedSecondary;
                }
                else
                {
                    return _activeTree;
                }
            }
        }

        /// <summary>
        /// Returns Article "active" in this chapter.
        /// </summary>
        public Article ActiveArticle
        {
            get
            {
                return _activeArticle;
            }
        }

        /// <summary>
        /// Returns reference to the Active Game's header.
        /// </summary>
        /// <returns></returns>
        public GameHeader GetActiveModelGameHeader()
        {
            GameHeader gameHeader = null;
            try
            {
                if (ModelGames.Count > 0)
                    gameHeader = ModelGames[ActiveModelGameIndex].Tree.Header;
            }
            catch (Exception ex)
            {
                AppLog.Message("GetActiveModelGameHeader()", ex);
            }

            return gameHeader;
        }

        /// <summary>
        /// Sets the ActiveVariationTree based on the passed type and index.
        /// </summary>
        /// <param name="gameType"></param>
        /// <param name="gameIndex"></param>
        public void SetActiveVariationTree(GameData.ContentType gameType, int gameIndex = 0)
        {
            switch (gameType)
            {
                case GameData.ContentType.STUDY_TREE:
                    _activeTree = StudyTree.Tree;
                    _activeArticle = StudyTree;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    if (gameIndex >= 0 && gameIndex < ModelGames.Count)
                    {
                        _activeTree = ModelGames[gameIndex].Tree;
                        _activeArticle = ModelGames[gameIndex];
                    }
                    break;
                case GameData.ContentType.EXERCISE:
                    if (gameIndex >= 0 && gameIndex < Exercises.Count)
                    {
                        _activeTree = Exercises[gameIndex].Tree;
                        _activeArticle = Exercises[gameIndex];
                    }
                    break;
                default:
                    _activeTree = null;
                    _activeArticle = null;
                    break;
            }
        }

        /// <summary>
        /// Number of this chapter.
        /// </summary>
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// Unadorned chapter title
        /// </summary>
        public string Title
        {
            get => _title ?? "";
        }

        /// <summary>
        /// The Title of this chapter.
        /// If raw is set to false and the title is empty
        /// it returns the default title.
        /// </summary>
        public string GetTitle(bool raw = false)
        {
            if (raw || !string.IsNullOrWhiteSpace(_title))
            {
                return _title ?? "";
            }
            else
            {
                return "Chapter " + Id.ToString();
            }
        }

        /// <summary>
        /// Sets the title of the Chapter.
        /// </summary>
        /// <param name="title"></param>
        public void SetTitle(string title)
        {
            _title = title;
        }

        /// <summary>
        /// Returns the numer of model games in this chapter
        /// </summary>
        /// <returns></returns>
        public int GetModelGameCount()
        {
            return ModelGames.Count();
        }

        /// <summary>
        /// Returns the numer of exercises in this chapter
        /// </summary>
        /// <returns></returns>
        public int GetExerciseCount()
        {
            return Exercises.Count();
        }

        /// <summary>
        /// Flag indictating whether this chapter is expanded in the ChaptersView
        /// </summary>
        public bool IsViewExpanded
        {
            get => _isViewExpanded;
            set => _isViewExpanded = value;
        }

        /// <summary>
        /// Flag indictating whether the Model Games list is expanded in the ChaptersView
        /// </summary>
        public bool IsModelGamesListExpanded
        {
            get => _isModelGamesListExpanded;
            set => _isModelGamesListExpanded = value;
        }

        /// <summary>
        /// Flag indictating whether the Model Games list is expanded in the ChaptersView
        /// </summary>
        public bool IsExercisesListExpanded
        {
            get => _isExercisesListExpanded;
            set => _isExercisesListExpanded = value;
        }

        /// <summary>
        /// Returns true if the chapter has at least one Model Game
        /// </summary>
        public bool HasAnyModelGame
        {
            get
            {
                return ModelGames.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if the chapter has at least one Exercise
        /// </summary>
        public bool HasAnyExercise
        {
            get
            {
                return Exercises.Count > 0;
            }
        }

        /// <summary>
        /// Returns the color of the side to move first in the exercise.
        /// </summary>
        /// <param name="exerciseIndex"></param>
        /// <returns></returns>
        public PieceColor GetSideToSolveExercise(int? exerciseIndex = null)
        {
            int index;

            if (exerciseIndex == null)
            {
                index = _activeExerciseIndex;
            }
            else
            {
                index = exerciseIndex.Value;
            }

            if (index >= 0 && index < Exercises.Count)
            {
                return Exercises[index].Tree.Nodes[0].ColorToMove;
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Adds a VariationTree to the list of Model Games
        /// </summary>
        /// <param name="game"></param>
        public void AddModelGame(VariationTree game)
        {
            Article article = new Article(game);
            ModelGames.Add(article);
        }

        /// <summary>
        /// Inserts Game Article at a requested index.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void InsertModelGame(Article article, int index)
        {
            ModelGames.Insert(index, article);   
        }

        /// <summary>
        /// Adds a VariationTree to the list of Exercises
        /// </summary>
        /// <param name="game"></param>
        public void AddExercise(VariationTree game)
        {
            Article article = new Article(game);
            Exercises.Add(article);
        }

        /// <summary>
        /// Inserts Exercise at a requested index.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void InsertExercise(Article article, int index)
        {
            Exercises.Insert(index, article);
        }

        /// <summary>
        /// Adds a new game to this chapter.
        /// The caller must handle exceptions.
        /// </summary>
        /// <param name="gm"></param>
        public int AddArticle(GameData gm, GameData.ContentType typ, GameData.ContentType targetcontentType = GameData.ContentType.GENERIC)
        {
            int index = -1;

            Article article = new Article(typ);
            PgnGameParser pp = new PgnGameParser(gm.GameText, article.Tree, gm.Header.GetFenString());
            article.Tree.Header = gm.Header.CloneMe(true);

            if (typ == GameData.ContentType.GENERIC)
            {
                typ = gm.GetContentType();
            }
            article.Tree.ContentType = typ;

            switch (typ)
            {
                case GameData.ContentType.STUDY_TREE:
                    StudyTree = article;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    if (targetcontentType == GameData.ContentType.GENERIC || targetcontentType == GameData.ContentType.MODEL_GAME)
                    {
                        ModelGames.Add(article);
                        index = ModelGames.Count - 1;
                    }
                    else
                    {
                        index = -1;
                    }
                    break;
                case GameData.ContentType.EXERCISE:
                    if (targetcontentType == GameData.ContentType.GENERIC || targetcontentType == GameData.ContentType.EXERCISE)
                    {
                        TreeUtils.RestartMoveNumbering(article.Tree);
                        Exercises.Add(article);
                        index = Exercises.Count - 1;
                    }
                    else
                    {
                        index = -1;
                    }
                    break;
            }

            return index;
        }
    }
}
