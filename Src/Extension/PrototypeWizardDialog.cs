using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardDialog : Form
{
    private readonly PrototypeWizardContractSnapshot _snapshot;
    private readonly CheckedListBox _servicesList = CreateCheckedListBox();
    private readonly CheckedListBox _createFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _updateFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _responseFieldsList = CreateCheckedListBox();
    private readonly CheckedListBox _filtersList = CreateCheckedListBox();
    private readonly TextBox _apiNameText = CreateSingleLineTextBox();
    private readonly TextBox _servicesBasePathText = CreateSingleLineTextBox();
    private readonly TextBox _restPathText = CreateSingleLineTextBox();
    private readonly TextBox _endpointsText = CreateReadOnlyTextBox();
    private readonly ComboBox _securityLevelCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly NumericUpDown _defaultPageSize = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSize = CreateNumericInput();
    private readonly ListBox _staticOrderList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly TextBox _requiredText = CreateReadOnlyTextBox();
    private readonly TextBox _summaryText = CreateReadOnlyTextBox();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    private Button? _nextButton;
    private bool _showingSummary;
    private bool _loadingSnapshot;
    private bool _servicesBasePathEditedManually;

    public PrototypeWizardDialog(PrototypeWizardContractSnapshot snapshot)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

        Text = "Genexus Open API Builder - Wizard";
        StartPosition = FormStartPosition.CenterParent;
        Width = 980;
        Height = 680;
        MinimumSize = new Size(780, 540);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout();
        WirePathSynchronization();
        LoadSnapshot();
    }

    public PrototypeWizardFlowSelection? Selection { get; private set; }

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
            Text = $"Wizard prototipico: Transaction '{_snapshot.TransactionName}' | Module '{_snapshot.ModuleName}'",
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(header, 0, 0);

        _tabs.TabPages.Add(CreateListTab("Servicos", _servicesList, "Servicos REST do MVP. Todos iniciam habilitados."));
        _tabs.TabPages.Add(CreateRequestTab());
        _tabs.TabPages.Add(CreateListTab("Response", _responseFieldsList, "Campos devolvidos no response principal."));
        _tabs.TabPages.Add(CreateListTab("Filtros List", _filtersList, "Filtros candidatos para o servico List."));
        _tabs.TabPages.Add(CreatePathsTab());
        _tabs.TabPages.Add(CreateSecurityTab());
        _tabs.TabPages.Add(CreatePaginationTab());
        _tabs.TabPages.Add(CreateOrderTab());
        _tabs.TabPages.Add(CreateRequiredTab());
        _tabs.TabPages.Add(CreateSummaryTab());
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

    private static TextBox CreateSingleLineTextBox()
    {
        return new TextBox { Dock = DockStyle.Top };
    }

    private static TextBox CreateReadOnlyTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
        };
    }

    private static NumericUpDown CreateNumericInput()
    {
        return new NumericUpDown
        {
            Dock = DockStyle.Top,
            Minimum = 1,
            Maximum = 100000,
        };
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

    private TabPage CreatePathsTab()
    {
        var tab = new TabPage("Paths");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 0, 0, 10),
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(fields, 0, "Nome API", _apiNameText);
        AddField(fields, 1, "Services base path", _servicesBasePathText);
        AddField(fields, 2, "RestPath", _restPathText);

        panel.Controls.Add(fields, 0, 0);
        panel.Controls.Add(CreateGroup("Paths dos servicos", _endpointsText), 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSecurityTab()
    {
        var tab = new TabPage("Seguranca");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Security Level unico aplicado aos servicos gerados no MVP.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_securityLevelCombo, 0, 1);
        panel.Controls.Add(new Label { AutoSize = true, Text = "Authentication inicia selecionado por seguranca. None permanece apenas como decisao prototipica nesta etapa.", Padding = new Padding(0, 12, 0, 0) }, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreatePaginationTab()
    {
        var tab = new TabPage("Paginacao");
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8),
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(fields, 0, "Default Page Size", _defaultPageSize);
        AddField(fields, 1, "Maximum Page Size", _maximumPageSize);
        tab.Controls.Add(fields);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage("Ordenacao");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Ordenacao estatica inicial. A chave primaria completa e acrescentada como desempate ascendente.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_staticOrderList, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateRequiredTab()
    {
        var tab = new TabPage("Obrigatorios");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Obrigatorio no payload significa presenca do membro no JSON, nao valor nao vazio.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_requiredText, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage("Resumo B034");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Resumo das decisoes acumuladas. B034 validara cancelamento seguro.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_summaryText, 0, 1);
        tab.Controls.Add(panel);
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

    private static void AddField(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.Controls.Add(new Label { AutoSize = true, Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(control, 1, row);
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

        _loadingSnapshot = true;
        var apiName = "api" + _snapshot.TransactionName;
        _apiNameText.Text = apiName;
        _servicesBasePathText.Text = apiName;
        _restPathText.Text = "/" + ToKebabCase(_snapshot.TransactionName);
        _servicesBasePathEditedManually = false;
        _loadingSnapshot = false;

        _securityLevelCombo.Items.Add("Authentication");
        _securityLevelCombo.Items.Add("None");
        _securityLevelCombo.SelectedItem = "Authentication";
        _defaultPageSize.Value = 50;
        _maximumPageSize.Value = 200;

        foreach (var item in GetStaticOrder())
        {
            _staticOrderList.Items.Add($"{item.Order}. {item.AttributeName} {item.Direction}");
        }

        RefreshEndpointsText();
        RefreshRequiredText();
    }

    private void WirePathSynchronization()
    {
        _apiNameText.TextChanged += (_, _) =>
        {
            if (!_loadingSnapshot && !_servicesBasePathEditedManually)
            {
                _servicesBasePathText.Text = _apiNameText.Text;
            }
        };

        _servicesBasePathText.TextChanged += (_, _) =>
        {
            if (!_loadingSnapshot && !string.Equals(_servicesBasePathText.Text, _apiNameText.Text, StringComparison.Ordinal))
            {
                _servicesBasePathEditedManually = true;
            }
        };
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

        if (_tabs.SelectedTab?.Text == "Paths")
        {
            RefreshEndpointsText();
        }

        if (_tabs.SelectedTab?.Text == "Obrigatorios")
        {
            RefreshRequiredText();
        }

        if (_tabs.SelectedIndex < _tabs.TabPages.Count - 2)
        {
            _tabs.SelectedIndex++;
            if (_tabs.SelectedTab?.Text == "Obrigatorios")
            {
                RefreshRequiredText();
            }

            return;
        }

        if (!TryCreateSelection())
        {
            return;
        }

        ShowSummary();
    }

    private bool TryCreateSelection()
    {
        var selectedServices = GetCheckedValues(_servicesList);
        if (selectedServices.Count == 0)
        {
            MessageBox.Show(this, "Selecione ao menos um servico.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var apiName = _apiNameText.Text.Trim();
        var servicesBasePath = _servicesBasePathText.Text.Trim();
        var restPath = _restPathText.Text.Trim();
        if (apiName.Length == 0 || servicesBasePath.Length == 0 || restPath.Length == 0)
        {
            MessageBox.Show(this, "Informe Nome API, Services base path e RestPath.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!restPath.StartsWith("/", StringComparison.Ordinal))
        {
            MessageBox.Show(this, "RestPath deve iniciar com '/'.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_defaultPageSize.Value > _maximumPageSize.Value)
        {
            MessageBox.Show(this, "Default Page Size deve ser menor ou igual a Maximum Page Size.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var contractSelection = new PrototypeWizardContractSelection(
            _snapshot.TransactionName,
            selectedServices,
            GetCheckedValues(_createFieldsList),
            GetCheckedValues(_updateFieldsList),
            GetCheckedValues(_responseFieldsList),
            GetCheckedValues(_filtersList));
        var reviewSelection = new PrototypeWizardReviewSelection(
            _snapshot.TransactionName,
            apiName,
            servicesBasePath,
            restPath,
            _securityLevelCombo.SelectedItem?.ToString() ?? "Authentication",
            (int)_defaultPageSize.Value,
            (int)_maximumPageSize.Value,
            GetStaticOrder());
        Selection = new PrototypeWizardFlowSelection(
            contractSelection,
            reviewSelection,
            GetRequiredDecisions(contractSelection));
        return true;
    }
    private void ShowSummary()
    {
        if (Selection is null)
        {
            return;
        }
        var contract = Selection.ContractSelection;
        var review = Selection.ReviewSelection;
        var createRequired = Selection.RequiredFields.Count(item => item.RequestName == "CreateRequest" && item.IsRequired);
        var updateRequired = Selection.RequiredFields.Count(item => item.RequestName == "UpdateRequest" && item.IsRequired);
        _summaryText.Text =
            $"Transaction: {contract.TransactionName}{Environment.NewLine}" +
            $"Servicos: {string.Join(", ", contract.SelectedServices)}{Environment.NewLine}" +
            $"CreateRequest: {contract.CreateFields.Count} campo(s), {createRequired} obrigatorio(s) no payload{Environment.NewLine}" +
            $"UpdateRequest: {contract.UpdateFields.Count} campo(s), {updateRequired} obrigatorio(s) no payload{Environment.NewLine}" +
            $"Response: {contract.ResponseFields.Count} campo(s){Environment.NewLine}" +
            $"ListFilters: {contract.ListFilters.Count} filtro(s){Environment.NewLine}" +
            $"ApiName: {review.ApiName}{Environment.NewLine}" +
            $"Services base path: {review.ServicesBasePath}{Environment.NewLine}" +
            $"RestPath: {review.RestPath}{Environment.NewLine}" +
            $"Security Level: {review.SecurityLevel}{Environment.NewLine}" +
            $"Paginacao: Default={review.DefaultPageSize}, Maximum={review.MaximumPageSize}{Environment.NewLine}" +
            $"Ordenacao: {string.Join(", ", review.StaticOrder.Select(item => item.AttributeName + " " + item.Direction))}{Environment.NewLine}{Environment.NewLine}" +
            FormatEndpoints(review.RestPath, contract.SelectedServices) + Environment.NewLine + Environment.NewLine +
            "B034 validara cancelamento seguro. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.";
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
    private void RefreshEndpointsText()
    {
        _endpointsText.Text = FormatEndpoints(_restPathText.Text.Trim(), GetCheckedValues(_servicesList));
    }
    private string FormatEndpoints(string restPath, IReadOnlyList<string> selectedServices)
    {
        var keyPath = restPath + FormatKeySuffix();
        var lines = new List<string>();
        foreach (var service in selectedServices)
        {
            var upperService = service.ToUpperInvariant();
            if (upperService == "LIST")
            {
                lines.Add("List   GET  " + restPath);
            }
            else if (upperService == "GET")
            {
                lines.Add("Get    GET  " + keyPath);
            }
            else if (upperService == "CREATE")
            {
                lines.Add("Create POST " + restPath);
            }
            else if (upperService == "UPDATE")
            {
                lines.Add("Update PUT  " + keyPath);
            }
            else
            {
                lines.Add(service + " <nao definido> " + restPath);
            }
        }
        return string.Join(Environment.NewLine, lines);
    }
    private string FormatKeySuffix()
    {
        var primaryKeyParts = _snapshot.Attributes
            .Where(attribute => attribute.IsPrimaryKey)
            .OrderBy(attribute => attribute.Order)
            .Select(attribute => attribute.Name)
            .ToArray();
        if (primaryKeyParts.Length == 0)
        {
            return string.Empty;
        }
        return "/" + string.Join("/", primaryKeyParts.Select(part => "{" + part + "}"));
    }
    private IReadOnlyList<PrototypeWizardStaticOrderPart> GetStaticOrder()
    {
        return _snapshot.Attributes
            .Where(attribute => attribute.IsPrimaryKey)
            .OrderBy(attribute => attribute.Order)
            .Select((attribute, index) => new PrototypeWizardStaticOrderPart(index + 1, attribute.Name, "ASC"))
            .ToArray();
    }
    private void RefreshRequiredText()
    {
        var selection = new PrototypeWizardContractSelection(
            _snapshot.TransactionName,
            GetCheckedValues(_servicesList),
            GetCheckedValues(_createFieldsList),
            GetCheckedValues(_updateFieldsList),
            GetCheckedValues(_responseFieldsList),
            GetCheckedValues(_filtersList));
        var decisions = GetRequiredDecisions(selection);
        var lines = decisions.Select(item => $"{item.RequestName}: {item.FieldName} -> Required={item.IsRequired} ({item.Reason})");
        _requiredText.Text = string.Join(Environment.NewLine, lines);
    }
    private IReadOnlyList<PrototypeWizardRequiredFieldDecision> GetRequiredDecisions(PrototypeWizardContractSelection selection)
    {
        var create = selection.CreateFields
            .Select(name => CreateRequiredDecision(name))
            .ToArray();
        var update = selection.UpdateFields
            .Select(name => new PrototypeWizardRequiredFieldDecision("UpdateRequest", name, true, "Update via PUT exige presenca de todo membro selecionado."))
            .ToArray();
        return create.Concat(update).ToArray();
    }
    private PrototypeWizardRequiredFieldDecision CreateRequiredDecision(string fieldName)
    {
        var attribute = _snapshot.Attributes.Single(item => string.Equals(item.Name, fieldName, StringComparison.Ordinal));
        if (attribute.IsSensitive)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo sensivel selecionado permanece opcional no prototipo.");
        }
        if (attribute.IsNullable)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo nullable pode ser omitido; valor vazio presente continua sujeito ao BC.");
        }
        return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, true, "Campo selecionado sem nulabilidade conhecida deve estar presente no JSON.");
    }
    private static IReadOnlyList<string> GetCheckedValues(CheckedListBox list)
    {
        return list.CheckedItems
            .OfType<ChoiceItem>()
            .Select(item => item.Value)
            .ToArray();
    }
    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;
        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        if (builder.Length > 0 && builder[builder.Length - 1] == '-')
        {
            builder.Length--;
        }

        return builder.Length == 0 ? "transacao" : builder.ToString();
    }

    private sealed class ChoiceItem
    {
        public ChoiceItem(string value, bool enabled, string label, string? disabledReason = null)
        {
            Value = value;
            Enabled = enabled;
            Label = label;
            DisabledReason = disabledReason;
        }

        public string Value { get; }

        public bool Enabled { get; }

        public string Label { get; }

        public string? DisabledReason { get; }

        public override string ToString()
        {
            if (Enabled || string.IsNullOrWhiteSpace(DisabledReason))
            {
                return Label;
            }

            return Label + " (bloqueado: " + DisabledReason + ")";
        }
    }
}

internal sealed class PrototypeWizardFlowSelection
{
    public PrototypeWizardFlowSelection(
        PrototypeWizardContractSelection contractSelection,
        PrototypeWizardReviewSelection reviewSelection,
        IReadOnlyList<PrototypeWizardRequiredFieldDecision> requiredFields)
    {
        ContractSelection = contractSelection ?? throw new ArgumentNullException(nameof(contractSelection));
        ReviewSelection = reviewSelection ?? throw new ArgumentNullException(nameof(reviewSelection));
        RequiredFields = requiredFields ?? throw new ArgumentNullException(nameof(requiredFields));
    }

    public PrototypeWizardContractSelection ContractSelection { get; }

    public PrototypeWizardReviewSelection ReviewSelection { get; }

    public IReadOnlyList<PrototypeWizardRequiredFieldDecision> RequiredFields { get; }
}

internal sealed class PrototypeWizardRequiredFieldDecision
{
    public PrototypeWizardRequiredFieldDecision(string requestName, string fieldName, bool isRequired, string reason)
    {
        RequestName = requestName;
        FieldName = fieldName;
        IsRequired = isRequired;
        Reason = reason;
    }

    public string RequestName { get; }

    public string FieldName { get; }

    public bool IsRequired { get; }

    public string Reason { get; }
}

internal static class PrototypeWizardFlowSessionState
{
    private static PrototypeWizardFlowSelection? _selection;

    public static PrototypeWizardFlowSelection? Selection => _selection;

    public static void Store(PrototypeWizardFlowSelection selection)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public static void Clear()
    {
        _selection = null;
    }

    public static void ClearIfTransactionChanged(string transactionName)
    {
        if (_selection is null)
        {
            return;
        }

        if (!string.Equals(_selection.ContractSelection.TransactionName, transactionName, StringComparison.Ordinal))
        {
            _selection = null;
        }
    }
}
