#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.UI.Framework.Services;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// B081 — tela final de resultado pós-aplicação (Wizard / Sync / Remover).
/// </summary>
internal sealed class ApiPlanApplicationFinalReportDialog : Form
{
    private readonly ApiPlanApplicationFinalReport _report;
    private readonly KBModel? _designModel;
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

    public ApiPlanApplicationFinalReportDialog(ApiPlanApplicationFinalReport report, KBModel? designModel = null)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _designModel = designModel;
        Text = "Genexus Open API Builder - Relatorio final";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(560, 420);
        BuildLayout();
        SizeToReportContent();
        Shown += (_, _) => ClearBodySelection();
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

    private void SizeToReportContent()
    {
        var working = Screen.FromControl(this).WorkingArea;
        var lineCount = Math.Max(1, _bodyBox.Lines.Length);
        var lineHeight = Math.Max(16, TextRenderer.MeasureText("Ag", _bodyBox.Font).Height + 1);
        var estimatedBodyHeight = (lineCount * lineHeight) + 40;
        var chromeHeight = 150;
        var preferredHeight = estimatedBodyHeight + chromeHeight;
        var preferredWidth = lineCount <= 18 ? 760 : 920;

        var maxHeight = Math.Max(MinimumSize.Height, working.Height - 60);
        preferredHeight = Math.Min(Math.Max(preferredHeight, MinimumSize.Height), maxHeight);
        preferredWidth = Math.Min(Math.Max(preferredWidth, MinimumSize.Width), Math.Max(MinimumSize.Width, working.Width - 80));

        Width = preferredWidth;
        Height = preferredHeight;

        var bodyAvailable = Math.Max(120, Height - chromeHeight);
        _bodyBox.ScrollBars = estimatedBodyHeight > bodyAvailable + 8
            ? ScrollBars.Vertical
            : ScrollBars.None;
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
            Text = _report.Headline,
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(headline, 0, 0);

        _bodyBox.Text = _report.BuildReadableBody(includeHeadline: false);
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
            Text = "Fechar",
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
                Text = "Abrir objeto principal",
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
                    $"Objeto principal '{_report.MainObjectName}' nao foi encontrado na KB.",
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
                "Nao foi possivel abrir o objeto principal: " + ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
