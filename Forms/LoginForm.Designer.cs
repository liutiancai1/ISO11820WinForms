namespace ISO11820WinForms
{
    partial class LoginForm
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
            label1 = new Label();
            label2 = new Label();
            lable1 = new Label();
            login = new Button();
            password = new TextBox();
            experimenter = new RadioButton();
            admin = new RadioButton();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(21, 39);
            label1.Name = "label1";
            label1.Size = new Size(290, 33);
            label1.TabIndex = 0;
            label1.Text = "建筑材料不燃性试验系统";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 16.2F, FontStyle.Bold, GraphicsUnit.Point);
            label2.ForeColor = SystemColors.HotTrack;
            label2.Location = new Point(476, 68);
            label2.Name = "label2";
            label2.Size = new Size(129, 37);
            label2.TabIndex = 1;
            label2.Text = "系统登录";
            // 
            // lable1
            // 
            lable1.AutoSize = true;
            lable1.Location = new Point(424, 207);
            lable1.Name = "lable1";
            lable1.Size = new Size(39, 20);
            lable1.TabIndex = 4;
            lable1.Text = "密码";
            // 
            // login
            // 
            login.Location = new Point(424, 263);
            login.Name = "login";
            login.Size = new Size(216, 40);
            login.TabIndex = 5;
            login.Text = "登录";
            login.UseVisualStyleBackColor = true;
            login.Click += login_Click;
            // 
            // password
            // 
            password.Location = new Point(480, 204);
            password.Name = "password";
            password.PasswordChar = '*';
            password.Size = new Size(125, 27);
            password.TabIndex = 6;
            // 
            // experimenter
            // 
            experimenter.AutoSize = true;
            experimenter.Location = new Point(425, 143);
            experimenter.Name = "experimenter";
            experimenter.Size = new Size(75, 24);
            experimenter.TabIndex = 7;
            experimenter.TabStop = true;
            experimenter.Text = "试验员";
            experimenter.UseVisualStyleBackColor = true;
            // 
            // admin
            // 
            admin.AutoSize = true;
            admin.Checked = true;
            admin.Location = new Point(546, 143);
            admin.Name = "admin";
            admin.Size = new Size(75, 24);
            admin.TabIndex = 8;
            admin.TabStop = true;
            admin.Text = "管理员";
            admin.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(admin);
            Controls.Add(experimenter);
            Controls.Add(password);
            Controls.Add(login);
            Controls.Add(lable1);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "LoginForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label lable1;
        private Button login;
        private TextBox password;
        private RadioButton experimenter;
        private RadioButton admin;
    }
}
