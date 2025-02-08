using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Doc_Recherche
{
    public partial class Form1 : Form
    {
        private List<string> allResults = new List<string>(); // Stocker les résultats complets

        public Form1()
        {
            InitializeComponent();
        }

        private void btnRechercher_Click(object sender, EventArgs e)
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

            List<string> fichiersTrouves = new List<string>();

            foreach (string dossier in dossiers)
            {
                if (Directory.Exists(dossier))
                {
                    List<string> inaccessibleDirectories = new List<string>();
                    string[] fichiers = GetFilesRecursively(dossier, inaccessibleDirectories)
                                        .Where(f => f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                                    f.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                                                    f.EndsWith(".4gl", StringComparison.OrdinalIgnoreCase))
                                        .ToArray();

                    txtDebug.AppendText($"Nombre de fichiers trouvés dans {dossier} : {fichiers.Length}\r\n");

                    fichiersTrouves.AddRange(fichiers);

                    if (inaccessibleDirectories.Any())
                    {
                        string message = "Les dossiers suivants n'ont pas pu être accédés :\n" + string.Join("\n", inaccessibleDirectories);
                        MessageBox.Show(message, "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    txtDebug.AppendText($"Le dossier spécifié n'existe pas : {dossier}\r\n");
                }
            }

            // Recherche séquentielle des mots-clés
            foreach (string motCle in motsCles)
            {
                List<string> fichiersTemp = new List<string>();

                foreach (string fichier in fichiersTrouves)
                {
                    try
                    {
                        string contenu = File.ReadAllText(fichier);
                        txtDebug.AppendText($"Analyse du fichier : {fichier}\r\n");

                        bool contientMotCle = Regex.IsMatch(contenu, motCle, RegexOptions.IgnoreCase);
                        txtDebug.AppendText($"Recherche du mot-clé '{motCle}' : {(contientMotCle ? "trouvé" : "non trouvé")}\r\n");

                        if (contientMotCle)
                        {
                            fichiersTemp.Add(fichier);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la lecture du fichier {fichier} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                fichiersTrouves = fichiersTemp;

                if (!fichiersTrouves.Any())
                {
                    break; // Si aucun fichier ne correspond, arrêter la recherche
                }
            }

            // Ajouter les fichiers trouvés à la liste des résultats
            foreach (string fichier in fichiersTrouves)
            {
                lstResultats.Items.Add(fichier);
                allResults.Add(fichier);
            }
        }

        private static string[] GetFilesRecursively(string rootDirectory, List<string> inaccessibleDirectories)
        {
            List<string> allFiles = new List<string>();

            try
            {
                // Ajouter les fichiers du dossier courant
                allFiles.AddRange(Directory.GetFiles(rootDirectory, "*.*", SearchOption.TopDirectoryOnly));

                // Parcourir les sous-dossiers
                foreach (string directory in Directory.GetDirectories(rootDirectory))
                {
                    try
                    {
                        allFiles.AddRange(GetFilesRecursively(directory, inaccessibleDirectories));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        inaccessibleDirectories.Add(directory);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                inaccessibleDirectories.Add(rootDirectory);
            }

            return allFiles.ToArray();
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
                string selectedFile = lstResultats.SelectedItem.ToString();
                txtDebug.AppendText($"Fichier sélectionné : {selectedFile}\r\n");
                // Ajoutez ici toute autre logique que vous souhaitez exécuter lorsque l'utilisateur sélectionne un fichier
            }
        }
    }
}