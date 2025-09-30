using DocXHandler;
using DocXHandler.RecentFiles;
using Microsoft.VisualBasic.Logging;
using NLogShared;
using oaiUI.RecentFiles;
using oaiUI.Services;
using oaiVectorStore;
using OpenAI;
using System.Configuration;
using System.Reflection;

namespace oaiUI
{

    public partial class MainForm : Form
    {
        private VectorStoreManager _vectorStoreManager;
        private IUserInterface _userInterface;
        private List<string> selectedFolders = new List<string>();
        private readonly ILastSelectionService _lastSelectionService = new LastSelectionService();
        private bool _isRebinding = false;

        // Designer controls with null-forgiving operator to suppress CS8618
        private ComboBox comboBoxVectorStores = null!;
        private TextBox txtNewVectorStoreName = null!;
        private Button btnSelectFolders = null!;
        private ListBox listBoxSelectedFolders = null!;
        private Button btnUploadFiles = null!;
        private Button btnFileSizeSummary = null!;
        // private Button btnGetGitChanges = null!;

        private System.Windows.Forms.ComboBox cmbSettingsVectorStore;

        private int processedFolders = 0; // Initialize to suppress CS0649
        private void btnDeleteVectorStoreAssoc_Click(object sender, EventArgs e)
        {
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedVectorStore))
            {
                MessageBox.Show("Please select a vector store.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Remove the association from the dictionary
            if (_vectorStoreManager.Folders.ContainsKey(selectedVectorStore))
            {
                _vectorStoreManager.Folders.Remove(selectedVectorStore);

                // Update the UI (remove from combobox and clear selected folders)
                var currentDataSource = comboBoxVectorStores.DataSource as List<string>;
                if (currentDataSource != null)
                {
                    currentDataSource.Remove(selectedVectorStore);
                    comboBoxVectorStores.DataSource = null; // Temporarily detach
                    comboBoxVectorStores.DataSource = currentDataSource; // Reattach
                }
                comboBoxVectorStores.SelectedItem = null;
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();

                // Save the updated data to the JSON file
                _vectorStoreManager.SaveVectorStoreFolderData();

                MessageBox.Show($"Folder associations for vector store '{selectedVectorStore}' deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No folder associations found for vector store '{selectedVectorStore}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private CtxLogger _log = new CtxLogger();

        // Store the mapping between vector store and selected folders
        // private Dictionary<string, VectorStoreConfig> _vectorStoreFolders = new Dictionary<string, VectorStoreConfig>();
        // private string _vectorStoreFoldersFilePath; // Path to save the mapping
        // private VectorStoreConfig _vectorStoreConfig;
        
        private IRecentFilesManager _recentFilesManager;

        public MainForm()
        {
            InitializeComponent();

            _log.ConfigureXml("Config/LogConfig.xml");

            _userInterface = new WinFormsUserInterface(toolStripStatusLabelInfo, progressBar1);

            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStores_SelectedIndexChanged; // moved earlier
            _vectorStoreManager = new VectorStoreManager(ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? @"..\..\vectorStoreFolders.json", _userInterface);
            LoadVectorStores();
            _vectorStoreManager.LoadVectorStoreFolderData(); // Load saved folder data on startup

            Text = $"VecTool v{Assembly.GetExecutingAssembly().GetName().Version}";

            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            _recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            _recentFilesPanel = new oaiUI.RecentFiles.RecentFilesPanel(_recentFilesManager);

            tabPage3.Controls.Clear();
            tabPage3.Controls.Add(_recentFilesPanel);
            _recentFilesPanel.Dock = DockStyle.Fill;

            // NEW: Wire up tab selection event
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

        }
        private void TabControl1_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                // Check if Recent Files tab is selected (index 2)
                if (tabControl1.SelectedIndex == 2)
                {
                    _log.Info("Recent Files tab selected, refreshing panel.");
                    _recentFilesPanel?.RefreshList();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to refresh Recent Files panel on tab selection.");
            }
        }


        //private void SettingsTab_InitializeData()
        //{
        //    try
        //    {
        //        // Load known vector stores from persisted configs
        //        var all = VectorStoreConfig.LoadAll();
        //        var names = all.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        //        cmbSettingsVectorStore.Items.Clear();
        //        cmbSettingsVectorStore.Items.AddRange(names.Cast<object>().ToArray());
        //    }
        //    catch
        //    {
        //        // Be defensive; no hard failure if settings cannot be loaded
        //    }
        //}

        //private void SettingsTab_LoadSelection(string? name)
        //{
        //    var global = VectorStoreConfig.FromAppConfig();
        //    var all = VectorStoreConfig.LoadAll();
        //    if (string.IsNullOrWhiteSpace(name))
        //    {
        //        txtExcludedFiles.Text = string.Empty;
        //        txtExcludedFolders.Text = string.Empty;
        //        chkInheritExcludedFiles.Checked = true;
        //        chkInheritExcludedFolders.Checked = true;
        //        return;
        //    }

        //    all.TryGetValue(name, out var per);
        //    var vm = PerVectorStoreSettings.From(name!, global, per);

        //    chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
        //    chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;
        //    txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
        //    txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);

        //    txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
        //    txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;
        //}

        //private void cmbSettingsVectorStore_SelectedIndexChanged(object? sender, EventArgs e)
        //{
        //    var selected = cmbSettingsVectorStore.Text?.Trim();
        //    SettingsTab_LoadSelection(selected);
        //}

        //private void chkInheritExcludedFiles_CheckedChanged(object? sender, EventArgs e)
        //{
        //    txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
        //}

        //private void chkInheritExcludedFolders_CheckedChanged(object? sender, EventArgs e)
        //{
        //    txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
        //}

        //private void btnSaveVsSettings_Click(object? sender, EventArgs e)
        //{
        //    var name = (cmbSettingsVectorStore.Text ?? string.Empty).Trim();
        //    if (string.IsNullOrWhiteSpace(name))
        //    {
        //        MessageBox.Show("Please enter or select a vector store name.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }

        //    var global = VectorStoreConfig.FromAppConfig();
        //    var all = VectorStoreConfig.LoadAll();

        //    var files = SplitLines(txtExcludedFiles.Text);
        //    var folders = SplitLines(txtExcludedFolders.Text);

        //    var vm = new PerVectorStoreSettings(
        //        name,
        //        useCustomExcludedFiles: !chkInheritExcludedFiles.Checked,
        //        useCustomExcludedFolders: !chkInheritExcludedFolders.Checked,
        //        customExcludedFiles: files,
        //        customExcludedFolders: folders);

        //    PerVectorStoreSettings.Save(all, vm, global);
        //    VectorStoreConfig.SaveAll(all);
        //    MessageBox.Show("Settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //}

        //private void btnResetVsSettings_Click(object? sender, EventArgs e)
        //{
        //    chkInheritExcludedFiles.Checked = true;
        //    chkInheritExcludedFolders.Checked = true;
        //    var global = VectorStoreConfig.FromAppConfig();
        //    txtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles ?? new List<string>());
        //    txtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders ?? new List<string>());
        //}

        //private static List<string> SplitLines(string text)
        //{
        //    return (text ?? string.Empty)
        //        .Replace("\r\n", "\n", StringComparison.Ordinal)
        //        .Split('\n')
        //        .Select(s => s.Trim())
        //        .Where(s => s.Length > 0)
        //        .Distinct(StringComparer.OrdinalIgnoreCase)
        //        .ToList();
        //}

        //protected override void OnLoad(EventArgs e)
        //{
        //    base.OnLoad(e);
        //    SettingsTab_InitializeData();
        //    cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStore_SelectedIndexChanged;
        //    chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFiles_CheckedChanged;
        //    chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFolders_CheckedChanged;
        //}

        private void btnClearFolders_Click(object sender, EventArgs e)
        {
            listBoxSelectedFolders.Items.Clear();
            selectedFolders.Clear();
            // Update the stored mapping when clearing folders
            if (comboBoxVectorStores.SelectedItem != null)
            {
                string selectedVectorStoreName = comboBoxVectorStores.SelectedItem.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(selectedVectorStoreName) && _vectorStoreManager.Folders.ContainsKey(selectedVectorStoreName))
                {
                    _vectorStoreManager.Folders[selectedVectorStoreName] = new VectorStoreConfig();
                    _vectorStoreManager.SaveVectorStoreFolderData();
                }
            }
        }

        private async void LoadVectorStores()
        {
            // Load vector store folder data first
            _vectorStoreManager.LoadVectorStoreFolderData();
            _isRebinding = true;
            try
            {
                _vectorStoreManager.LoadVectorStoreFolderData();
                try
                {
                    using var api = new OpenAIClient();
                    try
                    {
                        var vectorStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);
                        var combinedStores = vectorStores.Values
                            .Where(v => !_vectorStoreManager.Folders.ContainsKey(v))
                            .Union(_vectorStoreManager.Folders.Keys)
                            .Distinct()
                            .ToList();

                        comboBoxVectorStores.DataSource = combinedStores;
                        RestoreLastSelectedVectorStore();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading vector stores from OpenAI: {ex.Message}. Using local data.", "OpenAI Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        var localStores = _vectorStoreManager.Folders.Keys.Distinct().ToList();
                        comboBoxVectorStores.DataSource = localStores;
                        RestoreLastSelectedVectorStore();
                    }
                }
                catch
                {
                    MessageBox.Show("OpenAI API key is not configured. Cannot upload files.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var localStores = _vectorStoreManager.Folders.Keys.Distinct().ToList();
                    comboBoxVectorStores.DataSource = localStores;
                    RestoreLastSelectedVectorStore();
                }
            }
            finally
            {
                _isRebinding = false;
                PersistLastSelectedVectorStore(); // ensure file reflects the actual selected after restore
            }
        }

        private string? GetVectorStoreName()
        {
            // Get the vector store name
            string newVectorStoreName = txtNewVectorStoreName.Text.Trim();
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            string? vectorStoreName = string.IsNullOrEmpty(newVectorStoreName) ? selectedVectorStore : newVectorStoreName;

            // Modify the underlying data source
            var currentDataSource = comboBoxVectorStores.DataSource as List<string>;
            if (currentDataSource != null && !string.IsNullOrEmpty(newVectorStoreName) && !currentDataSource.Contains(newVectorStoreName))
            {
                currentDataSource.Add(newVectorStoreName);
                comboBoxVectorStores.DataSource = null; // Temporarily detach the data source
                comboBoxVectorStores.DataSource = currentDataSource; // Reattach the updated data source
                comboBoxVectorStores.SelectedItem = newVectorStoreName; // Select the new item
                SelectVectorStoreAndPersist(newVectorStoreName);
            }
            else if (!string.IsNullOrEmpty(selectedVectorStore))
            {
                comboBoxVectorStores.SelectedItem = selectedVectorStore;
                SelectVectorStoreAndPersist(selectedVectorStore);
            }

            // Clear the new vector store name textbox if a new name was used
            if (!string.IsNullOrEmpty(newVectorStoreName))
            {
                txtNewVectorStoreName.Text = "";
            }

            return vectorStoreName;
        }

        private void btnSelectFolders_Click(object sender, EventArgs e)
        {
            var vectorStoreName = GetVectorStoreName();
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select folders containing files to upload";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    if (!selectedFolders.Contains(selectedPath))
                    {
                        selectedFolders.Add(selectedPath);
                        UpdateSelectedFoldersUI();

                        // Save the selected folder for the current vector store
                        if (!string.IsNullOrEmpty(vectorStoreName))
                        {
                            if (!_vectorStoreManager.Folders.ContainsKey(vectorStoreName))
                            {
                                _vectorStoreManager.Folders[vectorStoreName] = new VectorStoreConfig
                                {
                                    ExcludedFiles = new List<string>(_vectorStoreManager.Config.ExcludedFiles),
                                    ExcludedFolders = new List<string>(_vectorStoreManager.Config.ExcludedFolders)
                                };
                            }
                            _vectorStoreManager.Folders[vectorStoreName].FolderPaths.Add(selectedPath);
                            _vectorStoreManager.SaveVectorStoreFolderData();
                        }
                    }
                }
            }
        }

