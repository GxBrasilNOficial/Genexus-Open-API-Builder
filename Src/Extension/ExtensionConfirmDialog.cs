#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class ExtensionConfirmDialog : Form
{
    private const int Pad = 12;
    private const int ColumnGap = 24;
    private const int ButtonGap = 8;
    private const int TargetTextWidth = 1040;

    private readonly PictureBox _iconBox;
    private readonly Label _introLabel;
    private readonly Label _identityLabel;
    private readonly Label _leftColumnLabel;
    private readonly Label _rightColumnLabel;
    private readonly Label _notesLabel;
    private readonly Label _confirmLabel;
    private readonly Button _yesButton;
    private readonly Button _noButton;

    public ExtensionConfirmDialog(
        string caption,
        string intro,
        ApiPlanGeneratedApiRemovalPlan plan,
        string confirmQuestion,
        ExtensionTexts texts)
    {
        Text = caption ?? string.Empty;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        BackColor = SystemColors.Control;

        var language = texts.Language;
        var identity = ExtensionOutputLocalization.Translate(
            $"Transaction: {plan.TransactionName}    API Object: {plan.ApiName}    Metadata File: {plan.MetadataFileName}",
            language);
        var leftColumn = ExtensionOutputLocalization.Translate(BuildLeftColumn(plan), language);
        var rightColumn = ExtensionOutputLocalization.Translate(BuildRightColumn(plan), language);
        var notes = ExtensionOutputLocalization.Translate(BuildNotes(plan), language);

        _iconBox = new PictureBox
        {
            Image = SystemIcons.Warning.ToBitmap(),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(Pad, Pad),
        };

        _introLabel = CreateLabel(intro);
        _identityLabel = CreateLabel(identity);
        _leftColumnLabel = CreateLabel(leftColumn);
        _rightColumnLabel = CreateLabel(rightColumn);
        _notesLabel = CreateLabel(notes);
        _confirmLabel = CreateLabel(confirmQuestion);

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

        Controls.Add(_iconBox);
        Controls.Add(_introLabel);
        Controls.Add(_identityLabel);
        Controls.Add(_leftColumnLabel);
        Controls.Add(_rightColumnLabel);
        Controls.Add(_notesLabel);
        Controls.Add(_confirmLabel);
        Controls.Add(_yesButton);
        Controls.Add(_noButton);

        Load += (_, _) => LayoutToWorkingArea();
        Shown += (_, _) => _noButton.Focus();
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text ?? string.Empty,
            UseMnemonic = false,
            BackColor = Color.Transparent,
        };
    }

    private void LayoutToWorkingArea()
    {
        var working = Screen.FromPoint(Cursor.Position).WorkingArea;
        var textLeft = Pad + SystemIcons.Warning.Width + Pad;
        var maxTextWidth = Math.Max(640, working.Width - textLeft - Pad - 48);
        var textWidth = Math.Min(TargetTextWidth, maxTextWidth);
        var columnWidth = Math.Max(280, (textWidth - ColumnGap) / 2);

        PlaceLabel(_introLabel, textLeft, Pad, textWidth);
        PlaceLabel(_identityLabel, textLeft, _introLabel.Bottom + 8, textWidth);

        var columnsTop = _identityLabel.Bottom + 8;
        PlaceLabel(_leftColumnLabel, textLeft, columnsTop, columnWidth);
        PlaceLabel(_rightColumnLabel, textLeft + columnWidth + ColumnGap, columnsTop, columnWidth);

        var columnsBottom = Math.Max(_leftColumnLabel.Bottom, _rightColumnLabel.Bottom);
        PlaceLabel(_notesLabel, textLeft, columnsBottom + 8, textWidth);
        PlaceLabel(_confirmLabel, textLeft, _notesLabel.Bottom + 8, textWidth);

        var noSize = _noButton.GetPreferredSize(Size.Empty);
        var yesSize = _yesButton.GetPreferredSize(Size.Empty);
        _noButton.Size = noSize;
        _yesButton.Size = yesSize;

        var buttonTop = _confirmLabel.Bottom + Pad;
        var clientWidth = textLeft + textWidth + Pad;
        var clientHeight = buttonTop + Math.Max(noSize.Height, yesSize.Height) + Pad;

        _noButton.Location = new Point(clientWidth - Pad - noSize.Width, buttonTop);
        _yesButton.Location = new Point(_noButton.Left - ButtonGap - yesSize.Width, buttonTop);
        ClientSize = new Size(clientWidth, clientHeight);

        var left = working.Left + Math.Max(0, (working.Width - Width) / 2);
        var top = working.Top + Math.Max(24, (working.Height - Height) / 5);
        left = Math.Min(left, working.Right - Width);
        top = Math.Min(top, working.Bottom - Height);
        Location = new Point(Math.Max(working.Left, left), Math.Max(working.Top, top));
    }

    private static void PlaceLabel(Label label, int left, int top, int maxWidth)
    {
        label.MaximumSize = new Size(maxWidth, 0);
        label.AutoSize = true;
        label.Location = new Point(left, top);
    }

    private static string BuildLeftColumn(ApiPlanGeneratedApiRemovalPlan plan)
    {
        var builder = new StringBuilder();
        builder.Append("Procedures (").Append(plan.ProcedureNames.Count).AppendLine("):");
        AppendItems(builder, plan.ProcedureNames);
        return builder.ToString().TrimEnd();
    }

    private static string BuildRightColumn(ApiPlanGeneratedApiRemovalPlan plan)
    {
        var builder = new StringBuilder();
        builder.Append("SDTs próprios (").Append(plan.OwnSdtNames.Count).AppendLine("):");
        AppendItems(builder, plan.OwnSdtNames);
        builder.AppendLine();
        builder.Append("SDTs compartilhados preservados (").Append(plan.SharedSdtNamesPreserved.Count).AppendLine("):");
        AppendItems(builder, plan.SharedSdtNamesPreserved);
        return builder.ToString().TrimEnd();
    }

    private static string BuildNotes(ApiPlanGeneratedApiRemovalPlan plan)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(plan.FolderName))
        {
            if (plan.FolderWasCreated)
            {
                builder.Append("Folder: ").Append(plan.FolderName).AppendLine(" (criado pela extensão; apagar só se ficar vazio)");
            }
            else
            {
                builder.Append("Folder: ").Append(plan.FolderName).AppendLine(" (reutilizado; nunca apagar)");
            }
        }

        builder.Append("Business Component da Transaction: não será revertido.");
        return builder.ToString().TrimEnd();
    }

    private static void AppendItems(StringBuilder builder, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("  (nenhum)");
            return;
        }

        foreach (var item in items)
        {
            builder.Append("  - ").AppendLine(item);
        }
    }
}
