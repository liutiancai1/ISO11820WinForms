namespace ISO11820WinForms.Forms
{
    partial class SetParamForm
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
            this.grpApparatus = new System.Windows.Forms.GroupBox();
            this.lblApparatusId = new System.Windows.Forms.Label();
            this.txtApparatusId = new System.Windows.Forms.TextBox();
            this.lblApparatusName = new System.Windows.Forms.Label();
            this.txtApparatusName = new System.Windows.Forms.TextBox();
            this.lblCheckDateFrom = new System.Windows.Forms.Label();
            this.dtpCheckDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblCheckDateTo = new System.Windows.Forms.Label();
            this.dtpCheckDateTo = new System.Windows.Forms.DateTimePicker();
            this.grpCommunication = new System.Windows.Forms.GroupBox();
            this.lblPidPort = new System.Windows.Forms.Label();
            this.txtPidPort = new System.Windows.Forms.TextBox();
            this.lblConstPower = new System.Windows.Forms.Label();
            this.numConstPower = new System.Windows.Forms.NumericUpDown();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpApparatus.SuspendLayout();
            this.grpCommunication.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numConstPower)).BeginInit();
            this.SuspendLayout();
            // 
            // grpApparatus
            // 
            this.grpApparatus.Controls.Add(this.lblApparatusId);
            this.grpApparatus.Controls.Add(this.txtApparatusId);
            this.grpApparatus.Controls.Add(this.lblApparatusName);
            this.grpApparatus.Controls.Add(this.txtApparatusName);
            this.grpApparatus.Controls.Add(this.lblCheckDateFrom);
            this.grpApparatus.Controls.Add(this.dtpCheckDateFrom);
            this.grpApparatus.Controls.Add(this.lblCheckDateTo);
            this.grpApparatus.Controls.Add(this.dtpCheckDateTo);
            this.grpApparatus.Location = new System.Drawing.Point(12, 12);
            this.grpApparatus.Name = "grpApparatus";
            this.grpApparatus.Size = new System.Drawing.Size(460, 180);
            this.grpApparatus.TabIndex = 0;
            this.grpApparatus.TabStop = false;
            this.grpApparatus.Text = "设备参数";
            // 
            // lblApparatusId
            // 
            this.lblApparatusId.AutoSize = true;
            this.lblApparatusId.Location = new System.Drawing.Point(15, 35);
            this.lblApparatusId.Name = "lblApparatusId";
            this.lblApparatusId.Size = new System.Drawing.Size(84, 20);
            this.lblApparatusId.TabIndex = 0;
            this.lblApparatusId.Text = "设备编号:";
            // 
            // txtApparatusId
            // 
            this.txtApparatusId.Location = new System.Drawing.Point(105, 32);
            this.txtApparatusId.Name = "txtApparatusId";
            this.txtApparatusId.Size = new System.Drawing.Size(330, 27);
            this.txtApparatusId.TabIndex = 1;
            // 
            // lblApparatusName
            // 
            this.lblApparatusName.AutoSize = true;
            this.lblApparatusName.Location = new System.Drawing.Point(15, 70);
            this.lblApparatusName.Name = "lblApparatusName";
            this.lblApparatusName.Size = new System.Drawing.Size(84, 20);
            this.lblApparatusName.TabIndex = 2;
            this.lblApparatusName.Text = "设备名称:";
            // 
            // txtApparatusName
            // 
            this.txtApparatusName.Location = new System.Drawing.Point(105, 67);
            this.txtApparatusName.Name = "txtApparatusName";
            this.txtApparatusName.Size = new System.Drawing.Size(330, 27);
            this.txtApparatusName.TabIndex = 3;
            // 
            // lblCheckDateFrom
            // 
            this.lblCheckDateFrom.AutoSize = true;
            this.lblCheckDateFrom.Location = new System.Drawing.Point(15, 105);
            this.lblCheckDateFrom.Name = "lblCheckDateFrom";
            this.lblCheckDateFrom.Size = new System.Drawing.Size(84, 20);
            this.lblCheckDateFrom.TabIndex = 4;
            this.lblCheckDateFrom.Text = "检定日期:";
            // 
            // dtpCheckDateFrom
            // 
            this.dtpCheckDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCheckDateFrom.Location = new System.Drawing.Point(105, 102);
            this.dtpCheckDateFrom.Name = "dtpCheckDateFrom";
            this.dtpCheckDateFrom.Size = new System.Drawing.Size(150, 27);
            this.dtpCheckDateFrom.TabIndex = 5;
            // 
            // lblCheckDateTo
            // 
            this.lblCheckDateTo.AutoSize = true;
            this.lblCheckDateTo.Location = new System.Drawing.Point(265, 105);
            this.lblCheckDateTo.Name = "lblCheckDateTo";
            this.lblCheckDateTo.Size = new System.Drawing.Size(29, 20);
            this.lblCheckDateTo.TabIndex = 6;
            this.lblCheckDateTo.Text = "至:";
            // 
            // dtpCheckDateTo
            // 
            this.dtpCheckDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpCheckDateTo.Location = new System.Drawing.Point(300, 102);
            this.dtpCheckDateTo.Name = "dtpCheckDateTo";
            this.dtpCheckDateTo.Size = new System.Drawing.Size(135, 27);
            this.dtpCheckDateTo.TabIndex = 7;
            // 
            // grpCommunication
            // 
            this.grpCommunication.Controls.Add(this.lblPidPort);
            this.grpCommunication.Controls.Add(this.txtPidPort);
            this.grpCommunication.Controls.Add(this.lblConstPower);
            this.grpCommunication.Controls.Add(this.numConstPower);
            this.grpCommunication.Location = new System.Drawing.Point(12, 198);
            this.grpCommunication.Name = "grpCommunication";
            this.grpCommunication.Size = new System.Drawing.Size(460, 115);
            this.grpCommunication.TabIndex = 1;
            this.grpCommunication.TabStop = false;
            this.grpCommunication.Text = "通信参数";
            // 
            // lblPidPort
            // 
            this.lblPidPort.AutoSize = true;
            this.lblPidPort.Location = new System.Drawing.Point(15, 35);
            this.lblPidPort.Name = "lblPidPort";
            this.lblPidPort.Size = new System.Drawing.Size(84, 20);
            this.lblPidPort.TabIndex = 0;
            this.lblPidPort.Text = "PID端口:";
            // 
            // txtPidPort
            // 
            this.txtPidPort.Location = new System.Drawing.Point(105, 32);
            this.txtPidPort.Name = "txtPidPort";
            this.txtPidPort.Size = new System.Drawing.Size(330, 27);
            this.txtPidPort.TabIndex = 1;
            // 
            // lblConstPower
            // 
            this.lblConstPower.AutoSize = true;
            this.lblConstPower.Location = new System.Drawing.Point(15, 75);
            this.lblConstPower.Name = "lblConstPower";
            this.lblConstPower.Size = new System.Drawing.Size(84, 20);
            this.lblConstPower.TabIndex = 2;
            this.lblConstPower.Text = "恒功率值:";
            // 
            // numConstPower
            // 
            this.numConstPower.Location = new System.Drawing.Point(105, 73);
            this.numConstPower.Maximum = new decimal(new int[] {
            25600,
            0,
            0,
            0});
            this.numConstPower.Name = "numConstPower";
            this.numConstPower.Size = new System.Drawing.Size(330, 27);
            this.numConstPower.TabIndex = 3;
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(150)))), ((int)(((byte)(255)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(150, 325);
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
            this.btnCancel.Location = new System.Drawing.Point(260, 325);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SetParamForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 376);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.grpCommunication);
            this.Controls.Add(this.grpApparatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetParamForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "参数设置";
            this.grpApparatus.ResumeLayout(false);
            this.grpApparatus.PerformLayout();
            this.grpCommunication.ResumeLayout(false);
            this.grpCommunication.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numConstPower)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpApparatus;
        private System.Windows.Forms.Label lblApparatusId;
        private System.Windows.Forms.TextBox txtApparatusId;
        private System.Windows.Forms.Label lblApparatusName;
        private System.Windows.Forms.TextBox txtApparatusName;
        private System.Windows.Forms.Label lblCheckDateFrom;
        private System.Windows.Forms.DateTimePicker dtpCheckDateFrom;
        private System.Windows.Forms.Label lblCheckDateTo;
        private System.Windows.Forms.DateTimePicker dtpCheckDateTo;
        private System.Windows.Forms.GroupBox grpCommunication;
        private System.Windows.Forms.Label lblPidPort;
        private System.Windows.Forms.TextBox txtPidPort;
        private System.Windows.Forms.Label lblConstPower;
        private System.Windows.Forms.NumericUpDown numConstPower;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}