        private void UpdateSelectedFoldersUI()
        {
            listBoxSelectedFolders.Items.Clear();
            foreach (var folder in selectedFolders)
            {
                listBoxSelectedFolders.Items.Add(folder);
            }
        }

        void WorkStart(string str, List<string> selectedFolders)
        {
            _userInterface.WorkStart(str, selectedFolders);
            btnDeleteAllVSFiles.Enabled = false;
            btnUploadFiles.Enabled = false;
            btnUploadNew.Enabled = false;
        }

        void WorkFinish()
        {
            _userInterface.WorkFinish();
            btnDeleteAllVSFiles.Enabled = true;
            btnUploadFiles.Enabled = true;
            btnUploadNew.Enabled = true;
        }

        private async void btnUploadFiles_Click(object sender, EventArgs e)
        {
            try
            {
                string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
                VectorStoreConfig? vectorStoreConfig = GetVectorStore(selectedVectorStore);
                if (vectorStoreConfig == null)
                {
                    return;
                }

                WorkStart("Upload/Replace files", selectedFolders);
                string newVectorStoreName = txtNewVectorStoreName.Text.Trim();
                string? vectorStoreName = string.IsNullOrEmpty(newVectorStoreName) ? selectedVectorStore : newVectorStoreName;

                try
                {
                    string? vectorStoreId = await _vectorStoreManager.RecreateVectorStore(vectorStoreName);

                    // Add new vector store name to the combo box if it's not already there
                    if (!string.IsNullOrEmpty(newVectorStoreName) && !comboBoxVectorStores.Items.Contains(newVectorStoreName))
                    {
                        comboBoxVectorStores.Items.Add(newVectorStoreName);
                    }

                    // Select the vector store in the combo box
                    comboBoxVectorStores.SelectedItem = vectorStoreName;
                    
                    if (!string.IsNullOrWhiteSpace(vectorStoreName))
                        SelectVectorStoreAndPersist(vectorStoreName);

                    // Clear the new vector store name textbox
                    txtNewVectorStoreName.Text = "";

                    // Upload files from all selected folders
                    await _vectorStoreManager.UploadFiles(vectorStoreId, selectedFolders);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading files: {ex.Message}");
                }
            }
            finally
            {
                WorkFinish();
            }
        }

