using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardContractDialog : Form
{
    private readonly PrototypeWizardContractSnapshot _snapshot;
    private readonly CheckedListBox _servicesList = CreateCheckedListBox();
    private readonly CheckedListBox _createFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _updateFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _responseFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _filtersList = CreateCheckedListBox();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox _summaryText = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = false,
    };

    private Button? _nextButton;
    private bool _showingSummary;

    public PrototypeWizardContractDialog(PrototypeWizardContractSnapshot snapshot)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

        Text = "Genexus Open API Builder - Wizard B031";
        StartPosition = FormStartPosition.CenterParent;
        Width = 940;
        Height = 640;
        MinimumSize = new Size(760, 520);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout();
        LoadSnapshot();
    }

    public PrototypeWizardContractSelection? Selection { get; private set; }

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

        var header = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            Text = $"Passo 2 - Configurar contrato: Transaction '{_snapshot.TransactionName}' | Module '{_snapshot.ModuleName}'",
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(header, 0, 0);

        _tabs.TabPages.Add(CreateListTab("Servicos", _servicesList, "Servicos REST do MVP. Todos iniciam habilitados."));
        _tabs.TabPages.Add(CreateRequestTab());
        _tabs.TabPages.Add(CreateListTab("Response", _responseFieldsList, "Campos devolvidos no response principal."));
        _tabs.TabPages.Add(CreateListTab("Filtros List", _filtersList, "Filtros candidatos para o servico List."));
        _tabs.TabPages.Add(CreateListTab("Resumo B032", _summaryText, "Resumo das decisoes acumuladas. B032 ainda nao executa nada na KB."));
        root.Controls.Add(_tabs, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0),
        };

        var next = CreateButton("Proximo");
        _nextButton = next;
        next.Click += (_, _) => AcceptSelection();
        var cancel = CreateButton("Cancelar");
        cancel.Click += (_, _) => CancelWizard();
        var back = CreateButton("Voltar");
        back.Click += (_, _) => GoBack();

        buttons.Controls.Add(next);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(back);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = next;
        CancelButton = cancel;
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(96, 30),
            Margin = new Padding(6, 0, 0, 0),
        };
    }

    private static CheckedListBox CreateCheckedListBox()
    {
        var list = new CheckedListBox
        {
            CheckOnClick = true,
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
            IntegralHeight = false,
        };
        list.ItemCheck += (_, args) =>
        {
            if (args.Index >= 0 && list.Items[args.Index] is ChoiceItem item && !item.Enabled)
            {
                args.NewValue = CheckState.Unchecked;
            }
        };
        return list;
    }

    private static TabPage CreateListTab(string title, Control list, string description)
    {
        var tab = new TabPage(title);
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = description, Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(list, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateRequestTab()
    {
        var tab = new TabPage("Requests");
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.Controls.Add(CreateGroup("CreateRequest", _createFieldsList), 0, 0);
        split.Controls.Add(CreateGroup("UpdateRequest", _updateFieldsList), 1, 0);
        tab.Controls.Add(split);
        return tab;
    }

    private static GroupBox CreateGroup(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        group.Controls.Add(content);
        return group;
    }

    private void LoadSnapshot()
    {
        foreach (var service in _snapshot.Services)
        {
            AddChoice(_servicesList, new ChoiceItem(service.Name, true, service.Name), service.DefaultSelected);
        }

        foreach (var attribute in _snapshot.Attributes)
        {
            var label = FormatAttribute(attribute);
            AddChoice(_createFieldsList, new ChoiceItem(attribute.Name, attribute.IsPayloadEligible, label, attribute.PayloadDisabledReason), attribute.DefaultCreateSelected);
            AddChoice(_updateFieldsList, new ChoiceItem(attribute.Name, attribute.IsUpdatePayloadEligible, label, attribute.UpdatePayloadDisabledReason), attribute.DefaultUpdateSelected);
            AddChoice(_responseFieldsList, new ChoiceItem(attribute.Name, true, label), attribute.DefaultResponseSelected);
            AddChoice(_filtersList, new ChoiceItem(attribute.Name, attribute.IsFilterEligible, FormatFilter(attribute), attribute.FilterDisabledReason), attribute.DefaultFilterSelected);
        }
    }

    private static string FormatAttribute(PrototypeWizardAttributeDecision attribute)
    {
        var markers = new List<string>();
        if (attribute.IsPrimaryKey)
        {
            markers.Add("PK");
        }

        if (attribute.IsDescription)
        {
            markers.Add("Description");
        }

        if (attribute.IsSensitive)
        {
            markers.Add("Sensivel");
        }

        if (attribute.IsFormula)
        {
            markers.Add("Formula");
        }

        if (attribute.IsNoAccept)
        {
            markers.Add("NoAccept");
        }

        if (attribute.IsAudit)
        {
            markers.Add("Auditoria");
        }

        var suffix = markers.Count == 0 ? string.Empty : " [" + string.Join(", ", markers) + "]";
        return $"{attribute.Name} ({attribute.DataType}, {attribute.Length}.{attribute.Decimals}){suffix}";
    }

    private static string FormatFilter(PrototypeWizardAttributeDecision attribute)
    {
        var baseText = FormatAttribute(attribute);
        if (!attribute.IsFilterEligible)
        {
            return baseText;
        }

        var options = new List<string> { attribute.FilterOperator };
        if (attribute.UsesPeriod)
        {
            options.Add("Periodo");
        }

        if (attribute.UsesRange)
        {
            options.Add("Intervalo");
        }

        return baseText + " -> " + string.Join(" / ", options);
    }

    private static void AddChoice(CheckedListBox list, ChoiceItem item, bool selected)
    {
        list.Items.Add(item, item.Enabled && selected);
    }

    private void AcceptSelection()
    {
        if (_showingSummary)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        if (_tabs.SelectedIndex < _tabs.TabPages.Count - 2)
        {
            _tabs.SelectedIndex++;
            return;
        }

        var selectedServices = GetCheckedValues(_servicesList);
        if (selectedServices.Count == 0)
        {
            MessageBox.Show(this, "Selecione ao menos um servico.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Selection = new PrototypeWizardContractSelection(
            _snapshot.TransactionName,
            selectedServices,
            GetCheckedValues(_createFieldsList),
            GetCheckedValues(_updateFieldsList),
            GetCheckedValues(_responseFieldsList),
            GetCheckedValues(_filtersList));
        ShowSummary();
    }

    private void ShowSummary()
    {
        if (Selection is null)
        {
            return;
        }

        _summaryText.Text =
            $"Transaction: {Selection.TransactionName}{Environment.NewLine}" +
            $"Servicos: {string.Join(", ", Selection.SelectedServices)}{Environment.NewLine}" +
            $"CreateRequest: {Selection.CreateFields.Count} campo(s){Environment.NewLine}" +
            $"UpdateRequest: {Selection.UpdateFields.Count} campo(s){Environment.NewLine}" +
            $"Response: {Selection.ResponseFields.Count} campo(s){Environment.NewLine}" +
            $"ListFilters: {Selection.ListFilters.Count} filtro(s){Environment.NewLine}{Environment.NewLine}" +
            "B032 revisara seguranca, paginacao, ordenacao, Services base path e RestPath." + Environment.NewLine +
            "Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.";

        _showingSummary = true;
        _tabs.SelectedIndex = _tabs.TabPages.Count - 1;
        if (_nextButton is not null)
        {
            _nextButton.Text = "Fechar";
        }
    }

    private void GoBack()
    {
        if (_showingSummary)
        {
            _showingSummary = false;
            _tabs.SelectedIndex = _tabs.TabPages.Count - 2;
            if (_nextButton is not null)
            {
                _nextButton.Text = "Proximo";
            }

            return;
        }

        if (_tabs.SelectedIndex > 0)
        {
            _tabs.SelectedIndex--;
            return;
        }

        Selection = null;
        DialogResult = DialogResult.Retry;
        Close();
    }

    private void CancelWizard()
    {
        Selection = null;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private static IReadOnlyList<string> GetCheckedValues(CheckedListBox list)
    {
        return list.CheckedItems
            .OfType<ChoiceItem>()
            .Select(item => item.Value)
            .ToArray();
    }

    private sealed class ChoiceItem
    {
        public ChoiceItem(string value, bool enabled, string text, string disabledReason = "")
        {
            Value = value;
            Enabled = enabled;
            Text = text;
            DisabledReason = disabledReason;
        }

        public string Value { get; }

        public bool Enabled { get; }

        public string Text { get; }

        public string DisabledReason { get; }

        public override string ToString()
        {
            return Enabled || DisabledReason.Length == 0
                ? Text
                : Text + " - " + DisabledReason;
        }
    }
}
