using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
#nullable disable

public partial class ViewerForm : Form
{
    private RichTextBox richTextBoxViewer;
    private Button btnShowAllLines;
    private Button btnShowFoundLines;
    private string fileContent;  // Contenu du fichier
    private List<int> lineNumbers;  // Liste des num�ros de lignes trouv�es

    public ViewerForm(string filePath, List<int> lineNumbers)
    {
        InitializeComponent();
        this.lineNumbers = lineNumbers;
        InitializeViewer(filePath);
    }

    private void InitializeComponent()
    {
        richTextBoxViewer = new RichTextBox();
        btnShowAllLines = new Button();
        btnShowFoundLines = new Button();
        SuspendLayout();

        // 
        // richTextBoxViewer
        // 
        richTextBoxViewer.Location = new Point(10, 3);
        richTextBoxViewer.Name = "richTextBoxViewer";
        richTextBoxViewer.Size = new Size(901, 674);
        richTextBoxViewer.TabIndex = 0;
        richTextBoxViewer.Text = "";

        // 
        // btnShowAllLines
        // 
        btnShowAllLines.Location = new Point(917, 101);
        btnShowAllLines.Name = "btnShowAllLines";
        btnShowAllLines.Size = new Size(160, 30);
        btnShowAllLines.TabIndex = 1;
        btnShowAllLines.Text = "Afficher toutes les lignes";
        btnShowAllLines.Click += BtnShowAllLines_Click;

        // 
        // btnShowFoundLines
        // 
        btnShowFoundLines.Location = new Point(917, 65);
        btnShowFoundLines.Name = "btnShowFoundLines";
        btnShowFoundLines.Size = new Size(160, 30);
        btnShowFoundLines.TabIndex = 2;
        btnShowFoundLines.Text = "Afficher les lignes trouv�es";
        btnShowFoundLines.Click += BtnShowFoundLines_Click;

        // 
        // ViewerForm
        // 
        ClientSize = new Size(1089, 689);
        Controls.Add(richTextBoxViewer);
        Controls.Add(btnShowAllLines);
        Controls.Add(btnShowFoundLines);
        Name = "ViewerForm";
        Text = "Visionneuse de Fichier";
        ResumeLayout(false);
    }

    private void InitializeViewer(string filePath)
    {
        try
        {
            // Charger le contenu du fichier
            fileContent = File.ReadAllText(filePath);

            // Diviser le contenu du fichier en lignes
            string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Cr�er un StringBuilder pour construire le texte format� avec les num�ros de ligne
            StringBuilder formattedContent = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                formattedContent.AppendLine($"{i + 1}: {lines[i]}");  // Ajouter le num�ro de ligne avant chaque ligne de texte
            }

            // Appliquer le texte format� au RichTextBox
            richTextBoxViewer.Text = formattedContent.ToString();
            richTextBoxViewer.ReadOnly = true;  // Rendre le RichTextBox en lecture seule

            // Surbriller les lignes sp�cifi�es
            HighlightLines(richTextBoxViewer.Text, lineNumbers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HighlightLines(string content, List<int> lineNumbers)
    {
        // Diviser le contenu format� en lignes
        string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        // Surligner toutes les lignes sp�cifi�es
        foreach (int lineNumber in lineNumbers)
        {
            // V�rifier que le num�ro de ligne est valide
            if (lineNumber <= lines.Length && lineNumber > 0)
            {
                string lineToHighlight = lines[lineNumber - 1]; // Ligne � surligner (1-index�)

                int startIndex = 0;
                int currentIndex = 0;

                // Effectuer une recherche de la ligne dans le RichTextBox
                while ((startIndex = richTextBoxViewer.Text.IndexOf(lineToHighlight, currentIndex, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    richTextBoxViewer.Select(startIndex, lineToHighlight.Length);
                    richTextBoxViewer.SelectionBackColor = Color.Yellow;  // Surligner en jaune
                    currentIndex = startIndex + lineToHighlight.Length;  // Continuer apr�s la ligne trouv�e
                }
            }
        }
    }

    // Bouton pour afficher toutes les lignes
    private void BtnShowAllLines_Click(object sender, EventArgs e)
    {
        try
        {
            // Cr�er un StringBuilder pour construire tout le contenu avec les num�ros de ligne
            StringBuilder formattedContent = new StringBuilder();

            string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                formattedContent.AppendLine($"{i + 1}: {lines[i]}");
            }

            // Appliquer tout le contenu au RichTextBox
            richTextBoxViewer.Text = formattedContent.ToString();
            richTextBoxViewer.ReadOnly = true;  // Rendre le RichTextBox en lecture seule

            // Surligner les lignes trouv�es
            HighlightLines(richTextBoxViewer.Text, lineNumbers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'affichage des lignes : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Bouton pour afficher uniquement les lignes trouv�es
    private void BtnShowFoundLines_Click(object sender, EventArgs e)
    {
        try
        {
            // Cr�er un StringBuilder pour afficher seulement les lignes trouv�es
            StringBuilder formattedContent = new StringBuilder();

            string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Ajouter uniquement les lignes trouv�es dans le StringBuilder
            foreach (var lineNumber in lineNumbers)
            {
                // V�rifier que le num�ro de ligne est valide
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    formattedContent.AppendLine($"{lineNumber}: {lines[lineNumber - 1]}");  // Ajouter la ligne sp�cifi�e
                }
            }

            // Appliquer les lignes trouv�es au RichTextBox
            richTextBoxViewer.Text = formattedContent.ToString();
            richTextBoxViewer.ReadOnly = true;  // Rendre le RichTextBox en lecture seule

            // Surligner les lignes trouv�es
            HighlightLines(richTextBoxViewer.Text, lineNumbers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'affichage des lignes trouv�es : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
