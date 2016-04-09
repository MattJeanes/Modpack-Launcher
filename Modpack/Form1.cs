using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Resources;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Modpack
{
    public partial class form : Form
    {
        WebClient webclient = (new WebClient());
        Process minecraft;
        List<string> modsdl;
        int modsindex;
        BackgroundWorker backgroundWorker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        int launcherversion = 6;

        bool downloadprogress = true;
        bool first = true;
        bool setup = false;
        bool setupmods = false;
        bool checklogin = false;
        bool preparekill = false;
        bool relaunching = false;

        static string site = "https://mattjeanes.com/data/modpack/";
        static string cd = Directory.GetCurrentDirectory();
        static string mc = cd + "\\.minecraft";
        static string modsf = mc + "\\mods";
        static string bin = cd + "\\bin";
        static string jar = bin + "\\minecraft.jar";
        static string zip = bin + "\\modpack.zip";
        static string version = bin + "\\version.txt";
        static string modlist = bin + "\\modlist.txt";
        static string readme = cd + "\\readme.txt";
        static string profile = mc + "\\launcher_profiles.json";
        static string updater = bin + "\\updater.exe";
        static string launcher = cd + "\\launcher.exe";
        static string versions = mc + "\\versions";

        Dictionary<string, bool> deletefiles = new Dictionary<string, bool>()
        {
            {"launcher_profiles.json",true}
        };

        Dictionary<string, bool> deletefolders = new Dictionary<string, bool>()
        {
            {"versions",true},
            {"resourcepacks",true},
            {"libraries",true},
            {"config",true}
        };

        Dictionary<string, bool> nooverwrite = new Dictionary<string, bool>()
        {
            {"options.txt",true}
        };

        public form()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
            InitializeComponent();

            webclient.DownloadDataCompleted += webclient_DownloadDataCompleted;
            webclient.DownloadProgressChanged += webclient_DownloadProgressChanged;
            webclient.DownloadFileCompleted += webclient_DownloadFileCompleted;

            this.Resize += new System.EventHandler(form_Resize);

            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;
        }

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (relaunching){
                launch();
            }
            else{
                consoleinfo("Minecraft closed");
                updatecheck();
            }
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string str = (string)e.UserState;
            if (str.Contains("Logging in with username & password"))
            {
                checklogin = true;
            }
            if (checklogin)
            {
                if (str.Contains("Installed CompleteVersion"))
                {
                    preparekill = true;
                }
                if (preparekill && !str.Contains("Installed CompleteVersion"))
                {
                    consoleinfo("Updating user profile");
                    preparekill = false;
                    checklogin = false;
                    relaunching = true;
                    minecraft.Kill();
                    updateprofile();
                }
                if (str.Contains("Couldn't log in"))
                {
                    checklogin = false;
                }
            }
            if (!str.Contains("Stream closed") && !str.Contains("Attempting to download") && !str.Contains("Finished downloading"))
            {
                consolewrite(str);
            }
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            StreamReader stream = minecraft.StandardOutput;
            while (!stream.EndOfStream)
            {
                worker.ReportProgress(0, stream.ReadLine());
            }
        }

        private void form_Load(object sender, EventArgs e)
        {
            form_Resize(sender, e);

            consoleinfo("Launcher loaded");

            updatecheck();
        }

        private void form_Resize(object sender, System.EventArgs e) 
        {
            console.Height = this.ClientSize.Height - launchbutton.Height + 1;
            progressbar.Width = this.ClientSize.Width - launchbutton.Width;
        }

        private void launchbutton_Click(object sender, EventArgs e)
        {
            launch();
        }

        private void consolewrite(string text)
        {
            console.AppendText(text + Environment.NewLine);
            console.ScrollToCaret();
        }

        private void consoleinfo(string text)
        {
            console.AppendText("[INFO] ", Color.Gray);
            console.AppendText(text + Environment.NewLine);
            console.ScrollToCaret();
        }

        private void consolewarn(string text)
        {
            console.AppendText("[WARN] ", Color.Yellow);
            console.AppendText(text + Environment.NewLine);
            console.ScrollToCaret();
        }

        private void consoleerr(string text)
        {
            console.AppendText("[ERROR] ", Color.Red);
            console.AppendText(text + Environment.NewLine);
            console.ScrollToCaret();
        }

        private void updatecheck()
        {
            if (File.Exists(updater))
            {
                String[] args = Environment.GetCommandLineArgs();
                foreach(string arg in args){
                    if (arg == "-update1")
                    {
                        consolewarn("Updating..");
                        File.Copy(updater, launcher, true);
                        Process.Start("launcher.exe", "-update2");
                        Application.Exit();
                    }
                    else if (arg == "-update2")
                    {
                        consoleinfo("Update complete");
                    }
                }
            }

            if (!Directory.Exists(mc) || !Directory.Exists(bin) || !File.Exists(version))
            {
                setup = true;
            }

            if(!Directory.Exists(modsf) || !File.Exists(modlist))
            {
                setupmods = true;
            }

            if (setup && setupmods)
            {
                if (Directory.GetFiles(cd, "*", SearchOption.TopDirectoryOnly).Length > 1)
                {
                    consolewarn("Launcher may be placed in wrong folder");
                    DialogResult result = MessageBox.Show("This launcher should be placed in an empty folder as it will generate files and folders around it. Press OK to exit or Cancel to continue.", "Did you put this in the wrong folder?", MessageBoxButtons.OKCancel);
                    if (result == DialogResult.OK)
                    {
                        consolewarn("Exiting launcher");
                        Environment.Exit(0);
                    }
                }
            }

            if (!Directory.Exists(mc))
            {
                consolewarn("'.minecraft' folder not found, creating");
                Directory.CreateDirectory(mc);
                setup = true;
            }

            if (!Directory.Exists(bin))
            {
                consolewarn("'bin' folder not found, creating");
                Directory.CreateDirectory(bin);
                setup = true;
            }

            if (!Directory.Exists(modsf))
            {
                consolewarn("'mods' folder not found, creating");
                Directory.CreateDirectory(modsf);
                setupmods = true;
            }

            if (!File.Exists(version))
            {
                consolewarn("'version' file not found, creating");
                StreamWriter sw = File.CreateText(version);
                sw.Write("0");
                sw.Close();
                setup = true;
            }

            if (!File.Exists(modlist))
            {
                consolewarn("'modlist' file not found, creating");
                StreamWriter sw = File.CreateText(modlist);
                sw.Write("");
                sw.Close();
                setupmods = true;
            }

            /*
            if (!File.Exists(readme))
            {
                consolewarn("'readme' file not found, creating");
                StreamWriter sw = File.CreateText(readme);
                sw.Write(@"First time installation (or after modpack update):
1. Launch Minecraft.
2. Login with your Minecraft username/password.
3. Play."
                );
                sw.Close();
            }
            */

            if (setup)
            {
                setupmods = true;
                consolewarn("One of more required files are missing, forcing modpack update");
            }

            if (setupmods)
            {
                consolewarn("One of more required mods files are missing, forcing mods update");
            }

            if (!File.Exists(jar))
            {
                consolewarn("Minecraft jar not found, downloading..");
                webclient.DownloadFileAsync(new Uri("https://s3.amazonaws.com/Minecraft.Download/launcher/Minecraft.jar"), jar, "jar");
            }
            else
            {
                beginupdatecheck();
            }
        }

        private void launch()
        {
            if (relaunching)
            {
                consolewarn("Rebooting Minecraft");
            }
            else
            {
                consoleinfo("Launching Minecraft");
            }
            launchbutton.Enabled = false;
            progressbar.Value = 0;
            try
            {
                Environment.SetEnvironmentVariable("AppData", cd, EnvironmentVariableTarget.Process);
                ProcessStartInfo _processStartInfo = new ProcessStartInfo();
                _processStartInfo.UseShellExecute = false;
                _processStartInfo.RedirectStandardOutput = true;
                _processStartInfo.WorkingDirectory = bin;
                _processStartInfo.FileName = @"java.exe";
                _processStartInfo.Arguments = "-jar minecraft.jar";
                _processStartInfo.CreateNoWindow = true;
                minecraft = Process.Start(_processStartInfo);
                backgroundWorker.RunWorkerAsync();
                relaunching = false;
            }
            catch
            {
                consoleerr("Failed to launch Minecraft, check Java installation");
                launchbutton.Enabled = true;
            }
        }
        private void beginupdatecheck()
        {
            consoleinfo("Checking for updates");
            webclient.DownloadDataAsync(new Uri(site + "launcherversion.txt"), "launcherversion");
        }

        private void updatemods()
        {
            webclient.DownloadDataAsync(new Uri(site + "modlist.php"), "modlist");
        }

        private void finishmodupdatecheck()
        {
            byte[] raw = webclient.DownloadData(new Uri(site + "modlist.php"));
            string result = System.Text.Encoding.UTF8.GetString(raw);
            StreamWriter sw = File.CreateText(modlist);
            sw.Write(result);
            sw.Close();
            setup = false;
            setupmods = false;
            consoleinfo("Update check complete");
            if (first)
            {
                first = false;
                launch();
            }
            else
            {
                launchbutton.Enabled = true;
                consoleinfo("Ready to launch");
            }
        }

        private void updatemodpack()
        {
            consoleinfo("Updating modpack");
            consoleinfo("Downloading modpack");
            webclient.DownloadFileAsync(new Uri(site + "modpack.zip"), zip, "modpack");
        }

        private void updatemods(List<string> add, List<string> rem, string[] servermods)
        {
            consoleinfo("Updating mods..");

            if (rem.Count > 0)
            {
                consoleinfo("Removing:");
                foreach (string mod in rem)
                {
                    consoleinfo("• " + mod);
                    string path = modsf + "\\" + mod;
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }

            if (add.Count > 0)
            {
                consoleinfo("Downloading:");

                downloadprogress = false;

                modsindex = 0;
                modsdl = add;
                downloadmod(0);
            }
            else
            {
                finishmodupdatecheck();
            }
        }

        private void downloadmod(int i)
        {
            string mod = modsdl[i];
            consoleinfo("• " + mod);
            string path = modsf + "\\" + mod.Replace("/", "\\");
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            webclient.DownloadFileAsync(new Uri(site + "mods/" + mod), path, "mod");
            double percent = Math.Min(((i + 1) / (double)modsdl.Count) * 100, 100);
            progressbar.Value = (int)percent;
        }

        private void updateprofile()
        {
            string key="profiles";
            string profiletext = File.ReadAllText(profile);
            JObject o = JObject.Parse(profiletext);
            JObject profiles = (JObject)o[key];
            JProperty toadd = new JProperty("lastVersionId", new JObject());
            string profilename = "";
            foreach (JToken child in profiles.Children())
            {
                profilename = child.Path.Remove(child.Path.IndexOf(key), key.Length).Substring(1);
                break;
            }

            string version = "";
            foreach (DirectoryInfo dir in new DirectoryInfo(versions).GetDirectories())
            {
                if (dir.Name.ToLower().Contains("forge"))
                {
                    version = dir.Name;
                }
            }

            if (version.Length > 0)
            {
                profiles[profilename]["lastVersionId"] = version;
                consoleinfo("Set minecraft version to " + version);
            }
            else
            {
                consolewarn("Unable to set minecraft version");
            }
            profiles[profilename]["javaArgs"] = "-XX:MaxPermSize=256M -Xms512M -Xmx4096M";
            File.Delete(profile);
            StreamWriter sw = File.CreateText(profile);
            sw.Write(JsonConvert.SerializeObject(o,Formatting.Indented));
            sw.Close();
        }

        private void updatelauncher()
        {
            consolewarn("Updating launcher");
            webclient.DownloadFileAsync(new Uri(site + "launcher.exe"), updater, "launcher");
        }

        private bool checkoverwrite(string name)
        {
            name = name.Replace(mc + "\\", "");
            if (nooverwrite.ContainsKey(name)){
                consoleinfo("Not overwriting " + name);
                return false;
            }else{
                consoleinfo("Overwriting " + name);
                return true;
            }
        }

        void webclient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            String id = (String)e.UserState;
            if (downloadprogress)
            {
                progressbar.Value = 0;
            }

            if (id == "jar")
            {
                consoleinfo("Minecraft jar downloaded");
                beginupdatecheck();
            }
            else if (id == "modpack")
            {
                consoleinfo("Modpack downloaded");

                bool preserve;

                if(setup || (MessageBox.Show("Do you want to preserve your files? If not, this will delete the entire minecraft folder.\n\nYou should delete everything if there are significant changes to the modpack.", "Preserve files or delete everything", MessageBoxButtons.YesNo) == DialogResult.No)){
                    consolewarn("Removing old modpack");

                    DirectoryInfo dirinfo = new DirectoryInfo(mc);

                    foreach (FileInfo file in dirinfo.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in dirinfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                    preserve = false;
                }
                else{
                    consoleinfo("Attempting to preserve files");

                    DirectoryInfo dirinfo = new DirectoryInfo(mc);

                    foreach (FileInfo file in dirinfo.GetFiles())
                    {
                        if (deletefiles.ContainsKey(file.Name))
                        {
                            consolewarn("Deleting " + file.Name);
                            file.Delete();
                        }
                        
                    }
                    foreach (DirectoryInfo dir in dirinfo.GetDirectories())
                    {
                        if (deletefolders.ContainsKey(dir.Name))
                        {
                            consolewarn("Deleting " + dir.Name);
                            dir.Delete(true);
                        }
                       
                    }
                    preserve = true;
                }

                consoleinfo("Extracting new modpack, please wait..");

                try
                {
                    FastZip fz = new FastZip();
                    fz.ExtractZip(zip, mc, FastZip.Overwrite.Prompt, checkoverwrite,"","",true);

                    consoleinfo("Modpack extracted");

                    File.Delete(zip);

                    byte[] raw = webclient.DownloadData(new Uri(site + "version.txt"));
                    string result = System.Text.Encoding.UTF8.GetString(raw);
                    StreamWriter sw = File.CreateText(version);
                    sw.Write(result);
                    sw.Close();

                    if (!preserve)
                    {
                        StreamWriter sw2 = File.CreateText(modlist);
                        sw2.Write("");
                        sw2.Close();
                    }
                }
                catch
                {
                    consoleerr("An error occured during modpack update");
                }

                updatemods();
            }
            else if (id == "mod")
            {
                modsindex += 1;
                if (modsindex < modsdl.Count)
                {
                    downloadmod(modsindex);
                }
                else
                {
                    downloadprogress = true;
                    progressbar.Value = 100;
                    finishmodupdatecheck();
                }
            }
            else if (id == "launcher")
            {
                consolewarn("Updating now..");
                Process.Start(updater, "-update1");
                Application.Exit();
            }
        }

        void webclient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            String id = (String)e.UserState;
            byte[] raw = e.Result;
            string result = System.Text.Encoding.UTF8.GetString(raw);
            progressbar.Value = 0;
            if (id == "launcherversion")
            {
                int lversion = launcherversion;
                int sversion = Convert.ToInt32(result);
                if (sversion > lversion)
                {
                    consolewarn("Launcher out of date");
                    updatelauncher();
                }
                else
                {
                    consoleinfo("Launcher up to date");
                    webclient.DownloadDataAsync(new Uri(site + "version.txt"), "version");
                }
            }
            if (id == "version")
            {
                int lversion = Convert.ToInt32(File.ReadAllText(version));
                int sversion = Convert.ToInt32(result);
                if (sversion > lversion || setup)
                {
                    consolewarn("Modpack out of date or missing");
                    if (setup || MessageBox.Show("Modpack update available, do you wish to update?", "Modpack Update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        updatemodpack();
                    }
                    else
                    {
                        consoleinfo("Not updating modpack");
                        updatemods();
                    }
                }
                else
                {
                    consoleinfo("Modpack up to date");
                    updatemods();
                }

            }
            else if (id == "modlist")
            {
                List<string> add = new List<string>();
                List<string> rem = new List<string>();

                string[] mods = File.ReadAllLines(modlist);
                string[] servermods = result.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var mod in mods)
                {
                    bool found = false;
                    foreach (var smod in servermods)
                    {
                        if (mod == smod)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        rem.Add(mod);
                    }
                }

                foreach (var smod in servermods)
                {
                    bool found = false;
                    foreach (var mod in mods)
                    {
                        if (smod == mod)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (setupmods || !found)
                    {
                        add.Add(smod);
                    }
                }

                if (add.Count() > 0 || rem.Count() > 0 || setupmods)
                {
                    consolewarn("Mods out of date or missing");
                    if (setupmods || MessageBox.Show("Mods update available, do you wish to update?", "Mods Update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        updatemods(add, rem, servermods);
                    }
                    else
                    {
                        consoleinfo("Not updating mods");
                        finishmodupdatecheck();
                    }
                }
                else
                {
                    consoleinfo("Mods up to date");
                    finishmodupdatecheck();
                }
            }
        }

        void webclient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (downloadprogress)
            {
                progressbar.Value = e.ProgressPercentage;
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

        }

    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

}
