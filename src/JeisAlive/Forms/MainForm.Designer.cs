namespace JeisAlive.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        // ── Form ────────────────────────────────────────────────
        this.SuspendLayout();
        this.AutoScaleDimensions = new SizeF(7F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(750, 720);
        this.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
        this.Text = "JeisAlive";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(600, 600);
        this.BackColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // ── Window Icon ─────────────────────────────────────────
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
        if (File.Exists(iconPath))
            this.Icon = new Icon(iconPath);

        // ── GroupBox: Payload ────────────────────────────────────
        grpPayload = new GroupBox();
        grpPayload.Text = "Payload";
        grpPayload.Location = new Point(12, 12);
        grpPayload.Size = new Size(720, 60);
        grpPayload.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        txtPayloadPath = new TextBox();
        txtPayloadPath.ReadOnly = true;
        txtPayloadPath.Location = new Point(12, 24);
        txtPayloadPath.Size = new Size(600, 25);
        txtPayloadPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnBrowsePayload = new Button();
        btnBrowsePayload.Text = "Browse...";
        btnBrowsePayload.Location = new Point(620, 22);
        btnBrowsePayload.Size = new Size(88, 28);
        btnBrowsePayload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowsePayload.Click += btnBrowsePayload_Click;

        grpPayload.Controls.Add(txtPayloadPath);
        grpPayload.Controls.Add(btnBrowsePayload);

        // ── GroupBox: Output ────────────────────────────────────
        grpOutput = new GroupBox();
        grpOutput.Text = "Output";
        grpOutput.Location = new Point(12, 80);
        grpOutput.Size = new Size(720, 95);
        grpOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        lblFormat = new Label();
        lblFormat.Text = "Format:";
        lblFormat.Location = new Point(12, 28);
        lblFormat.AutoSize = true;

        cboFormat = new ComboBox();
        cboFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFormat.Items.AddRange(new object[] { "Native EXE", "Batch (.bat)" });
        cboFormat.Location = new Point(72, 24);
        cboFormat.Size = new Size(150, 25);
        cboFormat.SelectedIndexChanged += cboFormat_SelectedIndexChanged;

        txtOutputPath = new TextBox();
        txtOutputPath.ReadOnly = true;
        txtOutputPath.Location = new Point(12, 58);
        txtOutputPath.Size = new Size(600, 25);
        txtOutputPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnBrowseOutput = new Button();
        btnBrowseOutput.Text = "Save As...";
        btnBrowseOutput.Location = new Point(620, 56);
        btnBrowseOutput.Size = new Size(88, 28);
        btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOutput.Click += btnBrowseOutput_Click;

        grpOutput.Controls.Add(lblFormat);
        grpOutput.Controls.Add(cboFormat);
        grpOutput.Controls.Add(txtOutputPath);
        grpOutput.Controls.Add(btnBrowseOutput);

        // ── GroupBox: Protection ────────────────────────────────
        grpProtection = new GroupBox();
        grpProtection.Text = "Protection";
        grpProtection.Location = new Point(12, 183);
        grpProtection.Size = new Size(720, 58);
        grpProtection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        flpProtection = new FlowLayoutPanel();
        flpProtection.Location = new Point(12, 22);
        flpProtection.Size = new Size(696, 30);
        flpProtection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        chkAntiDebug = new CheckBox();
        chkAntiDebug.Text = "Anti-Debug";
        chkAntiDebug.Checked = true;
        chkAntiDebug.AutoSize = true;
        chkAntiDebug.Margin = new Padding(6, 3, 20, 3);

        chkAntiVM = new CheckBox();
        chkAntiVM.Text = "Anti-VM";
        chkAntiVM.Checked = true;
        chkAntiVM.AutoSize = true;
        chkAntiVM.Margin = new Padding(6, 3, 20, 3);

        chkMelt = new CheckBox();
        chkMelt.Text = "Self-Delete (Melt)";
        chkMelt.Checked = true;
        chkMelt.AutoSize = true;
        chkMelt.Margin = new Padding(6, 3, 20, 3);

        flpProtection.Controls.Add(chkAntiDebug);
        flpProtection.Controls.Add(chkAntiVM);
        flpProtection.Controls.Add(chkMelt);
        grpProtection.Controls.Add(flpProtection);

        // ── GroupBox: Binder ────────────────────────────────────
        grpBinder = new GroupBox();
        grpBinder.Text = "Binder";
        grpBinder.Location = new Point(12, 249);
        grpBinder.Size = new Size(720, 190);
        grpBinder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        lstBoundFiles = new ListView();
        lstBoundFiles.View = View.Details;
        lstBoundFiles.FullRowSelect = true;
        lstBoundFiles.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        lstBoundFiles.Location = new Point(12, 24);
        lstBoundFiles.Size = new Size(696, 120);
        lstBoundFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        lstBoundFiles.Columns.Add("File Name", 350);
        lstBoundFiles.Columns.Add("Action", 120);
        lstBoundFiles.SelectedIndexChanged += lstBoundFiles_SelectedIndexChanged;

        pnlBinderButtons = new Panel();
        pnlBinderButtons.Location = new Point(12, 150);
        pnlBinderButtons.Size = new Size(696, 32);
        pnlBinderButtons.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        btnAddFile = new Button();
        btnAddFile.Text = "Add File...";
        btnAddFile.Location = new Point(0, 2);
        btnAddFile.Size = new Size(88, 28);
        btnAddFile.Click += btnAddFile_Click;

        btnRemoveFile = new Button();
        btnRemoveFile.Text = "Remove";
        btnRemoveFile.Location = new Point(96, 2);
        btnRemoveFile.Size = new Size(88, 28);
        btnRemoveFile.Click += btnRemoveFile_Click;

        cboFileAction = new ComboBox();
        cboFileAction.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFileAction.Items.AddRange(new object[] { "Open", "Execute" });
        cboFileAction.Location = new Point(200, 3);
        cboFileAction.Size = new Size(110, 25);
        cboFileAction.SelectedIndexChanged += cboFileAction_SelectedIndexChanged;

        pnlBinderButtons.Controls.Add(btnAddFile);
        pnlBinderButtons.Controls.Add(btnRemoveFile);
        pnlBinderButtons.Controls.Add(cboFileAction);

        grpBinder.Controls.Add(lstBoundFiles);
        grpBinder.Controls.Add(pnlBinderButtons);

        // ── ProgressBar ─────────────────────────────────────────
        prgProgress = new ProgressBar();
        prgProgress.Location = new Point(12, 447);
        prgProgress.Size = new Size(720, 22);
        prgProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // ── Build Button ────────────────────────────────────────
        btnBuild = new Button();
        btnBuild.Text = "Build";
        btnBuild.Size = new Size(120, 35);
        btnBuild.Location = new Point(315, 477);
        btnBuild.BackColor = Color.FromArgb(0, 120, 212);
        btnBuild.ForeColor = Color.White;
        btnBuild.FlatStyle = FlatStyle.Flat;
        btnBuild.FlatAppearance.BorderSize = 0;
        btnBuild.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        btnBuild.Anchor = AnchorStyles.Top;
        btnBuild.Click += btnBuild_Click;

        // ── GroupBox: Log ───────────────────────────────────────
        grpLog = new GroupBox();
        grpLog.Text = "Log";
        grpLog.Location = new Point(12, 520);
        grpLog.Size = new Size(720, 188);
        grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        txtLog = new TextBox();
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Location = new Point(12, 24);
        txtLog.Size = new Size(696, 152);
        txtLog.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        grpLog.Controls.Add(txtLog);

        // ── About Button ────────────────────────────────────────
        btnAbout = new Button();
        btnAbout.Text = "About";
        btnAbout.Size = new Size(60, 35);
        btnAbout.Location = new Point(445, 477);
        btnAbout.Anchor = AnchorStyles.Top;
        btnAbout.FlatStyle = FlatStyle.Flat;
        btnAbout.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
        btnAbout.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        btnAbout.ForeColor = Color.FromArgb(80, 80, 80);
        btnAbout.Cursor = Cursors.Hand;
        btnAbout.Click += btnAbout_Click;

        // ── Add all to form ─────────────────────────────────────
        this.Controls.Add(grpPayload);
        this.Controls.Add(grpOutput);
        this.Controls.Add(grpProtection);
        this.Controls.Add(grpBinder);
        this.Controls.Add(prgProgress);
        this.Controls.Add(btnBuild);
        this.Controls.Add(grpLog);
        this.Controls.Add(btnAbout);

        this.ResumeLayout(false);
    }

    #endregion

    private GroupBox grpPayload;
    private TextBox txtPayloadPath;
    private Button btnBrowsePayload;

    private GroupBox grpOutput;
    private Label lblFormat;
    private ComboBox cboFormat;
    private TextBox txtOutputPath;
    private Button btnBrowseOutput;

    private GroupBox grpProtection;
    private FlowLayoutPanel flpProtection;
    private CheckBox chkAntiDebug;
    private CheckBox chkAntiVM;
    private CheckBox chkMelt;

    private GroupBox grpBinder;
    private ListView lstBoundFiles;
    private Panel pnlBinderButtons;
    private Button btnAddFile;
    private Button btnRemoveFile;
    private ComboBox cboFileAction;

    private ProgressBar prgProgress;
    private Button btnBuild;
    private Button btnAbout;

    private GroupBox grpLog;
    private TextBox txtLog;
}
