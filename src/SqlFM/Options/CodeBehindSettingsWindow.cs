using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SqlFM.Core.Configuration;
using SqlFM.Core.Engine;
using SqlFM.Core.PresetStyles;
using SqlFM.Services;

namespace SqlFM.Options
{
    /// <summary>
    /// Pure code-behind settings window — no XAML parsing required.
    /// Works around SSMS 22 + SQL Server 2008 R2 mixed-environment issues
    /// where WPF BAML loader fails with BindToMethod/CreateInstanceWithCtorType.
    ///
    /// This is used as the primary settings window to guarantee it opens
    /// on all machines regardless of SQL Server version coexistence.
    /// </summary>
    public class CodeBehindSettingsWindow : Window
    {
        private SqlFormatStyle _style;
        private TextBox _previewBox;
        private ComboBox _styleCombo;
        private ComboBox _keywordCaseCombo;
        private ComboBox _commaPosCombo;
        private ComboBox _bracketModeCombo;
        private TextBox _indentSizeTxt;
        private TextBox _maxLineWidthTxt;
        private CheckBox _trimTrailingChk;
        private CheckBox _removeBlankChk;
        private CheckBox _joinNewLineChk;
        private CheckBox _alignClauseChk;
        private CheckBox _formatOnSaveChk;

        public CodeBehindSettingsWindow(SqlFormatStyle style)
        {
            _style = style.Clone();
            Title = "SqlFM Format Options";
            Width = 780;
            Height = 680;
            MinWidth = 600;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            Background = SystemColors.ControlBrush;

            BuildUI();
            LoadStyleToUI();
        }

        // ── UI Construction (pure code, zero XAML) ────────────────────

        private void BuildUI()
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180, GridUnitType.Pixel) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Toolbar
            var toolbar = BuildToolbar();
            Grid.SetRow(toolbar, 0);
            rootGrid.Children.Add(toolbar);

