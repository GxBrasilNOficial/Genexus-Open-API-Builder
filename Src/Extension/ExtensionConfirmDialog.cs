#nullable enable

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Confirmação Yes/No do Remover API gerada. A lista usa o mesmo texto da Output,
/// uma linha por objeto, sem quebrar nome; a pergunta e os botões ficam fora da rolagem.
/// </summary>
internal sealed class ExtensionConfirmDialog : Form
{
    private const int Pad = 12;
    private const int TargetTextWidth = 1040;

    private readonly IWin32Window? _owner;
    private readonly PictureBox _iconBox;
    private readonly Label _introLabel;
    private readonly Label _identityLabel;
    private readonly Label _notesLabel;
    private readonly Label _confirmLabel;
    private readonly Button _yesButton;
    private readonly Button _noButton;
    private readonly TextBox _bodyBox;
    private readonly TableLayoutPanel _header;
    private readonly TableLayoutPanel _footer;

    public ExtensionConfirmDialog(
        string caption,
        string intro,
        ApiPlanGeneratedApiRemovalPlan plan,
        string confirmQuestion,
        ExtensionTexts texts,
        IWin32Window? owner = null)
    {
        _owner = owner;
        Text = caption ?? string.Empty;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Font;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        BackColor = SystemColors.Control;

        var language = texts.Language;
        var identity = ExtensionOutputLocalization.Translate(
            $"Transaction: {plan.TransactionName}    API Object: {plan.ApiName}    Metadata File: {plan.MetadataFileName}",
            language);
        var lists = ExtensionOutputLocalization.Translate(plan.BuildConfirmationLists(), language);
        var notes = ExtensionOutputLocalization.Translate(BuildNotes(plan), language);

        _iconBox = new PictureBox
        {
            Image = SystemIcons.Warning.ToBitmap(),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Margin = new Padding(0, 0, Pad, 0),
        };

        _introLabel = CreateLabel(intro);
        _identityLabel = CreateLabel(identity);
        _notesLabel = CreateLabel(notes);
        _confirmLabel = CreateLabel(confirmQuestion);

        _bodyBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 9f),
            Text = lists,
            TabStop = false,
            HideSelection = true,
            BorderStyle = BorderStyle.FixedSingle,
        };

        _noButton = new Button
        {
            Text = texts.No,
            DialogResult = DialogResult.No,
            AutoSize = true,
            Padding = new Padding(12, 4, 12, 4),
        };
        _yesButton = new Button
        {
            Text = texts.Yes,
            DialogResult = DialogResult.Yes,
            AutoSize = true,
            Padding = new Padding(12, 4, 12, 4),
        };
        AcceptButton = _noButton;
        CancelButton = _noButton;

        _header = BuildHeader();
        _footer = BuildFooter();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(Pad),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(_header, 0, 0);
        root.Controls.Add(_bodyBox, 0, 1);
        root.Controls.Add(_footer, 0, 2);
        Controls.Add(root);

        Load += (_, _) => FitToCurrentWorkingArea(plan);
        Shown += (_, _) =>
        {
            FitToCurrentWorkingArea(plan);
            ClearBodySelection();
            _noButton.Focus();
        };
    }

    private TableLayoutPanel BuildHeader()
    {
        var text = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        text.Controls.Add(_introLabel, 0, 0);
        text.Controls.Add(_identityLabel, 0, 1);

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        header.Controls.Add(_iconBox, 0, 0);
        header.Controls.Add(text, 1, 0);
        return header;
    }

    private TableLayoutPanel BuildFooter()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0),
        };
        buttons.Controls.Add(_noButton);
        buttons.Controls.Add(_yesButton);

        var footer = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
        };
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.Controls.Add(_notesLabel, 0, 0);
        footer.Controls.Add(_confirmLabel, 0, 1);
        footer.Controls.Add(buttons, 0, 2);
        return footer;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text ?? string.Empty,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 0),
        };
    }

    private void ClearBodySelection()
    {
        _bodyBox.SelectionStart = 0;
        _bodyBox.SelectionLength = 0;
    }

    private void FitToCurrentWorkingArea(ApiPlanGeneratedApiRemovalPlan plan)
    {
        var working = GetTargetWorkingArea();
        var maxHeight = Math.Max(360, working.Height - 32);
        var maxWidth = Math.Max(640, working.Width - 32);
        var minimumHeight = Math.Min(420, maxHeight);
        var minimumWidth = Math.Min(720, maxWidth);
        MinimumSize = new Size(minimumWidth, minimumHeight);
        MaximumSize = new Size(maxWidth, maxHeight);

        var innerWidth = Math.Max(280, Math.Min(TargetTextWidth, maxWidth) - (Pad * 2) - SystemIcons.Warning.Width - Pad);
        _introLabel.MaximumSize = new Size(innerWidth, 0);
        _identityLabel.MaximumSize = new Size(innerWidth, 0);
        _notesLabel.MaximumSize = new Size(Math.Max(280, maxWidth - (Pad * 4)), 0);
        _confirmLabel.MaximumSize = new Size(Math.Max(280, maxWidth - (Pad * 4)), 0);

        var preferredWidth = MeasurePreferredWidth(plan, maxWidth, minimumWidth);
        var preferredHeight = MeasurePreferredHeight(preferredWidth);
        preferredHeight = Math.Min(Math.Max(preferredHeight, minimumHeight), maxHeight);
        Size = new Size(preferredWidth, preferredHeight);
        CenterInWorkingArea(working);
    }

    private int MeasurePreferredWidth(ApiPlanGeneratedApiRemovalPlan plan, int maxWidth, int minimumWidth)
    {
        var longest = plan.OwnSdtNames
            .Concat(plan.ProcedureNames)
            .Concat(plan.SharedSdtNamesPreserved)
            .DefaultIfEmpty(string.Empty)
            .Max(name => name.Length);
        var sample = "  - " + new string('W', Math.Max(24, longest));
        var lineWidth = TextRenderer.MeasureText(sample, _bodyBox.Font).Width + 48;
        var chrome = Width - ClientSize.Width;
        if (chrome < 16)
        {
            chrome = 24;
        }

        return Math.Min(Math.Max(lineWidth + chrome + (Pad * 2), minimumWidth), maxWidth);
    }

    private int MeasurePreferredHeight(int width)
    {
        var headerHeight = _header.GetPreferredSize(new Size(width, 0)).Height;
        var footerHeight = _footer.GetPreferredSize(new Size(width, 0)).Height;
        var lineHeight = Math.Max(16, TextRenderer.MeasureText("Ag", _bodyBox.Font).Height + 1);
        var lineCount = Math.Max(1, _bodyBox.Lines.Length);
        var bodyHeight = (lineCount * lineHeight) + 24;
        var chrome = Height - ClientSize.Height;
        if (chrome < 32)
        {
            chrome = 48;
        }

        return headerHeight + footerHeight + bodyHeight + chrome + (Pad * 2);
    }

    private Rectangle GetTargetWorkingArea()
    {
        if (_owner is not null && _owner.Handle != IntPtr.Zero)
        {
            return Screen.FromHandle(_owner.Handle).WorkingArea;
        }

        if (IsHandleCreated)
        {
            return Screen.FromHandle(Handle).WorkingArea;
        }

        var processMainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (processMainWindowHandle != IntPtr.Zero)
        {
            return Screen.FromHandle(processMainWindowHandle).WorkingArea;
        }

        return Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
    }

    private void CenterInWorkingArea(Rectangle working)
    {
        Location = new Point(
            working.Left + Math.Max(0, (working.Width - Width) / 2),
            working.Top + Math.Max(0, (working.Height - Height) / 2));
    }

    private static string BuildNotes(ApiPlanGeneratedApiRemovalPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.FolderName))
        {
            return "Business Component da Transaction: não será revertido.";
        }

        if (plan.FolderWasCreated)
        {
            return "Folder: " + plan.FolderName + " (criado pela extensão; apagar só se ficar vazio)"
                + Environment.NewLine
                + "Business Component da Transaction: não será revertido.";
        }

        return "Folder: " + plan.FolderName + " (reutilizado; nunca apagar)"
            + Environment.NewLine
            + "Business Component da Transaction: não será revertido.";
    }
}
