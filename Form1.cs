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
        private ConcurrentBag<string> resultLines = new ConcurrentBag<string>();
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
#nullable disable
        private async void btnRechercher_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor; // Changer le curseur en mode "chargement"
                await Task.Delay(100); // Petite pause pour forcer l'UI à se mettre à jour

                btnRechercher.Text = "En cours...";
                btnRechercher.Enabled = false;

                txtMotsCles.Enabled = false;
                txtDossier1.Enabled = false;
                txtDossier2.Enabled = false;
                txtDossier3.Enabled = false;
                txtFichier.Enabled = false;
                btnParcourirFichier.Enabled = false;
                

                lblStatus.Text = "Recherche en cours...";
                lblStatus.ForeColor = Color.Blue;
                lblStatus.Visible = true;

                txtDebug.Clear();
                txtDebug.AppendText("Début de la Recherche\r\n");

                // Log des dossiers spécifiés
                string[] dossiers = { txtDossier1.Text, txtDossier2.Text, txtDossier3.Text };
                txtDebug.AppendText($"Dossiers spécifiés : {string.Join(", ", dossiers)}\r\n");

                // Log des mots-clés spécifiés
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
                

                // Log du fichier spécifié
                if (!string.IsNullOrEmpty(txtFichier.Text))
                {
                    fichiersTrouves.Add(txtFichier.Text);
                    txtDebug.AppendText($"Fichier spécifié : {txtFichier.Text}\r\n");
                }

                // Log de la recherche des fichiers
                txtDebug.AppendText("Démarrage de la recherche des fichiers...\r\n");
                progressBarRecherche.Value = 25;
                labelPourcentage.Text = "Avancement";
                
                await Task.Run(() => SearchFiles(dossiers, fichiersTrouves, inaccessibleDirectories, this));
                
                if (inaccessibleDirectories.Any())
                {
                    string message = "Les dossiers suivants n'ont pas pu être accédés :\n" + string.Join("\n", inaccessibleDirectories);
                    MessageBox.Show(message, "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                // Log des mots-clés compilés
                txtDebug.AppendText("Compilation des mots-clés...\r\n");
                var regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
                var compiledKeywords = CompileKeywords(motsCles, regexOptions);
                if (compiledKeywords == null || !compiledKeywords.Any())
                {
                    txtDebug.AppendText("Aucun mot-clé valide n'a été spécifié.\r\n");
                    MessageBox.Show("Veuillez spécifier des mots-clés pour la recherche.");
                    return;
                }

                var lignesTrouvees = new ConcurrentBag<string>();

                BtnOuvrirDossier.Enabled = false;
                // Lancer les recherches dans les fichiers en parallèle avec le SemaphoreSlim
                txtDebug.AppendText("Lancement des recherches dans les fichiers...\r\n");
                progressBarRecherche.Value = 50;
                
                var tasks = fichiersTrouves.Select(fichier =>
                    SearchKeywordsInFileOptimized(fichier, compiledKeywords, lignesTrouvees, semaphore)).ToArray();

                progressBarRecherche.Value = 75;
                labelPourcentage.Text = "75%";

                await Task.WhenAll(tasks);  // Lancer toutes les tâches en parallèle

                

                foreach (string resultat in lignesTrouvees)
                {
                    allResults.Add(resultat);
                }

                lstResultats.DataSource = allResults.ToList();
                BtnOuvrirDossier.Enabled = true;
                lblStatus.Text = "Recherche terminée !";
                lblStatus.ForeColor = Color.Green;
                progressBarRecherche.Value = 100;
                labelPourcentage.Text = "100%";
                btnRechercher.Text = "Rechercher";
                btnRechercher.Enabled = true;
                txtMotsCles.Enabled = true;
                txtDossier1.Enabled = true;
                txtDossier2.Enabled = true;
                txtDossier3.Enabled = true;
                txtFichier.Enabled = true;
                btnParcourirFichier.Enabled = true;
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                // Log l'exception ici pour comprendre l'origine de l'erreur
                txtDebug.AppendText($"Une erreur s'est produite : {ex.Message}\r\n");
                MessageBox.Show($"Une erreur s'est produite : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cursor = Cursors.Default; // Réinitialise le curseur
            }
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
            if (!this.IsHandleCreated || this.IsDisposed || totalFiles == 0)
                return;

            // Calcul du pourcentage de progression
            int progress = (int)((processedFiles / (float)totalFiles) * 100);

            // Assurer que la valeur est bien dans la plage de 0 à 100
            progress = Math.Clamp(progress, 0, 100);

            // Vérifier si l'on est sur le thread UI, sinon utiliser Invoke
            if (progressBarRecherche.InvokeRequired)
            {
                progressBarRecherche.BeginInvoke(new Action(() => UpdateProgress(processedFiles, totalFiles)));
            }
            else
            {
                try
                {
                    progressBarRecherche.Value = progress;
                    labelPourcentage.Text = $"{progress}%";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur dans UpdateProgress : {ex.Message}");
                }
            }
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


        private async Task SearchKeywordsInFile(object fichiersTrouves, Regex[] compiledKeywords, ConcurrentBag<string> lignesTrouvees)
        {

            // Si fichiersTrouves est une collection (ex : List<string>), on exécute la recherche pour chaque fichier
            if (fichiersTrouves is IEnumerable<string> fichiersCollection)
            {
                var tasks = fichiersCollection.Select(fichier =>
                    SearchKeywordsInFileOptimized(fichier, compiledKeywords, lignesTrouvees, semaphore)  // Passer le sémaphore ici
                ).ToArray();

                await Task.WhenAll(tasks);
            }
            // Si fichiersTrouves est un seul fichier (ex : string), on le traite directement
            else if (fichiersTrouves is string fichier)
            {
                await SearchKeywordsInFileOptimized(fichier, compiledKeywords, lignesTrouvees, semaphore);  // Passer le sémaphore ici
            }
            else
            {
                // Gérer les cas où fichiersTrouves est invalide
                throw new ArgumentException("L'argument 'fichiersTrouves' doit être une collection ou un fichier unique.");
            }
        }


#nullable enable

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
#nullable disable
        private async Task SearchKeywordsInFileOptimized(string filePath, Regex[] compiledKeywords, ConcurrentBag<string> resultLines, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync(); // Gestion de la concurrence
            try
            {
                // Vérification si le fichier existe
                if (!File.Exists(filePath))
                {
                    resultLines.Add($"Fichier non trouvé : {filePath}");
                    return;
                }

                // Partie pour analyser le contenu du fichier (par exemple pour les fichiers HTML/4gl)
                if (filePath.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(stream, Encoding.UTF8)) // Encodage explicite
                        {
                            string contenu = await reader.ReadToEndAsync();
                            // Ici, vous pouvez traiter 'contenu' si nécessaire (par exemple pour analyser le HTML, etc.)
                        }
                    }
                    catch (Exception ex)
                    {
                        resultLines.Add($"Erreur lors de la lecture du fichier {filePath} : {ex.Message}");
                        return; // Sortie de la méthode en cas d'erreur lors de la lecture du fichier
                    }
                }

                // Partie pour analyser les lignes du fichier
                List<int> lignesTrouvees = new List<int>();
                int lineNumber = 0;

                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8)) // Encodage explicite
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        try
                        {
                            lineNumber++;

                            // Vérifier que la ligne est valide avant de la traiter
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Vérification si des mots-clés sont présents dans la ligne
                            if (compiledKeywords.Any(regex => regex.IsMatch(line)))
                            {
                                lignesTrouvees.Add(lineNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            resultLines.Add($"Erreur lors du traitement de la ligne {lineNumber} du fichier {filePath}: {ex.Message}");
                            // Continuer à analyser les lignes restantes, mais loguer l'erreur
                        }
                    }
                }

                // Si des lignes contenant des mots-clés sont trouvées, ajouter au résultat
                if (lignesTrouvees.Any())
                {
                    string resultat = $"{filePath} → Ligne {string.Join(", Ligne ", lignesTrouvees)}";
                    resultLines.Add(resultat);
                }
            }
            catch (Exception ex)
            {
                resultLines.Add($"Erreur générale avec {filePath}: {ex.Message}");
            }
            finally
            {
                semaphore.Release(); // Libération du sémaphore, même en cas d'exception
            }
        }
