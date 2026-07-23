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
    private readonly PrototypeBusinessComponentSnapshot _businessComponentSnapshot;
    private readonly Func<bool> _enableBusinessComponent;
    private readonly Action<string> _writeBusinessComponentOutput;
    private readonly FlowLayoutPanel _servicesList = CreateChoicePanel();
    private readonly FlowLayoutPanel _createFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _updateFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _responseFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _filtersList = CreateChoicePanel();
    private readonly TextBox _apiNameText = CreateSingleLineTextBox();
    private readonly TextBox _servicesBasePathText = CreateSingleLineTextBox();
    private readonly TextBox _restPathText = CreateSingleLineTextBox();
    private readonly TextBox _endpointsText = CreateReadOnlyTextBox();
    private readonly RadioButton _securityAuthenticationRadio = new() { AutoSize = true, Text = "Authentication", Checked = true, Margin = new Padding(0, 2, 18, 2) };
    private readonly RadioButton _securityAuthorizationRadio = new() { AutoSize = true, Text = "Authorization", Margin = new Padding(0, 2, 18, 2) };
    private readonly RadioButton _securityNoneRadio = new() { AutoSize = true, Text = "None", Margin = new Padding(0, 2, 18, 2) };
    private readonly NumericUpDown _defaultPageSize = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSize = CreateNumericInput();
    private readonly ListBox _staticOrderList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly TextBox _createRequiredText = CreateReadOnlyTextBox();
    private readonly TextBox _updateRequiredText = CreateReadOnlyTextBox();
    private readonly TextBox _businessComponentText = CreateReadOnlyTextBox();
    private readonly CheckBox _enableBusinessComponentCheck = new() { AutoSize = true, Text = "Habilitar Business Component agora", Dock = DockStyle.Top };
    private readonly TextBox _summaryDecisionText = CreateReadOnlyTextBox();
    private readonly TextBox _summaryEndpointText = CreateReadOnlyTextBox();
    private readonly Label _headerLabel = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
        Padding = new Padding(0, 0, 0, 8),
        TextAlign = ContentAlignment.MiddleLeft,
    };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Multiline = true };

    private Button? _nextButton;
    private bool _showingSummary;
    private bool _loadingSnapshot;
    private bool _servicesBasePathEditedManually;
    private bool _businessComponentEnabledDuringWizard;

    public PrototypeWizardDialog(PrototypeWizardContractSnapshot snapshot, PrototypeBusinessComponentSnapshot businessComponentSnapshot, Func<bool> enableBusinessComponent, Action<string> writeBusinessComponentOutput)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _businessComponentSnapshot = businessComponentSnapshot ?? throw new ArgumentNullException(nameof(businessComponentSnapshot));
        _enableBusinessComponent = enableBusinessComponent ?? throw new ArgumentNullException(nameof(enableBusinessComponent));
        _writeBusinessComponentOutput = writeBusinessComponentOutput ?? throw new ArgumentNullException(nameof(writeBusinessComponentOutput));

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

    public bool BusinessComponentEnabledDuringWizard => _businessComponentEnabledDuringWizard;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.None)
        {
            Selection = null;
            DialogResult = DialogResult.Cancel;
        }

        base.OnFormClosing(e);
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

        root.Controls.Add(_headerLabel, 0, 0);

        _tabs.TabPages.Add(CreateListTab("Serviços", _servicesList, "Serviços REST do MVP. Todos iniciam habilitados."));
        _tabs.TabPages.Add(CreateRequestTab());
        _tabs.TabPages.Add(CreateListTab("Response", _responseFieldsList, "Campos devolvidos no response principal."));
        _tabs.TabPages.Add(CreateFilterTab());
        _tabs.TabPages.Add(CreatePathsTab());
        _tabs.TabPages.Add(CreateSecurityTab());
        _tabs.TabPages.Add(CreatePaginationTab());
        _tabs.TabPages.Add(CreateOrderTab());
        _tabs.TabPages.Add(CreateRequiredTab());
        _tabs.TabPages.Add(CreateBusinessComponentTab());
        _tabs.TabPages.Add(CreateSummaryTab());
        _tabs.SelectedIndexChanged += (_, _) => RefreshCurrentTabLabel();
        RefreshCurrentTabLabel();
        root.Controls.Add(_tabs, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0),
        };

        var next = CreateButton("Próximo");
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

    private void RefreshCurrentTabLabel()
    {
        var selectedPage = _tabs.SelectedTab;
        if (selectedPage is null && _tabs.SelectedIndex >= 0 && _tabs.SelectedIndex < _tabs.TabPages.Count)
        {
            selectedPage = _tabs.TabPages[_tabs.SelectedIndex];
        }

        if (selectedPage is null && _tabs.TabPages.Count > 0)
        {
            selectedPage = _tabs.TabPages[0];
        }

        var tabName = selectedPage?.Text;
        var currentTab = string.IsNullOrWhiteSpace(tabName) ? "<nenhuma>" : tabName;
        _headerLabel.Text = $"Wizard prototípico: Module '{_snapshot.ModuleName}' | Transaction '{_snapshot.TransactionName}' | Aba atual: {currentTab}";
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

    private static FlowLayoutPanel CreateChoicePanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        panel.Resize += (_, _) => ResizeChoicePanelItems(panel);
        return panel;
    }

    private static void AddChoice(FlowLayoutPanel panel, ChoiceItem item, bool selected)
    {
        var check = new CheckBox
        {
            AutoSize = false,
            AutoEllipsis = false,
            CheckAlign = ContentAlignment.TopLeft,
            TextAlign = ContentAlignment.TopLeft,
            Text = item.ToString(),
            Tag = item,
            AutoCheck = item.Enabled,
            Checked = item.Enabled && selected,
            ForeColor = item.Enabled ? SystemColors.ControlText : SystemColors.ControlDarkDark,
            TabStop = item.Enabled,
            Margin = new Padding(0, 2, 0, 2),
        };
        panel.Controls.Add(check);
        ResizeChoice(check, panel);
    }

    private static void ResizeChoicePanelItems(FlowLayoutPanel panel)
    {
        foreach (var check in panel.Controls.OfType<CheckBox>())
        {
            ResizeChoice(check, panel);
        }
    }

    private static void ResizeChoice(CheckBox check, FlowLayoutPanel panel)
    {
        var width = Math.Max(80, panel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        var textWidth = Math.Max(40, width - 22);
        var measured = TextRenderer.MeasureText(
            check.Text,
            check.Font,
            new Size(textWidth, int.MaxValue),
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        check.Width = width;
        check.Height = Math.Max(22, measured.Height + 8);
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
            WordWrap = true,
        };
    }

    private static NumericUpDown CreateNumericInput()
    {
        return new NumericUpDown
        {
            Anchor = AnchorStyles.Left,
            Width = 120,
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

    private TabPage CreateFilterTab()
    {
        var tab = new TabPage("Filtros List");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Filtros candidatos para o serviço List.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_filtersList, 0, 1);
        tab.Controls.Add(panel);
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
        panel.Controls.Add(CreateGroup("Paths dos serviços", _endpointsText), 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSecurityTab()
    {
        var tab = new TabPage("Segurança");
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

        var options = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };
        options.Controls.Add(_securityAuthenticationRadio);
        options.Controls.Add(_securityAuthorizationRadio);
        options.Controls.Add(_securityNoneRadio);

        panel.Controls.Add(new Label { AutoSize = true, Text = "Security Level único aplicado aos serviços gerados no MVP.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(options, 0, 1);
        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Authentication inicia selecionado por segurança. Authorization exige permissões GAM coerentes. None deixa a API pública e exigirá confirmação antes da geração.",
            Padding = new Padding(0, 12, 0, 0),
        }, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreatePaginationTab()
    {
        var tab = new TabPage("Paginação");
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8),
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        AddField(fields, 0, "Default Page Size", _defaultPageSize);
        AddField(fields, 1, "Maximum Page Size", _maximumPageSize);
        tab.Controls.Add(fields);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage("Ordenação");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Ordenação estática inicial. A chave primária completa é acrescentada como desempate ascendente.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_staticOrderList, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateRequiredTab()
    {
        var tab = new TabPage("Obrigatórios");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Required significa presença do membro no JSON; vazio, false e 0 continuam valores enviados e validados pelo BC.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);

        var createGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "CreateRequest - Obrigatório no payload",
            Padding = new Padding(8),
        };
        createGroup.Controls.Add(_createRequiredText);

        var updateGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "UpdateRequest - Obrigatório no payload",
            Padding = new Padding(8),
        };
        updateGroup.Controls.Add(_updateRequiredText);

        panel.Controls.Add(createGroup, 0, 1);
        panel.Controls.Add(updateGroup, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private static string FormatRequiredDecision(PrototypeWizardRequiredFieldDecision item)
    {
        return $"{item.FieldName}: Required={item.IsRequired} | {item.Reason}";
    }

    private TabPage CreateBusinessComponentTab()
    {
        var tab = new TabPage("Business Component");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Business Component é obrigatório para gerar a API do MVP preservando regras via BC.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_enableBusinessComponentCheck, 0, 1);
        panel.Controls.Add(_businessComponentText, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage("Resumo B037");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Resumo das decisões acumuladas após exposição de bloqueios e validação de Business Component.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.Controls.Add(CreateGroup("Decisões", _summaryDecisionText), 0, 0);
        split.Controls.Add(CreateGroup("Endpoints e garantias", _summaryEndpointText), 1, 0);

        panel.Controls.Add(split, 0, 1);
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

        _defaultPageSize.Value = 50;
        _maximumPageSize.Value = 200;

        foreach (var item in GetStaticOrder())
        {
            _staticOrderList.Items.Add($"{item.Order}. {item.AttributeName} {item.Direction}");
        }

        RefreshEndpointsText();
        RefreshRequiredText();
        RefreshBusinessComponentText();
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
            markers.Add("Sensível");
        }

        if (attribute.IsFormula)
        {
            markers.Add("Fórmula");
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
            options.Add("Período");
        }

        if (attribute.UsesRange)
        {
            options.Add("Intervalo");
        }

        return baseText + " -> " + string.Join(" / ", options);
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

        if (_tabs.SelectedTab?.Text == "Obrigatórios")
        {
            RefreshRequiredText();
        }

        if (_tabs.SelectedTab?.Text == "Business Component" && !EnsureBusinessComponentReady())
        {
            return;
        }

        if (_tabs.SelectedIndex < _tabs.TabPages.Count - 2)
        {
            _tabs.SelectedIndex++;
            if (_tabs.SelectedTab?.Text == "Obrigatórios")
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

    private string GetSelectedSecurityLevel()
    {
        if (_securityAuthorizationRadio.Checked)
        {
            return "Authorization";
        }

        return _securityNoneRadio.Checked ? "None" : "Authentication";
    }
    private bool TryCreateSelection()
    {
        var selectedServices = GetCheckedValues(_servicesList);
        if (selectedServices.Count == 0)
        {
            MessageBox.Show(this, "Selecione ao menos um serviço.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            GetSelectedSecurityLevel(),
            (int)_defaultPageSize.Value,
            (int)_maximumPageSize.Value,
            GetStaticOrder());
        Selection = new PrototypeWizardFlowSelection(
            contractSelection,
            reviewSelection,
            GetRequiredDecisions(contractSelection),
            CreateBusinessComponentSelection());
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
        var businessComponent = Selection.BusinessComponentSelection;
        var createBlocked = CountBlocked(_createFieldsList);
        var updateBlocked = CountBlocked(_updateFieldsList);
        var filterBlocked = CountBlocked(_filtersList);
        _summaryDecisionText.Text =
            $"Transaction: {contract.TransactionName}{Environment.NewLine}" +
            $"Serviços: {string.Join(", ", contract.SelectedServices)}{Environment.NewLine}" +
            $"CreateRequest: {contract.CreateFields.Count} campo(s), {createRequired} obrigatório(s) no payload{Environment.NewLine}" +
            $"UpdateRequest: {contract.UpdateFields.Count} campo(s), {updateRequired} obrigatório(s) no payload{Environment.NewLine}" +
            $"Required: presença do membro JSON; não exige valor não vazio{Environment.NewLine}" +
            $"Response: {contract.ResponseFields.Count} campo(s){Environment.NewLine}" +
            $"ListFilters: {contract.ListFilters.Count} filtro(s){Environment.NewLine}" +
            $"ApiName: {review.ApiName}{Environment.NewLine}" +
            $"Services base path: {review.ServicesBasePath}{Environment.NewLine}" +
            $"RestPath: {review.RestPath}{Environment.NewLine}" +
            $"Security Level: {review.SecurityLevel}{Environment.NewLine}" +
            $"Paginação: Default={review.DefaultPageSize}, Maximum={review.MaximumPageSize}{Environment.NewLine}" +
            $"Ordenação: {string.Join(", ", review.StaticOrder.Select(item => item.AttributeName + " " + item.Direction))}{Environment.NewLine}" +
            $"B036 bloqueados visíveis: CreateRequest={createBlocked}, UpdateRequest={updateBlocked}, ListFilters={filterBlocked}{Environment.NewLine}" +
            $"Business Component: IsBusinessComponent={businessComponent.IsBusinessComponent}, Status='{businessComponent.Status}', EnabledDuringWizard={businessComponent.EnabledDuringWizard}";
        _summaryEndpointText.Text =
            FormatEndpoints(review.RestPath, contract.SelectedServices) + Environment.NewLine + Environment.NewLine +
            "B036 exibiu campos bloqueados com motivo no fluxo do wizard." + Environment.NewLine +
            "B037 consolidou Required como presença do membro JSON, distinguindo de valor não vazio." + Environment.NewLine +
            "Nenhum ApiPlan foi criado." + Environment.NewLine +
            "Nenhuma escolha foi persistida." + Environment.NewLine +
            "Nenhum objeto foi criado, alterado ou excluído pela geração.";
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
                _nextButton.Text = "Próximo";
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
                lines.Add(service + " <não definido> " + restPath);
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
    private void RefreshBusinessComponentText()
    {
        var effectiveStatus = IsBusinessComponentReady()
            ? "Apta via Business Component"
            : _businessComponentSnapshot.Status;
        _businessComponentText.Text =
            $"Transaction: {_businessComponentSnapshot.TransactionName}{Environment.NewLine}" +
            $"IsBusinessComponent: {IsBusinessComponentReady()}{Environment.NewLine}" +
            $"Status: {effectiveStatus}{Environment.NewLine}{Environment.NewLine}" +
            "Sem Business Component, o MVP bloqueia a geração da API. A habilitação exige confirmação explícita e altera a Transaction na KB; cancelar o wizard depois disso não reverte automaticamente a propriedade.";
        _enableBusinessComponentCheck.Enabled = !_businessComponentSnapshot.IsBusinessComponent && !_businessComponentEnabledDuringWizard;
        _enableBusinessComponentCheck.Visible = !_businessComponentSnapshot.IsBusinessComponent;
    }

    private bool EnsureBusinessComponentReady()
    {
        if (IsBusinessComponentReady())
        {
            return true;
        }

        if (!_enableBusinessComponentCheck.Checked)
        {
            _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Transaction='{_businessComponentSnapshot.TransactionName}' bloqueada: Business Component desabilitado e habilitacao explicita nao confirmada. Nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.");
            MessageBox.Show(this, "Business Component está desabilitado. Marque a habilitação explícita para continuar ou cancele o wizard.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshBusinessComponentText();
            return false;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Habilitar Business Component altera a Transaction '{_businessComponentSnapshot.TransactionName}' na KB. A alteração não será revertida automaticamente ao cancelar o wizard ou remover a extensão. Deseja habilitar agora?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Habilitacao de Business Component cancelada para Transaction='{_businessComponentSnapshot.TransactionName}'. Nenhuma alteracao foi feita na KB.");
            _enableBusinessComponentCheck.Checked = false;
            RefreshBusinessComponentText();
            return false;
        }

        try
        {
            if (!_enableBusinessComponent())
            {
                _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Falha ao confirmar Business Component habilitado para Transaction='{_businessComponentSnapshot.TransactionName}' apos gravacao.");
                MessageBox.Show(this, "Não foi possível confirmar Business Component habilitado após a gravação.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshBusinessComponentText();
                return false;
            }
        }
        catch (Exception ex)
        {
            _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Falha ao habilitar Business Component para Transaction='{_businessComponentSnapshot.TransactionName}': {ex.Message}");
            MessageBox.Show(this, "Falha ao habilitar Business Component: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            RefreshBusinessComponentText();
            return false;
        }

        _businessComponentEnabledDuringWizard = true;
        _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Business Component habilitado por confirmacao explicita para Transaction='{_businessComponentSnapshot.TransactionName}'. A alteracao foi gravada na KB e nao sera revertida automaticamente.");
        _enableBusinessComponentCheck.Checked = false;
        RefreshBusinessComponentText();
        return true;
    }

    private bool IsBusinessComponentReady()
    {
        return _businessComponentSnapshot.IsBusinessComponent || _businessComponentEnabledDuringWizard;
    }

    private PrototypeWizardBusinessComponentSelection CreateBusinessComponentSelection()
    {
        return new PrototypeWizardBusinessComponentSelection(
            _businessComponentSnapshot.TransactionName,
            IsBusinessComponentReady(),
            _businessComponentEnabledDuringWizard,
            IsBusinessComponentReady() ? "Apta via Business Component" : _businessComponentSnapshot.Status);
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
        _createRequiredText.Text = string.Join(Environment.NewLine, decisions
            .Where(item => item.RequestName == "CreateRequest")
            .Select(FormatRequiredDecision));
        _updateRequiredText.Text = string.Join(Environment.NewLine, decisions
            .Where(item => item.RequestName == "UpdateRequest")
            .Select(FormatRequiredDecision));
    }
    private IReadOnlyList<PrototypeWizardRequiredFieldDecision> GetRequiredDecisions(PrototypeWizardContractSelection selection)
    {
        var create = selection.CreateFields
            .Select(name => CreateRequiredDecision(name))
            .ToArray();
        var update = selection.UpdateFields
            .Select(name => new PrototypeWizardRequiredFieldDecision("UpdateRequest", name, true, "Update via PUT exige presença de todo membro selecionado."))
            .ToArray();
        return create.Concat(update).ToArray();
    }
    private PrototypeWizardRequiredFieldDecision CreateRequiredDecision(string fieldName)
    {
        var attribute = _snapshot.Attributes.Single(item => string.Equals(item.Name, fieldName, StringComparison.Ordinal));
        if (attribute.IsSensitive)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo sensível selecionado permanece opcional no protótipo; se enviado, o valor é validado pelo BC.");
        }
        if (attribute.IsNullable)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo nullable pode ser omitido; valor vazio presente continua valor enviado e sujeito ao BC.");
        }
        return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, true, "Campo selecionado sem nulabilidade conhecida deve estar presente no JSON; isso não exige valor não vazio.");
    }
    private static IReadOnlyList<string> GetCheckedValues(FlowLayoutPanel panel)
    {
        return panel.Controls
            .OfType<CheckBox>()
            .Where(control => control.Checked && control.Tag is ChoiceItem)
            .Select(control => ((ChoiceItem)control.Tag).Value)
            .ToArray();
    }
    private static int CountBlocked(FlowLayoutPanel panel)
    {
        return panel.Controls
            .OfType<CheckBox>()
            .Select(control => control.Tag as ChoiceItem)
            .Count(item => item is not null && !item.Enabled);
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

            return Label + " [Bloqueado - Motivo: " + DisabledReason + "]";
        }
    }
}

internal sealed class PrototypeWizardFlowSelection
{
    public PrototypeWizardFlowSelection(
        PrototypeWizardContractSelection contractSelection,
        PrototypeWizardReviewSelection reviewSelection,
        IReadOnlyList<PrototypeWizardRequiredFieldDecision> requiredFields,
        PrototypeWizardBusinessComponentSelection businessComponentSelection)
    {
        ContractSelection = contractSelection ?? throw new ArgumentNullException(nameof(contractSelection));
        ReviewSelection = reviewSelection ?? throw new ArgumentNullException(nameof(reviewSelection));
        RequiredFields = requiredFields ?? throw new ArgumentNullException(nameof(requiredFields));
        BusinessComponentSelection = businessComponentSelection ?? throw new ArgumentNullException(nameof(businessComponentSelection));
    }

    public PrototypeWizardContractSelection ContractSelection { get; }

    public PrototypeWizardReviewSelection ReviewSelection { get; }

    public IReadOnlyList<PrototypeWizardRequiredFieldDecision> RequiredFields { get; }

    public PrototypeWizardBusinessComponentSelection BusinessComponentSelection { get; }
}

internal sealed class PrototypeWizardBusinessComponentSelection
{
    public PrototypeWizardBusinessComponentSelection(string transactionName, bool isBusinessComponent, bool enabledDuringWizard, string status)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        IsBusinessComponent = isBusinessComponent;
        EnabledDuringWizard = enabledDuringWizard;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public string TransactionName { get; }

    public bool IsBusinessComponent { get; }

    public bool EnabledDuringWizard { get; }

    public string Status { get; }
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
