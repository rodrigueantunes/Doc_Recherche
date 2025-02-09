using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Doc_Recherche
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtDossier1;
        private System.Windows.Forms.TextBox txtDossier2;
        private System.Windows.Forms.TextBox txtDossier3;
        private System.Windows.Forms.TextBox txtMotsCles;
        private System.Windows.Forms.Button btnRechercher;
        private System.Windows.Forms.ListBox lstResultats;
        private System.Windows.Forms.TextBox txtDebug;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ProgressBar progressBarRecherche;
        private System.Windows.Forms.Label labelPourcentage;
        private Label lblStatus;
        private BindingSource bindingSource = new BindingSource();
        private Button btnOuvrirDossier;
        private BackgroundWorker bgWorker;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem menuItemOuvrirFichier;
        private ToolStripMenuItem menuItemOuvrirDossier;
        private static Dictionary<string, Regex> keywordCache = new Dictionary<string, Regex>();
        private ConcurrentBag<string> fichiersTrouves = new ConcurrentBag<string>();
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(8); // 🔹 Sémaphore partagé


        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtDossier1 = new TextBox();
            txtDossier2 = new TextBox();
            txtDossier3 = new TextBox();
            txtMotsCles = new TextBox();
            btnRechercher = new Button();
            lstResultats = new ListBox();
            txtDebug = new TextBox();
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            pictureBox1 = new PictureBox();
            progressBarRecherche = new ProgressBar();
            labelPourcentage = new Label();
            BtnOuvrirDossier = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // txtDossier1
            // 
            txtDossier1.Location = new Point(609, 14);
            txtDossier1.Margin = new Padding(4, 3, 4, 3);
            txtDossier1.Name = "txtDossier1";
            txtDossier1.Size = new Size(534, 23);
            txtDossier1.TabIndex = 0;
            // 
            // txtDossier2
            // 
            txtDossier2.Location = new Point(609, 47);
            txtDossier2.Margin = new Padding(4, 3, 4, 3);
            txtDossier2.Name = "txtDossier2";
            txtDossier2.Size = new Size(534, 23);
            txtDossier2.TabIndex = 1;
            // 
            // txtDossier3
            // 
            txtDossier3.Location = new Point(609, 81);
            txtDossier3.Margin = new Padding(4, 3, 4, 3);
            txtDossier3.Name = "txtDossier3";
            txtDossier3.Size = new Size(534, 23);
            txtDossier3.TabIndex = 2;
            // 
            // txtMotsCles
            // 
            txtMotsCles.Location = new Point(24, 123);
            txtMotsCles.Margin = new Padding(4, 3, 4, 3);
            txtMotsCles.Name = "txtMotsCles";
            txtMotsCles.Size = new Size(476, 23);
            txtMotsCles.TabIndex = 3;
            // 
            // btnRechercher
            // 
            btnRechercher.Location = new Point(822, 128);
            btnRechercher.Margin = new Padding(4, 3, 4, 3);
            btnRechercher.Name = "btnRechercher";
            btnRechercher.Size = new Size(88, 27);
            btnRechercher.TabIndex = 4;
            btnRechercher.Text = "Rechercher";
            btnRechercher.UseVisualStyleBackColor = true;
            btnRechercher.Enabled = false;
            btnRechercher.Click += btnRechercher_Click;
            // 
            // lstResultats
            // 
            lstResultats.FormattingEnabled = true;
            lstResultats.ItemHeight = 15;
            lstResultats.Location = new Point(24, 226);
            lstResultats.Margin = new Padding(4, 3, 4, 3);
            lstResultats.Name = "lstResultats";
            lstResultats.Size = new Size(1190, 364);
            lstResultats.TabIndex = 5;
            lstResultats.SelectedIndexChanged += lstResultats_SelectedIndexChanged;
            // 
            // txtDebug
            // 
            txtDebug.Location = new Point(24, 609);
            txtDebug.Margin = new Padding(4, 3, 4, 3);
            txtDebug.Multiline = true;
            txtDebug.Name = "txtDebug";
            txtDebug.Size = new Size(1190, 92);
            txtDebug.TabIndex = 6;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(24, 708);
            textBox1.Margin = new Padding(4, 3, 4, 3);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(1190, 23);
            textBox1.TabIndex = 7;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 103);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(189, 15);
            label1.TabIndex = 8;
            label1.Text = "Mots Clés (séparé par des virgules)";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(434, 17);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(128, 15);
            label2.TabIndex = 9;
            label2.Text = "Dossier de Recherche 1";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(434, 50);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(128, 15);
            label3.TabIndex = 10;
            label3.Text = "Dossier de Recherche 2";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(434, 81);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(128, 15);
            label4.TabIndex = 11;
            label4.Text = "Dossier de Recherche 3";
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.volume_software_sans_detour_h80;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(24, 7);
            pictureBox1.Margin = new Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(366, 89);
            pictureBox1.TabIndex = 12;
            pictureBox1.TabStop = false;
            // 
            // progressBarRecherche
            // 
            progressBarRecherche.Location = new Point(129, 175);
            progressBarRecherche.Margin = new Padding(4, 3, 4, 3);
            progressBarRecherche.Name = "progressBarRecherche";
            progressBarRecherche.Size = new Size(905, 27);
            progressBarRecherche.TabIndex = 0;
            // 
            // labelPourcentage
            // 
            labelPourcentage.AutoSize = true;
            labelPourcentage.Location = new Point(550, 180);
            labelPourcentage.Margin = new Padding(4, 0, 4, 0);
            labelPourcentage.Name = "labelPourcentage";
            labelPourcentage.Size = new Size(0, 15);
            labelPourcentage.TabIndex = 1;
            // 
            // BtnOuvrirDossier
            // 
            BtnOuvrirDossier.Location = new Point(935, 128);
            BtnOuvrirDossier.Name = "BtnOuvrirDossier";
            BtnOuvrirDossier.Size = new Size(99, 27);
            BtnOuvrirDossier.TabIndex = 13;
            BtnOuvrirDossier.Text = "Ouvrir Dossier";
            BtnOuvrirDossier.UseVisualStyleBackColor = true;
            BtnOuvrirDossier.Click += BtnOuvrirDossier_Click;
            BtnOuvrirDossier.Enabled = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1241, 749);
            Controls.Add(BtnOuvrirDossier);
            Controls.Add(labelPourcentage);
            Controls.Add(progressBarRecherche);
            Controls.Add(pictureBox1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(txtDebug);
            Controls.Add(lstResultats);
            Controls.Add(btnRechercher);
            Controls.Add(txtMotsCles);
            Controls.Add(txtDossier3);
            Controls.Add(txtDossier2);
            Controls.Add(txtDossier1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Doc Recherche / VSW - Antunes Rodrigue";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Button BtnOuvrirDossier;
    }
}