#nullable enable





        private void LstResultats_DoubleClick(object? sender, EventArgs e)
        {
            if (lstResultats.SelectedItem is string selectedResult && !string.IsNullOrEmpty(selectedResult))
            {
                try
                {
                    // Extraire le chemin du fichier et les lignes
                    string[] resultParts = selectedResult.Split(new string[] { " → " }, StringSplitOptions.None);

                    // Si le format n'est pas correct (pas de partie " → "), afficher une erreur.
                    if (resultParts.Length < 2)
                    {
                        MessageBox.Show("Le format de l'élément sélectionné est incorrect.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string filePath = resultParts[0];  // Le chemin du fichier
                    List<int> lineNumbers = new List<int>();

                    // Vérifier si des numéros de ligne existent dans la deuxième partie
                    string linePart = resultParts[1]; // Partie après " → ", contenant les lignes
                    string[] lineNumbersStr = linePart.Split(new string[] { "Ligne " }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var lineStr in lineNumbersStr)
                    {
                        // Nettoyer la chaîne en supprimant les espaces et autres caractères indésirables, y compris les virgules
                        string cleanedLineStr = lineStr.Trim().Replace(",", ""); // Suppression des virgules et espaces avant et après

                        // Vérifier si la chaîne nettoyée est un nombre entier valide
                        if (int.TryParse(cleanedLineStr, out int lineNumber))
                        {
                            lineNumbers.Add(lineNumber); // Ajouter la ligne trouvée
                        }
                        else
                        {
                            // Si une ligne n'est pas un entier valide, afficher une erreur.
                            MessageBox.Show($"Numéro de ligne invalide : {cleanedLineStr}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // Vérifier si le fichier existe
                    if (File.Exists(filePath))
                    {
                        // Ouvrir le fichier dans la visionneuse avec les lignes trouvées
                        var viewerForm = new ViewerForm(filePath, lineNumbers);
                        viewerForm.ShowDialog(); // Afficher le formulaire de visionneuse
                    }
                    else
                    {
                        MessageBox.Show($"Le fichier {filePath} n'existe pas.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Aucun fichier sélectionné.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if ((string.IsNullOrWhiteSpace(txtDossier1.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)) || (string.IsNullOrWhiteSpace(txtFichier.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)))
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
            if ((string.IsNullOrWhiteSpace(txtDossier1.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)) || (string.IsNullOrWhiteSpace(txtFichier.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)))
            {
                btnRechercher.Enabled = false;  // Désactiver le bouton
            }
            else
            {
                btnRechercher.Enabled = true;   // Activer le bouton
            }
        }

        private void txtFichier_TextChanged(object sender, EventArgs e)
        {
            // Vérifier si txtDossier1 et txtMotsCles ne sont pas vides ou null
            if ((string.IsNullOrWhiteSpace(txtDossier1.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)) || (string.IsNullOrWhiteSpace(txtFichier.Text) && string.IsNullOrWhiteSpace(txtMotsCles.Text)))
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
            if (lstResultats.SelectedItem is string selectedResult && !string.IsNullOrEmpty(selectedResult))
            {
                try
                {
                    // Extraire uniquement le chemin du fichier (avant la partie "→ Ligne")
                    string filePath = selectedResult.Split(new string[] { " → " }, StringSplitOptions.None)[0];

                    if (File.Exists(filePath))
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show($"Le fichier {filePath} n'existe pas.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Aucun fichier sélectionné.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void btnParcourirFichier_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Fichiers texte (*.txt;*.4gl;*.html;*.htm)|*.txt;*.4gl;*.html;*.htm|Tous les fichiers (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFichier.Text = openFileDialog.FileName;
                }
          }
        }
#nullable enable
    }
}
