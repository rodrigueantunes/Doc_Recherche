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
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // txtDossier1
            // 
            txtDossier1.Location = new Point(522, 12);
            txtDossier1.Name = "txtDossier1";
            txtDossier1.Size = new Size(458, 23);
            txtDossier1.TabIndex = 0;
            // 
            // txtDossier2
            // 
            txtDossier2.Location = new Point(522, 41);
            txtDossier2.Name = "txtDossier2";
            txtDossier2.Size = new Size(458, 23);
            txtDossier2.TabIndex = 1;
            // 
            // txtDossier3
            // 
            txtDossier3.Location = new Point(522, 70);
            txtDossier3.Name = "txtDossier3";
            txtDossier3.Size = new Size(458, 23);
            txtDossier3.TabIndex = 2;
            // 
            // txtMotsCles
            // 
            txtMotsCles.Location = new Point(21, 107);
            txtMotsCles.Name = "txtMotsCles";
            txtMotsCles.Size = new Size(409, 23);
            txtMotsCles.TabIndex = 3;
            // 
            // btnRechercher
            // 
            btnRechercher.Location = new Point(705, 111);
            btnRechercher.Name = "btnRechercher";
            btnRechercher.Size = new Size(75, 23);
            btnRechercher.TabIndex = 4;
            btnRechercher.Text = "Rechercher";
            btnRechercher.UseVisualStyleBackColor = true;
            btnRechercher.Click += btnRechercher_Click;
            // 
            // lstResultats
            // 
            lstResultats.FormattingEnabled = true;
            lstResultats.ItemHeight = 15;
            lstResultats.Location = new Point(21, 157);
            lstResultats.Name = "lstResultats";
            lstResultats.Size = new Size(1021, 364);
            lstResultats.TabIndex = 5;
            lstResultats.SelectedIndexChanged += lstResultats_SelectedIndexChanged;
            // 
            // txtDebug
            // 
            txtDebug.Location = new Point(21, 528);
            txtDebug.Multiline = true;
            txtDebug.Name = "txtDebug";
            txtDebug.Size = new Size(1021, 80);
            txtDebug.TabIndex = 6;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(21, 614);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(1021, 23);
            textBox1.TabIndex = 7;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 89);
            label1.Name = "label1";
            label1.Size = new Size(189, 15);
            label1.TabIndex = 8;
            label1.Text = "Mots Clés (séparé par des virgules)";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(372, 15);
            label2.Name = "label2";
            label2.Size = new Size(128, 15);
            label2.TabIndex = 9;
            label2.Text = "Dossier de Recherche 1";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(372, 43);
            label3.Name = "label3";
            label3.Size = new Size(128, 15);
            label3.TabIndex = 10;
            label3.Text = "Dossier de Recherche 2";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(372, 70);
            label4.Name = "label4";
            label4.Size = new Size(128, 15);
            label4.TabIndex = 11;
            label4.Text = "Dossier de Recherche 3";
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.volume_software_sans_detour_h80;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(21, 6);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(314, 77);
            pictureBox1.TabIndex = 12;
            pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            ClientSize = new Size(1064, 649);
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
            Name = "Form1";
            Text = "Doc Recherche / VSW - Antunes Rodrigue";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private PictureBox pictureBox1;
    }
}