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
        public static readonly Color AppBackground = Color.FromArgb(245, 243, 238);
        public static readonly Color Surface = Color.FromArgb(255, 254, 250);
        public static readonly Color SurfaceRaised = Color.FromArgb(251, 249, 244);
        public static readonly Color SurfaceStrong = Color.FromArgb(229, 236, 234);
        public static readonly Color SurfaceStrongAlt = Color.FromArgb(239, 243, 241);
        public static readonly Color Ink = Color.FromArgb(34, 44, 51);
        public static readonly Color InkMuted = Color.FromArgb(102, 113, 121);
        public static readonly Color Border = Color.FromArgb(212, 217, 213);
        public static readonly Color Accent = Color.FromArgb(17, 119, 112);
        public static readonly Color AccentHover = Color.FromArgb(24, 140, 132);
        public static readonly Color AccentSoft = Color.FromArgb(223, 239, 236);
        public static readonly Color Warning = Color.FromArgb(214, 153, 52);
        public static readonly Color WarningHover = Color.FromArgb(224, 164, 64);
        public static readonly Color Danger = Color.FromArgb(187, 82, 64);
        public static readonly Color DangerHover = Color.FromArgb(204, 98, 80);
        public static readonly Color Neutral = Color.FromArgb(88, 100, 110);
        public static readonly Color NeutralHover = Color.FromArgb(102, 114, 124);
        public static readonly Color MetricGlow = Color.FromArgb(205, 145, 34);
        public static readonly Color Success = Color.FromArgb(46, 145, 104);
        public static readonly Color SuccessSoft = Color.FromArgb(220, 240, 231);

        private static readonly Font BodyFont = new("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font EmphasisFont = new("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font CompactButtonFont = new("Microsoft YaHei UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font MenuFont = new("Microsoft YaHei UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font CompactMenuFont = new("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);

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
            button.Padding = compact ? new Padding(14, 6, 14, 6) : new Padding(16, 8, 16, 8);
            button.MinimumSize = new Size(0, compact ? 40 : 44);
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
            grid.BackgroundColor = SurfaceRaised;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 38);
            grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 32);
            grid.DefaultCellStyle.BackColor = SurfaceRaised;
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.SelectionBackColor = AccentSoft;
            grid.DefaultCellStyle.SelectionForeColor = Ink;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 243, 238);
            grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceStrong;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
            grid.ColumnHeadersDefaultCellStyle.Font = EmphasisFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.DefaultCellStyle.Font = BodyFont;
            }
        }

        public static void StyleMenuStrip(MenuStrip menuStrip, bool compact = false)
        {
            menuStrip.BackColor = compact ? SurfaceStrongAlt : SurfaceRaised;
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
            model.TitleColor = OxyColor.FromRgb(Ink.R, Ink.G, Ink.B);
            model.TextColor = OxyColor.FromRgb(InkMuted.R, InkMuted.G, InkMuted.B);
            model.PlotAreaBorderColor = OxyColor.FromRgb(Border.R, Border.G, Border.B);
            model.Background = OxyColor.FromRgb(SurfaceRaised.R, SurfaceRaised.G, SurfaceRaised.B);
            model.PlotAreaBackground = OxyColor.FromRgb(Surface.R, Surface.G, Surface.B);
            model.TitleFontSize = 15;

            foreach (var axis in model.Axes.OfType<Axis>())
            {
                axis.TitleColor = OxyColor.FromRgb(Ink.R, Ink.G, Ink.B);
                axis.TextColor = OxyColor.FromRgb(InkMuted.R, InkMuted.G, InkMuted.B);
                axis.TicklineColor = OxyColor.FromRgb(Border.R, Border.G, Border.B);
                axis.AxislineColor = OxyColor.FromRgb(Border.R, Border.G, Border.B);
                axis.ExtraGridlineColor = OxyColor.FromRgb(Border.R, Border.G, Border.B);
                axis.MajorGridlineColor = OxyColor.FromRgb(224, 218, 209);
                axis.MinorGridlineColor = OxyColor.FromRgb(236, 232, 225);
                axis.MinorGridlineStyle = LineStyle.Dot;
                axis.AxisTitleDistance = 12;
            }

            foreach (var series in model.Series.OfType<LineSeries>())
            {
                series.StrokeThickness = Math.Max(series.StrokeThickness, 2.4);
                series.LineJoin = LineJoin.Round;
            }
        }

        public static void StylePlotHost(Control plotHost)
        {
            plotHost.BackColor = SurfaceRaised;
            plotHost.Padding = new Padding(12);
        }

        public static void StyleCard(Panel panel)
        {
            panel.BackColor = SurfaceRaised;
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
                ? new Font("Consolas", 22F, FontStyle.Bold, GraphicsUnit.Point)
                : new Font("Consolas", 18F, FontStyle.Bold, GraphicsUnit.Point);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        }

        public static void StyleMetricTitle(Label titleLabel)
        {
            titleLabel.AutoSize = false;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.BorderStyle = BorderStyle.None;
            titleLabel.ForeColor = InkMuted;
            titleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        }

        public static Color GetBackColor(ButtonTone tone)
        {
            return tone switch
            {
                ButtonTone.Primary => Accent,
                ButtonTone.Secondary => Color.FromArgb(53, 96, 147),
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
                ButtonTone.Secondary => Color.FromArgb(67, 111, 162),
                ButtonTone.Warning => WarningHover,
                ButtonTone.Danger => DangerHover,
                _ => NeutralHover
            };
        }

        public static Color GetTextColor(ButtonTone tone)
        {
            return tone switch
            {
                ButtonTone.Warning => Ink,
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
                    groupBox.BackColor = SurfaceRaised;
                    groupBox.ForeColor = Ink;
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
                    textBox.BackColor = textBox.ReadOnly ? Color.FromArgb(240, 236, 229) : SurfaceRaised;
                    textBox.ForeColor = Ink;
                    textBox.Font = BodyFont;
                    return;
                case ComboBox comboBox:
                    comboBox.BackColor = SurfaceRaised;
                    comboBox.ForeColor = Ink;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.Font = BodyFont;
                    return;
                case DateTimePicker dateTimePicker:
                    dateTimePicker.CalendarMonthBackground = SurfaceRaised;
                    dateTimePicker.Font = BodyFont;
                    return;
                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = SurfaceRaised;
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

            public override Color MenuStripGradientBegin => _compact ? SurfaceStrongAlt : SurfaceRaised;
            public override Color MenuStripGradientEnd => _compact ? SurfaceStrongAlt : SurfaceRaised;
            public override Color ToolStripDropDownBackground => SurfaceRaised;
            public override Color ImageMarginGradientBegin => SurfaceRaised;
            public override Color ImageMarginGradientMiddle => SurfaceRaised;
            public override Color ImageMarginGradientEnd => SurfaceRaised;
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
