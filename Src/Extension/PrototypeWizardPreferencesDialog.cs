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
    private readonly CheckBox _suppressProgressPumpCheck = CreateCheckBox(string.Empty);
    private readonly NumericUpDown _defaultPageSizeInput = CreateNumericInput();
    private readonly NumericUpDown _maximumPageSizeInput = CreateNumericInput();
    private TableLayoutPanel? _root;

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
        // Duas colunas de quadros pedem o dobro da largura de antes. Medidas provisórias:
        // FitToContent, no fim deste construtor, ajusta a altura ao que o conteúdo mede e
        // limita ambas ao que cabe na tela.
        Width = 1280;
        Height = 780;
        MinimumSize = new Size(1000, 560);
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        ApplyLocalizedText();
        BuildLayout(status ?? string.Empty);
        LoadPreferences(preferences);
        FitToContent();
    }

    /// <summary>
    /// Altura inicial e mínima medidas do conteúdo, não fixadas em código: com rótulos que
    /// mudam de tamanho por idioma e por DPI, qualquer número escolhido a mão acaba escondendo
    /// algum quadro em alguma combinação. O mínimo passa a ser a altura que cabe tudo, então
    /// nem arrastando a borda o usuário consegue esconder um controle.
    ///
    /// Largura e altura ficam limitadas à área útil da tela: numa tela pequena a janela para
    /// de crescer e o <c>AutoScroll</c> do painel raiz assume, em vez de a janela nascer maior
    /// que o monitor.
    /// </summary>
    private void FitToContent()
    {
        if (_root is null)
        {
            return;
        }

        _root.PerformLayout();
        var contentHeight = _root.PreferredSize.Height;
        if (contentHeight <= 0)
        {
            return;
        }

        // A medição sai justa; a folga cobre o arredondamento do escalonamento por DPI, que de
        // outro modo pode comer o último pixel de um controle.
        const int SafetyMargin = 8;
        // A tela da IDE, não a primária: aqui o form ainda não tem handle, e `Screen.FromControl`
        // devolveria a primária. `GetWorkingArea` cai na janela principal do processo GeneXus,
        // que é o mesmo monitor onde `CenterOnIdeScreen` vai posicionar o diálogo.
        var working = ExtensionIdeScreenPlacement.GetWorkingArea(this, null);
        var chromeHeight = Height - ClientSize.Height;
        var desiredHeight = contentHeight + chromeHeight + SafetyMargin;

        var cappedWidth = Math.Min(Width, working.Width);
        var cappedHeight = Math.Min(desiredHeight, working.Height);

        MinimumSize = new Size(Math.Min(MinimumSize.Width, cappedWidth), cappedHeight);
        Size = new Size(cappedWidth, cappedHeight);
    }

    public PrototypeWizardPreferences? Preferences { get; private set; }

    private void BuildLayout(string status)
    {
        // Duas colunas: os quatro quadros ficam lado a lado em duas faixas, em vez de
        // empilhados. Encurta a janela quase pela metade e dá largura suficiente para os
        // rótulos longos do diagnóstico caberem em uma linha.
        //
        // Todas as linhas de conteúdo são AutoSize: elas medem o que cada quadro precisa, em
        // vez de repartir a altura disponível por porcentagem. Com porcentagem, um quadro cujo
        // conteúdo não coube era cortado em silêncio — foi o que aconteceu com «Tamanho máximo
        // da página» e com o segundo checkbox do diagnóstico —, e a única saída era o usuário
        // adivinhar que devia arrastar a borda da janela. A folga vai para a linha espaçadora,
        // e o AutoScroll cobre o resto (DPI alto, textos mais longos em outro idioma).
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
            AutoScroll = true,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);
        _root = root;

        var headerLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            MinimumSize = new Size(0, 28),
            Text = _texts.Translate("Preferencias gerais do wizard na KB ativa"),
            Padding = new Padding(0, 0, 0, 8),
        };
        root.Controls.Add(headerLabel, 0, 0);
        root.SetColumnSpan(headerLabel, 2);

        var statusBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = status,
            MinimumSize = new Size(0, 70),
        };
        root.Controls.Add(statusBox, 0, 1);
        root.SetColumnSpan(statusBox, 2);

        var optionsGroup = CreateContentGroup(_texts.Translate("Defaults de geracao"));

        var checks = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 3,
        };
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var index = 0; index < 3; index++)
        {
            checks.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        checks.Controls.Add(_generateSdtsCheck, 0, 0);
        checks.Controls.Add(_generateProceduresCheck, 1, 0);
        checks.Controls.Add(_generateApiObjectCheck, 0, 1);
        checks.Controls.Add(_applyBusinessComponentCheck, 1, 1);
        checks.Controls.Add(_applyListCheck, 0, 2);
        checks.Controls.Add(_generateMetadataCheck, 1, 2);
        optionsGroup.Controls.Add(checks);
        // Faixa de cima, à esquerda.
        root.Controls.Add(optionsGroup, 0, 2);

        var servicesGroup = CreateContentGroup(_texts.Translate("Servicos marcados por padrao"));

        var services = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };
        services.Controls.Add(_listServiceCheck);
        services.Controls.Add(_getServiceCheck);
        services.Controls.Add(_createServiceCheck);
        services.Controls.Add(_updateServiceCheck);
        services.Controls.Add(_deleteServiceCheck);
        servicesGroup.Controls.Add(services);
        // Faixa de baixo, à esquerda.
        root.Controls.Add(servicesGroup, 0, 3);

        var executionGroup = CreateContentGroup(_texts.Translate("Seguranca e paginacao"));

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
        // Faixa de cima, à direita.
        root.Controls.Add(executionGroup, 1, 2);

        // Quadro próprio, e por último: nenhuma destas opções pertence ao uso normal. A de
        // recuperação nasceu ocupando a linha de folga do quadro de geração, onde não é um
        // default de geração; a de supressão do Pump substitui a variável de ambiente
        // GOAB_B109_SUPPRESS_PUMP, que dependia do Windows e não tinha onde ser avisada.
        var diagnosticsGroup = CreateContentGroup(_texts.Translate("Diagnostico e recuperacao"));

        var diagnostics = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
        };
        diagnostics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        diagnostics.Controls.Add(
            CreateWrappingLabel(
                _texts.Translate("Opcoes de investigacao. Nao sao necessarias no uso normal da extensao."),
                new Padding(0, 0, 0, 6)),
            0,
            0);
        diagnostics.Controls.Add(_offerOrphanMetadataRecoveryCheck, 0, 1);
        diagnostics.Controls.Add(_suppressProgressPumpCheck, 0, 2);
        diagnosticsGroup.Controls.Add(diagnostics);
        // Faixa de baixo, à direita.
        root.Controls.Add(diagnosticsGroup, 1, 3);

        // Linha vazia que absorve a folga quando a janela é maior que o conteúdo. Sem ela, a
        // sobra iria para a última linha AutoSize e afastaria os botões do rodapé.
        var spacer = new Panel { Dock = DockStyle.Fill, Height = 0 };
        root.Controls.Add(spacer, 0, 4);
        root.SetColumnSpan(spacer, 2);

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
        root.Controls.Add(buttonsHost, 0, 5);
        root.SetColumnSpan(buttonsHost, 2);

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

    /// <summary>
    /// GroupBox que reporta a altura do próprio conteúdo, para a linha AutoSize do painel raiz
    /// poder reservá-la. O conteúdo precisa estar ancorado ao topo e também ser AutoSize.
    /// </summary>
    private static GroupBox CreateContentGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12),
        };
    }

    /// <summary>
    /// CheckBox cuja altura acompanha o texto. <c>AutoSize</c> não serve aqui: ele mede o
    /// rótulo em uma linha só e ignora a largura da célula, então o texto sairia do quadro.
    /// Com altura fixa, o rótulo que não cabe é quebrado em duas linhas e cortado — foi o que
    /// aconteceu com «Marcar REST via Business Component por padrão» a 125% de DPI, depois que
    /// as duas colunas estreitaram cada célula. Aqui a altura é medida a cada Resize, com a
    /// largura real disponível, e <paramref name="minimumHeight"/> é só o piso.
    /// </summary>
    private static CheckBox CreateCheckBox(string text, int minimumHeight = 30)
    {
        var check = new CheckBox
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = minimumHeight,
            Text = text,
            Margin = new Padding(0, 4, 0, 4),
        };

        // 26 px reservados para o quadrado do checkbox e o respiro até o texto.
        EnableAutoHeight(check, minimumHeight, reservedWidth: 26);
        return check;
    }

    /// <summary>
    /// Rótulo que ocupa a largura da célula e cresce em altura conforme o texto quebra.
    /// </summary>
    private static Label CreateWrappingLabel(string text, Padding padding, int minimumHeight = 24)
    {
        var label = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = minimumHeight,
            Text = text,
            Padding = padding,
        };

        EnableAutoHeight(label, minimumHeight, reservedWidth: padding.Horizontal);
        return label;
    }

    /// <summary>
    /// Faz o controle recalcular a própria altura sempre que a largura, o texto ou a fonte
    /// mudarem. <c>AutoSize</c> não resolve: ele mede o texto em uma linha só e ignora a
    /// largura da célula, então o rótulo transborda o quadro em vez de quebrar. Com altura
    /// fixa acontece o oposto — o texto quebra e é cortado, como ocorreu com «Marcar REST via
    /// Business Component por padrão» a 125% de DPI, depois que as duas colunas estreitaram
    /// cada célula.
    /// </summary>
    private static void EnableAutoHeight(Control control, int minimumHeight, int reservedWidth)
    {
        void Adjust(object? sender, EventArgs e)
        {
            if (control.Width <= 0 || string.IsNullOrEmpty(control.Text))
            {
                return;
            }

            const int VerticalPadding = 10;
            var availableWidth = Math.Max(control.Width - reservedWidth, 1);
            var measured = TextRenderer.MeasureText(
                control.Text,
                control.Font,
                new Size(availableWidth, int.MaxValue),
                TextFormatFlags.WordBreak);
            var desiredHeight = Math.Max(
                minimumHeight,
                measured.Height + VerticalPadding + control.Padding.Vertical);

            // A guarda encerra a recursão: atribuir Height dispara Resize de novo.
            if (control.Height != desiredHeight)
            {
                control.Height = desiredHeight;
            }
        }

        control.Resize += Adjust;
        control.TextChanged += Adjust;
        control.FontChanged += Adjust;
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
