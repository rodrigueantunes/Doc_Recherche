using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
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
            lstResultats.DoubleClick += LstResultats_DoubleClick; // Ajout du gestionnaire d'événements DoubleClick
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Logique à exécuter lors du chargement du formulaire
            txtDebug.AppendText("Formulaire chargé avec succès.\r\n");
        }

        private async void btnRechercher_Click(object sender, EventArgs e)
        {
            txtDebug.Clear(); // Clear previous debug messages
            txtDebug.AppendText("Début de la méthode btnRechercher_Click\r\n");

            string[] dossiers = { txtDossier1.Text, txtDossier2.Text, txtDossier3.Text };
            txtDebug.AppendText($"Dossiers spécifiés : {string.Join(", ", dossiers)}\r\n");

            string[] motsCles = txtMotsCles.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(m => m.Trim())
                                                 .Select(m => Regex.Escape(m)) // Échapper les caractères spéciaux
                                                 .ToArray();
            txtDebug.AppendText($"Mots-clés spécifiés : {string.Join(", ", motsCles)}\r\n");

            lstResultats.Items.Clear(); // Clear previous results
            allResults.Clear(); // Clear previous full results

            ConcurrentBag<string> fichiersTrouves = new ConcurrentBag<string>();
            List<string> inaccessibleDirectories = new List<string>();

            progressBarRecherche.Value = 0;
            labelPourcentage.Text = "0%";

            await Task.Run(() => SearchFiles(dossiers, fichiersTrouves, inaccessibleDirectories));

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

            // Ajouter les fichiers trouvés à la liste des résultats
            foreach (string fichier in fichiersTrouvesConcurrent)
            {
                lstResultats.Items.Add(fichier);
                allResults.Add(fichier);
            }
            progressBarRecherche.Value = 100;
            labelPourcentage.Text = "100%";
        }

        private static void SearchFiles(string[] dossiers, ConcurrentBag<string> fichiersTrouves, List<string> inaccessibleDirectories)
        {
            Parallel.ForEach(dossiers, dossier =>
            {
                if (Directory.Exists(dossier))
                {
                    try
                    {
                        foreach (var fichier in GetFilesRecursively(dossier, inaccessibleDirectories))
                        {
                            if (fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                                fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase))
                            {
                                fichiersTrouves.Add(fichier);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error
                        Console.WriteLine($"Erreur lors de l'accès au dossier {dossier} : {ex.Message}");
                    }
                }
                else
                {
                    // Log error
                    Console.WriteLine($"Le dossier spécifié n'existe pas : {dossier}");
                }
            });
        }

        private static Regex[] CompileKeywords(string[] motsCles, RegexOptions regexOptions)
        {
            return motsCles.Select(motCle => new Regex(motCle, regexOptions)).ToArray();
        }

        private void SearchKeywordsInFile(string fichier, Regex[] compiledKeywords, ConcurrentBag<string> fichiersTrouvesConcurrent)
        {
            try
            {
                // Vérification si le fichier a l'extension .4gl, traiter comme un fichier texte
                if (fichier.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase) || fichier.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || fichier.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
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
                else
                {
                    // Si le fichier n'est pas .4gl, .html ou .htm, ignorer ou gérer autrement
                    MessageBox.Show($"Le fichier {fichier} n'a pas été pris en charge", "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => MessageBox.Show($"Erreur lors de la lecture du fichier {fichier} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }
    

        private static IEnumerable<string> GetFilesRecursively(string rootDirectory, List<string> inaccessibleDirectories)
        {
            var allFiles = new ConcurrentBag<string>();

            try
            {
                // Ajouter les fichiers du dossier courant
                foreach (var file in Directory.GetFiles(rootDirectory, "*.*", SearchOption.TopDirectoryOnly))
                {
                    allFiles.Add(file);
                }

                // Parcourir les sous-dossiers
                Parallel.ForEach(Directory.GetDirectories(rootDirectory), directory =>
                {
                    try
                    {
                        foreach (var file in GetFilesRecursively(directory, inaccessibleDirectories))
                        {
                            allFiles.Add(file);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                    catch (IOException ex)
                    {
                        inaccessibleDirectories.Add(directory);
                        MessageBox.Show($"Erreur d'accès au dossier {directory} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                inaccessibleDirectories.Add(rootDirectory);
            }
            catch (IOException ex)
            {
                inaccessibleDirectories.Add(rootDirectory);
                MessageBox.Show($"Erreur d'accès au dossier {rootDirectory} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return allFiles;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string filterText = textBox1.Text.ToLower();
            txtDebug.AppendText($"Filtrage des résultats avec le texte : {filterText}\r\n");

            var filteredResults = allResults.Where(f => f.ToLower().Contains(filterText)).ToList();

            lstResultats.Items.Clear();
            foreach (var result in filteredResults)
            {
                lstResultats.Items.Add(result);
            }
        }

        private void lstResultats_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Logique pour gérer la sélection d'un élément dans lstResultats
            if (lstResultats.SelectedItem != null)
            {
                string selectedFile = lstResultats.SelectedItem?.ToString() ?? string.Empty;
                txtDebug.AppendText($"Fichier sélectionné : {selectedFile}\r\n");
                // Ajoutez ici toute autre logique que vous souhaitez exécuter lorsque l'utilisateur sélectionne un fichier
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
    }
}