        private void btnConvertToDocx_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {

                string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
                VectorStoreConfig? vectorStoreConfig= GetVectorStore(selectedVectorStore);
                if (vectorStoreConfig == null)
                {
                    return;
                }

                saveFileDialog.Filter = "Word Document|*.docx";
                saveFileDialog.Title = "Save DOCX File";
                saveFileDialog.DefaultExt = "docx";
                if (txtNewVectorStoreName.Text.Trim().Length > 0)
                { saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim(); }
                else
                { saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString(); }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _userInterface.WorkStart("Converting to DOCX", selectedFolders);

                        btnConvertToDocx.Enabled = false;
                        var docXHandler = new DocXHandler.DocXHandler(_userInterface, _recentFilesManager);
                        docXHandler.ConvertSelectedFoldersToDocx(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                        MessageBox.Show("Folders successfully converted to DOCX.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //catch (Exception ex)
                    //{
                    //    _log.Error(ex, "Error converting folders to DOCX");
                    //    MessageBox.Show($"Error converting folders to DOCX: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //}
                    finally
                    {
                        _userInterface.WorkFinish();
                        btnConvertToDocx.Enabled = true;
                    }
                }
            }
        }

        private VectorStoreConfig? GetVectorStore(string? selectedVectorStore)
        {
            VectorStoreConfig? vectorStoreConfig = null;
            if (!string.IsNullOrEmpty(selectedVectorStore) && _vectorStoreManager.Folders.ContainsKey(selectedVectorStore))
            {
                vectorStoreConfig = _vectorStoreManager.Folders[selectedVectorStore];
            }

            if (vectorStoreConfig == null)
            {
                MessageBox.Show("Please select a vector store first.", "No Vector Store Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            else
            {
                return vectorStoreConfig;
            }
        }

        private void btnConvertToMD_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            VectorStoreConfig? vectorStoreConfig = GetVectorStore(selectedVectorStore);
            if (vectorStoreConfig == null)
            {
                return;
            }


            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "MarkDown Document|*.md";
                saveFileDialog.Title = "Save MD File";
                saveFileDialog.DefaultExt = "md";
                if (txtNewVectorStoreName.Text.Trim().Length > 0)
                { saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim(); }
                else
                { saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString(); }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        btnConvertToMd.Enabled = false;
                        var mdHandler = new DocXHandler.MDHandler(_userInterface, _recentFilesManager);
                        mdHandler.ExportSelectedFolders(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                        MessageBox.Show("Folders successfully converted to MD.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error converting folders to MD: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnConvertToMd.Enabled = true;
                    }
                }
            }
        }

