namespace ISO11820WinForms.Forms
{
    partial class TestPhenoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpFlame = new System.Windows.Forms.GroupBox();
            this.chkFlame = new System.Windows.Forms.CheckBox();
            this.lblFlameTime = new System.Windows.Forms.Label();
            this.numFlameTime = new System.Windows.Forms.NumericUpDown();
            this.lblFlameDuration = new System.Windows.Forms.Label();
            this.numFlameDuration = new System.Windows.Forms.NumericUpDown();
            this.grpPostWeight = new System.Windows.Forms.GroupBox();
            this.lblPostWeight = new System.Windows.Forms.Label();
            this.txtPostWeight = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpFlame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFlameTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFlameDuration)).BeginInit();
            this.grpPostWeight.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpFlame
            // 
            this.grpFlame.Controls.Add(this.chkFlame);
            this.grpFlame.Controls.Add(this.lblFlameTime);
            this.grpFlame.Controls.Add(this.numFlameTime);
            this.grpFlame.Controls.Add(this.lblFlameDuration);
            this.grpFlame.Controls.Add(this.numFlameDuration);
            this.grpFlame.Location = new System.Drawing.Point(12, 12);
            this.grpFlame.Name = "grpFlame";
            this.grpFlame.Size = new System.Drawing.Size(360, 120);
            this.grpFlame.TabIndex = 0;
            this.grpFlame.TabStop = false;
            this.grpFlame.Text = "持续火焰";
            // 
            // chkFlame
            // 
            this.chkFlame.AutoSize = true;
            this.chkFlame.Location = new System.Drawing.Point(15, 25);
            this.chkFlame.Name = "chkFlame";
            this.chkFlame.Size = new System.Drawing.Size(106, 24);
            this.chkFlame.TabIndex = 0;
            this.chkFlame.Text = "发生火焰";
            this.chkFlame.UseVisualStyleBackColor = true;
            this.chkFlame.CheckedChanged += new System.EventHandler(this.chkFlame_CheckedChanged);
            // 
            // lblFlameTime
            // 
            this.lblFlameTime.AutoSize = true;
            this.lblFlameTime.Location = new System.Drawing.Point(15, 55);
            this.lblFlameTime.Name = "lblFlameTime";
            this.lblFlameTime.Size = new System.Drawing.Size(114, 20);
            this.lblFlameTime.TabIndex = 1;
            this.lblFlameTime.Text = "发生时间(s):";
            // 
            // numFlameTime
            // 
            this.numFlameTime.Enabled = false;
            this.numFlameTime.Location = new System.Drawing.Point(135, 53);
            this.numFlameTime.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numFlameTime.Name = "numFlameTime";
            this.numFlameTime.Size = new System.Drawing.Size(200, 27);
            this.numFlameTime.TabIndex = 2;
            // 
            // lblFlameDuration
            // 
            this.lblFlameDuration.AutoSize = true;
            this.lblFlameDuration.Location = new System.Drawing.Point(15, 85);
            this.lblFlameDuration.Name = "lblFlameDuration";
            this.lblFlameDuration.Size = new System.Drawing.Size(114, 20);
            this.lblFlameDuration.TabIndex = 3;
            this.lblFlameDuration.Text = "持续时间(s):";
            // 
            // numFlameDuration
            // 
            this.numFlameDuration.Enabled = false;
            this.numFlameDuration.Location = new System.Drawing.Point(135, 83);
            this.numFlameDuration.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numFlameDuration.Name = "numFlameDuration";
            this.numFlameDuration.Size = new System.Drawing.Size(200, 27);
            this.numFlameDuration.TabIndex = 4;
            // 
            // grpPostWeight
            // 
            this.grpPostWeight.Controls.Add(this.lblPostWeight);
            this.grpPostWeight.Controls.Add(this.txtPostWeight);
            this.grpPostWeight.Location = new System.Drawing.Point(12, 138);
            this.grpPostWeight.Name = "grpPostWeight";
            this.grpPostWeight.Size = new System.Drawing.Size(360, 70);
            this.grpPostWeight.TabIndex = 1;
            this.grpPostWeight.TabStop = false;
            this.grpPostWeight.Text = "试件质量";
            // 
            // lblPostWeight
            // 
            this.lblPostWeight.AutoSize = true;
            this.lblPostWeight.Location = new System.Drawing.Point(15, 35);
            this.lblPostWeight.Name = "lblPostWeight";
            this.lblPostWeight.Size = new System.Drawing.Size(104, 20);
            this.lblPostWeight.TabIndex = 0;
            this.lblPostWeight.Text = "残余质量(g):";
            // 
            // txtPostWeight
            // 
            this.txtPostWeight.Location = new System.Drawing.Point(135, 32);
            this.txtPostWeight.Name = "txtPostWeight";
            this.txtPostWeight.Size = new System.Drawing.Size(200, 27);
            this.txtPostWeight.TabIndex = 1;
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(100, 220);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 35);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(210, 220);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // TestPhenoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 271);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.grpPostWeight);
            this.Controls.Add(this.grpFlame);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TestPhenoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "试验记录";
            this.grpFlame.ResumeLayout(false);
            this.grpFlame.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFlameTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFlameDuration)).EndInit();
            this.grpPostWeight.ResumeLayout(false);
            this.grpPostWeight.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpFlame;
        private System.Windows.Forms.CheckBox chkFlame;
        private System.Windows.Forms.Label lblFlameTime;
        private System.Windows.Forms.NumericUpDown numFlameTime;
        private System.Windows.Forms.Label lblFlameDuration;
        private System.Windows.Forms.NumericUpDown numFlameDuration;
        private System.Windows.Forms.GroupBox grpPostWeight;
        private System.Windows.Forms.Label lblPostWeight;
        private System.Windows.Forms.TextBox txtPostWeight;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}
