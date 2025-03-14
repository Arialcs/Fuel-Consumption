namespace Fuel_Consumption
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            richTextBox1 = new RichTextBox();
            progressBar1 = new ProgressBar();
            label1 = new Label();
            textBox1 = new TextBox();
            label2 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(315, 71);
            button1.Name = "button1";
            button1.Size = new Size(147, 40);
            button1.TabIndex = 0;
            button1.Text = "Connect ";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(111, 71);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(198, 40);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "";
            richTextBox1.TextChanged += richTextBox1_TextChanged;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(577, 23);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(66, 343);
            progressBar1.TabIndex = 2;
            progressBar1.Click += progressBar1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(284, 23);
            label1.Name = "label1";
            label1.Size = new Size(56, 25);
            label1.TabIndex = 3;
            label1.Text = "Client";
            label1.Click += label1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(111, 156);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.RightToLeft = RightToLeft.No;
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(351, 210);
            textBox1.TabIndex = 4;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 79);
            label2.Name = "label2";
            label2.Size = new Size(97, 25);
            label2.TabIndex = 5;
            label2.Text = "IP Address";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(755, 411);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Controls.Add(progressBar1);
            Controls.Add(richTextBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private RichTextBox richTextBox1;
        private ProgressBar progressBar1;
        private Label label1;
        private TextBox textBox1;
        private Label label2;
    }
}
