#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Diálogo do comando de recuperação, no lugar do <c>MessageBox</c> nativo.
///
/// O motivo é largura: o `MessageBox` do Windows escolhe a própria, e o texto desta decisão —
/// o que aconteceu, o que está na KB, o que o encerramento faz e o que ele não faz — chegava
/// espremido numa coluna estreita, com a lista de alvos derretida dentro do parágrafo. Aqui a
/// mensagem tem largura de leitura, e o inventário fica num bloco próprio, monoespaçado e
/// rolável, uma linha por objeto.
///
/// O desenho é o mesmo do <see cref="ExtensionConfirmDialog"/> do Remover, de propósito: quem
/// já viu um reconhece o outro.
/// </summary>
internal sealed class ExtensionRecoveryDialog : Form
{
    private const int Pad = 12;

    private readonly IWin32Window? _owner;
    private readonly PictureBox _iconBox;
    private readonly Label _messageLabel;
    private readonly Label _questionLabel;
    private readonly TextBox _bodyBox;
    private readonly Button _primaryButton;
    private readonly Button? _secondaryButton;
    private readonly bool _hasDetails;

    private ExtensionRecoveryDialog(
        ExtensionTexts texts,
        string message,
        IReadOnlyList<string> details,
        string? question,
        bool warning,
        IWin32Window? owner)
    {
        _owner = owner;
        _hasDetails = details.Count > 0;
        Text = texts.RecoveryDialogTitle;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Font;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        BackColor = SystemColors.Control;

        _iconBox = new PictureBox
        {
            Image = (warning ? SystemIcons.Warning : SystemIcons.Information).ToBitmap(),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Margin = new Padding(0, 0, Pad, 0),
        };

        _messageLabel = CreateLabel(message);
        _questionLabel = CreateLabel(question ?? string.Empty);
        _questionLabel.Visible = !string.IsNullOrWhiteSpace(question);

        _bodyBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 9f),
            Text = string.Join(Environment.NewLine, details),
            TabStop = false,
            HideSelection = true,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = _hasDetails,
        };

        if (question is null)
        {
            _primaryButton = new Button
            {
                Text = texts.Close,
                DialogResult = DialogResult.OK,
                AutoSize = true,
                Padding = new Padding(12, 4, 12, 4),
            };
            AcceptButton = _primaryButton;
            CancelButton = _primaryButton;
        }
        else
        {
            // O botão seguro leva o foco e responde ao Enter e ao Esc: encerrar um registro é
            // decisão deliberada, nunca resultado de uma tecla apertada por hábito.
            _secondaryButton = new Button
            {
                Text = texts.No,
                DialogResult = DialogResult.No,
                AutoSize = true,
                Padding = new Padding(12, 4, 12, 4),
            };
            _primaryButton = new Button
            {
                Text = texts.Yes,
                DialogResult = DialogResult.Yes,
                AutoSize = true,
                Padding = new Padding(12, 4, 12, 4),
            };
            AcceptButton = _secondaryButton;
            CancelButton = _secondaryButton;
        }

        Controls.Add(BuildRoot());

        Load += (_, _) => FitToCurrentWorkingArea();
        Shown += (_, _) =>
        {
            FitToCurrentWorkingArea();
            _bodyBox.SelectionStart = 0;
            _bodyBox.SelectionLength = 0;
            (_secondaryButton ?? _primaryButton).Focus();
        };
    }

    /// <summary>Pergunta Sim/Não, com o inventário à vista. Devolve <c>true</c> para Sim.</summary>
    internal static bool Ask(
        IWin32Window? owner,
        ExtensionTexts texts,
        string message,
        IReadOnlyList<string> details,
        string question)
    {
        using var dialog = new ExtensionRecoveryDialog(texts, message, details, question, warning: true, owner);
        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == DialogResult.Yes;
    }

    /// <summary>Informa sem perguntar: diagnóstico de bloqueio ou desfecho da execução.</summary>
    internal static void Inform(
        IWin32Window? owner,
        ExtensionTexts texts,
        string message,
        IReadOnlyList<string> details,
        bool warning)
    {
        using var dialog = new ExtensionRecoveryDialog(texts, message, details, question: null, warning, owner);
        if (owner is null)
        {
            dialog.ShowDialog();
        }
        else
        {
            dialog.ShowDialog(owner);
        }
    }

    private TableLayoutPanel BuildRoot()
    {
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
        header.Controls.Add(_messageLabel, 1, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0),
        };
        if (_secondaryButton is not null)
        {
            buttons.Controls.Add(_secondaryButton);
        }

        buttons.Controls.Add(_primaryButton);

        var footer = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.Controls.Add(_questionLabel, 0, 0);
        footer.Controls.Add(buttons, 0, 1);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(Pad),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(_hasDetails ? SizeType.Percent : SizeType.AutoSize, _hasDetails ? 100f : 0f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_bodyBox, 0, 1);
        root.Controls.Add(footer, 0, 2);
        return root;
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

    /// <summary>
    /// Largura de leitura, limitada pela área útil do monitor em que a IDE está. O piso de 1080
    /// é o mesmo do diálogo do Remover: abaixo disso, a lista de objetos hierárquicos volta a
    /// quebrar no meio do nome.
    /// </summary>
    private void FitToCurrentWorkingArea()
    {
        var working = GetTargetWorkingArea();
        var maxWidth = Math.Max(640, working.Width - 32);
        var maxHeight = Math.Max(360, working.Height - 32);
        var preferredWidth = Math.Min(1080, maxWidth);
        MinimumSize = new Size(Math.Min(720, maxWidth), Math.Min(320, maxHeight));
        MaximumSize = new Size(maxWidth, maxHeight);

        var innerWidth = Math.Max(320, preferredWidth - (Pad * 4) - SystemIcons.Warning.Width - Pad);
        _messageLabel.MaximumSize = new Size(innerWidth, 0);
        _questionLabel.MaximumSize = new Size(Math.Max(320, preferredWidth - (Pad * 4)), 0);

        var height = _hasDetails
            ? Math.Min(maxHeight, Math.Max(480, PreferredSize.Height + MeasureDetailsHeight()))
            : Math.Min(maxHeight, Math.Max(240, PreferredSize.Height));
        Size = new Size(preferredWidth, height);
        Location = new Point(
            working.Left + Math.Max(0, (working.Width - Width) / 2),
            working.Top + Math.Max(0, (working.Height - Height) / 2));
    }

    private int MeasureDetailsHeight()
    {
        var lines = _bodyBox.Lines.Length;
        var lineHeight = TextRenderer.MeasureText("Ag", _bodyBox.Font).Height + 2;
        return Math.Min(420, Math.Max(80, lines * lineHeight));
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
}