        private async void btnConvertToPdf_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            VectorStoreConfig? vectorStoreConfig = GetVectorStore(selectedVectorStore);
            if (vectorStoreConfig == null)  
            {   
                return; 
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PDF Document|*.pdf";
                saveFileDialog.Title = "Save PDF File";
                saveFileDialog.DefaultExt = "pdf";


                if (txtNewVectorStoreName.Text.Trim().Length > 0)
                {
                    saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim();
                }
                else
                {
                    saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString();
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    btnConvertToPdf.Enabled = false;
                    try
                    {
                        WorkStart("Converting to PDF...", selectedFolders);
                        var pdfHandler = new DocXHandler.PdfHandler(_userInterface, _recentFilesManager);
                        pdfHandler.ConvertSelectedFoldersToPdf(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                        MessageBox.Show("Folders successfully converted to PDF.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error converting folders to PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        WorkFinish();
                        btnConvertToPdf.Enabled = true;
                    }
                }
            }
        }

        private async void btnGetGitChanges_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Markdown Document(*.md)|*.md";
            saveFileDialog.Title = "Save Git Changes File";
            saveFileDialog.DefaultExt = "md";

            const string gitChangesFileNameSuffix = "-git-changes";

            if (txtNewVectorStoreName.Text.Trim().Length > 0)
                saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim() + gitChangesFileNameSuffix;
            else
                saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString() + gitChangesFileNameSuffix;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    btnGetGitChanges.Enabled = false;
                    WorkStart("Getting Git changes...", selectedFolders);
                    var gitChangesHandler = new DocXHandler.GitChangesHandler(_userInterface, _recentFilesManager);

                    // Use the async method
                    string changes = await gitChangesHandler.GetGitChangesAsync(selectedFolders, saveFileDialog.FileName);

                    if (string.IsNullOrWhiteSpace(changes))
                    {
                        MessageBox.Show("No Git changes found in the selected folders.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Git changes successfully saved to file.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error getting Git changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    WorkFinish();
                    btnGetGitChanges.Enabled = true;
                }
            }
        }


