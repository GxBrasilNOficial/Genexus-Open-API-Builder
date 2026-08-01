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
    private readonly NumericUpDown _defaultPageSize = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSize = CreateNumericInput();
    private readonly ListBox _staticOrderList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly TextBox _createRequiredText = CreateReadOnlyTextBox();
    private readonly TextBox _updateRequiredText = CreateReadOnlyTextBox();
    private readonly TextBox _businessComponentText = CreateReadOnlyTextBox();
    private readonly CheckBox _enableBusinessComponentCheck = new() { AutoSize = true, Text = "Habilitar Business Component agora", Dock = DockStyle.Top };
    private readonly TextBox _summaryDecisionText = CreateReadOnlyTextBox();
    private readonly TextBox _summaryEndpointText = CreateReadOnlyTextBox();
    private readonly CheckBox _generateSdtsCheck = new() { AutoSize = true, Text = "Confirmar criacao ou reencontro de SDTs B040-B046 ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateProceduresCheck = new() { AutoSize = true, Text = "Confirmar criacao ou reencontro de Procedures B050-B053 ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateApiObjectCheck = new() { AutoSize = true, Text = "Confirmar criacao ou reencontro de API Object B054 ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _generateMetadataCheck = new() { AutoSize = true, Text = "Confirmar criacao ou reencontro de File JSON de metadata B060 ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _applyBusinessComponentCheck = new() { AutoSize = true, Text = "Aplicar Create/Update via Business Component ao concluir", Dock = DockStyle.Top };
    private readonly CheckBox _applyListCheck = new() { AutoSize = true, Text = "Completar List B070 ao concluir", Dock = DockStyle.Top };
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
    private bool _showingSummary;
    private bool _loadingSnapshot;
    private bool _servicesBasePathEditedManually;
    private bool _businessComponentEnabledDuringWizard;
    private bool _suppressGenerationPreviewRefresh;
    private bool _applyBusinessComponentWhenReady;
    private string _generationContext = "Plano da Transaction ainda nao consultado na KB.";

    public PrototypeWizardDialog(KBModel designModel, Transaction transaction, PrototypeWizardContractSnapshot snapshot, PrototypeBusinessComponentSnapshot businessComponentSnapshot, PrototypeWizardPreferences preferences, Func<bool> enableBusinessComponent, Action<string> writeBusinessComponentOutput)
    {
        _designModel = designModel ?? throw new ArgumentNullException(nameof(designModel));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _businessComponentSnapshot = businessComponentSnapshot ?? throw new ArgumentNullException(nameof(businessComponentSnapshot));
        _preferences = preferences?.Clone() ?? throw new ArgumentNullException(nameof(preferences));
        _enableBusinessComponent = enableBusinessComponent ?? throw new ArgumentNullException(nameof(enableBusinessComponent));
        _writeBusinessComponentOutput = writeBusinessComponentOutput ?? throw new ArgumentNullException(nameof(writeBusinessComponentOutput));

        Text = "Genexus Open API Builder - Wizard";
        StartPosition = FormStartPosition.CenterParent;
        Width = 1200;
        Height = 800;
        MinimumSize = new Size(900, 640);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout();
        WirePathSynchronization();
        LoadSnapshot();
        WireGenerationConfirmation();
        ApplyWizardPreferences();
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
        _tabs.TabPages.Add(CreateSdtGenerationTab());
        _tabs.TabPages.Add(CreateProcedureGenerationTab());
        _tabs.TabPages.Add(CreateApiObjectGenerationTab());
        _tabs.TabPages.Add(CreateBusinessComponentTab());
        _tabs.TabPages.Add(CreateListGenerationTab());
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
        _headerLabel.Text = $"Wizard: Module '{_snapshot.ModuleName}' | Transaction '{_snapshot.TransactionName}' | {_generationContext} | Aba atual: {currentTab}";
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
            RowCount = 4,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Business Component preserva as regras da Transaction. A confirmação abaixo altera as Procedures de Create e Update já geradas; não cria novos objetos.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_enableBusinessComponentCheck, 0, 1);
        panel.Controls.Add(_applyBusinessComponentCheck, 0, 2);
        panel.Controls.Add(_businessComponentText, 0, 3);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSdtGenerationTab()
    {
        var tab = new TabPage("SDTs");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Revise os SDTs planejados. A escrita so sera executada ao concluir o wizard se esta confirmacao estiver marcada e o preflight tecnico estiver OK.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_generateSdtsCheck, 0, 1);
        panel.Controls.Add(CreateGroup("SDTs planejados", _sdtGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateProcedureGenerationTab()
    {
        var tab = new TabPage("Procedures");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Revise as Procedures planejadas. Esta etapa depende dos SDTs B040-B046 confirmados ou ja reencontraveis na KB ativa.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_generateProceduresCheck, 0, 1);
        panel.Controls.Add(CreateGroup("Procedures planejadas", _procedureGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateApiObjectGenerationTab()
    {
        var tab = new TabPage("API Object");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Revise o API Object planejado. Esta etapa depende dos SDTs B040-B046 e das Procedures B050-B053 ja confirmados ou reencontraveis na KB ativa.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_generateApiObjectCheck, 0, 1);
        panel.Controls.Add(CreateGroup("API Object planejado", _apiObjectGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }
    private TabPage CreateMetadataGenerationTab()
    {
        var tab = new TabPage("Metadata");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Revise o File JSON de metadata. B060 grava apenas metadata persistente inicial do ApiPlan e depende do API Object proprio ja confirmado ou reencontrado.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_generateMetadataCheck, 0, 1);
        panel.Controls.Add(CreateGroup("File de metadata planejado", _metadataGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateListGenerationTab()
    {
        var tab = new TabPage("List");
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
        panel.Controls.Add(new Label { AutoSize = true, Text = "Revise a materializacao inicial do List. B070 altera a Procedure List e sincroniza o API Object com parametros de pagina, filtros e ListResponse.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);
        panel.Controls.Add(_applyListCheck, 0, 1);
        panel.Controls.Add(CreateGroup("List planejado", _listGenerationText), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage("Resumo");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = "Resumo das decisões acumuladas para montagem do ApiPlan em memória.", Padding = new Padding(0, 0, 0, 8) }, 0, 0);

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

    private void ApplyWizardPreferences()
    {
        _suppressGenerationPreviewRefresh = true;
        try
        {
            ApplyServicePreference("List", _preferences.ListServiceByDefault);
            ApplyServicePreference("Get", _preferences.GetServiceByDefault);
            ApplyServicePreference("Create", _preferences.CreateServiceByDefault);
            ApplyServicePreference("Update", _preferences.UpdateServiceByDefault);
            ApplySecurityPreference(_preferences.SecurityLevelByDefault);
            _defaultPageSize.Value = ClampNumeric(_defaultPageSize, _preferences.DefaultPageSizeByDefault);
            _maximumPageSize.Value = ClampNumeric(_maximumPageSize, _preferences.MaximumPageSizeByDefault);
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

        RefreshGenerationPreview();
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

        if (_tabs.SelectedTab?.Text == "Paths")
        {
            RefreshEndpointsText();
        }

        if (_tabs.SelectedTab?.Text == "Obrigatórios")
        {
            RefreshRequiredText();
        }

        if (_tabs.SelectedTab?.Text == "SDTs" || _tabs.SelectedTab?.Text == "Procedures" || _tabs.SelectedTab?.Text == "API Object" || _tabs.SelectedTab?.Text == "List" || _tabs.SelectedTab?.Text == "Metadata")
        {
            RefreshGenerationPreview();
        }

        if (_tabs.SelectedTab?.Text == "Business Component" &&
            !CompletePendingExplicitActions())
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

        if (!CompletePendingExplicitActions() || !TryCreateSelection())
        {
            return;
        }

        ShowSummary();
    }

    private void HandleSelectedTabChanged()
    {
        RefreshGenerationPreview();
        if (_tabs.SelectedTab?.Text == "Resumo")
        {
            if (!_showingSummary && CompletePendingExplicitActions() && TryCreateSelection())
            {
                ShowSummary();
            }

            RefreshCurrentTabLabel();
            return;
        }

        if (_showingSummary)
        {
            _showingSummary = false;
            if (_nextButton is not null)
            {
                _nextButton.Text = "Próximo";
            }
        }

        RefreshCurrentTabLabel();
    }

    private bool CompletePendingExplicitActions()
    {
        if (PrototypeWizardBusinessComponentNavigationPolicy.ShouldRequestEnableBeforeLeavingWizard(IsBusinessComponentReady(), _enableBusinessComponentCheck.Checked))
        {
            if (!EnsureBusinessComponentReady())
            {
                return false;
            }

            RefreshGenerationPreview();
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
            CreateBusinessComponentSelection(),
            _generateSdtsCheck.Checked,
            _generateProceduresCheck.Checked,
            _generateApiObjectCheck.Checked,
            _generateMetadataCheck.Checked,
            _applyListCheck.Checked,
            _applyBusinessComponentCheck.Checked && IsBusinessComponentReady());
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
            $"Business Component: IsBusinessComponent={businessComponent.IsBusinessComponent}, Status='{businessComponent.Status}', EnabledDuringWizard={businessComponent.EnabledDuringWizard}{Environment.NewLine}" +
            $"Gerar SDTs B040-B046: {Selection.GenerateSdts}{Environment.NewLine}" +
            $"Gerar Procedures B050-B053: {Selection.GenerateProcedures}{Environment.NewLine}" +
            $"Gerar API Object B054: {Selection.GenerateApiObject}{Environment.NewLine}" +
            $"Completar List B070: {Selection.ApplyList}{Environment.NewLine}" +
            $"Gravar metadata B060: {Selection.GenerateMetadata}{Environment.NewLine}" +
            $"Aplicar Create/Update via Business Component: {Selection.ApplyBusinessComponent}{Environment.NewLine}" +
            $"Estado da geracao: {_generationContext}";
        _summaryEndpointText.Text =
            FormatEndpoints(review.RestPath, contract.SelectedServices) + Environment.NewLine + Environment.NewLine +
            "B036 exibiu campos bloqueados com motivo no fluxo do wizard." + Environment.NewLine +
            "B037 consolidou Required como presença do membro JSON, distinguindo de valor não vazio." + Environment.NewLine +
            "ApiPlan sera montado em memoria ao concluir o wizard." + Environment.NewLine +
            "SDTs, Procedures, API Object, List B070 e metadata so serao escritos se as respectivas abas estiverem confirmadas e o preflight tecnico estiver OK." + Environment.NewLine +
            "A opção de Business Component altera somente Create e Update nas Procedures já geradas." + Environment.NewLine +
            "B070 completa a primeira versao do List; B060 grava o File JSON de metadata inicial.";
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
            ? "Concluir e aplicar"
            : "Concluir Teste";
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

    private void RefreshGenerationPreviewUnlessSuppressed()
    {
        if (!_suppressGenerationPreviewRefresh)
        {
            RefreshGenerationPreview();
        }
    }

    private void RefreshGenerationPreview()
    {
        var state = ReadGenerationState();
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
            $"Dependencia SDTs: {FormatDependencyState(sdtState, _generateSdtsCheck.Checked)}";
        _apiObjectGenerationText.Text = FormatGenerationState(apiState, _generateApiObjectCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"Dependencia Procedures: {FormatDependencyState(procedureState, _generateProceduresCheck.Checked)}";
        _listGenerationText.Text = FormatGenerationState(apiState, _applyListCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"Dependencia API Object: {FormatDependencyState(apiState, _generateApiObjectCheck.Checked || businessComponentConfirmed)}" + Environment.NewLine +
            $"Filtros planejados: {GetCheckedValues(_filtersList).Count}; Paginacao Default={_defaultPageSize.Value}, Maximum={_maximumPageSize.Value}.";
        _metadataGenerationText.Text = FormatGenerationState(metadataState, _generateMetadataCheck.Checked) + Environment.NewLine + Environment.NewLine +
            $"Dependencia List/API Object: {FormatDependencyState(apiState, _generateApiObjectCheck.Checked || businessComponentConfirmed || _applyListCheck.Checked)}";
    }

    private void ApplyBusinessComponentControlState(bool sdtsAvailable, bool proceduresAvailable, bool apiObjectAvailable)
    {
        var shouldApplyWhenAllowed = PrototypeWizardBusinessComponentNavigationPolicy.ResolveApplyBusinessComponentAfterGenerationRefresh(
            IsBusinessComponentReady(),
            _enableBusinessComponentCheck.Checked,
            sdtsAvailable,
            proceduresAvailable,
            apiObjectAvailable,
            _applyBusinessComponentCheck.Checked,
            _applyBusinessComponentWhenReady);
        var canApplyBusinessComponent = PrototypeWizardBusinessComponentNavigationPolicy.ShouldAllowApplyBusinessComponent(
            IsBusinessComponentReady(),
            _enableBusinessComponentCheck.Checked,
            sdtsAvailable,
            proceduresAvailable,
            apiObjectAvailable);
        if (IsBusinessComponentReady())
        {
            _applyBusinessComponentCheck.Text = canApplyBusinessComponent
                ? "Confirmar: Aplicar Create/Update via Business Component ao concluir"
                : "Bloqueado: confirme SDTs, Procedures e API Object";
        }
        else if (_enableBusinessComponentCheck.Checked)
        {
            _applyBusinessComponentCheck.Text = canApplyBusinessComponent
                ? "Confirmar: Aplicar Create/Update via Business Component após habilitar"
                : "Bloqueado: confirme SDTs, Procedures e API Object antes de aplicar BC";
        }
        else
        {
            _applyBusinessComponentCheck.Text = "Bloqueado: Business Component desabilitado";
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
        _applyListCheck.Text = "Confirmar: Completar List B070 ao concluir";
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

    private static string FormatDependencyState(ApiPlanGenerationStageState? state, bool confirmed)
    {
        if (confirmed)
        {
            return "confirmada nesta execucao";
        }

        return string.Equals(state?.Action, "Reencontrar e validar", StringComparison.Ordinal)
            ? "ja reencontrada na KB ativa"
            : "nao confirmada";
    }
    private static string FormatGenerationContext(ApiPlanGenerationState? state)
    {
        if (state is null)
        {
            return "Estado: plano em memoria";
        }

        var stages = new[] { state.Sdts, state.Procedures, state.ApiObject, state.MetadataFile };
        if (stages.Any(stage => stage.IsBlocked))
        {
            return "Estado: teste bloqueado";
        }

        if (stages.All(stage => string.Equals(stage.Action, "Reencontrar e validar", StringComparison.Ordinal)))
        {
            return "Estado: teste de reencontro";
        }

        if (stages.All(stage => string.Equals(stage.Action, "Criar", StringComparison.Ordinal)))
        {
            return "Estado: teste de criacao";
        }

        return "Estado: teste de complementacao";
    }
    private void ApplyGenerationControlState(CheckBox checkBox, ApiPlanGenerationStageState? state, bool dependencyConfirmed)
    {
        if (state is null)
        {
            checkBox.Text = "Estado atual indisponivel";
            checkBox.Enabled = false;
            checkBox.Checked = false;
            return;
        }

        checkBox.Text = $"Confirmar: {state.Action} {state.StageName} ao concluir";
        checkBox.Enabled = !state.IsBlocked && dependencyConfirmed;
        if (!checkBox.Enabled)
        {
            checkBox.Checked = false;
        }
    }

    private static string FormatGenerationState(ApiPlanGenerationStageState? state, bool confirmed)
    {
        if (state is null)
        {
            return "Estado atual da KB indisponivel. Ajuste os campos obrigatorios do contrato para consultar a geracao.";
        }

        return $"Estado atual da KB: {state.Action}{Environment.NewLine}{state.Detail}{Environment.NewLine}{Environment.NewLine}Confirmado para escrita: {confirmed}";
    }

    private ApiPlanGenerationState? ReadGenerationState()
    {
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
                GetStaticOrder());
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
                false);
            return ApiPlanGenerationStateReader.Read(_designModel, ApiPlanBuilder.Build(_transaction, selection));
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
            "Sem Business Component, a habilitação e a aplicação de Create/Update via Business Component ficam bloqueadas. O wizard pode continuar para etapas que não exigem habilitar essa propriedade. A habilitação exige confirmação explícita e altera a Transaction na KB; cancelar o wizard depois disso não reverte automaticamente a propriedade.";
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
        PrototypeWizardBusinessComponentSelection businessComponentSelection,
        bool generateSdts,
        bool generateProcedures,
        bool generateApiObject,
        bool generateMetadata,
        bool applyList,
        bool applyBusinessComponent)
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
