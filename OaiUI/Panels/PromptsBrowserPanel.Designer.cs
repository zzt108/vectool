#nullable enable
namespace VecTool.UI.Panels
{
    partial class PromptsBrowserPanel
    {
        private System.ComponentModel.IContainer components = null!;
        private ToolTip toolTip = null!;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel = null!;
        private System.Windows.Forms.Label lblFilter = null!;
        private System.Windows.Forms.ComboBox cmbFilterType = null!;
        private System.Windows.Forms.TextBox txtSearch = null!;
        private System.Windows.Forms.Button btnRefresh = null!;
        private System.Windows.Forms.TreeView treeViewHierarchy = null!;
        private System.Windows.Forms.ListView lvResults = null!;
        private System.Windows.Forms.Panel buttonPanel = null!;
        private System.Windows.Forms.Button btnCopy = null!;
        private System.Windows.Forms.Button btnEdit = null!;
        private System.Windows.Forms.Button btnNew = null!;
        private System.Windows.Forms.Button btnGit = null!;
        private System.Windows.Forms.Label lblStatus = null!;
        private System.Windows.Forms.SplitContainer splitContainerMain = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.cmbFilterType = new System.Windows.Forms.ComboBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            // Ensure SplitContainer fills the entire tab
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.None; // Allow both panels to resize
            this.splitContainerMain.SplitterDistance = 250; // Initial TreeView width
            this.splitContainerMain.SplitterWidth = 6; // Make splitter easier to grab

            this.treeViewHierarchy = new System.Windows.Forms.TreeView();
            this.lvResults = new System.Windows.Forms.ListView();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnGit = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();

            this.tableLayoutPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();

            // tableLayoutPanel
            this.tableLayoutPanel.ColumnCount = 4;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel.Controls.Add(this.lblFilter, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.cmbFilterType, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.txtSearch, 2, 0);
            this.tableLayoutPanel.Controls.Add(this.btnRefresh, 3, 0);
            this.tableLayoutPanel.Controls.Add(this.splitContainerMain, 0, 1); // Span all 4 columns
            this.tableLayoutPanel.SetColumnSpan(this.splitContainerMain, 4);
            this.tableLayoutPanel.Controls.Add(this.buttonPanel, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.lblStatus, 0, 3);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.Size = new System.Drawing.Size(900, 600);
            this.tableLayoutPanel.TabIndex = 0;

            // lblFilter
            this.lblFilter.AutoSize = true;
            this.lblFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFilter.Location = new System.Drawing.Point(6, 6);
            this.lblFilter.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(54, 23);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "Filter:";
            this.lblFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbFilterType
            this.cmbFilterType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbFilterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilterType.Items.AddRange(new object[] { "PROMPT*", "GUIDE*", "SPACE*", "All" });
            this.cmbFilterType.Location = new System.Drawing.Point(66, 6);
            this.cmbFilterType.Margin = new System.Windows.Forms.Padding(6);
            this.cmbFilterType.Name = "cmbFilterType";
            this.cmbFilterType.Size = new System.Drawing.Size(108, 23);
            this.cmbFilterType.TabIndex = 1;
            this.cmbFilterType.SelectedIndexChanged += new System.EventHandler(this.cmbFilterTypeSelectedIndexChanged);

            // txtSearch
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Location = new System.Drawing.Point(186, 6);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search prompts...";
            this.txtSearch.Size = new System.Drawing.Size(618, 23);
            this.txtSearch.TabIndex = 2;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearchTextChanged);

            // btnRefresh
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Location = new System.Drawing.Point(816, 6);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(6);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(78, 23);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "🔍";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefreshClick);

            // splitContainerMain configuration
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();

            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(6, 41);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(6);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Vertical;
            // 🔄 4:1 ratio for 900px panel = 720px tree : 180px list (80% : 20%)
            this.splitContainerMain.SplitterDistance = 720;
            this.splitContainerMain.SplitterWidth = 4;
            this.splitContainerMain.Size = new System.Drawing.Size(888, 473); // Row 1 height from TableLayout
            this.splitContainerMain.TabIndex = 4;
            this.splitContainerMain.Panel1MinSize = 200;
            this.splitContainerMain.Panel2MinSize = 150;

            // treeViewHierarchy
            this.treeViewHierarchy.MinimumSize = new System.Drawing.Size(150, 0); // Allow narrow collapse

            this.treeViewHierarchy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewHierarchy.Name = "treeViewHierarchy";
            this.treeViewHierarchy.TabIndex = 0;
            this.treeViewHierarchy.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewHierarchyAfterSelect);
            this.splitContainerMain.Panel1.Controls.Add(this.treeViewHierarchy);

            // lvResults
            this.tableLayoutPanel.SetColumnSpan(this.lvResults, 3);
            this.lvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvResults.FullRowSelect = true;
            this.lvResults.GridLines = true;
            this.lvResults.MultiSelect = false;
            this.lvResults.Name = "lvResults";
            this.lvResults.TabIndex = 1;
            this.lvResults.UseCompatibleStateImageBehavior = false;
            this.lvResults.View = System.Windows.Forms.View.Details;
            this.lvResults.Columns.Add("Fav", 40);
            this.lvResults.Columns.Add("Name", 200);
            this.lvResults.Columns.Add("Version", 60);
            this.lvResults.Columns.Add("Type", 80);
            this.lvResults.Columns.Add("Category", 120);
            this.lvResults.Columns.Add("Modified", 140);
            this.lvResults.ItemActivate += new System.EventHandler(this.lvResultsItemActivate);
            this.lvResults.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvResultsMouseClick);
            this.splitContainerMain.Panel2.Controls.Add(this.lvResults);

            // buttonPanel
            this.tableLayoutPanel.SetColumnSpan(this.buttonPanel, 4);
            this.buttonPanel.Controls.Add(this.btnCopy);
            this.buttonPanel.Controls.Add(this.btnEdit);
            this.buttonPanel.Controls.Add(this.btnNew);
            this.buttonPanel.Controls.Add(this.btnGit);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(6, 526);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(6);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(888, 38);
            this.buttonPanel.TabIndex = 6;

            // btnCopy
            this.btnCopy.Location = new System.Drawing.Point(10, 6);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(90, 28);
            this.btnCopy.TabIndex = 0;
            this.btnCopy.Text = "📋 Copy";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopyClick);

            // btnEdit
            this.btnEdit.Location = new System.Drawing.Point(110, 6);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(90, 28);
            this.btnEdit.TabIndex = 1;
            this.btnEdit.Text = "✏️ Edit";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEditClick);

            // btnNew
            this.btnNew.Location = new System.Drawing.Point(210, 6);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(90, 28);
            this.btnNew.TabIndex = 2;
            this.btnNew.Text = "➕ New";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNewClick);

            // btnGit
            this.btnGit.Location = new System.Drawing.Point(310, 6);
            this.btnGit.Name = "btnGit";
            this.btnGit.Size = new System.Drawing.Size(90, 28);
            this.btnGit.TabIndex = 3;
            this.btnGit.Text = "🔗 Git";
            this.btnGit.UseVisualStyleBackColor = true;
            this.btnGit.Click += new System.EventHandler(this.btnGitClick);

            // lblStatus
            this.tableLayoutPanel.SetColumnSpan(this.lblStatus, 4);
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(6, 576);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(6);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(888, 18);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "0 prompt(s)";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // PromptsBrowserPanel
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "PromptsBrowserPanel";
            this.Size = new System.Drawing.Size(900, 600);

            // Resume layout with SplitContainer
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
