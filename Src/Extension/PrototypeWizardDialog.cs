using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardDialog : Form
{
    private readonly ExtensionTexts _texts;
    private readonly KBModel _designModel;
    private readonly Transaction _transaction;
    private readonly PrototypeWizardContractSnapshot _snapshot;
    private readonly PrototypeBusinessComponentSnapshot _businessComponentSnapshot;
    private readonly PrototypeWizardPreferences _preferences;
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
    private readonly CheckBox _includeBcErrorMessagesCheck = new() { AutoSize = true, Checked = true, Margin = new Padding(0, 8, 0, 2) };
    private readonly Label _bcErrorMessagesWarningLabel = new() { AutoSize = true, ForeColor = Color.DarkGoldenrod, MaximumSize = new Size(780, 0), Margin = new Padding(0, 4, 0, 0) };
    private readonly NumericUpDown _defaultPageSize = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSize = CreateNumericInput();
    private readonly ListBox _staticOrderList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly FlowLayoutPanel _createRequiredList = CreateChoicePanel();
    private readonly TextBox _updateRequiredText = CreateReadOnlyTextBox();
    private readonly TextBox _businessComponentText = CreateReadOnlyTextBox();
    private readonly CheckBox _enableBusinessComponentCheck = new() { AutoSize = true, Text = "Habilitar Business Component agora", Dock = DockStyle.Top };
    private readonly TextBox _summaryDecisionText = CreateReadOnlyTextBox();
    private readonly TextBox _summaryEndpointText = CreateReadOnlyTextBox();
    private readonly CheckBox _generateSdtsCheck = new() { AutoSize = true, Text = "Confirmar: Criar ou validar estruturas de dados ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateProceduresCheck = new() { AutoSize = true, Text = "Confirmar: Criar ou validar Procedures ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateApiObjectCheck = new() { AutoSize = true, Text = "Confirmar: Criar ou validar API Object ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateMetadataCheck = new() { AutoSize = true, Text = "Confirmar: Gravar metadata da API ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _applyBusinessComponentCheck = new() { AutoSize = true, Text = "Completar Get/Create/Update REST ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _applyListCheck = new() { AutoSize = true, Text = "Completar listagem ao concluir", Dock = DockStyle.Top };
    private readonly TextBox _sdtGenerationText = CreateReadOnlyTextBox();
    private readonly TextBox _procedureGenerationText = CreateReadOnlyTextBox();
    private readonly TextBox _apiObjectGenerationText = CreateReadOnlyTextBox();
    private readonly TextBox _listGenerationText = CreateReadOnlyTextBox();
    private readonly TextBox _metadataGenerationText = CreateReadOnlyTextBox();
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
    private Button? _backButton;
    private bool _showingSummary;
    private bool _loadingSnapshot;
    private bool _servicesBasePathEditedManually;
    private bool _businessComponentEnabledDuringWizard;
    private bool _suppressGenerationPreviewRefresh;
    private bool _refreshGenerationPreviewRunning;
    private ApiPlanGenerationState? _cachedGenerationState;
    private string? _cachedGenerationFingerprint;
    private bool _apiObjectOwnershipDiagnosticWritten;
    private bool _applyBusinessComponentWhenReady;
    private string _generationContext = "Plano da Transaction ainda nao consultado na KB.";
    private readonly ComboBox _requestLevelSelector = CreateLevelComboBox();
    private readonly ComboBox _responseLevelSelector = CreateLevelComboBox();
    private readonly ComboBox _requiredLevelSelector = CreateLevelComboBox();
    private readonly CheckBox _includeLevelCheckRequests = new() { AutoSize = true };
    private readonly CheckBox _includeLevelCheckResponse = new() { AutoSize = true };
    private readonly CheckBox _includeLevelCheckRequired = new() { AutoSize = true };
    private readonly CheckBox _listCountCheckRequests = new() { AutoSize = true };
    private readonly CheckBox _listCountCheckResponse = new() { AutoSize = true };
    private readonly CheckBox _listCountCheckRequired = new() { AutoSize = true };
    private readonly Label _depthWarningRequests = CreateDepthWarningLabel();
    private readonly Label _depthWarningResponse = CreateDepthWarningLabel();
    private readonly Label _depthWarningRequired = CreateDepthWarningLabel();
    private readonly FlowLayoutPanel _levelCreateFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _levelUpdateFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _levelResponseFieldsList = CreateChoicePanel();
    private readonly FlowLayoutPanel _levelCreateRequiredList = CreateChoicePanel();
    private readonly Panel _requestHeaderBody = new() { Dock = DockStyle.Fill };
    private readonly Panel _requestLevelBody = new() { Dock = DockStyle.Fill };
    private readonly Panel _responseHeaderBody = new() { Dock = DockStyle.Fill };
    private readonly Panel _responseLevelBody = new() { Dock = DockStyle.Fill };
    private readonly Panel _requiredHeaderBody = new() { Dock = DockStyle.Fill };
    private readonly Panel _requiredLevelBody = new() { Dock = DockStyle.Fill };
    private readonly List<Control> _levelBars = new();
    private ApiPlanHierarchicalWizardSelection? _hierarchicalSelection;
    private string _currentLevelPathKey = string.Empty;
    private bool _syncingLevelUi;

    public PrototypeWizardDialog(KBModel designModel, Transaction transaction, PrototypeWizardContractSnapshot snapshot, PrototypeBusinessComponentSnapshot businessComponentSnapshot, PrototypeWizardPreferences preferences, Func<bool> enableBusinessComponent, Action<string> writeBusinessComponentOutput, ExtensionTexts texts)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _designModel = designModel ?? throw new ArgumentNullException(nameof(designModel));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _businessComponentSnapshot = businessComponentSnapshot ?? throw new ArgumentNullException(nameof(businessComponentSnapshot));
        _preferences = preferences?.Clone() ?? throw new ArgumentNullException(nameof(preferences));
        _enableBusinessComponent = enableBusinessComponent ?? throw new ArgumentNullException(nameof(enableBusinessComponent));
        _writeBusinessComponentOutput = writeBusinessComponentOutput ?? throw new ArgumentNullException(nameof(writeBusinessComponentOutput));

        Text = _texts.WizardTitle;
        StartPosition = FormStartPosition.CenterParent;
        Width = 1200;
        Height = 912;
        MinimumSize = new Size(900, 640);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        ApplyLocalizedText();
        _generationContext = _texts.Translate("Plano da Transaction ainda nao consultado na KB.");
        BuildLayout();
        WirePathSynchronization();
        LoadSnapshot();
        WireGenerationConfirmation();
        WireServiceSelectionRefresh();
        WireBusinessComponentErrorMessageWarning();
        ApplyWizardPreferences();
    }

    public PrototypeWizardFlowSelection? Selection { get; private set; }

    public bool BusinessComponentEnabledDuringWizard => _businessComponentEnabledDuringWizard;

    private void ApplyLocalizedText()
    {
        _securityAuthenticationRadio.Text = _texts.Translate("Authentication");
        _securityAuthorizationRadio.Text = _texts.Translate("Authorization");
        _securityNoneRadio.Text = _texts.Translate("None");
        _includeBcErrorMessagesCheck.Text = _texts.Translate("Incluir mensagens de erro do Business Component no corpo HTTP 422");
        _bcErrorMessagesWarningLabel.Text = _texts.Translate("Com Security Level = None a API e publica: as mensagens de regra de negocio da KB ficam visiveis no JSON de erro.");
        _enableBusinessComponentCheck.Text = _texts.Translate("Habilitar Business Component agora");
        _generateSdtsCheck.Text = _texts.Translate("Confirmar: Criar ou validar estruturas de dados ao concluir");
        _generateProceduresCheck.Text = _texts.Translate("Confirmar: Criar ou validar Procedures ao concluir");
        _generateApiObjectCheck.Text = _texts.Translate("Confirmar: Criar ou validar API Object ao concluir");
        _generateMetadataCheck.Text = _texts.Translate("Confirmar: Gravar metadata da API ao concluir");
        _applyBusinessComponentCheck.Text = _texts.Translate("Completar Get/Create/Update REST ao concluir");
        _applyListCheck.Text = _texts.Translate("Completar listagem ao concluir");
        _includeLevelCheckRequests.Text = _texts.Translate("Incluir este subnível");
        _includeLevelCheckResponse.Text = _texts.Translate("Incluir este subnível");
        _includeLevelCheckRequired.Text = _texts.Translate("Incluir este subnível");
        _listCountCheckRequests.Text = _texts.Translate("Incluir contador no List");
        _listCountCheckResponse.Text = _texts.Translate("Incluir contador no List");
        _listCountCheckRequired.Text = _texts.Translate("Incluir contador no List");
        var depthWarning = _texts.Translate(ApiPlanHierarchicalWizardSelection.DepthWarningText);
        _depthWarningRequests.Text = depthWarning;
        _depthWarningResponse.Text = depthWarning;
        _depthWarningRequired.Text = depthWarning;
    }

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

        _tabs.TabPages.Add(CreateListTab(_texts.Translate("Serviços"), _servicesList, _texts.Translate("Serviços REST do MVP. Todos iniciam habilitados.")));
        _tabs.TabPages.Add(CreateRequestTab());
        _tabs.TabPages.Add(CreateResponseTab());
        _tabs.TabPages.Add(CreateFilterTab());
        _tabs.TabPages.Add(CreateListGenerationTab());
        _tabs.TabPages.Add(CreatePathsTab());
        _tabs.TabPages.Add(CreateSecurityTab());
        _tabs.TabPages.Add(CreatePaginationTab());
        _tabs.TabPages.Add(CreateOrderTab());
        _tabs.TabPages.Add(CreateRequiredTab());
        _tabs.TabPages.Add(CreateSdtGenerationTab());
        _tabs.TabPages.Add(CreateProcedureGenerationTab());
        _tabs.TabPages.Add(CreateApiObjectGenerationTab());
        _tabs.TabPages.Add(CreateBusinessComponentTab());
        _tabs.TabPages.Add(CreateMetadataGenerationTab());
        _tabs.TabPages.Add(CreateSummaryTab());
        _tabs.SelectedIndexChanged += (_, _) => HandleSelectedTabChanged();
        RefreshCurrentTabLabel();
        root.Controls.Add(_tabs, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0),
        };

        var next = CreateButton(_texts.Next);
        _nextButton = next;
        next.Click += (_, _) => AcceptSelection();
        var cancel = CreateButton(_texts.Cancel);
        cancel.Click += (_, _) => CancelWizard();
        var back = CreateButton(_texts.Back);
        _backButton = back;
        back.Click += (_, _) => GoBack();

        buttons.Controls.Add(next);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(back);
        root.Controls.Add(buttons, 0, 2);
        RefreshBackButton();

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
        var currentTab = string.IsNullOrWhiteSpace(tabName) ? _texts.Translate("<nenhuma>") : tabName;
        _headerLabel.Text = $"{_texts.Translate("Wizard")}: Module '{_snapshot.ModuleName}' | Transaction '{_snapshot.TransactionName}' | {_generationContext} | {_texts.Translate("Aba atual")}: {currentTab}";
        RefreshBackButton();
    }

    private void RefreshBackButton()
    {
        if (_backButton is null)
        {
            return;
        }

        _backButton.Visible = _tabs.SelectedIndex > 0;
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

    private static Label CreateWrappingLabel(string text, int minimumHeight = 32, int topPadding = 0, int bottomPadding = 8)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, minimumHeight),
            Text = text,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, topPadding, 0, bottomPadding),
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
        panel.Controls.Add(CreateWrappingLabel(description), 0, 0);
        panel.Controls.Add(list, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private static ComboBox CreateLevelComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            // DropDownHeight é recalculado em BindLevelSelector / AdjustLevelComboDropDownHeight
            // para aproveitar o espaço vertical do diálogo (ex.: Empresa com 13+ níveis).
            IntegralHeight = false,
        };
    }

    /// <summary>
    /// Expande a lista do seletor de nível até caber todos os itens ou ~55% da altura do diálogo.
    /// </summary>
    private void AdjustLevelComboDropDownHeight(ComboBox selector)
    {
        if (selector is null || selector.Items.Count == 0)
        {
            return;
        }

        var itemHeight = selector.ItemHeight > 0
            ? selector.ItemHeight
            : Math.Max(16, selector.Font.Height + 2);
        var needed = (selector.Items.Count * itemHeight) + 4;
        var maxByDialog = Math.Max(itemHeight * 6, (int)(ClientSize.Height * 0.55));
        selector.DropDownHeight = Math.Min(needed, maxByDialog);
    }

    private static Label CreateDepthWarningLabel()
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkGoldenrod,
            MaximumSize = new Size(1100, 0),
            Padding = new Padding(0, 4, 0, 4),
            Visible = false,
        };
    }

    private Control CreateLevelBar(ComboBox selector, CheckBox includeLevel, CheckBox includeCount, Label warning)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 0, 0, 8),
            Visible = false,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var row = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 3,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.Controls.Add(selector, 0, 0);
        row.Controls.Add(includeLevel, 1, 0);
        row.Controls.Add(includeCount, 2, 0);
        panel.Controls.Add(row, 0, 0);
        panel.Controls.Add(warning, 0, 1);
        selector.SelectedIndexChanged += (_, _) => HandleLevelSelectorChanged(selector);
        selector.DropDown += (_, _) => AdjustLevelComboDropDownHeight(selector);
        includeLevel.CheckedChanged += (_, _) => HandleIncludeLevelChanged(includeLevel);
        includeCount.CheckedChanged += (_, _) => HandleIncludeListCountChanged(includeCount);
        _levelBars.Add(panel);
        return panel;
    }

    private TabPage CreateRequestTab()
    {
        var tab = new TabPage(_texts.Translate("Requests"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateLevelBar(_requestLevelSelector, _includeLevelCheckRequests, _listCountCheckRequests, _depthWarningRequests), 0, 0);

        var headerSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        headerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        headerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        headerSplit.Controls.Add(CreateGroup(_texts.RoleLabel("CreateRequest"), _createFieldsList), 0, 0);
        headerSplit.Controls.Add(CreateGroup(_texts.RoleLabel("UpdateRequest"), _updateFieldsList), 1, 0);
        _requestHeaderBody.Controls.Add(headerSplit);

        var levelSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        levelSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        levelSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        levelSplit.Controls.Add(CreateGroup(_texts.RoleLabel("CreateRequest"), _levelCreateFieldsList), 0, 0);
        levelSplit.Controls.Add(CreateGroup(_texts.RoleLabel("UpdateRequest"), _levelUpdateFieldsList), 1, 0);
        _requestLevelBody.Controls.Add(levelSplit);
        _requestLevelBody.Visible = false;

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_requestLevelBody);
        body.Controls.Add(_requestHeaderBody);
        panel.Controls.Add(body, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateResponseTab()
    {
        var tab = new TabPage(_texts.RoleLabel("Response"));
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
        panel.Controls.Add(CreateLevelBar(_responseLevelSelector, _includeLevelCheckResponse, _listCountCheckResponse, _depthWarningResponse), 0, 0);
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Campos devolvidos no response principal.")), 0, 1);

        _responseHeaderBody.Controls.Add(_responseFieldsList);
        _responseLevelBody.Controls.Add(_levelResponseFieldsList);
        _responseLevelBody.Visible = false;

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_responseLevelBody);
        body.Controls.Add(_responseHeaderBody);
        panel.Controls.Add(body, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateFilterTab()
    {
        var tab = new TabPage(_texts.Translate("Filtros List"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Filtros candidatos para o serviço List.")), 0, 0);
        panel.Controls.Add(_filtersList, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreatePathsTab()
    {
        var tab = new TabPage(_texts.Translate("Paths"));
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
        AddField(fields, 0, _texts.Translate("Nome API"), _apiNameText);
        AddField(fields, 1, _texts.Translate("Services base path"), _servicesBasePathText);
        AddField(fields, 2, "RestPath", _restPathText);

        panel.Controls.Add(fields, 0, 0);
        panel.Controls.Add(CreateGroup(_texts.Translate("Paths dos serviços"), _endpointsText), 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSecurityTab()
    {
        var tab = new TabPage(_texts.Translate("Segurança"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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

        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Security Level único aplicado aos serviços gerados no MVP.")), 0, 0);
        panel.Controls.Add(options, 0, 1);
        panel.Controls.Add(CreateWrappingLabel(
            _texts.Translate("Authentication inicia selecionado por segurança. Authorization exige permissões GAM coerentes. None deixa a API pública e exigirá confirmação antes da geração."),
            44,
            12,
            0), 0, 2);
        panel.Controls.Add(_includeBcErrorMessagesCheck, 0, 3);
        panel.Controls.Add(_bcErrorMessagesWarningLabel, 0, 4);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreatePaginationTab()
    {
        var tab = new TabPage(_texts.Translate("Paginação"));
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
        AddField(fields, 0, _texts.Translate("Default Page Size"), _defaultPageSize);
        AddField(fields, 1, _texts.Translate("Maximum Page Size"), _maximumPageSize);
        tab.Controls.Add(fields);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage(_texts.Translate("Ordenação"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Ordenação estática inicial. A chave primária completa é acrescentada como desempate ascendente.")), 0, 0);
        panel.Controls.Add(_staticOrderList, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateRequiredTab()
    {
        var tab = new TabPage(_texts.Translate("Obrigatórios"));
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
        panel.Controls.Add(CreateLevelBar(_requiredLevelSelector, _includeLevelCheckRequired, _listCountCheckRequired, _depthWarningRequired), 0, 0);
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Required marca membro obrigatório no payload: Create/Update respondem 400 quando ele chega ausente ou com o valor default do tipo (vazio, false ou 0). Chave primária não autonumerada inicia opcional no Create; marque aqui se quiser exigir o valor no payload. Em subnível, a marcação fica só na UI nesta frente: o writer ainda valida Required apenas no cabeçalho."), 64), 0, 1);

        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var createGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = $"{_texts.RoleLabel("CreateRequest")} - {_texts.Translate("Obrigatório no payload (editável)")}",
            Padding = new Padding(8),
        };
        createGroup.Controls.Add(_createRequiredList);

        var updateGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = $"{_texts.RoleLabel("UpdateRequest")} - {_texts.Translate("Obrigatório no payload")}",
            Padding = new Padding(8),
        };
        updateGroup.Controls.Add(_updateRequiredText);
        headerPanel.Controls.Add(createGroup, 0, 0);
        headerPanel.Controls.Add(updateGroup, 0, 1);
        _requiredHeaderBody.Controls.Add(headerPanel);

        var levelCreateGroup = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = $"{_texts.RoleLabel("CreateRequest")} - {_texts.Translate("Obrigatório no payload (editável)")}",
            Padding = new Padding(8),
        };
        levelCreateGroup.Controls.Add(_levelCreateRequiredList);
        _requiredLevelBody.Controls.Add(levelCreateGroup);
        _requiredLevelBody.Visible = false;

        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.Add(_requiredLevelBody);
        body.Controls.Add(_requiredHeaderBody);
        panel.Controls.Add(body, 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private string FormatRequiredDecision(PrototypeWizardRequiredFieldDecision item)
    {
        return $"{item.FieldName}: Required={item.IsRequired} | {_texts.Translate(item.Reason)}";
    }

    private TabPage CreateBusinessComponentTab()
    {
        var tab = new TabPage(_texts.Translate("Business Component"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Business Component preserva as regras da Transaction. A confirmação abaixo completa Get, Create e Update nas Procedures já geradas e sincroniza o API Object; não cria novos objetos."), 52), 0, 0);
        panel.Controls.Add(_enableBusinessComponentCheck, 0, 1);
        panel.Controls.Add(_applyBusinessComponentCheck, 0, 2);
        panel.Controls.Add(_businessComponentText, 0, 3);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSdtGenerationTab()
    {
        var tab = new TabPage(_texts.Translate("SDTs"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Revise os SDTs planejados. A escrita so sera executada ao concluir o wizard se esta confirmacao estiver marcada e o preflight tecnico estiver OK."), 44), 0, 0);
        panel.Controls.Add(_generateSdtsCheck, 0, 1);
        panel.Controls.Add(CreateGroup(_texts.Translate("SDTs planejados"), _sdtGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateProcedureGenerationTab()
    {
        var tab = new TabPage(_texts.Translate("Procedures"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Revise as Procedures planejadas. Esta etapa depende das estruturas de dados ja confirmadas ou reencontraveis na KB ativa."), 44), 0, 0);
        panel.Controls.Add(_generateProceduresCheck, 0, 1);
        panel.Controls.Add(CreateGroup(_texts.Translate("Procedures planejadas"), _procedureGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateApiObjectGenerationTab()
    {
        var tab = new TabPage(_texts.Translate("API Object"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Revise o API Object planejado. Esta etapa depende das estruturas de dados e das Procedures ja confirmadas ou reencontraveis na KB ativa."), 44), 0, 0);
        panel.Controls.Add(_generateApiObjectCheck, 0, 1);
        panel.Controls.Add(CreateGroup(_texts.Translate("API Object planejado"), _apiObjectGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }
    private TabPage CreateMetadataGenerationTab()
    {
        var tab = new TabPage(_texts.Translate("Metadata"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Revise o File JSON de metadata. A gravação depende do API Object próprio já confirmado ou reencontrado."), 44), 0, 0);
        panel.Controls.Add(_generateMetadataCheck, 0, 1);
        panel.Controls.Add(CreateGroup(_texts.Translate("File de metadata planejado"), _metadataGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateListGenerationTab()
    {
        var tab = new TabPage(_texts.Translate("List"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Revise a listagem da API. A conclusão atualiza a Procedure de listagem e sincroniza o API Object com parâmetros de página, filtros e retorno paginado."), 44), 0, 0);
        panel.Controls.Add(_applyListCheck, 0, 1);
        panel.Controls.Add(CreateGroup(_texts.Translate("List planejado"), _listGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage(_texts.Translate("Resumo"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Resumo das decisões acumuladas para montagem do ApiPlan em memória.")), 0, 0);

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.Controls.Add(CreateGroup(_texts.Translate("Decisões"), _summaryDecisionText), 0, 0);
        split.Controls.Add(CreateGroup(_texts.Translate("Endpoints e garantias"), _summaryEndpointText), 1, 0);

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
            AddChoice(_createFieldsList, new ChoiceItem(attribute.Name, attribute.IsPayloadEligible, label, _texts.Translate(attribute.PayloadDisabledReason), _texts.Translate("Bloqueado - Motivo: ")), attribute.DefaultCreateSelected);
            AddChoice(_updateFieldsList, new ChoiceItem(attribute.Name, attribute.IsUpdatePayloadEligible, label, _texts.Translate(attribute.UpdatePayloadDisabledReason), _texts.Translate("Bloqueado - Motivo: ")), attribute.DefaultUpdateSelected);
            AddChoice(_responseFieldsList, new ChoiceItem(attribute.Name, true, label), attribute.DefaultResponseSelected);
            AddChoice(_filtersList, new ChoiceItem(attribute.Name, attribute.IsFilterEligible, FormatFilter(attribute), _texts.Translate(attribute.FilterDisabledReason), _texts.Translate("Bloqueado - Motivo: ")), attribute.DefaultFilterSelected);
        }

        _loadingSnapshot = true;
        var existingApi = _snapshot.ExistingApiContract;
        var apiName = existingApi.ApiName ?? "api" + _snapshot.TransactionName;
        var servicesBasePath = existingApi.ServicesBasePath ?? apiName;
        _apiNameText.Text = apiName;
        _servicesBasePathText.Text = servicesBasePath;
        _restPathText.Text = existingApi.RestPath ?? "/" + ToKebabCase(_snapshot.TransactionName);
        _servicesBasePathEditedManually = !string.Equals(apiName, servicesBasePath, StringComparison.Ordinal);
        _loadingSnapshot = false;

        _defaultPageSize.Value = ClampNumeric(_defaultPageSize, existingApi.DefaultPageSize ?? 50);
        _maximumPageSize.Value = ClampNumeric(_maximumPageSize, existingApi.MaximumPageSize ?? 200);
        if (!string.IsNullOrWhiteSpace(existingApi.SecurityLevel))
        {
            ApplySecurityPreference(existingApi.SecurityLevel ?? "Authentication");
        }

        _includeBcErrorMessagesCheck.Checked = existingApi.IncludeBusinessComponentErrorMessages;

        foreach (var item in GetStaticOrder())
        {
            _staticOrderList.Items.Add($"{item.Order}. {item.AttributeName} {item.Direction}");
        }

        RefreshEndpointsText();
        RefreshRequiredText();
        RefreshBusinessComponentText();
        TryLoadHierarchicalSelection();
    }

    private void WireGenerationConfirmation()
    {
        _generateSdtsCheck.CheckedChanged += (_, _) =>
        {
            if (!_generateSdtsCheck.Checked)
            {
                _generateProceduresCheck.Checked = false;
            }

            _generateProceduresCheck.Enabled = _generateSdtsCheck.Checked;
            RefreshGenerationPreviewUnlessSuppressed();
        };
        _generateProceduresCheck.Enabled = false;
        _generateProceduresCheck.CheckedChanged += (_, _) => RefreshGenerationPreviewUnlessSuppressed();
        _generateApiObjectCheck.Enabled = false;
        _generateApiObjectCheck.CheckedChanged += (_, _) => RefreshGenerationPreviewUnlessSuppressed();
        _applyListCheck.Enabled = false;
        _applyListCheck.CheckedChanged += (_, _) => RefreshGenerationPreviewUnlessSuppressed();
        _generateMetadataCheck.Enabled = false;
        _generateMetadataCheck.CheckedChanged += (_, _) => RefreshGenerationPreviewUnlessSuppressed();
        _enableBusinessComponentCheck.CheckedChanged += (_, _) => RefreshGenerationPreviewUnlessSuppressed();
        _applyBusinessComponentCheck.CheckedChanged += (_, _) =>
        {
            if (_applyBusinessComponentCheck.Enabled)
            {
                _applyBusinessComponentWhenReady = _applyBusinessComponentCheck.Checked;
            }

            RefreshGenerationPreviewUnlessSuppressed();
        };
    }

    private void WireServiceSelectionRefresh()
    {
        foreach (var check in _servicesList.Controls.OfType<CheckBox>())
        {
            check.CheckedChanged += (_, _) =>
            {
                RefreshEndpointsText();
                RefreshGenerationPreviewUnlessSuppressed();
            };
        }
    }

    private void ApplyWizardPreferences()
    {
        _suppressGenerationPreviewRefresh = true;
        try
        {
        if (!_snapshot.ExistingApiContract.HasExistingApi)
        {
            ApplyServicePreference("List", _preferences.ListServiceByDefault);
            ApplyServicePreference("Get", _preferences.GetServiceByDefault);
            ApplyServicePreference("Create", _preferences.CreateServiceByDefault);
            ApplyServicePreference("Update", _preferences.UpdateServiceByDefault);
            ApplySecurityPreference(_preferences.SecurityLevelByDefault);
            _defaultPageSize.Value = ClampNumeric(_defaultPageSize, _preferences.DefaultPageSizeByDefault);
            _maximumPageSize.Value = ClampNumeric(_maximumPageSize, _preferences.MaximumPageSizeByDefault);
            _includeBcErrorMessagesCheck.Checked = _preferences.IncludeBusinessComponentErrorMessagesByDefault;
        }
            RefreshEndpointsText();
            RefreshRequiredText();
            ApplyPreference(_generateSdtsCheck, _preferences.GenerateSdtsByDefault);
            ApplyPreference(_generateProceduresCheck, _preferences.GenerateProceduresByDefault);
            ApplyPreference(_generateApiObjectCheck, _preferences.GenerateApiObjectByDefault);
            _applyBusinessComponentWhenReady = _preferences.ApplyBusinessComponentByDefault;
            ApplyPreference(_applyBusinessComponentCheck, _preferences.ApplyBusinessComponentByDefault);
            ApplyPreference(_applyListCheck, _preferences.ApplyListByDefault);
            ApplyPreference(_generateMetadataCheck, _preferences.GenerateMetadataByDefault);
        }
        finally
        {
            _suppressGenerationPreviewRefresh = false;
        }

        RefreshGenerationPreview(forceRefresh: true);
    }

    private void ApplyServicePreference(string serviceName, bool preferredChecked)
    {
        foreach (var check in _servicesList.Controls.OfType<CheckBox>())
        {
            if (check.Tag is ChoiceItem item && string.Equals(item.Value, serviceName, StringComparison.OrdinalIgnoreCase) && item.Enabled)
            {
                check.Checked = preferredChecked;
                return;
            }
        }
    }

    private void ApplySecurityPreference(string securityLevel)
    {
        var normalized = PrototypeWizardPreferences.NormalizeSecurityLevel(securityLevel);
        _securityAuthorizationRadio.Checked = string.Equals(normalized, PrototypeWizardPreferences.SecurityLevelAuthorization, StringComparison.Ordinal);
        _securityNoneRadio.Checked = string.Equals(normalized, PrototypeWizardPreferences.SecurityLevelNone, StringComparison.Ordinal);
        _securityAuthenticationRadio.Checked = string.Equals(normalized, PrototypeWizardPreferences.SecurityLevelAuthentication, StringComparison.Ordinal);
        RefreshBusinessComponentErrorMessageWarning();
    }

    private void WireBusinessComponentErrorMessageWarning()
    {
        _includeBcErrorMessagesCheck.CheckedChanged += (_, _) => RefreshBusinessComponentErrorMessageWarning();
        _securityAuthenticationRadio.CheckedChanged += (_, _) => RefreshBusinessComponentErrorMessageWarning();
        _securityAuthorizationRadio.CheckedChanged += (_, _) => RefreshBusinessComponentErrorMessageWarning();
        _securityNoneRadio.CheckedChanged += (_, _) => RefreshBusinessComponentErrorMessageWarning();
        RefreshBusinessComponentErrorMessageWarning();
    }

    private void RefreshBusinessComponentErrorMessageWarning()
    {
        _bcErrorMessagesWarningLabel.Visible = _includeBcErrorMessagesCheck.Checked
            && string.Equals(GetSelectedSecurityLevel(), PrototypeWizardPreferences.SecurityLevelNone, StringComparison.Ordinal);
    }

    private static decimal ClampNumeric(NumericUpDown input, int value)
    {
        return Math.Max(input.Minimum, Math.Min(input.Maximum, value));
    }

    private static void ApplyPreference(CheckBox checkBox, bool preferredChecked)
    {
        if (preferredChecked)
        {
            checkBox.Checked = true;
        }
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

    private void TryLoadHierarchicalSelection()
    {
        try
        {
            var snapshot = TransactionStructureReader.Read(_transaction);
            if (snapshot.RootLevel.ChildLevels.Count == 0)
            {
                return;
            }

            _hierarchicalSelection = ApiPlanHierarchicalWizardSelection.CreateDefault(snapshot.RootLevel);
            var persistedRoot = _snapshot.ExistingApiContract.PersistedHierarchicalRoot;
            if (persistedRoot is not null)
            {
                _hierarchicalSelection.ApplyPersistedPrune(persistedRoot);
            }
            foreach (var bar in _levelBars)
            {
                bar.Visible = true;
            }

            _syncingLevelUi = true;
            try
            {
                BindLevelSelector(_requestLevelSelector);
                BindLevelSelector(_responseLevelSelector);
                BindLevelSelector(_requiredLevelSelector);
            }
            finally
            {
                _syncingLevelUi = false;
            }

            SelectLevel(_hierarchicalSelection.RootPathKey);
        }
        catch (Exception ex)
        {
            _hierarchicalSelection = null;
            foreach (var bar in _levelBars)
            {
                bar.Visible = false;
            }

            var detail = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
            var message =
                "[Genexus Open API Builder][Wizard] Falha ao ler a estrutura hierárquica da Transaction '" +
                _transaction.Name +
                "'. O Wizard segue no caminho de nível único (sem subníveis). Detalhe: " +
                detail;
            _writeBusinessComponentOutput(message);
            MessageBox.Show(
                this,
                _texts.Translate(
                    "Não foi possível ler os subníveis desta Transaction. O Wizard continua só com o cabeçalho. Detalhe: ") +
                detail,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void BindLevelSelector(ComboBox selector)
    {
        selector.Items.Clear();
        if (_hierarchicalSelection is null)
        {
            return;
        }

        foreach (var option in _hierarchicalSelection.Options)
        {
            selector.Items.Add(option);
        }

        AdjustLevelComboDropDownHeight(selector);
    }

    private void HandleLevelSelectorChanged(ComboBox sender)
    {
        if (_syncingLevelUi ||
            _hierarchicalSelection is null ||
            sender.SelectedItem is not ApiPlanHierarchicalWizardSelection.LevelNode node)
        {
            return;
        }

        FlushCurrentSublevelFromUi();
        SelectLevel(node.PathKey);
        RefreshGenerationPreviewUnlessSuppressed();
    }

    private void HandleIncludeLevelChanged(CheckBox sender)
    {
        if (_syncingLevelUi || _hierarchicalSelection is null || string.IsNullOrEmpty(_currentLevelPathKey))
        {
            return;
        }

        FlushCurrentSublevelFromUi();
        _hierarchicalSelection.SetLevelIncluded(_currentLevelPathKey, sender.Checked);
        _syncingLevelUi = true;
        try
        {
            _includeLevelCheckRequests.Checked = sender.Checked;
            _includeLevelCheckResponse.Checked = sender.Checked;
            _includeLevelCheckRequired.Checked = sender.Checked;
        }
        finally
        {
            _syncingLevelUi = false;
        }

        SelectLevel(_currentLevelPathKey);
        RefreshGenerationPreviewUnlessSuppressed();
    }

    private void HandleIncludeListCountChanged(CheckBox sender)
    {
        if (_syncingLevelUi || _hierarchicalSelection is null || string.IsNullOrEmpty(_currentLevelPathKey))
        {
            return;
        }

        _hierarchicalSelection.SetIncludeListCount(_currentLevelPathKey, sender.Checked);
        _syncingLevelUi = true;
        try
        {
            _listCountCheckRequests.Checked = sender.Checked;
            _listCountCheckResponse.Checked = sender.Checked;
            _listCountCheckRequired.Checked = sender.Checked;
        }
        finally
        {
            _syncingLevelUi = false;
        }

        RefreshGenerationPreviewUnlessSuppressed();
    }

    private void FlushCurrentSublevelFromUi()
    {
        if (_hierarchicalSelection is null || string.IsNullOrEmpty(_currentLevelPathKey))
        {
            return;
        }

        var node = _hierarchicalSelection.GetNode(_currentLevelPathKey);
        if (node.IsRoot)
        {
            return;
        }

        _hierarchicalSelection.ReplaceSelectedFields(_currentLevelPathKey, "CreateRequest", GetCheckedValues(_levelCreateFieldsList));
        _hierarchicalSelection.ReplaceSelectedFields(_currentLevelPathKey, "UpdateRequest", GetCheckedValues(_levelUpdateFieldsList));
        _hierarchicalSelection.ReplaceSelectedFields(_currentLevelPathKey, "Response", GetCheckedValues(_levelResponseFieldsList));
        if (node.CanIncludeListCount)
        {
            _hierarchicalSelection.SetIncludeListCount(_currentLevelPathKey, _listCountCheckRequests.Checked);
        }
    }

    private void SelectLevel(string pathKey)
    {
        if (_hierarchicalSelection is null)
        {
            return;
        }

        _currentLevelPathKey = pathKey;
        var node = _hierarchicalSelection.GetNode(pathKey);
        var included = _hierarchicalSelection.IsLevelIncluded(pathKey);
        var showHeader = node.IsRoot;
        _syncingLevelUi = true;
        try
        {
            SyncSelector(_requestLevelSelector, pathKey);
            SyncSelector(_responseLevelSelector, pathKey);
            SyncSelector(_requiredLevelSelector, pathKey);
            _includeLevelCheckRequests.Visible = !showHeader;
            _includeLevelCheckResponse.Visible = !showHeader;
            _includeLevelCheckRequired.Visible = !showHeader;
            _includeLevelCheckRequests.Checked = included;
            _includeLevelCheckResponse.Checked = included;
            _includeLevelCheckRequired.Checked = included;
            _listCountCheckRequests.Visible = node.CanIncludeListCount;
            _listCountCheckResponse.Visible = node.CanIncludeListCount;
            _listCountCheckRequired.Visible = node.CanIncludeListCount;
            if (node.CanIncludeListCount)
            {
                var includeCount = _hierarchicalSelection.GetIncludeListCount(pathKey);
                _listCountCheckRequests.Checked = includeCount;
                _listCountCheckResponse.Checked = includeCount;
                _listCountCheckRequired.Checked = includeCount;
            }

            var warn = _hierarchicalSelection.WarnUnvalidatedDepth;
            _depthWarningRequests.Visible = warn;
            _depthWarningResponse.Visible = warn;
            _depthWarningRequired.Visible = warn;
            _requestHeaderBody.Visible = showHeader;
            _requestLevelBody.Visible = !showHeader;
            _responseHeaderBody.Visible = showHeader;
            _responseLevelBody.Visible = !showHeader;
            _requiredHeaderBody.Visible = showHeader;
            _requiredLevelBody.Visible = !showHeader;
        }
        finally
        {
            _syncingLevelUi = false;
        }

        if (!showHeader)
        {
            PopulateLevelFieldLists(node, included);
            RefreshLevelRequiredText();
        }
    }

    private static void SyncSelector(ComboBox selector, string pathKey)
    {
        for (var index = 0; index < selector.Items.Count; index++)
        {
            if (selector.Items[index] is ApiPlanHierarchicalWizardSelection.LevelNode node &&
                string.Equals(node.PathKey, pathKey, StringComparison.Ordinal))
            {
                selector.SelectedIndex = index;
                return;
            }
        }
    }

    private void PopulateLevelFieldLists(ApiPlanHierarchicalWizardSelection.LevelNode node, bool included)
    {
        var selection = _hierarchicalSelection;
        if (selection is null)
        {
            return;
        }

        _levelCreateFieldsList.SuspendLayout();
        _levelUpdateFieldsList.SuspendLayout();
        _levelResponseFieldsList.SuspendLayout();
        _levelCreateFieldsList.Controls.Clear();
        _levelUpdateFieldsList.Controls.Clear();
        _levelResponseFieldsList.Controls.Clear();
        foreach (var field in node.Level.Fields)
        {
            var label = FormatLevelField(field);
            var createReason = ApiPlanHierarchicalWizardSelection.FieldDisabledReason(field, "CreateRequest");
            var updateReason = ApiPlanHierarchicalWizardSelection.FieldDisabledReason(field, "UpdateRequest");
            var createEnabled = included && createReason is null;
            var updateEnabled = included && updateReason is null;
            AddChoice(
                _levelCreateFieldsList,
                new ChoiceItem(field.Name, createEnabled, label, createReason is null ? null : _texts.Translate(createReason), _texts.Translate("Bloqueado - Motivo: ")),
                included && selection.IsFieldSelected(node.PathKey, "CreateRequest", field.Name) && createReason is null);
            AddChoice(
                _levelUpdateFieldsList,
                new ChoiceItem(field.Name, updateEnabled, label, updateReason is null ? null : _texts.Translate(updateReason), _texts.Translate("Bloqueado - Motivo: ")),
                included && selection.IsFieldSelected(node.PathKey, "UpdateRequest", field.Name) && updateReason is null);
            AddChoice(
                _levelResponseFieldsList,
                new ChoiceItem(field.Name, included, label),
                included && selection.IsFieldSelected(node.PathKey, "Response", field.Name));
        }

        _levelCreateFieldsList.ResumeLayout();
        _levelUpdateFieldsList.ResumeLayout();
        _levelResponseFieldsList.ResumeLayout();
    }

    private void RefreshLevelRequiredText()
    {
        if (_hierarchicalSelection is null || string.IsNullOrEmpty(_currentLevelPathKey))
        {
            return;
        }

        var node = _hierarchicalSelection.GetNode(_currentLevelPathKey);
        if (node.IsRoot)
        {
            return;
        }

        _levelCreateRequiredList.SuspendLayout();
        _levelCreateRequiredList.Controls.Clear();
        foreach (var fieldName in _hierarchicalSelection.GetSelectedFields(_currentLevelPathKey, "CreateRequest"))
        {
            AddChoice(_levelCreateRequiredList, new ChoiceItem(fieldName, true, fieldName), false);
        }

        _levelCreateRequiredList.ResumeLayout();
    }

    private string FormatLevelField(ApiPlanLevelField field)
    {
        var markers = new List<string>();
        if (field.IsPrimaryKey)
        {
            markers.Add("PK");
        }

        if (field.IsFormula)
        {
            markers.Add(_texts.Translate("Fórmula"));
        }

        if (field.IsNoAccept)
        {
            markers.Add("NoAccept");
        }

        if (field.IsAutonumber)
        {
            markers.Add("Autonumber");
        }

        var suffix = markers.Count == 0 ? string.Empty : " [" + string.Join(", ", markers) + "]";
        return $"{field.Name} ({field.DataType}, {field.Length}.{field.Decimals}){suffix}";
    }

    private string FormatHierarchicalSummary()
    {
        if (_hierarchicalSelection is null || !_hierarchicalSelection.HasSublevels)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(Environment.NewLine);
        builder.Append(_texts.Translate("Subníveis selecionados"));
        builder.Append(": ");
        builder.Append(_hierarchicalSelection.CountSelectedSublevels());
        builder.Append(Environment.NewLine);
        if (_cachedGenerationState is not null)
        {
            builder.Append(_texts.Translate("SDTs planejados"));
            builder.Append(": ");
            builder.Append(_cachedGenerationState.Sdts.Detail);
            builder.Append(Environment.NewLine);
        }

        if (_hierarchicalSelection.WarnUnvalidatedDepth)
        {
            builder.Append(_texts.Translate(ApiPlanHierarchicalWizardSelection.DepthWarningText));
            builder.Append(Environment.NewLine);
        }

        return builder.ToString().TrimEnd();
    }

    private string FormatHierarchicalGuarantee()
    {
        return string.Empty;
    }

    private string FormatAttribute(PrototypeWizardAttributeDecision attribute)
    {
        var markers = new List<string>();
        if (attribute.IsPrimaryKey)
        {
            markers.Add("PK");
        }

        if (attribute.IsDescription)
        {
            markers.Add(_texts.Translate("Description"));
        }

        if (attribute.IsSensitive)
        {
            markers.Add(_texts.Translate("Sensível"));
        }

        if (attribute.IsFormula)
        {
            markers.Add(_texts.Translate("Fórmula"));
        }

        if (attribute.IsNoAccept)
        {
            markers.Add("NoAccept");
        }

        if (attribute.IsAudit)
        {
            markers.Add(_texts.Translate("Auditoria"));
        }

        var suffix = markers.Count == 0 ? string.Empty : " [" + string.Join(", ", markers) + "]";
        return $"{attribute.Name} ({attribute.DataType}, {attribute.Length}.{attribute.Decimals}){suffix}";
    }

    private string FormatFilter(PrototypeWizardAttributeDecision attribute)
    {
        var baseText = FormatAttribute(attribute);
        if (!attribute.IsFilterEligible)
        {
            return baseText;
        }

        var options = new List<string> { _texts.Translate(attribute.FilterOperator) };
        if (attribute.UsesPeriod)
        {
            options.Add(_texts.Translate("Período"));
        }

        if (attribute.UsesRange)
        {
            options.Add(_texts.Translate("Intervalo"));
        }

        return baseText + " -> " + string.Join(" / ", options);
    }

    private void AcceptSelection()
    {
        if (_showingSummary)
        {
            if (!CompletePendingExplicitActions())
            {
                return;
            }

            if (!TryCreateSelection())
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        if (_tabs.SelectedTab?.Text == _texts.Translate("Paths"))
        {
            RefreshEndpointsText();
        }

        if (_tabs.SelectedTab?.Text == _texts.Translate("Obrigatórios"))
        {
            RefreshRequiredText();
        }

        // Preview de geracao: so na troca de aba (HandleSelectedTabChanged) ou ao montar o Resumo.
        // Evita ReadGenerationState duplicado no Próximo entre abas SDTs..Metadata.

        if (_tabs.SelectedTab?.Text == _texts.Translate("Business Component") &&
            !CompletePendingExplicitActions())
        {
            return;
        }

        if (_tabs.SelectedIndex < _tabs.TabPages.Count - 2)
        {
            _tabs.SelectedIndex++;
            if (_tabs.SelectedTab?.Text == _texts.Translate("Obrigatórios"))
            {
                RefreshRequiredText();
            }

            return;
        }

        // Ultimo "Próximo" antes do Resumo (ex.: saindo de Metadata sem clicar na aba):
        // força preview fresco — mesmo contrato do clique direto em Resumo.
        RefreshGenerationPreview(forceRefresh: true);
        if (!CompletePendingExplicitActions() || !TryCreateSelection())
        {
            return;
        }

        ShowSummary();
    }

    private void HandleSelectedTabChanged()
    {
        var tabName = _tabs.SelectedTab?.Text ?? "<null>";
        if (string.Equals(tabName, _texts.Translate("Resumo"), StringComparison.Ordinal))
        {
            // Clique direto (ou navegacao) em Resumo: sempre recalcula estado de geracao
            // e monta o resumo com as selecoes atuais, sem exigir percorrer aba a aba.
            RefreshGenerationPreview(forceRefresh: true);
            if (!_showingSummary && CompletePendingExplicitActions() && TryCreateSelection())
            {
                ShowSummary();
            }

            RefreshCurrentTabLabel();
            return;
        }

        if (ShouldRefreshGenerationPreviewOnTab(tabName))
        {
            RefreshGenerationPreview(forceRefresh: false);
        }

        if (string.Equals(tabName, _texts.Translate("Obrigatórios"), StringComparison.Ordinal))
        {
            if (_hierarchicalSelection is not null &&
                !string.IsNullOrEmpty(_currentLevelPathKey) &&
                !_hierarchicalSelection.GetNode(_currentLevelPathKey).IsRoot)
            {
                RefreshLevelRequiredText();
            }
            else
            {
                RefreshRequiredText();
            }
        }

        if (_showingSummary)
        {
            _showingSummary = false;
            if (_nextButton is not null)
            {
                _nextButton.Text = _texts.Next;
            }
        }

        RefreshCurrentTabLabel();
    }

    private bool ShouldRefreshGenerationPreviewOnTab(string tabName)
    {
        return string.Equals(tabName, _texts.Translate("SDTs"), StringComparison.Ordinal)
            || string.Equals(tabName, _texts.Translate("Procedures"), StringComparison.Ordinal)
            || string.Equals(tabName, _texts.Translate("API Object"), StringComparison.Ordinal)
            || string.Equals(tabName, _texts.Translate("Business Component"), StringComparison.Ordinal)
            || string.Equals(tabName, _texts.Translate("List"), StringComparison.Ordinal)
            || string.Equals(tabName, _texts.Translate("Metadata"), StringComparison.Ordinal);
    }

    private bool CompletePendingExplicitActions()
    {
        if (PrototypeWizardBusinessComponentNavigationPolicy.ShouldRequestEnableBeforeLeavingWizard(IsBusinessComponentReady(), _enableBusinessComponentCheck.Checked))
        {
            if (!EnsureBusinessComponentReady())
            {
                return false;
            }

            RefreshGenerationPreview(forceRefresh: true);
        }

        return true;
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
        FlushCurrentSublevelFromUi();
        var selectedServices = GetCheckedValues(_servicesList);
        if (selectedServices.Count == 0)
        {
            MessageBox.Show(this, _texts.Translate("Selecione ao menos um serviço."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var apiName = _apiNameText.Text.Trim();
        var servicesBasePath = _servicesBasePathText.Text.Trim();
        var restPath = _restPathText.Text.Trim();
        if (apiName.Length == 0 || servicesBasePath.Length == 0 || restPath.Length == 0)
        {
            MessageBox.Show(this, _texts.Translate("Informe Nome API, Services base path e RestPath."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!restPath.StartsWith("/", StringComparison.Ordinal))
        {
            MessageBox.Show(this, _texts.Translate("RestPath deve iniciar com '/'."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_defaultPageSize.Value > _maximumPageSize.Value)
        {
            MessageBox.Show(this, _texts.Translate("Default Page Size deve ser menor ou igual a Maximum Page Size."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            GetStaticOrder(),
            _includeBcErrorMessagesCheck.Checked);
        Selection = new PrototypeWizardFlowSelection(
            contractSelection,
            reviewSelection,
            GetRequiredDecisions(contractSelection),
            CreateBusinessComponentSelection(),
            _generateSdtsCheck.Checked,
            _generateProceduresCheck.Checked,
            _generateApiObjectCheck.Checked,
            _generateMetadataCheck.Checked,
            _applyListCheck.Checked,
            _applyBusinessComponentCheck.Checked && IsBusinessComponentReady(),
            _hierarchicalSelection);
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
            $"{_texts.Translate("Serviços")}: {string.Join(", ", contract.SelectedServices)}{Environment.NewLine}" +
            $"{_texts.RoleLabel("CreateRequest")}: {contract.CreateFields.Count} {_texts.Translate("campo(s)")}, {createRequired} {_texts.Translate("obrigatório(s) no payload")}{Environment.NewLine}" +
            $"{_texts.RoleLabel("UpdateRequest")}: {contract.UpdateFields.Count} {_texts.Translate("campo(s)")}, {updateRequired} {_texts.Translate("obrigatório(s) no payload")}{Environment.NewLine}" +
            $"Required: {_texts.Translate("obrigatório no payload; 400 quando ausente ou com o valor default do tipo (vazio, false ou 0)")}{Environment.NewLine}" +
            $"{_texts.RoleLabel("Response")}: {contract.ResponseFields.Count} {_texts.Translate("campo(s)")}{Environment.NewLine}" +
            $"{_texts.RoleLabel("ListFilters")}: {contract.ListFilters.Count} {_texts.Translate("filtro(s)")}{Environment.NewLine}" +
            $"ApiName: {review.ApiName}{Environment.NewLine}" +
            $"Services base path: {review.ServicesBasePath}{Environment.NewLine}" +
            $"RestPath: {review.RestPath}{Environment.NewLine}" +
            $"{_texts.Translate("Security Level")}: {review.SecurityLevel}{Environment.NewLine}" +
            $"{_texts.Translate("Incluir mensagens de erro do Business Component no corpo HTTP 422")}: {review.IncludeBusinessComponentErrorMessages}{Environment.NewLine}" +
            $"{_texts.Translate("Paginação")}: Default={review.DefaultPageSize}, Maximum={review.MaximumPageSize}{Environment.NewLine}" +
            $"{_texts.Translate("Ordenação")}: {string.Join(", ", review.StaticOrder.Select(item => item.AttributeName + " " + item.Direction))}{Environment.NewLine}" +
            $"{_texts.Translate("Campos bloqueados visíveis")}: CreateRequest={createBlocked}, UpdateRequest={updateBlocked}, ListFilters={filterBlocked}{Environment.NewLine}" +
            $"Business Component: IsBusinessComponent={businessComponent.IsBusinessComponent}, Status='{_texts.Translate(businessComponent.Status)}', EnabledDuringWizard={businessComponent.EnabledDuringWizard}{Environment.NewLine}" +
            $"{_texts.Translate("Criar ou validar estruturas de dados")}: {Selection.GenerateSdts}{Environment.NewLine}" +
            $"{_texts.Translate("Criar ou validar Procedures")}: {Selection.GenerateProcedures}{Environment.NewLine}" +
            $"{_texts.Translate("Criar ou validar API Object")}: {Selection.GenerateApiObject}{Environment.NewLine}" +
            $"{_texts.Translate("Completar listagem")}: {Selection.ApplyList}{Environment.NewLine}" +
            $"{_texts.Translate("Gravar metadata da API")}: {Selection.GenerateMetadata}{Environment.NewLine}" +
            $"{_texts.Translate("Completar Get/Create/Update REST")}: {Selection.ApplyBusinessComponent}{Environment.NewLine}" +
            $"{_texts.Translate("Estado da geracao")}: {_generationContext}" +
            FormatHierarchicalSummary();
        _summaryEndpointText.Text =
            FormatEndpoints(review.RestPath, contract.SelectedServices) + Environment.NewLine + Environment.NewLine +
            _texts.Translate("Campos bloqueados ficam visíveis com motivo no fluxo do wizard.") + Environment.NewLine +
            _texts.Translate("Required marca membro obrigatório no payload: Create/Update respondem 400 quando ele chega ausente ou com o valor default do tipo (vazio, false ou 0).") + Environment.NewLine +
            _texts.Translate("ApiPlan sera montado em memoria ao concluir o wizard.") + Environment.NewLine +
            _texts.Translate("Estruturas de dados, Procedures, API Object, listagem e metadata so serao escritos se as respectivas abas estiverem confirmadas e o preflight tecnico estiver OK.") + Environment.NewLine +
            _texts.Translate("A opção de Business Component completa Get/Create/Update e status HTTP nas Procedures já geradas.") + Environment.NewLine +
            _texts.Translate("A listagem completa a primeira versão paginada do endpoint; a metadata grava o File JSON inicial.") +
            FormatHierarchicalGuarantee();
        _showingSummary = true;
        _tabs.SelectedIndex = _tabs.TabPages.Count - 1;
        RefreshCompletionCaption();
    }
    private void RefreshCompletionCaption()
    {
        if (_nextButton is null)
        {
            return;
        }

        _nextButton.Text = _generateSdtsCheck.Checked || _generateProceduresCheck.Checked || _generateApiObjectCheck.Checked || _generateMetadataCheck.Checked || _applyListCheck.Checked || _applyBusinessComponentCheck.Checked
            ? _texts.CompleteAndApply
            : _texts.CompleteTest;
    }
    private void GoBack()
    {
        if (_showingSummary)
        {
            _showingSummary = false;
            _tabs.SelectedIndex = _tabs.TabPages.Count - 2;
            if (_nextButton is not null)
            {
                _nextButton.Text = _texts.Next;
            }

            return;
        }

        if (_tabs.SelectedIndex <= 0)
        {
            return;
        }

        _tabs.SelectedIndex--;
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
                lines.Add(service + " " + _texts.Translate("<não definido>") + " " + restPath);
            }
        }
        return string.Join(Environment.NewLine, lines);
    }

    private void RefreshGenerationPreviewUnlessSuppressed()
    {
        if (!_suppressGenerationPreviewRefresh)
        {
            RefreshGenerationPreview(forceRefresh: false);
        }
    }

    private void RefreshGenerationPreview(bool forceRefresh)
    {
        if (_refreshGenerationPreviewRunning)
        {
            return;
        }

        _refreshGenerationPreviewRunning = true;
        try
        {
            var fingerprint = BuildGenerationPreviewFingerprint();
            if (!forceRefresh &&
                _cachedGenerationState is not null &&
                string.Equals(_cachedGenerationFingerprint, fingerprint, StringComparison.Ordinal))
            {
                ApplyGenerationPreviewState(_cachedGenerationState);
                WriteApiObjectOwnershipDiagnosticOnce(_cachedGenerationState);
                return;
            }

            var state = ReadGenerationState();
            _cachedGenerationState = state;
            _cachedGenerationFingerprint = fingerprint;
            ApplyGenerationPreviewState(state);
            WriteApiObjectOwnershipDiagnosticOnce(state);
        }
        finally
        {
            _refreshGenerationPreviewRunning = false;
        }
    }

    private string BuildGenerationPreviewFingerprint()
    {
        return string.Join(
            "|",
            string.Join(",", GetCheckedValues(_servicesList)),
            string.Join(",", GetCheckedValues(_createFieldsList)),
            string.Join(",", GetCheckedValues(_updateFieldsList)),
            string.Join(",", GetCheckedValues(_responseFieldsList)),
            string.Join(",", GetCheckedValues(_filtersList)),
            string.Join(",", GetCheckedValues(_createRequiredList)),
            _apiNameText.Text.Trim(),
            _servicesBasePathText.Text.Trim(),
            _restPathText.Text.Trim(),
            GetSelectedSecurityLevel(),
            _defaultPageSize.Value.ToString(),
            _maximumPageSize.Value.ToString(),
            _generateSdtsCheck.Checked ? "1" : "0",
            _generateProceduresCheck.Checked ? "1" : "0",
            _generateApiObjectCheck.Checked ? "1" : "0",
            _generateMetadataCheck.Checked ? "1" : "0",
            _applyListCheck.Checked ? "1" : "0",
            _applyBusinessComponentCheck.Checked ? "1" : "0",
            _enableBusinessComponentCheck.Checked ? "1" : "0",
            IsBusinessComponentReady() ? "1" : "0",
            _hierarchicalSelection?.Fingerprint() ?? string.Empty);
    }

    private void ApplyGenerationPreviewState(ApiPlanGenerationState? state)
    {
        _generationContext = FormatGenerationContext(state);
        RefreshCurrentTabLabel();
        var sdtState = state?.Sdts;
        var procedureState = state?.Procedures;
        var apiState = state?.ApiObject;
        var metadataState = state?.MetadataFile;

        var sdtsAvailable = IsDependencyAvailable(sdtState, _generateSdtsCheck.Checked);
        var proceduresAvailable = IsDependencyAvailable(procedureState, _generateProceduresCheck.Checked);
        var baseApiObjectAvailable = IsDependencyAvailable(apiState, _generateApiObjectCheck.Checked);
        ApplyBusinessComponentControlState(sdtsAvailable, proceduresAvailable, baseApiObjectAvailable);
        var businessComponentConfirmed = _applyBusinessComponentCheck.Checked && IsBusinessComponentReady();
        var apiObjectAvailable = baseApiObjectAvailable || businessComponentConfirmed;
        ApplyGenerationControlState(_generateSdtsCheck, sdtState, true);
        ApplyGenerationControlState(_generateProceduresCheck, procedureState, sdtsAvailable);
        ApplyGenerationControlState(_generateApiObjectCheck, apiState, proceduresAvailable);
        ApplyListControlState(apiState, apiObjectAvailable);
        ApplyGenerationControlState(_generateMetadataCheck, metadataState, apiObjectAvailable);

        _sdtGenerationText.Text = FormatGenerationState(sdtState, _generateSdtsCheck.Checked);
        _procedureGenerationText.Text = FormatGenerationState(procedureState, _generateProceduresCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"{_texts.Translate("Dependencia")} SDTs: {FormatDependencyState(sdtState, _generateSdtsCheck.Checked)}";
        _apiObjectGenerationText.Text = FormatGenerationState(apiState, _generateApiObjectCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"{_texts.Translate("Dependencia")} Procedures: {FormatDependencyState(procedureState, _generateProceduresCheck.Checked)}";
        _listGenerationText.Text = FormatGenerationState(apiState, _applyListCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"{_texts.Translate("Dependencia")} API Object: {FormatDependencyState(apiState, _generateApiObjectCheck.Checked || businessComponentConfirmed)}" + Environment.NewLine +
            $"{_texts.Translate("Filtros planejados")}: {GetCheckedValues(_filtersList).Count}; {_texts.Translate("Paginacao")} Default={_defaultPageSize.Value}, Maximum={_maximumPageSize.Value}.";
        _metadataGenerationText.Text = FormatGenerationState(metadataState, _generateMetadataCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"{_texts.Translate("Dependencia")} List/API Object: {FormatDependencyState(apiState, _generateApiObjectCheck.Checked || businessComponentConfirmed || _applyListCheck.Checked)}";
    }

    private void ApplyBusinessComponentControlState(bool sdtsAvailable, bool proceduresAvailable, bool apiObjectAvailable)
    {
        var hasGetCreateUpdateServices = PrototypeWizardBusinessComponentNavigationPolicy.HasGetCreateUpdateServices(
            GetCheckedValues(_servicesList));
        var shouldApplyWhenAllowed = PrototypeWizardBusinessComponentNavigationPolicy.ResolveApplyBusinessComponentAfterGenerationRefresh(
            IsBusinessComponentReady(),
            _enableBusinessComponentCheck.Checked,
            sdtsAvailable,
            proceduresAvailable,
            apiObjectAvailable,
            hasGetCreateUpdateServices,
            _applyBusinessComponentCheck.Checked,
            _applyBusinessComponentWhenReady);
        var canApplyBusinessComponent = PrototypeWizardBusinessComponentNavigationPolicy.ShouldAllowApplyBusinessComponent(
            IsBusinessComponentReady(),
            _enableBusinessComponentCheck.Checked,
            sdtsAvailable,
            proceduresAvailable,
            apiObjectAvailable,
            hasGetCreateUpdateServices);
        if (!hasGetCreateUpdateServices)
        {
            _applyBusinessComponentCheck.Text = _texts.Translate("Bloqueado: marque Get, Create e Update nos Serviços");
        }
        else if (IsBusinessComponentReady())
        {
            _applyBusinessComponentCheck.Text = canApplyBusinessComponent
                ? _texts.Translate("Confirmar: Completar Get/Create/Update REST ao concluir")
                : _texts.Translate("Bloqueado: confirme SDTs, Procedures e API Object");
        }
        else if (_enableBusinessComponentCheck.Checked)
        {
            _applyBusinessComponentCheck.Text = canApplyBusinessComponent
                ? _texts.Translate("Confirmar: Completar Get/Create/Update REST após habilitar")
                : _texts.Translate("Bloqueado: confirme SDTs, Procedures e API Object antes de aplicar BC");
        }
        else
        {
            _applyBusinessComponentCheck.Text = _texts.Translate("Bloqueado: Business Component desabilitado");
        }

        _applyBusinessComponentCheck.Enabled = canApplyBusinessComponent;
        _applyBusinessComponentCheck.Checked = shouldApplyWhenAllowed;
    }

    private void ApplyBusinessComponentControlState()
    {
        ApplyBusinessComponentControlState(false, false, false);
    }

    private void ApplyListControlState(ApiPlanGenerationStageState? apiState, bool apiObjectAvailable)
    {
        _applyListCheck.Text = _texts.Translate("Confirmar: Completar listagem ao concluir");
        _applyListCheck.Enabled = apiState is not null && !apiState.IsBlocked && apiObjectAvailable;
        if (!_applyListCheck.Enabled)
        {
            _applyListCheck.Checked = false;
        }
    }

    private static bool IsDependencyAvailable(ApiPlanGenerationStageState? state, bool confirmed)
    {
        return confirmed || string.Equals(state?.Action, "Reencontrar e validar", StringComparison.Ordinal);
    }

    private string FormatDependencyState(ApiPlanGenerationStageState? state, bool confirmed)
    {
        if (confirmed)
        {
            return _texts.Translate("confirmada nesta execucao");
        }

        return string.Equals(state?.Action, "Reencontrar e validar", StringComparison.Ordinal)
            ? _texts.Translate("ja reencontrada na KB ativa")
            : _texts.Translate("nao confirmada");
    }
    private string FormatGenerationContext(ApiPlanGenerationState? state)
    {
        if (state is null)
        {
            return _texts.Translate("Estado: plano em memoria");
        }

        var stages = new[] { state.Sdts, state.Procedures, state.ApiObject, state.MetadataFile };
        if (stages.Any(stage => stage.IsBlocked))
        {
            var conflictCount = stages.SelectMany(stage => stage.CollisionConflicts).Count();
            return conflictCount > 0
                ? $"{_texts.Translate("Estado: teste bloqueado")} ({conflictCount} {_texts.Translate("conflito(s)")})"
                : _texts.Translate("Estado: teste bloqueado");
        }

        if (stages.All(stage => string.Equals(stage.Action, "Reencontrar e validar", StringComparison.Ordinal)))
        {
            return _texts.Translate("Estado: teste de reencontro");
        }

        if (stages.All(stage => string.Equals(stage.Action, "Criar", StringComparison.Ordinal)))
        {
            return _texts.Translate("Estado: teste de criacao");
        }

        return _texts.Translate("Estado: teste de complementacao");
    }
    private void ApplyGenerationControlState(CheckBox checkBox, ApiPlanGenerationStageState? state, bool dependencyConfirmed)
    {
        if (state is null)
        {
            checkBox.Text = _texts.Translate("Estado atual indisponivel");
            checkBox.Enabled = false;
            checkBox.Checked = false;
            return;
        }

        checkBox.Text = $"{_texts.Translate("Confirmar")}: {_texts.Translate(state.Action)} {FormatGenerationStageName(state.StageName)} {_texts.Translate("ao concluir")}";
        checkBox.Enabled = !state.IsBlocked && dependencyConfirmed;
        if (!checkBox.Enabled)
        {
            checkBox.Checked = false;
        }
    }

    private string FormatGenerationStageName(string stageName)
    {
        if (string.Equals(stageName, "SDTs", StringComparison.Ordinal))
        {
            return _texts.Translate("estruturas de dados");
        }

        if (string.Equals(stageName, "Metadata File", StringComparison.Ordinal))
        {
            return _texts.Translate("metadata da API");
        }

        return stageName;
    }

    private string FormatGenerationState(ApiPlanGenerationStageState? state, bool confirmed)
    {
        if (state is null)
        {
            return _texts.Translate("Estado atual da KB indisponivel. Ajuste os campos obrigatorios do contrato para consultar a geracao.");
        }

        var localizedDetail = ExtensionOutputLocalization.Translate(state.Detail, _texts.Language);
        return $"{_texts.Translate("Estado atual da KB")}: {_texts.Translate(state.Action)}{Environment.NewLine}{localizedDetail}{Environment.NewLine}{Environment.NewLine}{_texts.Translate("Confirmado para escrita")}: {confirmed}";
    }

    private void WriteApiObjectOwnershipDiagnosticOnce(ApiPlanGenerationState? state)
    {
        if (_apiObjectOwnershipDiagnosticWritten || state is null || !state.ApiObject.IsBlocked)
        {
            return;
        }

        _apiObjectOwnershipDiagnosticWritten = true;
        Package.WriteApiObjectBaselineDiagnostic(_writeBusinessComponentOutput, state);
    }

    private ApiPlanGenerationState? ReadGenerationState()
    {
        FlushCurrentSublevelFromUi();
        var selectedServices = GetCheckedValues(_servicesList);
        if (selectedServices.Count == 0)
        {
            return null;
        }

        try
        {
            var contract = new PrototypeWizardContractSelection(
                _snapshot.TransactionName,
                selectedServices,
                GetCheckedValues(_createFieldsList),
                GetCheckedValues(_updateFieldsList),
                GetCheckedValues(_responseFieldsList),
                GetCheckedValues(_filtersList));
            var apiName = string.IsNullOrWhiteSpace(_apiNameText.Text) ? "api" + _snapshot.TransactionName : _apiNameText.Text.Trim();
            var review = new PrototypeWizardReviewSelection(
                _snapshot.TransactionName,
                apiName,
                string.IsNullOrWhiteSpace(_servicesBasePathText.Text) ? apiName : _servicesBasePathText.Text.Trim(),
                string.IsNullOrWhiteSpace(_restPathText.Text) ? "/" + ToKebabCase(_snapshot.TransactionName) : _restPathText.Text.Trim(),
                GetSelectedSecurityLevel(),
                (int)_defaultPageSize.Value,
                (int)_maximumPageSize.Value,
                GetStaticOrder(),
                _includeBcErrorMessagesCheck.Checked);
            var selection = new PrototypeWizardFlowSelection(
                contract,
                review,
                GetRequiredDecisions(contract),
                CreateBusinessComponentSelection(),
                false,
                false,
                false,
                false,
                false,
                false,
                _hierarchicalSelection);
            return ApiPlanGenerationStateReader.ReadForIntentionalChange(_designModel, _transaction, ApiPlanBuilder.Build(_designModel, _transaction, selection));
        }
        catch
        {
            return null;
        }
    }
    private static string ResolveProcedureBacklogId(string serviceName)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
        {
            return "B050";
        }

        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return "B051";
        }

        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return "B052";
        }

        return string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase) ? "B053" : "B050-B053";
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
        if (_snapshot.ExistingApiContract.StaticOrder.Count > 0)
        {
            return _snapshot.ExistingApiContract.StaticOrder
                .Select(item => new PrototypeWizardStaticOrderPart(item.Order, item.AttributeName, item.Direction))
                .ToArray();
        }

        return _snapshot.Attributes
            .Where(attribute => attribute.IsPrimaryKey)
            .OrderBy(attribute => attribute.Order)
            .Select((attribute, index) => new PrototypeWizardStaticOrderPart(index + 1, attribute.Name, "ASC"))
            .ToArray();
    }
    private void RefreshBusinessComponentText()
    {
        var effectiveStatus = IsBusinessComponentReady()
            ? _texts.Translate("Apta via Business Component")
            : _texts.Translate(_businessComponentSnapshot.Status);
        _businessComponentText.Text =
            $"Transaction: {_businessComponentSnapshot.TransactionName}{Environment.NewLine}" +
            $"IsBusinessComponent: {IsBusinessComponentReady()}{Environment.NewLine}" +
            $"Status: {effectiveStatus}{Environment.NewLine}{Environment.NewLine}" +
            _texts.Translate("Sem Business Component, a habilitação e a aplicação REST de Get/Create/Update ficam bloqueadas. O wizard pode continuar para etapas que não exigem habilitar essa propriedade. A habilitação exige confirmação explícita e altera a Transaction na KB; cancelar o wizard depois disso não reverte automaticamente a propriedade.");
        _enableBusinessComponentCheck.Enabled = !_businessComponentSnapshot.IsBusinessComponent && !_businessComponentEnabledDuringWizard;
        _enableBusinessComponentCheck.Visible = !_businessComponentSnapshot.IsBusinessComponent;
        ApplyBusinessComponentControlState();
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
                MessageBox.Show(this, _texts.Translate("Business Component está desabilitado. Marque a habilitação explícita para continuar ou cancele o wizard."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshBusinessComponentText();
            return false;
        }

        var confirmation = MessageBox.Show(
            this,
            string.Format(
                _texts.Translate("Habilitar Business Component altera a Transaction '{0}' na KB. A alteração não será revertida automaticamente ao cancelar o wizard ou remover a extensão. Deseja habilitar agora?"),
                _businessComponentSnapshot.TransactionName),
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
                MessageBox.Show(this, _texts.Translate("Não foi possível confirmar Business Component habilitado após a gravação."), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshBusinessComponentText();
                return false;
            }
        }
        catch (Exception ex)
        {
            _writeBusinessComponentOutput($"[Genexus Open API Builder][B035] Falha ao habilitar Business Component para Transaction='{_businessComponentSnapshot.TransactionName}': {ex.Message}");
            MessageBox.Show(this, _texts.Translate("Falha ao habilitar Business Component: ") + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        var previousRequired = new HashSet<string>(GetCheckedValues(_createRequiredList), StringComparer.OrdinalIgnoreCase);
        var hadPreviousChoices = _createRequiredList.Controls.Count > 0;

        _createRequiredList.SuspendLayout();
        _createRequiredList.Controls.Clear();
        foreach (var fieldName in selection.CreateFields)
        {
            var defaultRequired = DefaultCreateRequired(fieldName);
            var selected = hadPreviousChoices
                ? previousRequired.Contains(fieldName)
                : defaultRequired;
            AddChoice(
                _createRequiredList,
                new ChoiceItem(fieldName, true, FormatCreateRequiredChoiceLabel(fieldName, selected)),
                selected);
        }

        foreach (var check in _createRequiredList.Controls.OfType<CheckBox>())
        {
            check.CheckedChanged += CreateRequiredCheckChanged;
        }

        _createRequiredList.ResumeLayout();

        var decisions = GetRequiredDecisions(selection);
        _updateRequiredText.Text = string.Join(Environment.NewLine, decisions
            .Where(item => item.RequestName == "UpdateRequest")
            .Select(FormatRequiredDecision));
    }

    private void CreateRequiredCheckChanged(object? sender, EventArgs e)
    {
        if (sender is not CheckBox check || check.Tag is not ChoiceItem item)
        {
            return;
        }

        check.Text = FormatCreateRequiredChoiceLabel(item.Value, check.Checked);
        ResizeChoice(check, _createRequiredList);
    }

    private string FormatCreateRequiredChoiceLabel(string fieldName, bool isRequired)
    {
        var decision = BuildCreateRequiredDecision(fieldName, isRequired);
        return $"{fieldName}: Required={decision.IsRequired} | {_texts.Translate(decision.Reason)}";
    }

    private IReadOnlyList<PrototypeWizardRequiredFieldDecision> GetRequiredDecisions(PrototypeWizardContractSelection selection)
    {
        var requiredChecked = new HashSet<string>(GetCheckedValues(_createRequiredList), StringComparer.OrdinalIgnoreCase);
        var createRequiredListInitialized = _createRequiredList.Controls.Count > 0;
        var create = selection.CreateFields
            .Select(name =>
            {
                var isRequired = createRequiredListInitialized
                    ? requiredChecked.Contains(name)
                    : DefaultCreateRequired(name);
                return BuildCreateRequiredDecision(name, isRequired);
            })
            .ToArray();
        var update = selection.UpdateFields
            .Select(name => new PrototypeWizardRequiredFieldDecision("UpdateRequest", name, true, "Update via PUT exige todo membro selecionado preenchido; ausente ou com o valor default do tipo (vazio, false ou 0) devolve 400."))
            .ToArray();
        return create.Concat(update).ToArray();
    }

    private bool DefaultCreateRequired(string fieldName)
    {
        if (_snapshot.ExistingApiContract.TryGetCreateRequired(fieldName, out var existingRequired))
        {
            return existingRequired;
        }

        var attribute = _snapshot.Attributes.Single(item => string.Equals(item.Name, fieldName, StringComparison.Ordinal));
        if (attribute.IsSensitive)
        {
            return false;
        }

        // Opcao 2 (2026-08-06): PK nao autonumerada entra no Create selecionada, porem opcional
        // por padrao, para permitir rules/BC preencherem chave omitida ou com default do tipo.
        if (attribute.IsPrimaryKey)
        {
            return false;
        }

        if (attribute.IsNullable)
        {
            return false;
        }

        return true;
    }

    private PrototypeWizardRequiredFieldDecision BuildCreateRequiredDecision(string fieldName, bool isRequired)
    {
        var attribute = _snapshot.Attributes.Single(item => string.Equals(item.Name, fieldName, StringComparison.Ordinal));
        if (isRequired)
        {
            return new PrototypeWizardRequiredFieldDecision(
                "CreateRequest",
                fieldName,
                true,
                "Campo marcado como obrigatório no payload; ausente ou com o valor default do tipo (vazio, false ou 0) devolve 400.");
        }

        if (attribute.IsSensitive)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo sensível selecionado permanece opcional no protótipo; se enviado, o valor é validado pelo BC.");
        }

        if (attribute.IsPrimaryKey)
        {
            return new PrototypeWizardRequiredFieldDecision(
                "CreateRequest",
                fieldName,
                false,
                "Chave primária não autonumerada inicia opcional no CreateRequest; omitida ou com default do tipo fica a cargo do BC/rules. Marque para exigir no payload.");
        }

        if (attribute.IsNullable)
        {
            return new PrototypeWizardRequiredFieldDecision("CreateRequest", fieldName, false, "Campo nullable pode ser omitido; valor vazio presente continua valor enviado e sujeito ao BC.");
        }

        return new PrototypeWizardRequiredFieldDecision(
            "CreateRequest",
            fieldName,
            false,
            "Campo opcional no CreateRequest; omitido ou com default do tipo fica a cargo do BC/rules.");
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
        public ChoiceItem(string value, bool enabled, string label, string? disabledReason = null, string blockedPrefix = "Bloqueado - Motivo: ")
        {
            Value = value;
            Enabled = enabled;
            Label = label;
            DisabledReason = disabledReason;
            BlockedPrefix = blockedPrefix;
        }

        public string Value { get; }

        public bool Enabled { get; }

        public string Label { get; }

        public string? DisabledReason { get; }

        public string BlockedPrefix { get; }

        public override string ToString()
        {
            if (Enabled || string.IsNullOrWhiteSpace(DisabledReason))
            {
                return Label;
            }

            return Label + " [" + BlockedPrefix + DisabledReason + "]";
        }
    }
}

internal sealed class PrototypeWizardFlowSelection
{
    public PrototypeWizardFlowSelection(
        PrototypeWizardContractSelection contractSelection,
        PrototypeWizardReviewSelection reviewSelection,
        IReadOnlyList<PrototypeWizardRequiredFieldDecision> requiredFields,
        PrototypeWizardBusinessComponentSelection businessComponentSelection,
        bool generateSdts,
        bool generateProcedures,
        bool generateApiObject,
        bool generateMetadata,
        bool applyList,
        bool applyBusinessComponent,
        ApiPlanHierarchicalWizardSelection? hierarchicalSelection = null)
    {
        ContractSelection = contractSelection ?? throw new ArgumentNullException(nameof(contractSelection));
        ReviewSelection = reviewSelection ?? throw new ArgumentNullException(nameof(reviewSelection));
        RequiredFields = requiredFields ?? throw new ArgumentNullException(nameof(requiredFields));
        BusinessComponentSelection = businessComponentSelection ?? throw new ArgumentNullException(nameof(businessComponentSelection));
        GenerateSdts = generateSdts;
        GenerateProcedures = generateProcedures;
        GenerateApiObject = generateApiObject;
        GenerateMetadata = generateMetadata;
        ApplyList = applyList;
        ApplyBusinessComponent = applyBusinessComponent;
        HierarchicalSelection = hierarchicalSelection;
    }

    public PrototypeWizardContractSelection ContractSelection { get; }

    public PrototypeWizardReviewSelection ReviewSelection { get; }

    public IReadOnlyList<PrototypeWizardRequiredFieldDecision> RequiredFields { get; }

    public PrototypeWizardBusinessComponentSelection BusinessComponentSelection { get; }

    public bool GenerateSdts { get; }

    public bool GenerateProcedures { get; }

    public bool GenerateApiObject { get; }

    public bool GenerateMetadata { get; }

    public bool ApplyList { get; }

    public bool ApplyBusinessComponent { get; }

    public ApiPlanHierarchicalWizardSelection? HierarchicalSelection { get; }
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
