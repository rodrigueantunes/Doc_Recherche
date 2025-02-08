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
        private List<string> allResults = new List<string>(); // Stocker les r�sultats complets

        public Form1()
        {
            InitializeComponent();
        }

        private void btnRechercher_Click(object sender, EventArgs e)
        {
            txtDebug.Clear(); // Clear previous debug messages
            txtDebug.AppendText("D�but de la m�thode btnRechercher_Click\r\n");

            string[] dossiers = { txtDossier1.Text, txtDossier2.Text, txtDossier3.Text };
            txtDebug.AppendText($"Dossiers sp�cifi�s : {string.Join(", ", dossiers)}\r\n");

            string[] motsCles = txtMotsCles.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(m => m.Trim())
                                                 .Select(m => Regex.Escape(m)) // �chapper les caract�res sp�ciaux
                                                 .ToArray();
            txtDebug.AppendText($"Mots-cl�s sp�cifi�s : {string.Join(", ", motsCles)}\r\n");

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

                    txtDebug.AppendText($"Nombre de fichiers trouv�s dans {dossier} : {fichiers.Length}\r\n");

                    fichiersTrouves.AddRange(fichiers);

                    if (inaccessibleDirectories.Any())
                    {
                        string message = "Les dossiers suivants n'ont pas pu �tre acc�d�s :\n" + string.Join("\n", inaccessibleDirectories);
                        MessageBox.Show(message, "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    txtDebug.AppendText($"Le dossier sp�cifi� n'existe pas : {dossier}\r\n");
                }
            }

            // Recherche s�quentielle des mots-cl�s
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
                        txtDebug.AppendText($"Recherche du mot-cl� '{motCle}' : {(contientMotCle ? "trouv�" : "non trouv�")}\r\n");

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
                    break; // Si aucun fichier ne correspond, arr�ter la recherche
                }
            }

            // Ajouter les fichiers trouv�s � la liste des r�sultats
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
            txtDebug.AppendText($"Filtrage des r�sultats avec le texte : {filterText}\r\n");

            var filteredResults = allResults.Where(f => f.ToLower().Contains(filterText)).ToList();

            lstResultats.Items.Clear();
            foreach (var result in filteredResults)
            {
                lstResultats.Items.Add(result);
            }
        }

        private void lstResultats_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Logique pour g�rer la s�lection d'un �l�ment dans lstResultats
            if (lstResultats.SelectedItem != null)
            {
                string selectedFile = lstResultats.SelectedItem.ToString();
                txtDebug.AppendText($"Fichier s�lectionn� : {selectedFile}\r\n");
                // Ajoutez ici toute autre logique que vous souhaitez ex�cuter lorsque l'utilisateur s�lectionne un fichier
            }
        }
    }
}