        private void UpdateProgress()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(UpdateProgress));
            }
            else
            {
                progressBar1.Value = processedFolders;
                progressBar1.Update();
                Application.DoEvents();
            }
            toolStripStatusLabelMax.Text = progressBar1.Maximum.ToString();
            toolStripStatusLabelCurrent.Text = progressBar1.Value.ToString();
        }

        private async void btnDeleteAllVSFiles_ClickAsync(object sender, EventArgs e)
        {
            using var api = new OpenAIClient();
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedVectorStore))
            {
                MessageBox.Show("Please select a vector store.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                WorkStart($"Delete VectorStore files from {selectedVectorStore}", selectedFolders);
                var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);
                if (existingStores.ContainsKey(selectedVectorStore))
                {
                    var vectorStoreId = existingStores.First(s => s.Key == selectedVectorStore).Key;
                    await _vectorStoreManager.DeleteAllVSFiles(api, vectorStoreId);
                    var fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId);
                    toolStripStatusLabelInfo.Text = $"Files deleted successfully. Remaining:{fileIds.Count}";
                    MessageBox.Show($"Files deleted successfully. Remaining:{fileIds.Count}");
                }
                else
                {
                    MessageBox.Show($"Vector store '{selectedVectorStore}' not found on OpenAI.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting files: {ex.Message}");
            }
        }

        private void btnUploadNew_Click(object sender, EventArgs e)
        {
            // Implementation for uploading new files (if needed)
        }

        private void btnFileSizeSummary_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            VectorStoreConfig? vectorStoreConfig = GetVectorStore(selectedVectorStore);
            if (vectorStoreConfig == null)
            {
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Markdown File|*.md";
                saveFileDialog.Title = "Save File Size Summary";

                string fileName = $"{comboBoxVectorStores.SelectedItem}_size_summary";

                saveFileDialog.FileName = fileName;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        btnFileSizeSummary.Enabled = false;
                        _userInterface.WorkStart("Generating file size summary...", selectedFolders);
                        var sizeHandler = new FileSizeSummaryHandler(_userInterface, _recentFilesManager);
                        sizeHandler.GenerateFileSizeSummary(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);

                        MessageBox.Show("File size summary generated successfully.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error generating file size summary: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _userInterface.WorkFinish();
                        btnFileSizeSummary.Enabled = true;
                    }
                }
            }
        }

        private void comboBoxVectorStores_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                string? selectedVectorStoreName = comboBox.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedVectorStoreName))
                {
                    LoadSelectedFoldersForVectorStore(selectedVectorStoreName);
                    if (!_isRebinding)
                        _lastSelectionService.SetLastSelectedVectorStore(selectedVectorStoreName);
                }
            }
        }

        private void RestoreLastSelectedVectorStore()
        {
            try
            {
                var last = _lastSelectionService.GetLastSelectedVectorStore();
                if (string.IsNullOrWhiteSpace(last))
                {
                    // Fallback: select first if available
                    if (comboBoxVectorStores.Items.Count > 0)
                        comboBoxVectorStores.SelectedIndex = 0;
                    return;
                }

                // Try exact match; if not found, fallback to first item
                if (comboBoxVectorStores.Items.Contains(last))
                {
                    comboBoxVectorStores.SelectedItem = last;
                }
                else if (comboBoxVectorStores.Items.Count > 0)
                {
                    comboBoxVectorStores.SelectedIndex = 0;
                }
            }
            catch
            {
                if (comboBoxVectorStores.Items.Count > 0)
                    comboBoxVectorStores.SelectedIndex = 0;
            }
        }

        private void PersistLastSelectedVectorStore()
        {
            var name = comboBoxVectorStores.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                _lastSelectionService.SetLastSelectedVectorStore(name);
            }
        }

        private void SelectVectorStoreAndPersist(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            comboBoxVectorStores.SelectedItem = name;
            if (!_isRebinding)
                PersistLastSelectedVectorStore();
        }

        private void LoadSelectedFoldersForVectorStore(string? vectorStoreName)
        {
            if (!string.IsNullOrEmpty(vectorStoreName))
            {
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();

                if (_vectorStoreManager.Folders.ContainsKey(vectorStoreName))
                {
                    selectedFolders.AddRange(_vectorStoreManager.Folders[vectorStoreName].FolderPaths);
                    UpdateSelectedFoldersUI();
                }
            }
        }
    }
}
