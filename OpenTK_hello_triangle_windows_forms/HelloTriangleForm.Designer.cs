/// <summary>
/// [opentk/GLControl](https://github.com/opentk/GLControl)
/// </summary>

namespace OpenTK_hello_triangle_windows_forms
{
    partial class HelloTriangleForm
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
            this.glControl = new OpenTK.WinForms.GLControl();
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            this.glControl.APIVersion = new System.Version(3, 3, 0, 0);
            this.glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            this.glControl.IsEventDriven = true;
            this.glControl.Location = new System.Drawing.Point(0, 0);
            this.glControl.Name = "glControl";
            this.glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            this.glControl.Size = new System.Drawing.Size(400, 400);
            this.glControl.TabIndex = 0;
            this.glControl.Text = "glControl";
            this.glControl.Load += new System.EventHandler(this.glControl_Load);
            // 
            // HelloTriangleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 400);
            this.Controls.Add(this.glControl);
            this.Name = "HelloTriangleForm";
            this.Text = "Hello Triangle Windows Forms";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.HelloTriangleForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.WinForms.GLControl glControl;
    }
}

