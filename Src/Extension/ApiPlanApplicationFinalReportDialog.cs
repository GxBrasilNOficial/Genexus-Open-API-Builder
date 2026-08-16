#nullable enable

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.UI.Framework.Services;
using GenexusOpenApiBuilder.Extension.Diagnostics;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// B081 — tela final de resultado pós-aplicação (Wizard / Sync / Remover).
/// </summary>
internal sealed class ApiPlanApplicationFinalReportDialog : Form
{
    private readonly ApiPlanApplicationFinalReport _report;
    private readonly KBModel? _designModel;
    private readonly ExtensionTexts _texts;
    private readonly IWin32Window? _owner;
    private readonly TextBox _bodyBox = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font(FontFamily.GenericMonospace, 9f),
        WordWrap = true,
        TabStop = false,
        HideSelection = true,
    };

    public ApiPlanApplicationFinalReportDialog(ApiPlanApplicationFinalReport report, KBModel? designModel, ExtensionTexts texts, IWin32Window? owner = null)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _designModel = designModel;
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _owner = owner;
        Text = _texts.FinalReportTitle;
        // O owner e a janela ativa do GeneXus. O relatorio deve permanecer no
        // monitor da IDE, mesmo quando o cursor estiver em outro monitor.
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Font;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(560, 420);
        BuildLayout();
        SizeToReportContent();
        Shown += (_, _) =>
        {
            FitToCurrentWorkingArea();
            EnsureBodyScrollBars();
            ClearBodySelection();
        };
        Resize += (_, _) => EnsureBodyScrollBars();
    }

    private void ClearBodySelection()
    {
        _bodyBox.SelectionStart = 0;
        _bodyBox.SelectionLength = 0;
        if (AcceptButton is Control accept)
        {
            accept.Focus();
        }
    }

    private void EnsureBodyScrollBars()
    {
        if (!IsHandleCreated || _bodyBox.TextLength == 0 || _bodyBox.ClientSize.Height <= 0)
        {
            return;
        }

        var lastCharPosition = _bodyBox.GetPositionFromCharIndex(_bodyBox.TextLength - 1);
        var lineHeight = Math.Max(16, TextRenderer.MeasureText("Ag", _bodyBox.Font).Height + 1);
        var contentBottom = lastCharPosition.Y + lineHeight + 4;
        _bodyBox.ScrollBars = contentBottom > _bodyBox.ClientSize.Height
            ? ScrollBars.Vertical
            : ScrollBars.None;
    }

    private void SizeToReportContent()
    {
        var working = GetTargetWorkingArea();
        var lineCount = Math.Max(1, _bodyBox.Lines.Length);
        var lineHeight = Math.Max(16, TextRenderer.MeasureText("Ag", _bodyBox.Font).Height + 1);
        var estimatedBodyHeight = (lineCount * lineHeight) + 40;
        var chromeHeight = 150;
        // Amplia a area de leitura, sem ultrapassar os limites da area util abaixo.
        var preferredHeight = (int)Math.Ceiling((estimatedBodyHeight + chromeHeight) * 1.10);
        var baseWidth = lineCount <= 18 ? 760 : 920;
        var preferredWidth = (int)Math.Ceiling(baseWidth * 1.20);

        var maxHeight = Math.Max(260, working.Height - 32);
        var maxWidth = Math.Max(360, working.Width - 32);
        var minimumHeight = Math.Min(420, maxHeight);
        var minimumWidth = Math.Min(560, maxWidth);
        MinimumSize = new Size(minimumWidth, minimumHeight);
        MaximumSize = new Size(maxWidth, maxHeight);

        preferredHeight = Math.Min(Math.Max(preferredHeight, minimumHeight), maxHeight);
        preferredWidth = Math.Min(Math.Max(preferredWidth, minimumWidth), maxWidth);

        Size = new Size(preferredWidth, preferredHeight);
        CenterInWorkingArea(working);

        var bodyAvailable = Math.Max(120, Height - chromeHeight);
        _bodyBox.ScrollBars = estimatedBodyHeight > bodyAvailable + 8
            ? ScrollBars.Vertical
            : ScrollBars.None;
    }

    private void FitToCurrentWorkingArea()
    {
        var working = GetTargetWorkingArea();
        var maxHeight = Math.Max(260, working.Height - 32);
        var maxWidth = Math.Max(360, working.Width - 32);
        var minimumHeight = Math.Min(420, maxHeight);
        var minimumWidth = Math.Min(560, maxWidth);

        MinimumSize = new Size(minimumWidth, minimumHeight);
        MaximumSize = new Size(maxWidth, maxHeight);
        Size = new Size(
            Math.Min(Math.Max(Width, minimumWidth), maxWidth),
            Math.Min(Math.Max(Height, minimumHeight), maxHeight));
        CenterInWorkingArea(working);
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

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var headline = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Text = ExtensionOutputLocalization.Translate(_report.Headline, _texts.Language),
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(headline, 0, 0);

        _bodyBox.Text = _report.BuildReadableBody(
            includeHeadline: false,
            localize: message => ExtensionOutputLocalization.Translate(message, _texts.Language));
        root.Controls.Add(_bodyBox, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
        };

        var closeButton = new Button
        {
            Text = _texts.Close,
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Padding = new Padding(12, 4, 12, 4),
        };
        AcceptButton = closeButton;
        CancelButton = closeButton;
        buttons.Controls.Add(closeButton);

        if (CanOpenMainObject())
        {
            var openButton = new Button
            {
                Text = _texts.OpenMainObject,
                AutoSize = true,
                Padding = new Padding(12, 4, 12, 4),
            };
            openButton.Click += (_, _) => TryOpenMainObject();
            buttons.Controls.Add(openButton);
        }

        root.Controls.Add(buttons, 0, 2);
        ClearBodySelection();
    }

    private bool CanOpenMainObject()
    {
        return _designModel is not null
            && _report.MainObjectGuid.HasValue
            && !string.IsNullOrWhiteSpace(_report.MainObjectName)
            && UIServices.IsDocumentManagerAvailable
            && !string.Equals(_report.Operation, "Remover", StringComparison.OrdinalIgnoreCase);
    }

    private void TryOpenMainObject()
    {
        if (!CanOpenMainObject() || _designModel is null || !_report.MainObjectGuid.HasValue)
        {
            return;
        }

        try
        {
            var kbObject = _designModel.Objects.Get(_report.MainObjectGuid.Value);
            if (kbObject is null)
            {
                MessageBox.Show(
                    this,
                    string.Format(
                        _texts.Translate("Objeto principal '{0}' não foi encontrado na KB."),
                        _report.MainObjectName),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            UIServices.DocumentManager.OpenDocument(kbObject, new OpenDocumentOptions { ActivateWindow = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                string.Format(
                    _texts.Translate("Não foi possível abrir o objeto principal: {0}"),
                    ex.Message),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
