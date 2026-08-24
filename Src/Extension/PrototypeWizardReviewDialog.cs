using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class PrototypeWizardReviewDialog : Form
{
    private readonly ExtensionTexts _texts;
    private readonly PrototypeWizardReviewSnapshot _snapshot;
    private readonly TextBox _apiNameText = CreateSingleLineTextBox();
    private readonly TextBox _servicesBasePathText = CreateSingleLineTextBox();
    private readonly TextBox _restPathText = CreateSingleLineTextBox();
    private readonly ComboBox _securityLevelCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly NumericUpDown _defaultPageSize = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSize = CreateNumericInput();
    private readonly ListBox _staticOrderList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly TextBox _endpointsText = CreateReadOnlyTextBox();
    private readonly TextBox _summaryText = CreateReadOnlyTextBox();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    private Button? _nextButton;
    private bool _showingSummary;
    private bool _loadingSnapshot;
    private bool _servicesBasePathEditedManually;

    public PrototypeWizardReviewDialog(PrototypeWizardReviewSnapshot snapshot, ExtensionTexts texts)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));

        Text = _texts.WizardReviewTitle;
        StartPosition = FormStartPosition.CenterParent;
        Width = 940;
        Height = 640;
        MinimumSize = new Size(760, 520);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout();
        WirePathSynchronization();
        LoadSnapshot();
    }

    public PrototypeWizardReviewSelection? Selection { get; private set; }

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
            Text = $"{_texts.Translate("Passo 3 - Revisar paths e seguranca")}: Transaction '{_snapshot.TransactionName}' | Module '{_snapshot.ModuleName}'",
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(header, 0, 0);

        _tabs.TabPages.Add(CreatePathsTab());
        _tabs.TabPages.Add(CreateSecurityTab());
        _tabs.TabPages.Add(CreatePaginationTab());
        _tabs.TabPages.Add(CreateOrderTab());
        _tabs.TabPages.Add(CreateSummaryTab());
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
        back.Click += (_, _) => GoBack();

        buttons.Controls.Add(next);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(back);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = next;
        CancelButton = cancel;
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
        panel.Controls.Add(CreateGroup(_texts.Translate("Paths dos servicos"), _endpointsText), 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSecurityTab()
    {
        var tab = new TabPage(_texts.Translate("Seguranca"));
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
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Security Level unico aplicado aos servicos gerados no MVP.")), 0, 0);
        panel.Controls.Add(_securityLevelCombo, 0, 1);
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Authentication inicia selecionado por seguranca. None permanece apenas como decisao prototipica nesta etapa."), 44, 12, 0), 0, 2);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreatePaginationTab()
    {
        var tab = new TabPage(_texts.Translate("Paginacao"));
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
        AddField(fields, 0, _texts.Translate("Default Page Size"), _defaultPageSize);
        AddField(fields, 1, _texts.Translate("Maximum Page Size"), _maximumPageSize);
        tab.Controls.Add(fields);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage(_texts.Translate("Ordenacao"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Ordenacao estatica inicial. A chave primaria completa e acrescentada como desempate ascendente.")), 0, 0);
        panel.Controls.Add(_staticOrderList, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateSummaryTab()
    {
        var tab = new TabPage(_texts.Translate("Resumo B033"));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateWrappingLabel(_texts.Translate("Resumo das decisoes acumuladas. B033 ainda nao executa nada na KB.")), 0, 0);
        panel.Controls.Add(_summaryText, 0, 1);
        tab.Controls.Add(panel);
        return tab;
    }

    private void LoadSnapshot()
    {
        _loadingSnapshot = true;
        _apiNameText.Text = _snapshot.ApiName;
        _servicesBasePathText.Text = _snapshot.ServicesBasePath;
        _restPathText.Text = _snapshot.RestPath;
        _servicesBasePathEditedManually = false;
        _loadingSnapshot = false;
        _securityLevelCombo.Items.Add("Authentication");
        _securityLevelCombo.Items.Add("None");
        _securityLevelCombo.SelectedItem = _snapshot.SecurityLevel;
        _defaultPageSize.Value = _snapshot.DefaultPageSize;
        _maximumPageSize.Value = _snapshot.MaximumPageSize;
        RefreshEndpointsText();

        foreach (var item in _snapshot.StaticOrder)
        {
            _staticOrderList.Items.Add($"{item.Order}. {item.AttributeName} {item.Direction}");
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
            Dock = DockStyle.Top,
            Minimum = 1,
            Maximum = 100000,
        };
    }

    private static void AddField(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.Controls.Add(new Label { AutoSize = true, Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(control, 1, row);
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
            if (_tabs.SelectedTab?.Text == _texts.Translate("Paths"))
            {
                RefreshEndpointsText();
            }

            _tabs.SelectedIndex++;
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

        Selection = new PrototypeWizardReviewSelection(
            _snapshot.TransactionName,
            apiName,
            servicesBasePath,
            restPath,
            _securityLevelCombo.SelectedItem?.ToString() ?? _snapshot.SecurityLevel,
            (int)_defaultPageSize.Value,
            (int)_maximumPageSize.Value,
            _snapshot.StaticOrder,
            true);
        return true;
    }

    private void ShowSummary()
    {
        if (Selection is null)
        {
            return;
        }

        _summaryText.Text =
            $"Transaction: {Selection.TransactionName}{Environment.NewLine}" +
            $"ApiName: {Selection.ApiName}{Environment.NewLine}" +
            $"Services base path: {Selection.ServicesBasePath}{Environment.NewLine}" +
            $"RestPath: {Selection.RestPath}{Environment.NewLine}" +
            $"Security Level: {Selection.SecurityLevel}{Environment.NewLine}" +
            $"Paginacao: Default={Selection.DefaultPageSize}, Maximum={Selection.MaximumPageSize}{Environment.NewLine}" +
            $"Ordenacao: {string.Join(", ", Selection.StaticOrder.Select(item => item.AttributeName + " " + item.Direction))}{Environment.NewLine}{Environment.NewLine}" +
            FormatEndpoints(Selection.RestPath) + Environment.NewLine + Environment.NewLine +
            "B033 validara campos obrigatorios. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.";

        _showingSummary = true;
        _tabs.SelectedIndex = _tabs.TabPages.Count - 1;
        if (_nextButton is not null)
        {
            _nextButton.Text = _texts.Close;
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
                _nextButton.Text = _texts.Next;
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
        _endpointsText.Text = FormatEndpoints(_restPathText.Text.Trim());
    }

    private string FormatEndpoints(string restPath)
    {
        var keyPath = restPath + FormatKeySuffix();
        var lines = new List<string>();
        foreach (var service in _snapshot.SelectedServices)
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
        if (_snapshot.PrimaryKeyParts.Count == 0)
        {
            return string.Empty;
        }

        return "/" + string.Join("/", _snapshot.PrimaryKeyParts.Select(part => "{" + part.Name + "}"));
    }
}
