#nullable enable

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// B082 — casca visível enquanto Apply/Remover/Sync/abertura rodam no thread da UI.
/// Abortar é cooperativo (entre objetos). Preferível a fechar o GeneXus na marra.
/// Não é TopMost: o usuário pode ir a outro aplicativo; o quadro só sobe sobre o GeneXus quando ele está em primeiro plano.
/// </summary>
internal sealed class ExtensionBusyProgressDialog : Form
{
    private const int DialogWidth = 832;
    private const int DialogHeight = 420;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int SwpNoActivate = 0x0010;

    private readonly Label _stageLabel;
    private readonly Label _itemLabel;
    private readonly Label _timingLabel;
    private readonly Label _hintLabel;
    private readonly ProgressBar _bar;
    private readonly Button _abortButton;
    private readonly ApiPlanBusyProgressSession _session;
    private readonly ExtensionTexts _texts;
    private readonly string _lastItemMsFormat;
    private bool _abortArmed;

    public ExtensionBusyProgressDialog(string title, ExtensionTexts texts, ApiPlanBusyProgressSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _lastItemMsFormat = texts.BusyProgressLastItemMs;

        Text = title ?? "Genexus Open API Builder";
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ControlBox = false;
        ShowInTaskbar = true;
        ShowIcon = false;
        TopMost = false;
        ClientSize = new Size(DialogWidth, DialogHeight);
        MinimumSize = new Size(DialogWidth, DialogHeight);
        Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        BackColor = SystemColors.Control;

        _stageLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = texts.BusyProgressStarting,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _itemLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = string.Empty,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _timingLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = string.Empty,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
        };

        _bar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
            Height = 28,
        };

        _hintLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = texts.BusyProgressAbortHint,
            TextAlign = ContentAlignment.TopLeft,
        };

        _abortButton = new Button
        {
            Text = texts.BusyProgressAbort,
            AutoSize = true,
            MinimumSize = new Size(110, 32),
            Padding = new Padding(16, 6, 16, 6),
            Margin = new Padding(0, 4, 0, 8),
            Anchor = AnchorStyles.Right,
        };
        _abortButton.Click += (_, _) => OnAbortClicked(texts);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 8, 8),
        };
        footer.Controls.Add(_abortButton);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // stage
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));  // item
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));  // timing
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // bar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // hint
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));  // abort

        root.Controls.Add(_stageLabel, 0, 0);
        root.Controls.Add(_itemLabel, 0, 1);
        root.Controls.Add(_timingLabel, 0, 2);
        root.Controls.Add(_bar, 0, 3);
        root.Controls.Add(_hintLabel, 0, 4);
        root.Controls.Add(footer, 0, 5);

        // Dock.Fill ignora Form.Padding; o painel garante margem em volta, inclusive abaixo do Abortar.
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 16, 16, 16),
        };
        host.Controls.Add(root);
        Controls.Add(host);
    }

    public ApiPlanBusyProgressSession Session => _session;

    public void ApplyUpdate(ApiPlanBusyProgressUpdate update)
    {
        if (IsDisposed)
        {
            return;
        }

        var stageText = _texts.BusyProgressStageLabel(update.Stage);
        var itemText = _texts.BusyProgressItemLabel(update.ItemName);
        _stageLabel.Text = string.IsNullOrWhiteSpace(stageText)
            ? string.Empty
            : stageText;

        if (update.Total > 0)
        {
            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Maximum = Math.Max(update.Total, 1);
            _bar.Value = Math.Max(0, Math.Min(update.Current, _bar.Maximum));
            _itemLabel.Text = $"{update.Current}/{update.Total}  {itemText}";
        }
        else
        {
            _bar.Style = ProgressBarStyle.Marquee;
            _itemLabel.Text = itemText;
        }

        _timingLabel.Text = update.ElapsedMs >= 0
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, _lastItemMsFormat, update.ElapsedMs)
            : string.Empty;

        if (_abortArmed)
        {
            _abortButton.Enabled = false;
        }

        Refresh();
        KeepAboveGeneXusWhenGeneXusIsForeground();
    }

    private static bool IsCurrentProcessForeground()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        return processId == (uint)Process.GetCurrentProcess().Id;
    }

    private void KeepAboveGeneXusWhenGeneXusIsForeground()
    {
        if (!IsHandleCreated || IsDisposed || !IsCurrentProcessForeground())
        {
            return;
        }

        SetWindowPos(Handle, new IntPtr(0), 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private void OnAbortClicked(ExtensionTexts texts)
    {
        if (_abortArmed)
        {
            return;
        }

        _abortArmed = true;
        _session.RequestAbort();
        _abortButton.Enabled = false;
        _hintLabel.Text = texts.BusyProgressAbortRequested;
        Refresh();
        Application.DoEvents();
    }
}