            // Row 1: Main scrollable settings
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(8, 8, 8, 0)
            };
            scrollViewer.Content = BuildMainPanel();
            Grid.SetRow(scrollViewer, 1);
            rootGrid.Children.Add(scrollViewer);

            // Row 2: Preview
            var previewPanel = BuildPreviewPanel();
            Grid.SetRow(previewPanel, 2);
            rootGrid.Children.Add(previewPanel);

            // Row 3: Buttons
            var buttonBar = BuildButtonBar();
            Grid.SetRow(buttonBar, 3);
            rootGrid.Children.Add(buttonBar);

            Content = rootGrid;
        }

        private Border BuildToolbar()
        {
            var inner = new DockPanel { LastChildFill = true };

            var styleLabel = MakeLabel("Preset Style:");
            _styleCombo = new ComboBox { Width = 160, Margin = new Thickness(6, 0, 12, 0) };
            _styleCombo.Items.Add("Default");
            _styleCombo.Items.Add("CommasBefore");
            _styleCombo.Items.Add("RightAlign");
            _styleCombo.Items.Add("CompactIndented");
            _styleCombo.Items.Add("SingleLineCompact");
            _styleCombo.SelectionChanged += (s, e) => OnStyleChanged();

            var leftPanel = new StackPanel { Orientation = Orientation.Horizontal };
            leftPanel.Children.Add(styleLabel);
            leftPanel.Children.Add(_styleCombo);

            DockPanel.SetDock(leftPanel, Dock.Left);
            inner.Children.Add(leftPanel);

            return new Border
            {
                Background = SystemColors.ControlLightBrush,
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 6, 10, 6),
                Child = inner
            };
        }

        private StackPanel BuildMainPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };

            panel.Children.Add(MakeHeader("Core Settings"));

            var coreGroup = new Border
            {
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 8, 12, 8),
                Background = SystemColors.WindowBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var coreInner = new StackPanel();

            coreInner.Children.Add(MakeRow("Indent Size:", _indentSizeTxt = MakeTextBox(60)));
            coreInner.Children.Add(MakeRow("Max Line Width:", _maxLineWidthTxt = MakeTextBox(60)));
            coreInner.Children.Add(MakeRow("Keyword Case:", _keywordCaseCombo = MakeEnumCombo<KeywordCase>()));
            coreInner.Children.Add(MakeRow("Comma Position:", _commaPosCombo = MakeEnumCombo<CommaPosition>()));
            coreInner.Children.Add(MakeRow("Bracket Mode:", _bracketModeCombo = MakeEnumCombo<BracketMode>()));

            _trimTrailingChk = MakeCheckBox("Trim trailing spaces");
            _removeBlankChk = MakeCheckBox("Remove extra blank lines");
            _joinNewLineChk = MakeCheckBox("JOIN keyword on new line");
            _alignClauseChk = MakeCheckBox("Align clause keywords (SELECT/FROM/WHERE)");

            coreInner.Children.Add(_trimTrailingChk);
            coreInner.Children.Add(_removeBlankChk);
            coreInner.Children.Add(_joinNewLineChk);
            coreInner.Children.Add(_alignClauseChk);

            coreGroup.Child = coreInner;
            panel.Children.Add(coreGroup);

            return panel;
        }

        private Grid BuildPreviewPanel()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerBorder = new Border
            {
                Background = SystemColors.ControlDarkBrush,
                Padding = new Thickness(8, 4, 8, 4)
            };
            headerBorder.Child = new TextBlock { Text = "SQL Preview", FontWeight = FontWeights.SemiBold };
            Grid.SetRow(headerBorder, 0);
            grid.Children.Add(headerBorder);

            _previewBox = new TextBox
            {
                IsReadOnly = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                FontSize = 12,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = SystemColors.WindowBrush,
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 8, 8, 8),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(4, 4, 4, 0)
            };
            Grid.SetRow(_previewBox, 1);
            grid.Children.Add(_previewBox);

            return grid;
        }

        private Border BuildButtonBar()
        {
            var inner = new DockPanel { LastChildFill = true };

            _formatOnSaveChk = new CheckBox
            {
                Content = "Format on save",
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnOk = MakeButton("OK", 80, (s, e) => { ApplyChanges(); DialogResult = true; Close(); });
            btnOk.IsDefault = true;

            var btnCancel = MakeButton("Cancel", 80, (s, e) => { DialogResult = false; Close(); });
            btnCancel.IsCancel = true;

            var btnApply = MakeButton("Apply", 80, (s, e) => ApplyChanges());

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnApply);

            DockPanel.SetDock(_formatOnSaveChk, Dock.Left);
            DockPanel.SetDock(btnPanel, Dock.Right);
            inner.Children.Add(_formatOnSaveChk);
            inner.Children.Add(btnPanel);

            return new Border
            {
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(10, 8, 10, 8),
                Child = inner
            };
        }

        // ── Data Binding Helpers ───────────────────────────────────────

        private void LoadStyleToUI()
        {
            _styleCombo.SelectedItem = _style.Name ?? "Default";
            _indentSizeTxt.Text = _style.Global.IndentSize.ToString();
            _maxLineWidthTxt.Text = _style.Global.MaxLineWidth.ToString();
            _keywordCaseCombo.SelectedItem = _style.Global.KeywordCase;
            _commaPosCombo.SelectedItem = _style.Dml.CommaPosition;
            _bracketModeCombo.SelectedItem = _style.Global.SquareBracketMode;

            _trimTrailingChk.IsChecked = _style.Global.TrimTrailingSpaces;
            _removeBlankChk.IsChecked = _style.Global.RemoveExtraBlankLines;
            _joinNewLineChk.IsChecked = _style.Dml.JoinKeywordNewLine;
            _alignClauseChk.IsChecked = _style.Dml.AlignClauseKeyword;

            _formatOnSaveChk.IsChecked = StyleManager.LoadFormatOnSave();

            UpdatePreview();
        }

        private void ApplyChanges()
        {
            if (int.TryParse(_indentSizeTxt.Text, out var indent))
                _style.Global.IndentSize = indent;
            if (int.TryParse(_maxLineWidthTxt.Text, out var lineW))
                _style.Global.MaxLineWidth = lineW;

            _style.Global.KeywordCase = (KeywordCase?)_keywordCaseCombo.SelectedItem ?? KeywordCase.Upper;
            _style.Dml.CommaPosition = (CommaPosition?)_commaPosCombo.SelectedItem ?? CommaPosition.After;
            _style.Global.SquareBracketMode = (BracketMode?)_bracketModeCombo.SelectedItem ?? BracketMode.Keep;

            _style.Global.TrimTrailingSpaces = _trimTrailingChk.IsChecked == true;
            _style.Global.RemoveExtraBlankLines = _removeBlankChk.IsChecked == true;
            _style.Dml.JoinKeywordNewLine = _joinNewLineChk.IsChecked == true;
            _style.Dml.AlignClauseKeyword = _alignClauseChk.IsChecked == true;

            StyleManager.SaveStyle(_style);
            StyleManager.SaveFormatOnSave(_formatOnSaveChk.IsChecked == true);

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                const string sampleSql =
                    "select t1.id, t1.name, t2.email from dbo.Users t1 " +
                    "inner join dbo.Contacts t2 on t1.id = t2.user_id " +
                    "where t1.is_active = 1 order by t1.name asc";

                var pipeline = new FormatterPipeline();
                pipeline.LoadStyle(_style);
                _previewBox.Text = pipeline.Format(sampleSql).FormattedSql;
            }
            catch (Exception ex)
            {
                _previewBox.Text = "[Preview error] " + ex.Message;
            }
        }

        private void OnStyleChanged()
        {
            var name = _styleCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(name)) return;

            SqlFormatStyle? preset = name switch
            {
                "Default" => PresetStyleFactory.CreateDefault(),
                "CommasBefore" => PresetStyleFactory.CreateCommasBefore(),
                "RightAlign" => PresetStyleFactory.CreateRightAlign(),
                "CompactIndented" => PresetStyleFactory.CreateCompactIndented(),
                "SingleLineCompact" => PresetStyleFactory.CreateSingleLineCompact(),
                _ => null
            };

            if (preset != null)
            {
                _style = preset;
                LoadStyleToUI();
            }
        }

        // ── Factory helpers for UI elements ───────────────────────────

        private static TextBlock MakeLabel(string text)
        {
            return new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Width = 150 };
        }

        private static TextBox MakeTextBox(double width)
        {
            return new TextBox { Width = width, Margin = new Thickness(0, 0, 8, 0) };
        }

        private static CheckBox MakeCheckBox(string content)
        {
            return new CheckBox
            {
                Content = content,
                Margin = new Thickness(0, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static Button MakeButton(string content, double width, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = content,
                Width = width,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(4, 2, 4, 2)
            };
            btn.Click += onClick;
            return btn;
        }

        private static TextBlock MakeHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        private static StackPanel MakeRow(string label, Control control)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };
            row.Children.Add(MakeLabel(label));
            row.Children.Add(control);
            return row;
        }

        private static ComboBox MakeEnumCombo<TEnum>() where TEnum : struct, Enum
        {
            var combo = new ComboBox { Width = 140, Margin = new Thickness(0, 0, 8, 0) };
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
                combo.Items.Add(value);
            return combo;
        }
    }
}
