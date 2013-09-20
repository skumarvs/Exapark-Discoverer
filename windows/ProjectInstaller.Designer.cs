namespace com.exapark.tools.cloud
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.exaparkServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.exaparkServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // exaparkServiceProcessInstaller
            // 
            this.exaparkServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.exaparkServiceProcessInstaller.Password = null;
            this.exaparkServiceProcessInstaller.Username = null;
            // 
            // exaparkServiceInstaller
            // 
            this.exaparkServiceInstaller.ServiceName = "Exapark Server Discoverer Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.exaparkServiceProcessInstaller,
            this.exaparkServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller exaparkServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller exaparkServiceInstaller;
    }
}