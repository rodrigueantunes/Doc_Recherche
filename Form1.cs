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
            BtnOuvrirDossier.Click += BtnOuvrirDossier_Click;
            txtMotsCles.TextChanged += txtMotsCles_TextChanged;
            txtDossier1.TextChanged += txtDossier1_TextChanged;
            // Initialisation du ContextMenuStrip
            contextMenuStrip = new ContextMenuStrip();
            menuItemOuvrirFichier = new ToolStripMenuItem("Ouvrir le fichier");
            menuItemOuvrirDossier = new ToolStripMenuItem("Ouvrir le dossier");

            // Ajout des items au ContextMenuStrip
            contextMenuStrip.Items.Add(menuItemOuvrirFichier);
            contextMenuStrip.Items.Add(menuItemOuvrirDossier);

            // Assignation du ContextMenuStrip à la ListBox
            lstResultats.ContextMenuStrip = contextMenuStrip;

            // Gestion des événements de clic sur les items du menu
            menuItemOuvrirFichier.Click += menuItemOuvrirFichier_Click;
            menuItemOuvrirDossier.Click += menuItemOuvrirDossier_Click;

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

            await Task.WhenAll(tasks);  // Lancer toutes les tâches en parallèle

            foreach (string fichier in fichiersTrouvesConcurrent)
            {
                allResults.Add(fichier);
            }

            lstResultats.DataSource = allResults;
            progressBarRecherche.Value = 100;
            labelPourcentage.Text = "100%";
            BtnOuvrirDossier.Enabled = true;
            lblStatus.Text = "Recherche terminée !";
            lblStatus.ForeColor = Color.Green;

            btnRechercher.Text = "Rechercher";
            btnRechercher.Enabled = true;

            txtMotsCles.Enabled = true;
            txtDossier1.Enabled = true;
            txtDossier2.Enabled = true;
            txtDossier3.Enabled = true;

            Cursor = Cursors.Default;
        }





        private async Task SearchFiles(string[] dossiers, ConcurrentBag<string> fichiersTrouves, List<string> inaccessibleDirectories, Form1 form)
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
            var tasks = dossiers.Select(dossier => Task.Run(() =>
            {
                if (Directory.Exists(dossier))
                {
                    try
                    {
                        var files = GetFilesRecursively(dossier, inaccessibleDirectories).ToList();
                        foreach (var fichier in files)
                        {
                            // Filtrage des fichiers dès le début
                            if (fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase))
                            {
                                fichiersTrouves.Add(fichier);
                            }

                            processedFiles++;

                            // Mettre à jour la progression tous les 100 fichiers traités
                            if (processedFiles % 100 == 0)
                            {
                                form.UpdateProgress(processedFiles, totalFiles);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        inaccessibleDirectories.Add(dossier);
                    }
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assurez-vous de mettre à jour la progression après la fin de la recherche
            form.UpdateProgress(processedFiles, totalFiles);
        }



        public void UpdateProgress(int processedFiles, int totalFiles)
        {
            int progress = (int)((processedFiles / (float)totalFiles) * 95);

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

            // Utilisation de Directory.EnumerateFiles pour charger les fichiers de manière paresseuse
            try
            {
                // Récupérer les fichiers du dossier courant (filtrage au niveau des extensions)
                var files = Directory.EnumerateFiles(convertedDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(f => f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                                f.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                                                f.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase));

                allFiles.AddRange(files);

                // Recherche dans les sous-dossiers en parallèle pour accélérer le processus
                var directories = Directory.EnumerateDirectories(convertedDirectory);
                Parallel.ForEach(directories, (directory) =>
                {
                    try
                    {
                        allFiles.AddRange(GetFilesRecursively(directory, inaccessibleDirectories).ToList());
                    }
                    catch (UnauthorizedAccessException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                    catch (IOException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                });
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


        private async Task SearchKeywordsInFile(object fichiersTrouves, Regex[] compiledKeywords, ConcurrentBag<string> fichiersTrouvesConcurrent)
        {
            // Si fichiersTrouves est une collection (ex : List<string>), on passe à l'exécution normale
            if (fichiersTrouves is IEnumerable<string> fichiersCollection)
            {
                var tasks = fichiersCollection.Select(fichier =>
                    SearchKeywordsInFileOptimized(fichier, compiledKeywords, fichiersTrouvesConcurrent)
                ).ToArray();

                await Task.WhenAll(tasks);
            }
            // Si fichiersTrouves est un seul fichier (ex : string), on crée une collection temporaire
            else if (fichiersTrouves is string fichier)
            {
                await SearchKeywordsInFileOptimized(fichier, compiledKeywords, fichiersTrouvesConcurrent);
            }
            else
            {
                // Gérer les cas où fichiersTrouves n'est ni une collection, ni un fichier unique
                throw new ArgumentException("L'argument 'fichiersTrouves' doit être une collection ou un fichier unique.");
            }
        }


        private async Task SearchKeywordsInFileOptimizedWithMemoryMapped(string fichier, Regex[] compiledKeywords, ConcurrentBag<string> fichiersTrouvesConcurrent)
{
    const int bufferSize = 8192; // Lecture par blocs de 8 Ko

    await Task.Run(() =>
    {
        try
        {
            if (!(fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase) ||
                  fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                  fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)))
                return;  // Si l'extension n'est pas valide, on quitte immédiatement

            using (var mmf = MemoryMappedFile.CreateFromFile(fichier, FileMode.Open, fichier, 0, MemoryMappedFileAccess.Read))
            using (var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                byte[] buffer = new byte[bufferSize];
                long offset = 0;
                StringBuilder contenuBuffer = new StringBuilder(); // Accumulation pour conserver les mots coupés

                while (offset < accessor.Capacity)
                {
                    int bytesRead = accessor.ReadArray(offset, buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;  // Sécurité pour éviter une boucle infinie

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Gérer les mots coupés entre les blocs
                    contenuBuffer.Append(chunk);
                    string contenu = contenuBuffer.ToString();

                    // Vérifier si tous les mots-clés sont présents
                    if (compiledKeywords.All(regex => regex.IsMatch(contenu)))
                    {
                        fichiersTrouvesConcurrent.Add(fichier);
                        break;  // On arrête dès qu'on trouve tous les mots-clés
                    }

                    // Conserver les derniers caractères du buffer pour ne pas couper un mot
                    int overlap = 100; // On garde 100 caractères pour éviter la coupure
                    contenuBuffer.Clear();
                    if (chunk.Length > overlap)
                        contenuBuffer.Append(chunk.Substring(chunk.Length - overlap));

                    offset += bytesRead;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lecture fichier {fichier} avec MemoryMappedFile : {ex.Message}");
        }
    });
}


        private async Task SearchKeywordsInFileOptimized(string fichier, Regex[] compiledKeywords, ConcurrentBag<string> fichiersTrouvesConcurrent)
        {
            await semaphore.WaitAsync();
            try
            {
                if (fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase) ||
                    fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                    fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        string contenu = await reader.ReadToEndAsync();
                        if (compiledKeywords.All(regex => regex.IsMatch(contenu)))
                        {
                            fichiersTrouvesConcurrent.Add(fichier);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lecture fichier {fichier}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
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

        private void BtnOuvrirDossier_Click(object sender, EventArgs e)
        {
            if (lstResultats.SelectedItem is string selectedFile && !string.IsNullOrEmpty(selectedFile))
            {
                string directory = Path.GetDirectoryName(selectedFile);
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

        private void menuItemOuvrirFichier_Click(object sender, EventArgs e)
        {
            // Ouvrir le fichier en fonction de l'élément sélectionné dans la ListBox
            var fichier = lstResultats.SelectedItem.ToString();
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

        private void menuItemOuvrirDossier_Click(object sender, EventArgs e)
        {
            // Ouvrir le dossier contenant le fichier
            var fichier = lstResultats.SelectedItem.ToString();
            var dossier = Path.GetDirectoryName(fichier);
            if (Directory.Exists(dossier))
            {
                Process.Start("explorer.exe", dossier);  // Ouvre le dossier dans l'Explorateur de fichiers
            }
            else
            {
                MessageBox.Show("Dossier introuvable.");
            }
        }
        private static Regex CompileKeyword(string keyword)
        {
            if (!keywordCache.ContainsKey(keyword))
            {
                keywordCache[keyword] = new Regex(keyword, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            return keywordCache[keyword];
        }

        private static Regex[] CompileKeywords(string[] motsCles)
        {
            return motsCles.Select(motCle => new Regex(motCle, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray();
        }
#nullable enable
    }
}
