using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Doc_Recherche
{
    public partial class Form1 : Form
    {
        private List<string> allResults = new List<string>(); // Stocker les résultats complets

        public Form1()
        {
            InitializeComponent();
            lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(10, 150), // Ajuste selon ton interface
                ForeColor = Color.Blue,
                Visible = false
            };
            Controls.Add(lblStatus);
            bindingSource.DataSource = allResults;
            lstResultats.DataSource = bindingSource;
            lstResultats.DoubleClick += LstResultats_DoubleClick; // Ajout du gestionnaire d'événements DoubleClick
            lstResultats.MouseDown += LstResultats_RightClick;
            BtnOuvrirDossier.Click += BtnOuvrirDossier_Click;
            txtMotsCles.TextChanged += txtMotsCles_TextChanged;
            txtDossier1.TextChanged += txtDossier1_TextChanged;

        }
        private void Form1_Load(object sender, EventArgs e)
        {

            txtDebug.Clear();
            // Logique à exécuter lors du chargement du formulaire
            txtDebug.AppendText("Formulaire chargé avec succès.\r\n");
        }

        private async void btnRechercher_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor; // Changer le curseur en mode "chargement"
            await Task.Delay(100); // Petite pause pour forcer l'UI à se mettre à jour

            btnRechercher.Text = "Recherche en cours...";
            btnRechercher.Enabled = false;

            txtMotsCles.Enabled = false;
            txtDossier1.Enabled = false;
            txtDossier2.Enabled = false;
            txtDossier3.Enabled = false;

            lblStatus.Text = "Recherche en cours...";
            lblStatus.ForeColor = Color.Blue;
            lblStatus.Visible = true;

            txtDebug.Clear();
            txtDebug.AppendText("Début de la Recherche\r\n");


            string[] dossiers = { txtDossier1.Text, txtDossier2.Text, txtDossier3.Text };
            txtDebug.AppendText($"Dossiers spécifiés : {string.Join(", ", dossiers)}\r\n");

            string[] motsCles = txtMotsCles.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(m => m.Trim())
                                                 .Select(m => Regex.Escape(m))
                                                 .ToArray();
            txtDebug.AppendText($"Mots-clés spécifiés : {string.Join(", ", motsCles)}\r\n");

            lstResultats.DataSource = null;
            allResults.Clear();

            ConcurrentBag<string> fichiersTrouves = new ConcurrentBag<string>();
            List<string> inaccessibleDirectories = new List<string>();

            progressBarRecherche.Value = 0;
            labelPourcentage.Text = "0%";

            await Task.Run(() => SearchFiles(dossiers, fichiersTrouves, inaccessibleDirectories, this));

            if (inaccessibleDirectories.Any())
            {
                string message = "Les dossiers suivants n'ont pas pu être accédés :\n" + string.Join("\n", inaccessibleDirectories);
                MessageBox.Show(message, "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            var regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            var compiledKeywords = CompileKeywords(motsCles, regexOptions);

            var fichiersTrouvesConcurrent = new ConcurrentBag<string>();
            var tasks = fichiersTrouves.Select(fichier => Task.Run(() => SearchKeywordsInFile(fichier, compiledKeywords, fichiersTrouvesConcurrent))).ToArray();

            await Task.WhenAll(tasks);

            foreach (string fichier in fichiersTrouvesConcurrent)
            {
                allResults.Add(fichier);
            }

            lstResultats.DataSource = allResults;
            progressBarRecherche.Value = 100;
            labelPourcentage.Text = "100%";

            lblStatus.Text = "Recherche terminée !";
            lblStatus.ForeColor = Color.Green;

            btnRechercher.Text = "Rechercher";
            btnRechercher.Enabled = true;

            txtMotsCles.Enabled = true;
            txtDossier1.Enabled = true;
            txtDossier2.Enabled = true;
            txtDossier3.Enabled = true;

            if (BtnOuvrirDossier.InvokeRequired)
            {
                BtnOuvrirDossier.Invoke(new Action(() => BtnOuvrirDossier.Enabled = true));
            }
            else
            {
                BtnOuvrirDossier.Enabled = true;
            }

            Cursor = Cursors.Default;
        }




        private static void SearchFiles(string[] dossiers, ConcurrentBag<string> fichiersTrouves, List<string> inaccessibleDirectories, Form1 form)
        {
            int totalFiles = 0;
            int processedFiles = 0;

            // Compter le nombre total de fichiers à traiter
            foreach (var dossier in dossiers)
            {
                if (Directory.Exists(dossier))
                {
                    try
                    {
                        totalFiles += GetFilesRecursively(dossier, inaccessibleDirectories).Count();
                    }
                    catch (Exception)
                    {
                        inaccessibleDirectories.Add(dossier);
                    }
                }
            }

            // Recherche des fichiers dans les dossiers
            Parallel.ForEach(dossiers, dossier =>
            {
                if (Directory.Exists(dossier))
                {
                    try
                    {
                        var files = GetFilesRecursively(dossier, inaccessibleDirectories).ToList();
                        foreach (var fichier in files)
                        {
                            // Ajouter le fichier à la liste des résultats si c'est un fichier valide
                            if (fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase))
                            {
                                fichiersTrouves.Add(fichier);
                            }

                            // Mettre à jour la progression
                            processedFiles++;
                            form.UpdateProgress(processedFiles, totalFiles); // Appel à la méthode d'instance
                        }
                    }
                    catch (Exception)
                    {
                        inaccessibleDirectories.Add(dossier);
                    }
                }
            });
        }


        public void UpdateProgress(int processedFiles, int totalFiles)
        {
            int progress = (int)((processedFiles / (float)totalFiles) * 100);

            progressBarRecherche.Invoke(new Action(() =>
            {
                progressBarRecherche.Value = progress;
                labelPourcentage.Text = $"{progress}%";
            }));
        }


        private static Regex[] CompileKeywords(string[] motsCles, RegexOptions regexOptions)
        {
            return motsCles.Select(motCle => new Regex(motCle, regexOptions)).ToArray();
        }


        private static IEnumerable<string> GetFilesRecursively(string rootDirectory, List<string> inaccessibleDirectories)
        {
            // Conversion du chemin en UNC si nécessaire
            string convertedDirectory = ConvertToUNCPath(rootDirectory);
            Debug.WriteLine($"Recherche dans : {convertedDirectory}");

            var allFiles = new List<string>();

            try
            {
                // Récupérer les fichiers du dossier courant
                allFiles.AddRange(Directory.GetFiles(convertedDirectory, "*.*", SearchOption.TopDirectoryOnly));

                // Parcourir les sous-dossiers
                foreach (var directory in Directory.GetDirectories(convertedDirectory))
                {
                    try
                    {
                        allFiles.AddRange(GetFilesRecursively(directory, inaccessibleDirectories));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                    catch (IOException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                inaccessibleDirectories.Add(convertedDirectory);
            }
            catch (IOException)
            {
                inaccessibleDirectories.Add(convertedDirectory);
            }

            return allFiles;
        }

        private static string ConvertToUNCPath(string path)
        {
            // Si le chemin est déjà un chemin UNC ou s'il n'est pas absolu, le retourner directement
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.StartsWith(@"\\"))
                return path;

            try
            {
                // Extraire la lettre du lecteur (ex : "Z:")
                string drive = path.Substring(0, 2);
                StringBuilder sb = new StringBuilder(512);
                int capacity = sb.Capacity;
                int result = WNetGetConnection(drive, sb, ref capacity);
                if (result == 0)
                {
                    string uncRoot = sb.ToString().TrimEnd();
                    // Extraire le reste du chemin après "Z:\"
                    string rest = path.Substring(3);
                    return Path.Combine(uncRoot, rest);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la conversion en UNC : {ex.Message}");
            }
            return path;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

        private void SearchKeywordsInFile(string fichier, Regex[] compiledKeywords, ConcurrentBag<string> fichiersTrouvesConcurrent)
        {
            try
            {
                if (fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase) ||
                    fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                    fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        string contenu = reader.ReadToEnd();
                        bool contientTousMotsCles = compiledKeywords.All(regex => regex.IsMatch(contenu));

                        if (contientTousMotsCles)
                        {
                            fichiersTrouvesConcurrent.Add(fichier);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => txtDebug.AppendText($"Erreur lecture {fichier} : {ex.Message}\r\n")));
                }
                else
                {
                    txtDebug.AppendText($"Erreur lecture {fichier} : {ex.Message}\r\n");
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string filterText = textBox1.Text.ToLower();
            txtDebug.AppendText($"Filtrage des résultats avec le texte : {filterText}\r\n");

            // Applique le filtrage à la BindingSource sans réaffecter DataSource
            bindingSource.Filter = $"[NomFichier] LIKE '%{filterText}%'";
        }

        private void lstResultats_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Logique pour gérer la sélection d'un élément dans lstResultats
            if (lstResultats.SelectedItem != null)
            {
                string selectedFile = lstResultats.SelectedItem?.ToString() ?? string.Empty;
                txtDebug.AppendText($"Fichier sélectionné : {selectedFile}\r\n");
                // Ajoutez ici toute autre logique souhaitée lors de la sélection d'un fichier
            }
        }

        private void LstResultats_DoubleClick(object? sender, EventArgs e)
        {
            if (lstResultats.SelectedItem is string selectedFile && !string.IsNullOrEmpty(selectedFile))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(selectedFile) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du fichier {selectedFile} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #nullable disable
        private void LstResultats_RightClick(object sender, MouseEventArgs e)
        {
            if (sender != null && sender is ListBox lst && e.Button == MouseButtons.Right && lst.SelectedItem is string selectedFile)
            {
                string directory = Path.GetDirectoryName(selectedFile);
                Process.Start(new ProcessStartInfo("explorer.exe", directory ?? string.Empty));
            }
        }
        private void BtnOuvrirDossier_Click(object sender, EventArgs e)
        {
            if (lstResultats.SelectedItem is string selectedFile && !string.IsNullOrEmpty(selectedFile))
            {
                string directory = Path.GetDirectoryName(selectedFile);

                // Ouvrir le dossier dans l'explorateur
                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", directory));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du dossier {directory}: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un fichier dans la liste.", "Aucune sélection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void txtMotsCles_TextChanged(object sender, EventArgs e)
        {
            // Vérifier si txtDossier1 et txtMotsCles ne sont pas vides ou null
            if (string.IsNullOrWhiteSpace(txtDossier1.Text) || string.IsNullOrWhiteSpace(txtMotsCles.Text))
            {
                btnRechercher.Enabled = false;  // Désactiver le bouton
            }
            else
            {
                btnRechercher.Enabled = true;   // Activer le bouton
            }
        }

        private void txtDossier1_TextChanged(object sender, EventArgs e)
        {
            // Vérifier si txtDossier1 et txtMotsCles ne sont pas vides ou null
            if (string.IsNullOrWhiteSpace(txtDossier1.Text) || string.IsNullOrWhiteSpace(txtMotsCles.Text))
            {
                btnRechercher.Enabled = false;  // Désactiver le bouton
            }
            else
            {
                btnRechercher.Enabled = true;   // Activer le bouton
            }
        }
        #nullable enable
    }
}
