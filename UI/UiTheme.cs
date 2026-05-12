using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace ISO11820WinForms.UI
{
    internal enum ButtonTone
    {
        Primary,
        Secondary,
        Warning,
        Danger,
        Neutral
    }

    internal static class UiTheme
    {
        private const string UiFontFamily = "Microsoft YaHei";
        private const string NumberFontFamily = "Consolas";

        public static readonly Color AppBackground = Color.FromArgb(0xec, 0xee, 0xf1);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceRaised = Color.FromArgb(0xf7, 0xf9, 0xfc);
        public static readonly Color SurfaceStrong = Color.FromArgb(0xe1, 0xe7, 0xed);
        public static readonly Color SurfaceStrongAlt = Color.FromArgb(0xda, 0xe2, 0xe8);
        public static readonly Color Ink = Color.FromArgb(0x33, 0x44, 0x55);
        public static readonly Color TitleInk = Color.FromArgb(0x44, 0x55, 0x66);
        public static readonly Color InkMuted = Color.FromArgb(0x66, 0x77, 0x88);
        public static readonly Color Border = Color.FromArgb(0xc7, 0xd0, 0xda);
        public static readonly Color GridHeader = Color.FromArgb(0x1f, 0x2a, 0x44);
        public static readonly Color GridLine = Color.FromArgb(0xe2, 0xe6, 0xed);
        public static readonly Color GridAlternate = Color.FromArgb(0xf7, 0xf9, 0xfc);
        public static readonly Color GridSelection = Color.FromArgb(0xd6, 0xe4, 0xff);
        public static readonly Color Accent = Color.FromArgb(0x3a, 0x6b, 0x54);
        public static readonly Color AccentHover = Color.FromArgb(0x45, 0x7a, 0x61);
        public static readonly Color AccentSoft = Color.FromArgb(0xe2, 0xef, 0xe9);
        public static readonly Color Warning = Color.FromArgb(0x84, 0x63, 0x2f);
        public static readonly Color WarningHover = Color.FromArgb(0x95, 0x72, 0x3a);
        public static readonly Color Danger = Color.FromArgb(0xa0, 0x55, 0x41);
        public static readonly Color DangerHover = Color.FromArgb(0xb1, 0x64, 0x50);
        public static readonly Color Neutral = Color.FromArgb(0x6c, 0x78, 0x86);
        public static readonly Color NeutralHover = Color.FromArgb(0x7b, 0x87, 0x95);
        public static readonly Color MetricGlow = Color.FromArgb(0x9a, 0x73, 0x35);
        public static readonly Color Success = Color.FromArgb(0x2f, 0x86, 0x61);
        public static readonly Color SuccessSoft = Color.FromArgb(0xe2, 0xef, 0xe9);
        public static readonly Color VideoBackground = Color.FromArgb(0x1a, 0x1a, 0x1a);
        public static readonly Color VideoLabel = Color.FromArgb(0x7b, 0xff, 0x7b);
        public static readonly Color VideoMuted = Color.FromArgb(0x88, 0x88, 0x88);
        public static readonly Color VideoBorder = Color.FromArgb(0xac, 0xa8, 0x99);

        private static readonly Font BodyFont = new(UiFontFamily, 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font EmphasisFont = new(UiFontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font CompactButtonFont = new(UiFontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font MenuFont = new(UiFontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font CompactMenuFont = new(UiFontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font TableHeaderFont = new(UiFontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);

        public static void ApplyFormTheme(Form form, bool dialog = false)
        {
            form.BackColor = AppBackground;
            form.Font = BodyFont;
            form.ForeColor = Ink;
            if (dialog)
            {
                form.Padding = new Padding(1);
            }
        }

        public static void ApplyToControlTree(Control root)
        {
            foreach (Control child in root.Controls)
            {
                StyleControl(child);
                if (child.HasChildren)
                {
                    ApplyToControlTree(child);
                }
            }
        }

        public static void StyleButton(Button button, ButtonTone tone, bool compact = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = Shift(GetBackColor(tone), 10);
            button.FlatAppearance.MouseOverBackColor = GetHoverColor(tone);
            button.ForeColor = GetTextColor(tone);
            button.BackColor = GetBackColor(tone);
            button.Font = compact ? CompactButtonFont : EmphasisFont;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.AutoEllipsis = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = compact ? new Padding(10, 2, 10, 2) : new Padding(16, 8, 16, 8);

            var minimumHeight = compact ? 44 : 44;
            var minimumWidth = compact ? 118 : 96;
            var preferredTextWidth = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font).Width;
            var preferredWidth = Math.Max(
                minimumWidth,
                preferredTextWidth + button.Padding.Left + button.Padding.Right + 18);

            button.MinimumSize = new Size(preferredWidth, minimumHeight);
            if (button.Width < preferredWidth)
            {
                button.Width = preferredWidth;
            }
        }

        public static void StyleChipRadioButton(RadioButton radioButton, bool selected)
        {
            radioButton.Appearance = Appearance.Button;
            radioButton.AutoSize = false;
            radioButton.FlatStyle = FlatStyle.Flat;
            radioButton.UseVisualStyleBackColor = false;
            radioButton.FlatAppearance.BorderSize = 1;
            radioButton.FlatAppearance.BorderColor = Border;
            radioButton.Font = EmphasisFont;
            radioButton.TextAlign = ContentAlignment.MiddleCenter;
            radioButton.Padding = new Padding(10, 0, 10, 0);
            radioButton.BackColor = selected ? AccentSoft : SurfaceRaised;
            radioButton.ForeColor = selected ? Accent : InkMuted;
        }

        public static void StyleDataGridView(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = GridLine;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 34);
            grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 30);
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.SelectionBackColor = GridSelection;
            grid.DefaultCellStyle.SelectionForeColor = Ink;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Ink;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = GridSelection;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Ink;
            grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = TableHeaderFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.DefaultCellStyle.Font = BodyFont;
                column.DefaultCellStyle.BackColor = Surface;
                column.DefaultCellStyle.ForeColor = Ink;
                column.DefaultCellStyle.SelectionBackColor = GridSelection;
                column.DefaultCellStyle.SelectionForeColor = Ink;
                column.HeaderCell.Style.BackColor = GridHeader;
                column.HeaderCell.Style.ForeColor = Color.White;
                column.HeaderCell.Style.SelectionBackColor = GridHeader;
                column.HeaderCell.Style.SelectionForeColor = Color.White;
            }
        }

        public static void StyleMenuStrip(MenuStrip menuStrip, bool compact = false)
        {
            menuStrip.BackColor = compact ? SurfaceStrongAlt : AppBackground;
            menuStrip.ForeColor = Ink;
            menuStrip.RenderMode = ToolStripRenderMode.Professional;
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(compact));
            menuStrip.AutoSize = false;
            menuStrip.Height = Math.Max(menuStrip.Height, compact ? 42 : 48);
            menuStrip.Padding = compact ? new Padding(12, 6, 12, 6) : new Padding(14, 8, 14, 8);
            foreach (ToolStripItem item in menuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.ForeColor = Ink;
                    menuItem.Font = compact ? CompactMenuFont : MenuFont;
                    menuItem.Padding = compact ? new Padding(12, 6, 12, 6) : new Padding(14, 8, 14, 8);
                }
            }
        }

        public static void StylePlot(PlotModel model, string title)
        {
            model.Title = title;
            model.DefaultFont = UiFontFamily;
            model.DefaultFontSize = 10.5;
            model.TitleColor = OxyColor.FromRgb(TitleInk.R, TitleInk.G, TitleInk.B);
            model.TextColor = OxyColor.FromRgb(InkMuted.R, InkMuted.G, InkMuted.B);
            model.PlotAreaBorderColor = OxyColors.Transparent;
            model.PlotAreaBorderThickness = new OxyThickness(0);
            model.Background = OxyColors.Transparent;
            model.PlotAreaBackground = OxyColor.FromRgb(Surface.R, Surface.G, Surface.B);
            model.TitleFontSize = 12;
            model.PlotMargins = new OxyThickness(48, 12, 16, 34);
            model.Padding = new OxyThickness(2, 2, 2, 2);

            foreach (var axis in model.Axes.OfType<Axis>())
            {
                axis.TitleColor = OxyColor.FromRgb(TitleInk.R, TitleInk.G, TitleInk.B);
                axis.TextColor = OxyColor.FromRgb(InkMuted.R, InkMuted.G, InkMuted.B);
                axis.TicklineColor = OxyColor.FromRgb(0x88, 0x88, 0x88);
                axis.AxislineColor = OxyColor.FromRgb(0x99, 0x99, 0x99);
                axis.ExtraGridlineColor = OxyColor.FromRgb(GridLine.R, GridLine.G, GridLine.B);
                axis.MajorGridlineColor = OxyColor.FromRgb(GridLine.R, GridLine.G, GridLine.B);
                axis.MinorGridlineColor = OxyColor.FromRgb(0xf0, 0xf2, 0xf5);
                axis.MinorGridlineStyle = LineStyle.Dot;
                axis.AxisTitleDistance = 10;
                axis.FontSize = 10;
                axis.TitleFontSize = 10;
            }

            foreach (var series in model.Series.OfType<LineSeries>())
            {
                series.StrokeThickness = Math.Min(Math.Max(series.StrokeThickness, 1.8), 2.2);
                series.LineJoin = LineJoin.Round;
            }
        }

        public static void StylePlotHost(Control plotHost)
        {
            plotHost.BackColor = Surface;
            plotHost.Padding = new Padding(12);
        }

        public static void StyleVideoSurface(Control videoSurface)
        {
            videoSurface.BackColor = VideoBackground;
            videoSurface.ForeColor = VideoLabel;
            videoSurface.Font = new Font(NumberFontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
            videoSurface.Padding = new Padding(8);
        }

        public static void StyleCard(Panel panel)
        {
            panel.BackColor = Surface;
            panel.Padding = new Padding(14);
            panel.Margin = new Padding(0, 0, 0, 10);
        }

        public static void StyleMetricValue(Label valueLabel, Color accentColor, bool emphasize = false)
        {
            valueLabel.AutoSize = false;
            valueLabel.BackColor = Color.Transparent;
            valueLabel.BorderStyle = BorderStyle.None;
            valueLabel.ForeColor = accentColor;
            valueLabel.Font = emphasize
                ? new Font(NumberFontFamily, 22F, FontStyle.Bold, GraphicsUnit.Point)
                : new Font(NumberFontFamily, 18F, FontStyle.Bold, GraphicsUnit.Point);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        }

        public static void StyleMetricTitle(Label titleLabel)
        {
            titleLabel.AutoSize = false;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.BorderStyle = BorderStyle.None;
            titleLabel.ForeColor = InkMuted;
            titleLabel.Font = new Font(UiFontFamily, 9F, FontStyle.Bold, GraphicsUnit.Point);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        }

        public static Color GetBackColor(ButtonTone tone)
        {
            return tone switch
            {
                ButtonTone.Primary => Accent,
                ButtonTone.Secondary => Neutral,
                ButtonTone.Warning => Warning,
                ButtonTone.Danger => Danger,
                _ => Neutral
            };
        }

        public static Color GetHoverColor(ButtonTone tone)
        {
            return tone switch
            {
                ButtonTone.Primary => AccentHover,
                ButtonTone.Secondary => NeutralHover,
                ButtonTone.Warning => WarningHover,
                ButtonTone.Danger => DangerHover,
                _ => NeutralHover
            };
        }

        public static Color GetTextColor(ButtonTone tone)
        {
            return tone switch
            {
                ButtonTone.Secondary => Color.Black,
                ButtonTone.Neutral => Color.Black,
                _ => Color.White
            };
        }

        private static void StyleControl(Control control)
        {
            switch (control)
            {
                case MenuStrip menuStrip:
                    StyleMenuStrip(menuStrip);
                    return;
                case DataGridView dataGridView:
                    StyleDataGridView(dataGridView);
                    return;
                case GroupBox groupBox:
                    groupBox.BackColor = Surface;
                    groupBox.ForeColor = TitleInk;
                    groupBox.Font = EmphasisFont;
                    groupBox.Padding = new Padding(14, 18, 14, 14);
                    return;
                case Panel panel:
                    if (panel.Tag?.ToString() != "theme-skip")
                    {
                        panel.BackColor = panel.BackColor == Color.Transparent ? panel.BackColor : AppBackground;
                    }
                    return;
                case Button button:
                    if (button.Tag is ButtonTone tone)
                    {
                        StyleButton(button, tone);
                    }
                    else
                    {
                        StyleButton(button, ButtonTone.Neutral);
                    }
                    return;
                case TextBox textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = textBox.ReadOnly ? SurfaceRaised : Surface;
                    textBox.ForeColor = Ink;
                    textBox.Font = BodyFont;
                    return;
                case ComboBox comboBox:
                    comboBox.BackColor = Surface;
                    comboBox.ForeColor = Ink;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.Font = BodyFont;
                    return;
                case DateTimePicker dateTimePicker:
                    dateTimePicker.CalendarMonthBackground = Surface;
                    dateTimePicker.Font = BodyFont;
                    return;
                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = Surface;
                    numericUpDown.ForeColor = Ink;
                    numericUpDown.Font = BodyFont;
                    return;
                case CheckBox checkBox:
                    checkBox.ForeColor = Ink;
                    checkBox.Font = BodyFont;
                    return;
                case RadioButton radioButton:
                    radioButton.ForeColor = Ink;
                    radioButton.Font = BodyFont;
                    return;
                case TabControl tabControl:
                    tabControl.Appearance = TabAppearance.FlatButtons;
                    tabControl.BackColor = AppBackground;
                    foreach (TabPage page in tabControl.TabPages)
                    {
                        page.BackColor = AppBackground;
                    }
                    return;
                case Label label:
                    label.ForeColor = Ink;
                    if (label.Font.Bold)
                    {
                        label.Font = EmphasisFont;
                    }
                    else
                    {
                        label.Font = BodyFont;
                    }
                    return;
            }
        }

        private static Color Shift(Color color, int delta)
        {
            int Clamp(int channel) => Math.Max(0, Math.Min(255, channel));
            return Color.FromArgb(
                Clamp(color.R + delta),
                Clamp(color.G + delta),
                Clamp(color.B + delta));
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            private readonly bool _compact;

            public ThemeColorTable(bool compact)
            {
                _compact = compact;
            }

            public override Color MenuStripGradientBegin => _compact ? SurfaceStrongAlt : AppBackground;
            public override Color MenuStripGradientEnd => _compact ? SurfaceStrongAlt : AppBackground;
            public override Color ToolStripDropDownBackground => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color MenuItemSelected => Accent;
            public override Color MenuItemSelectedGradientBegin => Accent;
            public override Color MenuItemSelectedGradientEnd => Accent;
            public override Color MenuItemBorder => Accent;
            public override Color MenuItemPressedGradientBegin => AccentSoft;
            public override Color MenuItemPressedGradientMiddle => AccentSoft;
            public override Color MenuItemPressedGradientEnd => AccentSoft;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Border;
        }
    }
}
