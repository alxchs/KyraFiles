using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FilesCustomInstaller
{
    public class InstallerForm : Form
    {
        private TextBox txtDest;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkLaunch;
        private Button btnInstall;
        private Button btnCancel;
        private ProgressBar progressBar;
        private Label lblStatus;
        private bool isInstalling = false;
        private bool isFinished = false;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Instalador do Files Custom — v4.2.37.0";
            this.Size = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.White;

            // Header Panel
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 80;
            headerPanel.BackColor = Color.FromArgb(240, 244, 248);
            headerPanel.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(215, 225, 235), 1), 0, 79, this.Width, 79);
            };

            Label lblTitle = new Label();
            lblTitle.Text = "Instalação do Files Custom";
            lblTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(20, 60, 120);
            lblTitle.Location = new Point(24, 14);
            lblTitle.AutoSize = true;
            headerPanel.Controls.Add(lblTitle);

            Label lblSubTitle = new Label();
            lblSubTitle.Text = "Versão 4.2.37.0 (x64) • Todas as Colunas do Explorer e Features OneCommander";
            lblSubTitle.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblSubTitle.ForeColor = Color.FromArgb(80, 95, 110);
            lblSubTitle.Location = new Point(25, 45);
            lblSubTitle.AutoSize = true;
            headerPanel.Controls.Add(lblSubTitle);

            this.Controls.Add(headerPanel);

            // Destination Folder Group
            Label lblDestPrompt = new Label();
            lblDestPrompt.Text = "Pasta de destino da instalação:";
            lblDestPrompt.Location = new Point(25, 100);
            lblDestPrompt.AutoSize = true;
            this.Controls.Add(lblDestPrompt);

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\FilesCustom");
            txtDest = new TextBox();
            txtDest.Text = defaultPath;
            txtDest.Location = new Point(25, 125);
            txtDest.Size = new Size(420, 26);
            this.Controls.Add(txtDest);

            btnBrowse = new Button();
            btnBrowse.Text = "Procurar...";
            btnBrowse.Location = new Point(455, 123);
            btnBrowse.Size = new Size(95, 28);
            btnBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtDest.Text;
                    fbd.Description = "Selecione a pasta de instalação do Files Custom:";
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtDest.Text = fbd.SelectedPath;
                    }
                }
            };
            this.Controls.Add(btnBrowse);

            // Shortcut Options
            chkDesktop = new CheckBox();
            chkDesktop.Text = "Criar atalho na Área de Trabalho";
            chkDesktop.Checked = true;
            chkDesktop.Location = new Point(25, 168);
            chkDesktop.AutoSize = true;
            this.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox();
            chkStartMenu.Text = "Criar atalho no Menu Iniciar";
            chkStartMenu.Checked = true;
            chkStartMenu.Location = new Point(25, 198);
            chkStartMenu.AutoSize = true;
            this.Controls.Add(chkStartMenu);

            chkLaunch = new CheckBox();
            chkLaunch.Text = "Executar o Files Custom ao concluir";
            chkLaunch.Checked = true;
            chkLaunch.Location = new Point(25, 228);
            chkLaunch.AutoSize = true;
            this.Controls.Add(chkLaunch);

            // Progress & Status
            lblStatus = new Label();
            lblStatus.Text = "Pronto para instalar.";
            lblStatus.Location = new Point(25, 268);
            lblStatus.Size = new Size(525, 20);
            lblStatus.ForeColor = Color.FromArgb(70, 80, 90);
            this.Controls.Add(lblStatus);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(25, 292);
            progressBar.Size = new Size(525, 22);
            progressBar.Style = ProgressBarStyle.Continuous;
            this.Controls.Add(progressBar);

            // Bottom Buttons
            btnInstall = new Button();
            btnInstall.Text = "Instalar";
            btnInstall.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnInstall.Location = new Point(345, 332);
            btnInstall.Size = new Size(100, 32);
            btnInstall.BackColor = Color.FromArgb(16, 110, 190);
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnCancel = new Button();
            btnCancel.Text = "Cancelar";
            btnCancel.Location = new Point(455, 332);
            btnCancel.Size = new Size(95, 32);
            btnCancel.Click += (s, e) => {
                if (isInstalling)
                {
                    if (MessageBox.Show("A instalação está em andamento. Deseja realmente cancelar?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.Close();
                    }
                }
                else
                {
                    this.Close();
                }
            };
            this.Controls.Add(btnCancel);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (isFinished)
            {
                if (chkLaunch.Checked)
                {
                    LaunchApp();
                }
                this.Close();
                return;
            }

            if (isInstalling) return;

            string targetDir = txtDest.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Por favor, selecione uma pasta de destino válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isInstalling = true;
            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtDest.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            btnCancel.Enabled = false;

            Thread worker = new Thread(() => RunInstallation(targetDir));
            worker.IsBackground = true;
            worker.Start();
        }

        private void RunInstallation(string targetDir)
        {
            try
            {
                UpdateStatus("Criando diretório de instalação...", 5);
                Directory.CreateDirectory(targetDir);

                UpdateStatus("Localizando pacote autocontido...", 10);
                Stream zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FilesAppPayload.zip");
                
                // Fallback: se não estiver embutido, busca no diretório do instalador
                if (zipStream == null)
                {
                    string localZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FilesApp-v4.2.37.0-x64-standalone.zip");
                    if (File.Exists(localZip))
                    {
                        zipStream = File.OpenRead(localZip);
                    }
                }

                if (zipStream == null)
                {
                    throw new FileNotFoundException("Pacote autocontido (FilesAppPayload.zip) não encontrado nem embutido.");
                }

                using (zipStream)
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    int total = archive.Entries.Count;
                    int count = 0;
                    byte[] buffer = new byte[65536]; // 64KB buffer streaming (disciplina de memória: chunk por chunk)

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        count++;
                        int pct = 10 + (int)((count / (float)total) * 60);

                        // Se a entrada começa com a pasta raiz do zip, remove o prefixo
                        string entryName = entry.FullName;
                        if (entryName.StartsWith("FilesApp-v4.2.37.0-x64-standalone/", StringComparison.OrdinalIgnoreCase))
                        {
                            entryName = entryName.Substring("FilesApp-v4.2.37.0-x64-standalone/".Length);
                        }

                        if (string.IsNullOrEmpty(entryName)) continue;

                        string destFilePath = Path.Combine(targetDir, entryName);

                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(destFilePath);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));

                        if (count % 25 == 0 || count == total)
                        {
                            UpdateStatus(string.Format("Extraindo: {0} ({1}/{2})", Path.GetFileName(entryName), count, total), pct);
                        }

                        using (Stream entryStream = entry.Open())
                        using (FileStream fileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            int bytesRead;
                            while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }

                UpdateStatus("Registrando pacote no Windows (AppX)...", 75);
                string manifestPath = Path.Combine(targetDir, "AppxManifest.xml");
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException("AppxManifest.xml não encontrado após a extração.");
                }

                RegisterAppxPackage(manifestPath);

                UpdateStatus("Criando atalhos...", 85);
                CreateShortcuts(targetDir);

                UpdateStatus("Configurando desinstalador no Windows...", 95);
                CreateUninstaller(targetDir);

                UpdateStatus("Instalação concluída com sucesso!", 100);

                this.Invoke(new Action(() => {
                    isInstalling = false;
                    isFinished = true;
                    btnInstall.Text = "Concluir";
                    btnInstall.Enabled = true;
                    btnCancel.Visible = false;
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => {
                    isInstalling = false;
                    lblStatus.Text = "Erro na instalação.";
                    lblStatus.ForeColor = Color.Red;
                    btnInstall.Enabled = true;
                    btnCancel.Enabled = true;
                    MessageBox.Show("Ocorreu um erro durante a instalação:\n\n" + ex.Message, "Erro na Instalação", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void UpdateStatus(string message, int progress)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(message, progress)));
                return;
            }
            lblStatus.Text = message;
            progressBar.Value = Math.Min(100, Math.Max(0, progress));
        }

        private void RegisterAppxPackage(string manifestPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Register '{0}' -ForceApplicationShutdown -ForceUpdateFromAnyVersion\"", manifestPath);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    // Tenta uma segunda vez sem ForceApplicationShutdown
                    psi.Arguments = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Register '{0}'\"", manifestPath);
                    using (Process p2 = Process.Start(psi))
                    {
                        p2.WaitForExit();
                    }
                }
            }
        }

        private void CreateShortcuts(string targetDir)
        {
            string exePath = Path.Combine(targetDir, "Files.exe");
            string appId = "FilesDev_ykqwq8d6ps0ag!App";

            if (chkDesktop.Checked)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnkPath = Path.Combine(desktopPath, "Files Custom.lnk");
                CreateShellShortcut(lnkPath, exePath, appId);
            }

            if (chkStartMenu.Checked)
            {
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string lnkPath = Path.Combine(startMenuPath, "Files Custom.lnk");
                CreateShellShortcut(lnkPath, exePath, appId);
            }
        }

        private void CreateShellShortcut(string shortcutPath, string targetExe, string appId)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = "explorer.exe";
                    shortcut.Arguments = "shell:AppsFolder\\" + appId;
                    shortcut.IconLocation = targetExe + ",0";
                    shortcut.Description = "Files Custom — Gerenciador de Arquivos Moderno";
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
                    shortcut.Save();
                }
            }
            catch { }
        }

        private void CreateUninstaller(string targetDir)
        {
            try
            {
                string uninstallerCmd = Path.Combine(targetDir, "Desinstalar.cmd");
                string uninstallerPs1 = Path.Combine(targetDir, "Desinstalar.ps1");

                string ps1Content = 
                    "# Desinstalador do Files Custom\r\n" +
                    "Write-Host 'Desinstalando Files Custom...' -ForegroundColor Cyan\r\n" +
                    "$pkg = Get-AppxPackage -Name 'FilesDev'\r\n" +
                    "if ($pkg) { Remove-AppxPackage -Package $pkg.PackageFullName }\r\n" +
                    "$desktopLnk = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Files Custom.lnk'\r\n" +
                    "if (Test-Path $desktopLnk) { Remove-Item $desktopLnk -Force }\r\n" +
                    "$startLnk = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\\Files Custom.lnk'\r\n" +
                    "if (Test-Path $startLnk) { Remove-Item $startLnk -Force }\r\n" +
                    "Remove-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\FilesCustom' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                    "Write-Host 'Arquivos do aplicativo prontos para remoção.' -ForegroundColor Green\r\n";

                File.WriteAllText(uninstallerPs1, ps1Content);

                string cmdContent = 
                    "@echo off\r\n" +
                    "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"%~dp0Desinstalar.ps1\"\r\n" +
                    "echo Files Custom foi desinstalado.\r\n" +
                    "pause\r\n";

                File.WriteAllText(uninstallerCmd, cmdContent);

                // Registro no Windows (Adicionar/Remover Programas)
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\FilesCustom"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "Files Custom");
                        key.SetValue("DisplayVersion", "4.2.37.0");
                        key.SetValue("Publisher", "Files Community / Alexandre");
                        key.SetValue("InstallLocation", targetDir);
                        key.SetValue("DisplayIcon", Path.Combine(targetDir, "Files.exe"));
                        key.SetValue("UninstallString", "\"" + uninstallerCmd + "\"");
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private void LaunchApp()
        {
            try
            {
                Process.Start("explorer.exe", "shell:AppsFolder\\FilesDev_ykqwq8d6ps0ag!App");
            }
            catch { }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isSilent = false;
            string customTarget = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "/silent" || arg == "-silent" || arg == "/s" || arg == "-s")
                {
                    isSilent = true;
                }
                else if (arg.StartsWith("/dir=") || arg.StartsWith("-dir="))
                {
                    customTarget = args[i].Substring(5).Trim('\"');
                }
            }

            if (isSilent)
            {
                string target = !string.IsNullOrEmpty(customTarget) 
                    ? customTarget 
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\FilesCustom");
                
                InstallerForm form = new InstallerForm();
                form.RunInstallation(target);
            }
            else
            {
                Application.Run(new InstallerForm());
            }
        }
    }
}
