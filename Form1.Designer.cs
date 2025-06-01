namespace FireDetectorApp;

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
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Detector de Incendios"; // Changed form title
        // 
        // btnLoadImage
        // 
        this.btnLoadImage = new System.Windows.Forms.Button();
        this.btnLoadImage.Location = new System.Drawing.Point(12, 12);
        this.btnLoadImage.Name = "btnLoadImage";
        this.btnLoadImage.Size = new System.Drawing.Size(120, 30);
        this.btnLoadImage.TabIndex = 0;
        this.btnLoadImage.Text = "Cargar Imagen";
        this.btnLoadImage.UseVisualStyleBackColor = true;
        // 
        // pictureBoxPreview
        // 
        this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
        this.pictureBoxPreview.Location = new System.Drawing.Point(12, 48);
        this.pictureBoxPreview.Name = "pictureBoxPreview";
        this.pictureBoxPreview.Size = new System.Drawing.Size(776, 350); // Adjusted size
        this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.pictureBoxPreview.TabIndex = 1;
        this.pictureBoxPreview.TabStop = false;
        // 
        // lblResult
        // 
        this.lblResult = new System.Windows.Forms.Label();
        this.lblResult.AutoSize = true;
        this.lblResult.Location = new System.Drawing.Point(12, 370);
        this.lblResult.Name = "lblResult";
        this.lblResult.Size = new System.Drawing.Size(130, 15);
        this.lblResult.TabIndex = 2;
        this.lblResult.Text = "Esperando acción...";
        // 
        // lblPersistentResult
        // 
        this.lblPersistentResult = new System.Windows.Forms.Label();
        this.lblPersistentResult.AutoSize = true;
        this.lblPersistentResult.Location = new System.Drawing.Point(12, 395); // Posición debajo de lblResult
        this.lblPersistentResult.Name = "lblPersistentResult";
        this.lblPersistentResult.Size = new System.Drawing.Size(100, 15);
        this.lblPersistentResult.TabIndex = 3;
        this.lblPersistentResult.Text = ""; // Inicialmente vacío
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.lblResult);
        this.Controls.Add(this.pictureBoxPreview);
        this.Controls.Add(this.btnLoadImage);
        this.Controls.Add(this.lblPersistentResult); // <-- Añadir la nueva etiqueta a los controles del formulario
        this.Name = "Form1";
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Button btnLoadImage;
    private System.Windows.Forms.PictureBox pictureBoxPreview;
    private System.Windows.Forms.Label lblResult;
    private System.Windows.Forms.Label lblPersistentResult; // <-- Nueva etiqueta
}
