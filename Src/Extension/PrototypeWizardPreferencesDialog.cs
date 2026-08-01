using System;
using System.Drawing;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardPreferencesDialog : Form
{
    private readonly CheckBox _generateSdtsCheck = CreateCheckBox("Marcar SDTs por padrao");
    private readonly CheckBox _generateProceduresCheck = CreateCheckBox("Marcar Procedures por padrao");
    private readonly CheckBox _generateApiObjectCheck = CreateCheckBox("Marcar API Object por padrao");
    private readonly CheckBox _generateMetadataCheck = CreateCheckBox("Marcar metadata da API por padrao");
    private readonly CheckBox _applyListCheck = CreateCheckBox("Marcar listagem por padrao");
    private readonly CheckBox _applyBusinessComponentCheck = CreateCheckBox("Marcar Get/Create/Update REST por padrao");
    private readonly CheckBox _listServiceCheck = CreateCheckBox("List");
    private readonly CheckBox _getServiceCheck = CreateCheckBox("Get");
    private readonly CheckBox _createServiceCheck = CreateCheckBox("Create");
    private readonly CheckBox _updateServiceCheck = CreateCheckBox("Update");
    private readonly ComboBox _securityLevelCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly NumericUpDown _defaultPageSizeInput = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSizeInput = CreateNumericInput();

    public PrototypeWizardPreferencesDialog(PrototypeWizardPreferences preferences, string status)
    {
        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        Text = "Genexus Open API Builder - Preferencias do Wizard";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        Width = 860;
        Height = 680;
        MinimumSize = new Size(780, 580);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

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
            RowCount = 6,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            MinimumSize = new Size(0, 28),
            Text = "Preferencias gerais do wizard na KB ativa",
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
            Text = "Defaults de geracao",
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
            Text = "Servicos marcados por padrao",
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
        servicesGroup.Controls.Add(services);
        root.Controls.Add(servicesGroup, 0, 3);

        var executionGroup = new GroupBox
        {
            Text = "Seguranca e paginacao",
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
            RowCount = 3,
        };
        execution.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        execution.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(execution, 0, "Security Level", _securityLevelCombo);
        AddField(execution, 1, "Default Page Size", _defaultPageSizeInput);
        AddField(execution, 2, "Maximum Page Size", _maximumPageSizeInput);
        executionGroup.Controls.Add(execution);
        root.Controls.Add(executionGroup, 0, 4);

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

        var save = CreateButton("Salvar");
        save.Click += (_, _) => SaveAndClose();
        var cancel = CreateButton("Cancelar");
        cancel.Click += (_, _) => CancelAndClose();

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        buttonsHost.Controls.Add(buttons);
        buttonsHost.Resize += (_, _) =>
        {
            buttons.Left = Math.Max(0, buttonsHost.ClientSize.Width - buttons.Width);
        };
        root.Controls.Add(buttonsHost, 0, 5);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private void LoadPreferences(PrototypeWizardPreferences preferences)
    {
        _generateSdtsCheck.Checked = preferences.GenerateSdtsByDefault;
        _generateProceduresCheck.Checked = preferences.GenerateProceduresByDefault;
        _generateApiObjectCheck.Checked = preferences.GenerateApiObjectByDefault;
        _generateMetadataCheck.Checked = preferences.GenerateMetadataByDefault;
        _applyListCheck.Checked = preferences.ApplyListByDefault;
        _applyBusinessComponentCheck.Checked = preferences.ApplyBusinessComponentByDefault;
        _listServiceCheck.Checked = preferences.ListServiceByDefault;
        _getServiceCheck.Checked = preferences.GetServiceByDefault;
        _createServiceCheck.Checked = preferences.CreateServiceByDefault;
        _updateServiceCheck.Checked = preferences.UpdateServiceByDefault;
        _securityLevelCombo.SelectedItem = PrototypeWizardPreferences.NormalizeSecurityLevel(preferences.SecurityLevelByDefault);
        _defaultPageSizeInput.Value = Math.Max(_defaultPageSizeInput.Minimum, Math.Min(_defaultPageSizeInput.Maximum, preferences.DefaultPageSizeByDefault));
        _maximumPageSizeInput.Value = Math.Max(_maximumPageSizeInput.Minimum, Math.Min(_maximumPageSizeInput.Maximum, preferences.MaximumPageSizeByDefault));
    }

    private void SaveAndClose()
    {
        if (!_listServiceCheck.Checked && !_getServiceCheck.Checked && !_createServiceCheck.Checked && !_updateServiceCheck.Checked)
        {
            MessageBox.Show(this, "Marque ao menos um servico padrao.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_defaultPageSizeInput.Value > _maximumPageSizeInput.Value)
        {
            MessageBox.Show(this, "Default Page Size deve ser menor ou igual a Maximum Page Size.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            SecurityLevelByDefault = PrototypeWizardPreferences.NormalizeSecurityLevel(_securityLevelCombo.SelectedItem as string),
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

    private static CheckBox CreateCheckBox(string text)
    {
        return new CheckBox
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 30,
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
