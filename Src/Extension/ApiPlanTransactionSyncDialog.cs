#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class ApiPlanTransactionSyncDialog : Form
{
    private readonly ApiPlanTransactionSyncPreview _preview;
    private readonly Dictionary<string, CheckedListBox> _addedLists = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ComboBox> _sdtCombos = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBox _summaryBox = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
    };

    public ApiPlanTransactionSyncDialog(ApiPlanTransactionSyncPreview preview)
    {
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        Text = "Genexus Open API Builder - Sincronizar com a Transaction";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        Width = 920;
        Height = 740;
        MinimumSize = new Size(820, 620);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        BuildLayout();
    }

    public ApiPlanTransactionSyncChoices? Choices { get; private set; }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Text = $"Sincronizar API com a Transaction '{_preview.TransactionName}'",
            Padding = new Padding(0, 0, 0, 8),
        }, 0, 0);

        _summaryBox.Text = _preview.Diff.BuildSummary();
        if (_preview.Diff.Removed.Count > 0 || _preview.Diff.Modified.Any(item => item.Details.Any(detail => detail.StartsWith("tipo ", StringComparison.Ordinal))))
        {
            _summaryBox.Text += Environment.NewLine + Environment.NewLine
                + "Avisos: remocoes e mudancas de tipo podem quebrar clientes; novo campo obrigatorio via BC pode quebrar Create.";
        }

        root.Controls.Add(_summaryBox, 0, 1);

        var addedGroup = new GroupBox
        {
            Text = "Campos adicionados — marque onde incluir",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        var addedLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
        };
        addedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        addedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        addedLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        addedLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        addedGroup.Controls.Add(addedLayout);
        AddRoleList(addedLayout, 0, 0, "Response", preferDefault: true);
        AddRoleList(addedLayout, 1, 0, "CreateRequest", preferDefault: false);
        AddRoleList(addedLayout, 0, 1, "UpdateRequest", preferDefault: false);
        AddRoleList(addedLayout, 1, 1, "ListFilters", preferDefault: false);
        root.Controls.Add(addedGroup, 0, 2);

        var conflictGroup = new GroupBox
        {
            Text = "Conflitos de SDT editado manualmente",
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        var conflictLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
        };
        conflictLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        conflictLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        conflictGroup.Controls.Add(conflictLayout);
        if (_preview.SdtConflicts.Count == 0)
        {
            conflictLayout.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Nenhum conflito de SDT detectado.",
            }, 0, 0);
        }
        else
        {
            var row = 0;
            foreach (var conflict in _preview.SdtConflicts)
            {
                conflictLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                conflictLayout.Controls.Add(new Label
                {
                    AutoSize = true,
                    Text = $"{conflict.SdtName}: {conflict.Reason}",
                    Dock = DockStyle.Fill,
                }, 0, row);
                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill,
                };
                combo.Items.AddRange(new object[]
                {
                    ApiPlanTransactionSyncSdtResolution.Replace,
                    ApiPlanTransactionSyncSdtResolution.Keep,
                    ApiPlanTransactionSyncSdtResolution.Cancel,
                });
                combo.SelectedIndex = 0;
                _sdtCombos[conflict.SdtName] = combo;
                conflictLayout.Controls.Add(combo, 1, row);
                row++;
            }
        }

        root.Controls.Add(conflictGroup, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
        };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true };
        var apply = new Button { Text = "Aplicar sincronizacao", AutoSize = true };
        apply.Click += (_, _) => SaveAndClose();
        AcceptButton = apply;
        CancelButton = cancel;
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(apply);
        root.Controls.Add(buttons, 0, 4);
    }

    private void AddRoleList(TableLayoutPanel layout, int column, int row, string role, bool preferDefault)
    {
        var cell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4),
        };
        cell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cell.Controls.Add(new Label
        {
            AutoSize = true,
            Text = role,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 4),
        }, 0, 0);

        var list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            HorizontalScrollbar = true,
        };
        foreach (var added in _preview.Diff.Added)
        {
            var current = added.Current;
            if (current is null)
            {
                continue;
            }

            var eligible = role switch
            {
                "CreateRequest" => current.IsWritableByCreate,
                "UpdateRequest" => current.IsWritableByUpdate,
                "ListFilters" => current.IsFilterEligible,
                _ => true,
            };
            if (!eligible)
            {
                continue;
            }

            var shortGuid = current.AttributeGuid.Length > 8
                ? current.AttributeGuid.Substring(0, 8)
                : current.AttributeGuid;
            var index = list.Items.Add(new RoleFieldItem(current.AttributeGuid, $"{current.Name} [{shortGuid}…]"));
            var shouldCheck = preferDefault
                ? current.DefaultResponseSelected || (!current.IsSensitive && !current.IsAuditField)
                : role switch
                {
                    "CreateRequest" => current.DefaultCreateSelected,
                    "UpdateRequest" => current.DefaultUpdateSelected,
                    "ListFilters" => current.DefaultFilterSelected,
                    _ => false,
                };
            list.SetItemChecked(index, shouldCheck);
        }

        _addedLists[role] = list;
        cell.Controls.Add(list, 0, 1);
        layout.Controls.Add(cell, column, row);
    }

    private void SaveAndClose()
    {
        var includeAdded = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        foreach (var pair in _addedLists)
        {
            var guids = new List<string>();
            foreach (var item in pair.Value.CheckedItems)
            {
                if (item is RoleFieldItem fieldItem)
                {
                    guids.Add(fieldItem.AttributeGuid);
                }
            }

            includeAdded[pair.Key] = guids;
        }

        var resolutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _sdtCombos)
        {
            var selected = pair.Value.SelectedItem?.ToString() ?? ApiPlanTransactionSyncSdtResolution.Replace;
            if (string.Equals(selected, ApiPlanTransactionSyncSdtResolution.Cancel, StringComparison.Ordinal))
            {
                Choices = new ApiPlanTransactionSyncChoices(cancel: true, includeAdded, resolutions);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            resolutions[pair.Key] = selected;
        }

        Choices = new ApiPlanTransactionSyncChoices(cancel: false, includeAdded, resolutions);
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class RoleFieldItem
    {
        public RoleFieldItem(string attributeGuid, string displayText)
        {
            AttributeGuid = attributeGuid ?? throw new ArgumentNullException(nameof(attributeGuid));
            DisplayText = displayText ?? throw new ArgumentNullException(nameof(displayText));
        }

        public string AttributeGuid { get; }
        public string DisplayText { get; }
        public override string ToString() => DisplayText;
    }
}
