﻿using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using ChessPosition;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChessPosition.GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the Chapters view.
    /// The view is hosted in a RichTextBox.
    /// </summary>
    public class ChaptersView : RichTextBuilder
    {
        // Application's Main Window
        private MainWindow _mainWin;

        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_WORKBOOK_TITLE = "workbook_title";
        private static readonly string STYLE_CHAPTER_TITLE = "chapter_title";
        private static readonly string STYLE_SUBHEADER = "subheader";
        private static readonly string STYLE_MODEL_GAME = "model_game";
        private static readonly string STYLE_EXERCISE = "exercise";

        private const string SUBHEADER_INDENT        = "        ";
        private const string SUBHEADER_DOUBLE_INDENT = "            ";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_WORKBOOK_TITLE] = new RichTextPara(0, 10, 18, FontWeights.Bold, null, TextAlignment.Left),
            [STYLE_CHAPTER_TITLE] = new RichTextPara(10, 10, 16, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_SUBHEADER] = new RichTextPara(40, 10, 14, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_MODEL_GAME] = new RichTextPara(70, 5, 14, FontWeights.Normal, null, TextAlignment.Left),
            [STYLE_EXERCISE] = new RichTextPara(90, 5, 14, FontWeights.Normal, null, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, null, TextAlignment.Left),
        };

        /// <summary>
        /// Maps chapter Ids to Chapter objects
        /// </summary>
        private Dictionary<int, Paragraph> _dictChapterParas = new Dictionary<int, Paragraph>();

        /// <summary>
        /// Names and prefixes for the Runs.
        /// They will identify the element of the Workbook structure being shown.
        /// In the run names they will be followed by the element's index in their
        /// respective list.
        /// </summary>
        private readonly string _run_chapter_expand_char_ = "_run_chapter_expand_char_";
        private readonly string _run_model_games_expand_char_ = "_run_model_games_expand_char_";
        private readonly string _run_exercises_expand_char_ = "_run_exercises_expand_char_";

        private readonly string _run_chapter_title_ = "_run_chapter_title_";
        private readonly string _run_study_tree_ = "study_tree_";
        private readonly string _run_model_games_header_ = "_run_model_games_";
        private readonly string _run_model_game_ = "_run_model_game_";
        private readonly string _run_exercises_header_ = "_run_exercises_";
        private readonly string _run_exercise_ = "_run_exercise_";

        /// <summary>
        /// Names and prefixes for the Paragraphs.
        /// </summary>
        private readonly string _par_workbook_title_ = "par_workbook_title_";
        private readonly string _par_chapter_ = "par_chapter_";

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public ChaptersView(FlowDocument doc, MainWindow mainWin) : base(doc)
        {
            _mainWin = mainWin;
        }

        /// <summary>
        /// Builds a list of paragraphs to show the chapters in this Workbook and their content.
        /// </summary>
        public void BuildFlowDocumentForChaptersView()
        {
            Document.Blocks.Clear();
            Document.PageWidth = 2000; // prevent word wrap
            _dictChapterParas.Clear();

            AddNewParagraphToDoc(STYLE_WORKBOOK_TITLE, WorkbookManager.SessionWorkbook.Title);

            foreach (Chapter chapter in WorkbookManager.SessionWorkbook.Chapters)
            {
                Paragraph para = BuildChapterParagraph(chapter);
                Document.Blocks.Add(para);
                _dictChapterParas[chapter.Id] = para;
            }

            HighlightActiveChapter();
        }

        /// <summary>
        /// Highlights the title of the ActiveChapter.
        /// </summary>
        public void HighlightActiveChapter()
        {
            try
            {
                int activeChapterId = WorkbookManager.SessionWorkbook.ActiveChapter.Id;
                // iterate over the runs with chapter name and set the font for the active one to bold
                foreach (Block b in Document.Blocks)
                {
                    if (b is Paragraph)
                    {
                        foreach (Inline r in ((Paragraph)b).Inlines)
                        {
                            int chapterId = GetNodeIdFromRunName((r as Run).Name, _run_chapter_title_);
                            if (chapterId >= 0)
                            {
                                if (chapterId == activeChapterId)
                                {
                                    (r as Run).FontWeight = FontWeights.Bold;
                                }
                                else
                                {
                                    (r as Run).FontWeight = FontWeights.Normal;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Identifies Paragraph for the chapter and then
        /// calls BuildChapterParagraph.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public Paragraph RebuildChapterParagraph(Chapter chapter)
        {
            Paragraph para = null; ;

            foreach (Block b in Document.Blocks)
            {
                if (b is Paragraph)
                {
                    int id = TextUtils.GetIdFromPrefixedString((b as Paragraph).Name);
                    if (id == chapter.Id)
                    {
                        para = b as Paragraph;
                        break;
                    }
                }
            }

            return BuildChapterParagraph(chapter, para);
        }

        /// <summary>
        /// Builds a paragraph for a single chapter
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        private Paragraph BuildChapterParagraph(Chapter chapter, Paragraph para = null)
        {
            if (chapter == null)
            {
                return null;
            }

            try
            {
                if (para == null)
                {
                    para = AddNewParagraphToDoc(STYLE_CHAPTER_TITLE, "");
                    _dictChapterParas[chapter.Id] = para;
                }
                else
                {
                    para.Inlines.Clear();
                }

                para.Name = _par_chapter_ + chapter.Id.ToString();

                char expandCollapse = chapter.IsViewExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_CHAPTER_TITLE, expandCollapse.ToString() + " ");
                rExpandChar.Name = _run_chapter_expand_char_ + chapter.Id.ToString();
                rExpandChar.MouseDown += EventChapterExpandSymbolClicked;
                para.Inlines.Add(rExpandChar);

                Run rTitle = CreateRun(STYLE_CHAPTER_TITLE, chapter.Title);
                if (chapter.Id == WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                {
                    rTitle.FontWeight = FontWeights.Bold;
                }
                rTitle.Name = _run_chapter_title_ + chapter.Id.ToString();
                rTitle.MouseDown += EventChapterRunClicked;
                para.Inlines.Add(rTitle);

                if (chapter.IsViewExpanded)
                {
                    InsertStudyRun(para);
                    InsertModelGamesRuns(para, chapter);
                    InsertExercisesRun(para, chapter);
                }

                return para;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Inserts a Run representing the Study of the Chapter.
        /// There is always precisely one run in each chapter.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertStudyRun(Paragraph para)
        {
            para.Inlines.Add(new Run("\n"));
            Run r = CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT + "Study Tree");
            r.Name = _run_study_tree_;
            para.Inlines.Add(r);
            return r;
        }

        /// <summary>
        /// Inserts the Model Games subheader
        /// and model games titles
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertModelGamesRuns(Paragraph para, Chapter chapter)
        {
            para.Inlines.Add(new Run("\n"));
            para.Inlines.Add(CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT));
            InsertExpandCollapseSymbolRun(para, _run_model_games_expand_char_, chapter.IsModelGamesListExpanded, chapter.HasAnyModelGame);
            Run r = CreateRun(STYLE_SUBHEADER, "Model Games");
            r.Name = _run_model_games_header_;
            para.Inlines.Add(r);

            if (chapter.IsModelGamesListExpanded)
            {
                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    para.Inlines.Add(new Run("\n"));
                    Run rGame = CreateRun(STYLE_SUBHEADER, SUBHEADER_DOUBLE_INDENT + chapter.ModelGames[i].Header.BuildGameHeaderLine());
                    rGame.Name = _run_model_game_ + i.ToString();
                    rGame.MouseDown += EventModelGameRunClicked;
                    para.Inlines.Add(rGame);
                }
            }

            return r;
        }

        /// <summary>
        /// Inserts a Run with the Expand/Collapse symbol.
        /// If hasContent is false, no symbol will be inserted.
        /// </summary>
        /// <param name="isExpanded"></param>
        /// <param name="hasContent"></param>
        /// <returns></returns>
        private Run InsertExpandCollapseSymbolRun(Paragraph para, string prefix, bool isExpanded, bool hasContent)
        {
            if (hasContent)
            {
                char expandCollapse = isExpanded ? Constants.CharCollapse : Constants.CharExpand;
                Run rExpandChar = CreateRun(STYLE_SUBHEADER, expandCollapse.ToString() + " ");
                rExpandChar.Name = prefix + WorkbookManager.SessionWorkbook.ActiveChapter.Id;
                rExpandChar.MouseDown += EventModelGamesExpandSymbolClicked;
                para.Inlines.Add(rExpandChar);

                return rExpandChar;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Inserts the Model Games subheader
        /// and model games titles
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertExercisesRun(Paragraph para, Chapter chapter)
        {
            para.Inlines.Add(new Run("\n"));
            Run r = CreateRun(STYLE_SUBHEADER, SUBHEADER_INDENT + "Exercises");
            r.Name = _run_exercises_header_;
            para.Inlines.Add(r);
            return r;
        }

        /// <summary>
        /// Event handler invoked when a Chapter Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterRunClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = GetNodeIdFromRunName(r.Name, _run_chapter_title_);
                if (chapterId >= 0)
                {
                    WorkbookManager.LastClickedChapterId = chapterId;
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.SelectChapter(chapterId, true);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        WorkbookManager.EnableChaptersMenus(_mainWin._cmChapters, true);
                        _mainWin.SelectChapter(chapterId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventChapterRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler invoked when a Model Game Run was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGameRunClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                Run r = (Run)e.Source;
                int gameIndex = TextUtils.GetIdFromPrefixedString(r.Name);
                if (chapter != null && gameIndex >= 0 && gameIndex < chapter.ModelGames.Count)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.SelectModelGame(gameIndex);
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        //WorkbookManager.EnableModelGamesMenus(_mainWin._cmChapters, true);
                        //_mainWin.SelectModelGame(chapterId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventModelGameRunClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// An expand/collapse character on the Model Games list was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventModelGamesExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = TextUtils.GetIdFromPrefixedString(r.Name);
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                if (chapter.Id != WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                {
                    _mainWin.SelectChapter(chapterId, false);
                }
                chapter.IsModelGamesListExpanded = !chapter.IsModelGamesListExpanded; 
                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExpandSymbolClicked(): " + ex.Message);
            }
        }

        /// <summary>
        /// An expand/collapse character for the chapter was clicked.
        /// Establish which chapter this is for, check its expand/collapse status and flip it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventChapterExpandSymbolClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;
                int chapterId = GetNodeIdFromRunName(r.Name, _run_chapter_expand_char_);
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(chapterId);
                bool? isExpanded = chapter.IsViewExpanded;

                if (isExpanded == true)
                {
                    chapter.IsViewExpanded = false;
                }
                else if (isExpanded == false)
                {
                    chapter.IsViewExpanded = true;
                }

                BuildChapterParagraph(chapter, _dictChapterParas[chapter.Id]);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EventExpandSymbolClicked(): " + ex.Message);
            }
        }
    }
}
