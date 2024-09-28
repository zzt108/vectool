using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using OpenAI_API;
using OpenAI_API.Embedding;
using System.Configuration;

namespace oai
{
    public partial class Form1 : Form
    {
        private OpenAIAPI api;
        private string apiKey;
        private List<(string FileName, Vector<float> Embedding)> vectorStore = new List<(string, Vector<float>)>();

        public Form1()
        {
            InitializeComponent();
            apiKey = ConfigurationManager.AppSettings["OaiApiKey"];
            api = new OpenAIAPI(apiKey);
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = folderBrowser.SelectedPath;
                }
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFolderPath.Text))
            {
                MessageBox.Show("Please select a folder first.");
                return;
            }

            string[] extensions = { ".cs", ".csproj", ".sln" };
            var files = Directory.GetFiles(txtFolderPath.Text, "*.*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

            progressBar.Maximum = files.Count();
            progressBar.Value = 0;

            foreach (var file in files)
            {
                string content = File.ReadAllText(file);
                var embeddingResult = await api.Embeddings.CreateEmbeddingAsync(content);
                var embedding = new Vector<float>(embeddingResult.Data[0].Embedding.ToArray());
                vectorStore.Add((file, embedding));
                progressBar.Value++;
            }

            MessageBox.Show($"Uploaded {vectorStore.Count} files to the vector store.");
        }

        private void btnAskQuestion_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtQuestion.Text))
            {
                MessageBox.Show("Please enter a question.");
                return;
            }

            // For simplicity, we'll just show the file names in the vector store
            txtAnswer.Text = string.Join(Environment.NewLine, vectorStore.Select(v => v.FileName));
        }
    }
}