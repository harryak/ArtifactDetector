namespace ArbitraryArtifactDetector
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.ServiceProcess.ServiceInstaller detectorServiceInstaller;

        private System.ServiceProcess.ServiceProcessInstaller detectorServiceProcessInstaller;

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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.detectorServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.detectorServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // detectorServiceProcessInstaller
            // 
            this.detectorServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.detectorServiceProcessInstaller.Password = null;
            this.detectorServiceProcessInstaller.Username = null;
            // 
            // detectorServiceInstaller
            // 
            this.detectorServiceInstaller.DelayedAutoStart = true;
            this.detectorServiceInstaller.Description = "Service of the ITS.APE framework to monitor running tests.";
            this.detectorServiceInstaller.DisplayName = "ITS.APE Detector Service";
            this.detectorServiceInstaller.ServiceName = "ITS.APE Detector Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.detectorServiceProcessInstaller,
            this.detectorServiceInstaller});

        }

        #endregion
    }
}