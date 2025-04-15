namespace LaleMapTest
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
            textBox1 = new TextBox();
            pictureBox1 = new PictureBox();
            button2 = new Button();
            button3 = new Button();
            listBox1 = new ListBox();
            textBox2 = new TextBox();
            button4 = new Button();
            button5 = new Button();
            listBox2 = new ListBox();
            button6 = new Button();
            button7 = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(322, 12);
            button1.Name = "button1";
            button1.Size = new Size(86, 23);
            button1.TabIndex = 0;
            button1.Text = "2 Fetch Map";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(157, 23);
            textBox1.TabIndex = 1;
            textBox1.Text = "1";
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            pictureBox1.Location = new Point(12, 41);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(656, 530);
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(175, 12);
            button2.Name = "button2";
            button2.Size = new Size(141, 23);
            button2.TabIndex = 3;
            button2.Text = "1 Load Amos Pac Pic";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(414, 12);
            button3.Name = "button3";
            button3.Size = new Size(84, 23);
            button3.TabIndex = 4;
            button3.Text = "3 Draw Map";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new Point(674, 432);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(634, 139);
            listBox1.TabIndex = 5;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // textBox2
            // 
            textBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox2.Location = new Point(987, 41);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(321, 379);
            textBox2.TabIndex = 6;
            textBox2.Text = "Senaryo";
            // 
            // button4
            // 
            button4.Location = new Point(547, 12);
            button4.Name = "button4";
            button4.Size = new Size(121, 23);
            button4.TabIndex = 7;
            button4.Text = "Try Unpack File...";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // button5
            // 
            button5.Location = new Point(674, 12);
            button5.Name = "button5";
            button5.Size = new Size(126, 23);
            button5.TabIndex = 8;
            button5.Text = "Save Map as Text";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // listBox2
            // 
            listBox2.FormattingEnabled = true;
            listBox2.ItemHeight = 15;
            listBox2.Location = new Point(674, 41);
            listBox2.Name = "listBox2";
            listBox2.Size = new Size(307, 379);
            listBox2.TabIndex = 9;
            listBox2.SelectedIndexChanged += listBox2_SelectedIndexChanged;
            // 
            // button6
            // 
            button6.Location = new Point(897, 12);
            button6.Name = "button6";
            button6.Size = new Size(84, 23);
            button6.TabIndex = 10;
            button6.Text = "Utility";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // button7
            // 
            button7.Location = new Point(987, 12);
            button7.Name = "button7";
            button7.Size = new Size(140, 23);
            button7.TabIndex = 11;
            button7.Text = "Dump All Events";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1320, 602);
            Controls.Add(button7);
            Controls.Add(button6);
            Controls.Add(listBox2);
            Controls.Add(button5);
            Controls.Add(button4);
            Controls.Add(textBox2);
            Controls.Add(listBox1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(pictureBox1);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Lale Map Viewer 2025 Ref/retrojen.org";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox textBox1;
        private PictureBox pictureBox1;
        private Button button2;
        private Button button3;
        private ListBox listBox1;
        private TextBox textBox2;
        private Button button4;
        private Button button5;
        private ListBox listBox2;
        private Button button6;
        private Button button7;
    }
}
