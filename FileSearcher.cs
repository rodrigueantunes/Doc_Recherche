using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Doc_Recherche
{
    public class FileSearcher
    {
        public static void SearchFiles(string rootDirectory)
        {
            List<string> inaccessibleDirectories = new List<string>();

            try
            {
                string[] fichiers = GetFilesRecursively(rootDirectory, inaccessibleDirectories);
                // Traitez les fichiers ici
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur s'est produite : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (inaccessibleDirectories.Any())
            {
                string message = "Les dossiers suivants n'ont pas pu être accédés :\n" + string.Join("\n", inaccessibleDirectories);
                MessageBox.Show(message, "Avertissement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string[] GetFilesRecursively(string rootDirectory, List<string> inaccessibleDirectories)
        {
            List<string> allFiles = new List<string>();

            try
            {
                foreach (string directory in Directory.GetDirectories(rootDirectory))
                {
                    try
                    {
                        allFiles.AddRange(Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                                                   .Where(f => f.EndsWith(".html") || f.EndsWith(".htm") || f.EndsWith(".4gl")));
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
    }
}