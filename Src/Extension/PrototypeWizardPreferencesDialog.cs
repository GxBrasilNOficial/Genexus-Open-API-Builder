using System;
using System.Drawing;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardPreferencesDialog : Form
{
    private readonly ExtensionTexts _texts;
    private readonly CheckBox _generateSdtsCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _generateProceduresCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _generateApiObjectCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _generateMetadataCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _applyListCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _applyBusinessComponentCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _listServiceCheck = CreateCheckBox("List");
    private readonly CheckBox _getServiceCheck = CreateCheckBox("Get");
    private readonly CheckBox _createServiceCheck = CreateCheckBox("Create");
    private readonly CheckBox _updateServiceCheck = CreateCheckBox("Update");
    private readonly CheckBox _deleteServiceCheck = CreateCheckBox("Delete");
    private readonly ComboBox _securityLevelCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly CheckBox _includeBcErrorMessagesCheck = CreateCheckBox(string.Empty);
    private readonly CheckBox _offerOrphanMetadataRecoveryCheck = CreateCheckBox(string.Empty);
    // Altura dobrada: o rótulo descreve o efeito colateral e é longo — 592 px em espanhol,
    // contra 710 px úteis com a janela no tamanho mínimo. Cabe em uma linha a 100% de DPI,
    // mas não a 125% ou 150%, onde AutoScaleMode.Font amplia a fonte. Com AutoSize=false o
    // CheckBox quebra o texto, e corta o que passar da altura declarada.
    private readonly CheckBox _suppressProgressPumpCheck = CreateCheckBox(string.Empty, height: 56);
    private readonly NumericUpDown _defaultPageSizeInput = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSizeInput = CreateNumericInput();

    public PrototypeWizardPreferencesDialog(PrototypeWizardPreferences preferences, string status, ExtensionTexts texts)
    {
        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        Text = _texts.PreferencesDialogTitle;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        Width = 860;
        Height = 780;
        MinimumSize = new Size(780, 680);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        ApplyLocalizedText();
        BuildLayout(status ?? string.Empty);
        LoadPreferences(preferences);
    }

    public PrototypeWizardPreferences? Preferences { get; private set; }

    private void BuildLayout(string status)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 18));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 27));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            MinimumSize = new Size(0, 28),
            Text = _texts.Translate("Preferencias gerais do wizard na KB ativa"),
            Padding = new Padding(0, 0, 0, 8),
        }, 0, 0);

        root.Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = status,
            MinimumSize = new Size(0, 70),
        }, 0, 1);

        var optionsGroup = new GroupBox
        {
            Text = _texts.Translate("Defaults de geracao"),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };

        var checks = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            AutoScroll = true,
        };
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var index = 0; index < 3; index++)
        {
            checks.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        checks.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        checks.Controls.Add(_generateSdtsCheck, 0, 0);
        checks.Controls.Add(_generateProceduresCheck, 1, 0);
        checks.Controls.Add(_generateApiObjectCheck, 0, 1);
        checks.Controls.Add(_applyBusinessComponentCheck, 1, 1);
        checks.Controls.Add(_applyListCheck, 0, 2);
        checks.Controls.Add(_generateMetadataCheck, 1, 2);
        optionsGroup.Controls.Add(checks);
        root.Controls.Add(optionsGroup, 0, 2);

        var servicesGroup = new GroupBox
        {
            Text = _texts.Translate("Servicos marcados por padrao"),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };

        var services = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };
        services.Controls.Add(_listServiceCheck);
        services.Controls.Add(_getServiceCheck);
        services.Controls.Add(_createServiceCheck);
        services.Controls.Add(_updateServiceCheck);
        services.Controls.Add(_deleteServiceCheck);
        servicesGroup.Controls.Add(services);
        root.Controls.Add(servicesGroup, 0, 3);

        var executionGroup = new GroupBox
        {
            Text = _texts.Translate("Seguranca e paginacao"),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };

        _securityLevelCombo.Items.Add(PrototypeWizardPreferences.SecurityLevelAuthentication);
        _securityLevelCombo.Items.Add(PrototypeWizardPreferences.SecurityLevelAuthorization);
        _securityLevelCombo.Items.Add(PrototypeWizardPreferences.SecurityLevelNone);

        var execution = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4,
        };
        execution.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        execution.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(execution, 0, _texts.Translate("Security Level"), _securityLevelCombo);
        execution.Controls.Add(_includeBcErrorMessagesCheck, 0, 1);
        execution.SetColumnSpan(_includeBcErrorMessagesCheck, 2);
        AddField(execution, 2, _texts.Translate("Default Page Size"), _defaultPageSizeInput);
        AddField(execution, 3, _texts.Translate("Maximum Page Size"), _maximumPageSizeInput);
        executionGroup.Controls.Add(execution);
        root.Controls.Add(executionGroup, 0, 4);

        // Quadro próprio, e por último: nenhuma destas opções pertence ao uso normal. A de
        // recuperação nasceu ocupando a linha de folga do quadro de geração, onde não é um
        // default de geração; a de supressão do Pump substitui a variável de ambiente
        // GOAB_B109_SUPPRESS_PUMP, que dependia do Windows e não tinha onde ser avisada.
        var diagnosticsGroup = new GroupBox
        {
            Text = _texts.Translate("Diagnostico e recuperacao"),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };

        var diagnostics = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoScroll = true,
        };
        diagnostics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        diagnostics.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = _texts.Translate("Opcoes de investigacao. Nao sao necessarias no uso normal da extensao."),
            Padding = new Padding(0, 0, 0, 6),
        }, 0, 0);
        diagnostics.Controls.Add(_offerOrphanMetadataRecoveryCheck, 0, 1);
        diagnostics.Controls.Add(_suppressProgressPumpCheck, 0, 2);
        diagnosticsGroup.Controls.Add(diagnostics);
        root.Controls.Add(diagnosticsGroup, 0, 5);

        var buttonsHost = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 48,
        };

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Location = new Point(0, 10),
        };

        var save = CreateButton(_texts.Save);
        save.Click += (_, _) => SaveAndClose();
        var cancel = CreateButton(_texts.Cancel);
        cancel.Click += (_, _) => CancelAndClose();

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        buttonsHost.Controls.Add(buttons);
        buttonsHost.Resize += (_, _) =>
        {
            buttons.Left = Math.Max(0, buttonsHost.ClientSize.Width - buttons.Width);
        };
        root.Controls.Add(buttonsHost, 0, 6);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private void LoadPreferences(PrototypeWizardPreferences preferences)
    {
        _generateSdtsCheck.Checked = preferences.GenerateSdtsByDefault;
        _generateProceduresCheck.Checked = preferences.GenerateProceduresByDefault;
        _generateApiObjectCheck.Checked = preferences.GenerateApiObjectByDefault;
        _generateMetadataCheck.Checked = preferences.GenerateMetadataByDefault;
        _offerOrphanMetadataRecoveryCheck.Checked = preferences.OfferOrphanMetadataRecovery;
        _suppressProgressPumpCheck.Checked = preferences.SuppressProgressPumpDuringSaves;
        _applyListCheck.Checked = preferences.ApplyListByDefault;
        _applyBusinessComponentCheck.Checked = preferences.ApplyBusinessComponentByDefault;
        _listServiceCheck.Checked = preferences.ListServiceByDefault;
        _getServiceCheck.Checked = preferences.GetServiceByDefault;
        _createServiceCheck.Checked = preferences.CreateServiceByDefault;
        _updateServiceCheck.Checked = preferences.UpdateServiceByDefault;
        _deleteServiceCheck.Checked = preferences.DeleteServiceByDefault;
        _securityLevelCombo.SelectedItem = PrototypeWizardPreferences.NormalizeSecurityLevel(preferences.SecurityLevelByDefault);
        _includeBcErrorMessagesCheck.Checked = preferences.IncludeBusinessComponentErrorMessagesByDefault;
        _defaultPageSizeInput.Value = Math.Max(_defaultPageSizeInput.Minimum, Math.Min(_defaultPageSizeInput.Maximum, preferences.DefaultPageSizeByDefault));
        _maximumPageSizeInput.Value = Math.Max(_maximumPageSizeInput.Minimum, Math.Min(_maximumPageSizeInput.Maximum, preferences.MaximumPageSizeByDefault));
    }

    private void SaveAndClose()
    {
        if (!_listServiceCheck.Checked && !_getServiceCheck.Checked && !_createServiceCheck.Checked && !_updateServiceCheck.Checked && !_deleteServiceCheck.Checked)
        {
            MessageBox.Show(this, _texts.Translate("Marque ao menos um servico padrao."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_deleteServiceCheck.Checked && !(_getServiceCheck.Checked && _createServiceCheck.Checked && _updateServiceCheck.Checked))
        {
            MessageBox.Show(
                this,
                _texts.Translate("Delete marcado exige Get, Create e Update nos servicos padrao. Marque os tres ou desmarque Delete."),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_defaultPageSizeInput.Value > _maximumPageSizeInput.Value)
        {
            MessageBox.Show(this, _texts.Translate("Default Page Size deve ser menor ou igual a Maximum Page Size."), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Preferences = new PrototypeWizardPreferences
        {
            GenerateSdtsByDefault = _generateSdtsCheck.Checked,
            GenerateProceduresByDefault = _generateProceduresCheck.Checked,
            GenerateApiObjectByDefault = _generateApiObjectCheck.Checked,
            GenerateMetadataByDefault = _generateMetadataCheck.Checked,
            ApplyListByDefault = _applyListCheck.Checked,
            ApplyBusinessComponentByDefault = _applyBusinessComponentCheck.Checked,
            ListServiceByDefault = _listServiceCheck.Checked,
            GetServiceByDefault = _getServiceCheck.Checked,
            CreateServiceByDefault = _createServiceCheck.Checked,
            UpdateServiceByDefault = _updateServiceCheck.Checked,
            DeleteServiceByDefault = _deleteServiceCheck.Checked,
            SecurityLevelByDefault = PrototypeWizardPreferences.NormalizeSecurityLevel(_securityLevelCombo.SelectedItem as string),
            IncludeBusinessComponentErrorMessagesByDefault = _includeBcErrorMessagesCheck.Checked,
            OfferOrphanMetadataRecovery = _offerOrphanMetadataRecoveryCheck.Checked,
            SuppressProgressPumpDuringSaves = _suppressProgressPumpCheck.Checked,
            DefaultPageSizeByDefault = (int)_defaultPageSizeInput.Value,
            MaximumPageSizeByDefault = (int)_maximumPageSizeInput.Value,
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void CancelAndClose()
    {
        Preferences = null;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void ApplyLocalizedText()
    {
        _generateSdtsCheck.Text = _texts.Translate("Marcar SDTs por padrao");
        _generateProceduresCheck.Text = _texts.Translate("Marcar Procedures por padrao");
        _generateApiObjectCheck.Text = _texts.Translate("Marcar API Object por padrao");
        _generateMetadataCheck.Text = _texts.Translate("Marcar metadata da API por padrao");
        _offerOrphanMetadataRecoveryCheck.Text = _texts.Translate("Oferecer recuperacao de metadata orfa no Wizard");
        _suppressProgressPumpCheck.Text = _texts.Translate("Suprimir a atualizacao da tela durante as gravacoes - a janela congela e Abortar nao responde (B109)");
        _applyListCheck.Text = _texts.Translate("Marcar listagem por padrao");
        _applyBusinessComponentCheck.Text = _texts.Translate("Marcar REST via Business Component por padrao");
        _includeBcErrorMessagesCheck.Text = _texts.Translate("Incluir mensagens de erro do Business Component no corpo HTTP 422");
    }

    private static CheckBox CreateCheckBox(string text, int height = 30)
    {
        return new CheckBox
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = height,
            Text = text,
            Margin = new Padding(0, 4, 0, 4),
        };
    }

    private static NumericUpDown CreateNumericInput()
    {
        return new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100000,
            Width = 120,
        };
    }

    private static void AddField(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.Controls.Add(new Label { AutoSize = true, Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(control, 1, row);
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
}
