using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

class Tuin : Window
{
    static string Root=AppDomain.CurrentDomain.BaseDirectory;
    static string[] Games={"Quake II"};
    static string[] Dirs={"baseq2"};
    static string[] Maps={"start","start","start"};
    static string DefaultQuake=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam","steamapps","common","Quake 2");
    static string DefaultNR=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),@"Tuin Quake II RTX\game");
    ComboBox campaign, resolution, passes;
    CheckBox neural, fullscreen, remastered;
    TextBox source, destination, nrsource;
    TextBlock status;
    Button action;
    bool installing=false;
    static Brush Ink=new SolidColorBrush(Color.FromRgb(18,20,21));
    static Brush Gold=new SolidColorBrush(Color.FromRgb(239,170,91));
    static Brush Muted=new SolidColorBrush(Color.FromRgb(177,178,174));
    static BitmapImage Asset(string name) {var b=new BitmapImage(); b.BeginInit(); b.StreamSource=Assembly.GetExecutingAssembly().GetManifestResourceStream(name); b.CacheOption=BitmapCacheOption.OnLoad; b.EndInit(); b.Freeze(); return b;}
    static TextBlock Text(string text,double size,Brush color){return new TextBlock{Text=text,FontSize=size,Foreground=color,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};}
    static Button Btn(string text){return new Button{Content=text,Height=40,Padding=new Thickness(14,4,14,4),Margin=new Thickness(0,4,0,10),Background=Gold,Foreground=Ink,FontWeight=FontWeights.SemiBold};}
    static ComboBox Combo(string[] choices,int selected){var c=new ComboBox{Height=34,FontSize=14,Margin=new Thickness(0,0,0,14),Foreground=Brushes.Black}; foreach(var s in choices)c.Items.Add(s); c.SelectedIndex=selected; return c;}
    static string ChooseFolder(string initial){using(var d=new System.Windows.Forms.FolderBrowserDialog()){d.SelectedPath=Directory.Exists(initial)?initial:""; return d.ShowDialog()==System.Windows.Forms.DialogResult.OK?d.SelectedPath:initial;}}
    TextBox Folder(StackPanel parent,string label,string initial){parent.Children.Add(Text(label,12,Muted)); var row=new DockPanel{Margin=new Thickness(0,0,0,10)};var b=Btn("…");b.Width=38;b.Height=30;b.Margin=new Thickness(6,0,0,0);DockPanel.SetDock(b,Dock.Right);row.Children.Add(b);var t=new TextBox{Text=initial,Height=30,Padding=new Thickness(5),VerticalContentAlignment=VerticalAlignment.Center};row.Children.Add(t);b.Click+=(s,e)=>t.Text=ChooseFolder(t.Text);parent.Children.Add(row);return t;}
    Tuin()
    {
#if INSTALLER
        Title="Tuin Quake II RTX — Setup";
#else
        Title="Tuin Quake II RTX";
#endif
        Width=1080;Height=760;MinWidth=920;MinHeight=700;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Ink;FontFamily=new FontFamily("Segoe UI");Icon=Asset("icon.png");
        var grid=new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});Content=grid;
        var hero=new Grid{Background=new ImageBrush(Asset("scene2.png")){Stretch=Stretch.UniformToFill,AlignmentX=AlignmentX.Center}};grid.Children.Add(hero);
        hero.Children.Add(new Border{Background=new LinearGradientBrush(Color.FromArgb(30,0,0,0),Color.FromArgb(245,10,12,13),90)});
        var branding=new StackPanel{Margin=new Thickness(38),VerticalAlignment=VerticalAlignment.Bottom};hero.Children.Add(branding);
        branding.Children.Add(new Image{Source=Asset("icon.png"),Width=110,Height=110,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,22)});
        branding.Children.Add(Text("WELCOME BACK\nTO STROGGOS.",37,Brushes.White));
        branding.Children.Add(Text("QUAKE II RTX",24,Gold));branding.Children.Add(Text("TUIN EDITION",14,Muted));
        branding.Children.Add(Text("Classic Quake II. Path-traced light.\nRemastered models. Your choice of rendering.",15,Muted));
        var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(scroll,1);grid.Children.Add(scroll);var panel=new StackPanel{Margin=new Thickness(32,26,32,24)};scroll.Content=panel;
#if INSTALLER
        panel.Children.Add(Text("Make yourself at home.",27,Brushes.White));panel.Children.Add(Text("Install Quake II from your owned game files. Choose the optional extras below.",14,Muted));
        source=Folder(panel,"YOUR QUAKE II FOLDER (contains baseq2)",DefaultQuake);
        destination=Folder(panel,"INSTALL TO A NEW FOLDER",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Tuin Quake II RTX"));
        panel.Children.Add(Text("Included: TuinRTX path tracing and crisp original textures. Optional remastered models are converted locally from your rerelease files.",14,Muted));
        remastered=new CheckBox{Content="Remastered models (recommended)",IsChecked=true,Foreground=Gold,FontSize=15,Margin=new Thickness(0,10,0,12)};panel.Children.Add(remastered);
        neural=new CheckBox{Content="Install DLSS 5 experimental option",Foreground=Gold,FontSize=15,Margin=new Thickness(0,10,0,12)};panel.Children.Add(neural);
        nrsource=Folder(panel,"OPTIONAL: EXISTING CHICKEN / DLSS 5 FILES FOLDER",Directory.Exists(DefaultNR)?DefaultNR:"");nrsource.IsEnabled=false;neural.Checked+=(s,e)=>nrsource.IsEnabled=true;neural.Unchecked+=(s,e)=>nrsource.IsEnabled=false;
        panel.Children.Add(Text("Choose your extracted Chicken + NR files. Setup also downloads LumeniteFX from its author. Leave this off to add it later. Experimental rendering can reduce frame rate and may crash on exit.",12,Muted));
        var help=Btn("DLSS 5 setup instructions ↗");help.Click+=(s,e)=>Process.Start("https://github.com/tuin-boop/TuinQuake2RTX/blob/codex/tuin-launcher-neural/Packaging/TuinEdition/README.md");panel.Children.Add(help);
        action=Btn("INSTALL TUIN EDITION");action.Height=48;panel.Children.Add(action);action.Click+=async(s,e)=>await InstallClicked();
#else
        panel.Children.Add(Text("Return to Stroggos.",27,Brushes.White));panel.Children.Add(Text("Original Quake II campaign.",14,Muted));campaign=Combo(Games,0);panel.Children.Add(campaign);
        panel.Children.Add(Text("DISPLAY",12,Muted));resolution=Combo(new[]{"1280 × 720","1920 × 1080","2560 × 1440","3840 × 2160"},1);panel.Children.Add(resolution);
        fullscreen=new CheckBox{Content="Fullscreen",Foreground=Brushes.White,Margin=new Thickness(0,0,0,20)};panel.Children.Add(fullscreen);
        panel.Children.Add(Text("TUINRTX · PATH TRACING",14,Gold));panel.Children.Add(Text("Original textures with nearest sampling. Choose remastered models independently of experimental neural rendering.",14,Muted));
        remastered=new CheckBox{Content="Use remastered models",Foreground=Gold,Margin=new Thickness(0,8,0,8),IsEnabled=File.Exists(Path.Combine(Root,"game","baseq2","z_remastered_models.pkz"))||File.Exists(Path.Combine(Root,"game","baseq2","z_remastered_models.pkz.disabled")),IsChecked=File.Exists(Path.Combine(Root,"game","baseq2","z_remastered_models.pkz"))};panel.Children.Add(remastered);
        neural=new CheckBox{Content="DLSS 5 neural rendering — experimental",Foreground=Gold,FontSize=14,Margin=new Thickness(0,12,0,12)};panel.Children.Add(neural);
        bool available=HasNR(Path.Combine(Root,"game"));neural.IsEnabled=available;
        panel.Children.Add(Text(available?"Optional appearance changes. Costs GPU time; shutdown and image quality are still experimental.":"Optional component not installed. Add your downloaded files below to enable it.",12,Muted));
        passes=Combo(new[]{"1 pass · recommended","2 passes","3 passes","4 passes"},0);passes.IsEnabled=false;panel.Children.Add(passes);neural.Checked+=(s,e)=>passes.IsEnabled=true;neural.Unchecked+=(s,e)=>passes.IsEnabled=false;
        if(!available){var add=Btn("Add DLSS 5 files…");panel.Children.Add(add);add.Click+=async(s,e)=>{try{string from=ChooseFolder("");if(string.IsNullOrEmpty(from))return;add.IsEnabled=false;status.Text="Importing files and downloading LumeniteFX...";await Task.Run(()=>ImportNR(from,Path.Combine(Root,"game")));neural.IsEnabled=true;add.IsEnabled=false;status.Text="DLSS 5 installed. Enable the checkbox to try it.";}catch(Exception ex){add.IsEnabled=true;status.Text=ex.Message;}};var help=Btn("DLSS 5 setup instructions ↗");panel.Children.Add(help);help.Click+=(s,e)=>Process.Start("https://github.com/tuin-boop/TuinQuake2RTX/blob/codex/tuin-launcher-neural/Packaging/TuinEdition/README.md");}
        action=Btn("PLAY QUAKE II  →");action.Height=52;panel.Children.Add(action);action.Click+=(s,e)=>Play();
        var shots=Btn("Open screenshots");shots.Background=new SolidColorBrush(Color.FromRgb(54,57,57));shots.Foreground=Brushes.White;panel.Children.Add(shots);shots.Click+=(s,e)=>{var p=Path.Combine(Root,"Screenshots");Directory.CreateDirectory(p);Process.Start(new ProcessStartInfo(p){UseShellExecute=true});};
        panel.Children.Add(Text("During neural play: Esc → Ctrl+Home → Deep Fried Chicken. Print Screen saves captures when the overlay is enabled.",12,Muted));
        LoadSettings();
#endif
        status=Text("Ready.",12,Muted);panel.Children.Add(status);
        Closing+=(s,e)=>{if(installing)e.Cancel=true;};
    }
    static bool HasNR(string dir){return new[]{"deep-fried-chicken.addon64","deep-fried-chicken-nvngx.dll","nvngx_dlssnr.dll"}.All(f=>File.Exists(Path.Combine(dir,f)));}
    static void Ini(string path,string section,string key,string value){var lines=File.Exists(path)?File.ReadAllLines(path).ToList():new System.Collections.Generic.List<string>();int start=lines.FindIndex(x=>x.Trim()=="["+section+"]");if(start<0){lines.Add("["+section+"]");lines.Add(key+"="+value);}else{int end=start+1;while(end<lines.Count&&!lines[end].TrimStart().StartsWith("["))end++;int index=lines.FindIndex(start+1,end-start-1,x=>x.StartsWith(key+"=",StringComparison.OrdinalIgnoreCase));if(index>=0)lines[index]=key+"="+value;else lines.Insert(end,key+"="+value);}File.WriteAllLines(path,lines);}
    static void ConfigureOverlay(string game,string addons){string ini=Path.Combine(game,"ReShade.ini");Ini(ini,"ADDON","AddonPath",addons);Ini(ini,"ADDON","LoadFromDllMain","deep-fried-chicken.addon64");Ini(ini,"GENERAL","PresetPath",Path.Combine(game,"ReShadePreset.ini"));Ini(ini,"INPUT","KeyOverlay","36,1,0,0");Ini(ini,"INPUT","KeyScreenshot","44,0,0,0");Ini(ini,"INPUT","InputProcessing","2");Ini(ini,"SCREENSHOT","SavePath",Path.Combine(Root,"Screenshots"));Ini(ini,"SCREENSHOT","FileFormat","1");Ini(ini,"GENERAL","EffectSearchPaths",Path.Combine(game,@"reshade-shaders\Shaders\**"));Ini(ini,"GENERAL","TextureSearchPaths",Path.Combine(game,@"reshade-shaders\Textures\**"));Ini(ini,"GENERAL","PreprocessorDefinitions","DLSS5_MV_PROVIDER=3");}

    static void GetLumenite(string game){
        string dest=Path.Combine(game,"reshade-shaders");
        if(File.Exists(Path.Combine(dest,"Shaders","lumenite_Kernel.fx")))return;
        string temp=Path.Combine(Path.GetTempPath(),"TuinLumenite-"+Guid.NewGuid().ToString("N")+".zip");
        try{
            System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;
            using(var web=new System.Net.WebClient())web.DownloadFile("https://codeload.github.com/umar-afzaal/LumeniteFX/zip/f8cbbb4eccfcb7adf0d74bb358ba349272e3c1e9",temp);
            using(var zip=ZipFile.OpenRead(temp)){foreach(var e in zip.Entries){
                int slash=e.FullName.IndexOf('/');if(slash<0||string.IsNullOrEmpty(e.Name))continue;string name=e.FullName.Substring(slash+1);
                if(!name.StartsWith("Shaders/")&&!name.StartsWith("Textures/")&&name!="LICENSE.md"&&name!="NOTICE")continue;
                string path=Child(dest,name.Replace('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(path));e.ExtractToFile(path,true);
            }}
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    static void ImportNR(string from,string to){if(!HasNR(from))throw new Exception("Choose a folder containing deep-fried-chicken.addon64, deep-fried-chicken-nvngx.dll and nvngx_dlssnr.dll. Extract downloaded ZIP files first.");foreach(var p in Process.GetProcessesByName("q2rtx")){try{if(string.Equals(p.MainModule.FileName,Path.Combine(to,"q2rtx.exe"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Close this game before adding neural files.");}catch(System.ComponentModel.Win32Exception){}}GetLumenite(to);foreach(var name in new[]{"deep-fried-chicken.addon64","deep-fried-chicken-nvngx.dll","nvngx_dlssnr.dll","deep-fried-chicken.cfg"}){var f=Path.Combine(from,name);if(File.Exists(f)&&!string.Equals(Path.GetFullPath(f),Path.GetFullPath(Path.Combine(to,name)),StringComparison.OrdinalIgnoreCase))File.Copy(f,Path.Combine(to,name),true);}}
    static void CopyTree(string from,string to){Directory.CreateDirectory(to);foreach(var f in Directory.GetFiles(from))File.Copy(f,Path.Combine(to,Path.GetFileName(f)),true);foreach(var d in Directory.GetDirectories(from))CopyTree(d,Path.Combine(to,Path.GetFileName(d)));}
    static string Child(string root,string name){string full=Path.GetFullPath(Path.Combine(root,name));if(!full.StartsWith(Path.GetFullPath(root).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new Exception("Unsafe archive path");return full;}

    static string ExtractPayload(string target){
        string temp=Path.Combine(Path.GetTempPath(),"TuinQ2-"+Guid.NewGuid().ToString("N")+".zip");
        try{
            using(var exe=File.OpenRead(Assembly.GetExecutingAssembly().Location)){
                exe.Seek(-16,SeekOrigin.End);var br=new BinaryReader(exe);long offset=br.ReadInt64();
                if(System.Text.Encoding.ASCII.GetString(br.ReadBytes(8))!="TUINQ2Z1"||offset<0||offset>exe.Length-16)throw new Exception("Installer payload is missing.");
                long remaining=exe.Length-16-offset;exe.Position=offset;
                using(var output=File.Create(temp)){byte[] buffer=new byte[1024*1024];while(remaining>0){int n=exe.Read(buffer,0,(int)Math.Min(buffer.Length,remaining));if(n==0)throw new EndOfStreamException();output.Write(buffer,0,n);remaining-=n;}}
            }
            using(var zip=ZipFile.OpenRead(temp)){foreach(var entry in zip.Entries){if(string.IsNullOrEmpty(entry.Name))continue;string dest=Child(target,entry.FullName.Replace('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(dest));entry.ExtractToFile(dest);}}
            return target;
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static void Install(string game,string target,string nr,bool models,Action<string> progress){
        game=Path.GetFullPath(game);target=Path.GetFullPath(target);
        if(Directory.Exists(target)&&Directory.EnumerateFileSystemEntries(target).Any())throw new Exception("Choose a new or empty installation folder.");
        string classic=Path.Combine(game,"baseq2","pak0.pak"),remaster=Path.Combine(game,"rerelease","baseq2","pak0.pak");
        if(!File.Exists(classic))throw new Exception("Missing baseq2/pak0.pak. Select your original Quake II game folder.");
        if(models&&!File.Exists(remaster))throw new Exception("Remastered models need rerelease/baseq2/pak0.pak. Install the Quake II rerelease, or untick remastered models.");
        if(nr!=null&&!HasNR(nr))throw new Exception("The optional folder must contain Chicken add-on, its NGX DLL and nvngx_dlssnr.dll.");
        Directory.CreateDirectory(target);progress("Extracting TuinRTX (about 1 GB)...");
        ExtractPayload(target);
        string dest=Path.Combine(target,"game","baseq2");
        progress("Importing original Quake II...");
        foreach(string name in new[]{"pak0.pak","pak1.pak","pak2.pak"}){string from=Path.Combine(game,"baseq2",name);if(File.Exists(from))File.Copy(from,Path.Combine(dest,name));}
        string music=Path.Combine(game,"baseq2","music");if(Directory.Exists(music))CopyTree(music,Path.Combine(dest,"music"));
        if(models){
            progress("Converting remastered models. This can take several minutes...");
            var info=new ProcessStartInfo(Path.Combine(target,"tools","TuinModelConverter.exe")){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=target,RedirectStandardOutput=true,RedirectStandardError=true,Arguments="--classic \""+classic+"\" --remaster \""+remaster+"\" --output \""+Path.Combine(dest,"z_remastered_models.pkz")+"\""};
            using(var proc=new Process{StartInfo=info}){proc.Start();var stdout=proc.StandardOutput.ReadToEndAsync();var stderr=proc.StandardError.ReadToEndAsync();proc.WaitForExit();Task.WaitAll(stdout,stderr);File.WriteAllText(Path.Combine(target,"model-conversion.log"),stdout.Result+stderr.Result);if(proc.ExitCode!=0)throw new Exception("Model conversion failed. See model-conversion.log in the install folder.");}
        }
        if(nr!=null){progress("Importing optional Chicken files...");ImportNR(nr,Path.Combine(target,"game"));}
        Directory.CreateDirectory(Path.Combine(target,"Screenshots"));
        File.WriteAllText(Path.Combine(target,"installation.txt"),"Tuin Quake II RTX\r\nRemastered models: "+models+"\r\nOptional NR: "+(nr!=null));
        progress("Installed. Ready to play.");
    }

    static void CreateShortcut(string target){
        try{
            var type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type);
            object link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Tuin Quake II RTX.lnk")});
            var lt=link.GetType();lt.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{Path.Combine(target,"Tuin Quake II RTX.exe")});
            lt.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{target});
            lt.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
        }catch{/* Installation remains usable when desktop shortcuts are unavailable. */}
    }
    async Task InstallClicked(){if(installing||installed)return;string g=source.Text,t=destination.Text,n=neural.IsChecked==true?nrsource.Text:null;bool models=remastered.IsChecked==true;installing=true;action.IsEnabled=false;try{await Task.Run(()=>Install(g,t,n,models,msg=>Dispatcher.Invoke(new Action(()=>status.Text=msg))));CreateShortcut(t);action.Content="OPEN PLAY LAUNCHER";action.IsEnabled=true;action.Click+=(s,e)=>{Process.Start(new ProcessStartInfo(Path.Combine(t,"Tuin Quake II RTX.exe")){WorkingDirectory=t,UseShellExecute=true});Close();};source.IsEnabled=false;destination.IsEnabled=false;neural.IsEnabled=false;nrsource.IsEnabled=false;remastered.IsEnabled=false;installed=true;}catch(Exception ex){status.Text=ex.Message;action.IsEnabled=true;}finally{installing=false;}}
    bool installed=false;
    static string ReadLiveLog(string path,long offset){try{using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){file.Position=file.Length<offset?0:offset;using(var reader=new StreamReader(file))return reader.ReadToEnd();}}catch(IOException){return "";}}
    static long LogLength(string path){return File.Exists(path)?new FileInfo(path).Length:0;}
    static bool Ready(string console,string sr,string bridge,bool nr){return console.Contains("Q2RTX initialized");}
    void ShowLoading(Process process,string game,bool nr,long srOffset,long bridgeOffset,DateTime started)
    {
        var loading=new Window{Title="Loading — Tuin Quake II RTX",Width=820,Height=480,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,ShowInTaskbar=false,Topmost=true,Background=Ink,Icon=Asset("icon.png")};
        var canvas=new Grid{Background=new ImageBrush(Asset("scene2.png")){Stretch=Stretch.UniformToFill}};loading.Content=canvas;
        canvas.Children.Add(new Border{Background=new LinearGradientBrush(Color.FromArgb(110,10,12,13),Color.FromArgb(245,10,12,13),90)});
        var content=new StackPanel{Margin=new Thickness(38),VerticalAlignment=VerticalAlignment.Bottom};canvas.Children.Add(content);
        content.Children.Add(new Image{Source=Asset("icon.png"),Width=72,Height=72,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,16)});
        content.Children.Add(Text("DEPLOYING TO STROGGOS",27,Gold));
        content.Children.Add(Text("Preparing "+Games[campaign.SelectedIndex]+". Startup can take 30–60 seconds.",15,Brushes.White));
        var phase=Text("Starting the renderer…",14,Muted);content.Children.Add(phase);
        content.Children.Add(new ProgressBar{IsIndeterminate=true,Height=5,Foreground=Gold,Background=Ink,Margin=new Thickness(0,2,0,18)});
        var hide=Btn("Hide loading screen");hide.HorizontalAlignment=HorizontalAlignment.Left;hide.Background=Ink;hide.Foreground=Brushes.White;content.Children.Add(hide);
        var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
        bool ready=false;action.IsEnabled=false;
        loading.Closed+=(s,e)=>{timer.Stop();action.IsEnabled=true;if(!ready)status.Text="Loading screen hidden. Quake continues starting.";};
        hide.Click+=(s,e)=>{loading.Close();WindowState=WindowState.Minimized;};
        timer.Tick+=(s,e)=>{
            if(process.HasExited){ready=true;timer.Stop();loading.Close();WindowState=WindowState.Normal;status.Text="Quake closed during startup. Check game/baseq2/logs/console.log for details.";return;}
            string consolePath=Path.Combine(game,@"baseq2\logs\console.log");
            string console=File.Exists(consolePath)&&File.GetLastWriteTimeUtc(consolePath)>=started?ReadLiveLog(consolePath,0):"";
            string sr=ReadLiveLog(Path.Combine(game,"native-dlss.log"),srOffset);
            string bridge=ReadLiveLog(Path.Combine(game,"dlss5-vk-bridge.log"),bridgeOffset);
            int elapsed=(int)(DateTime.UtcNow-started).TotalSeconds;
            string stage=!console.Contains("Quake Initialized")?"Preparing path tracing…":nr&&!bridge.Contains("first D3D12 evaluate completed")?"Preparing experimental neural rendering…":"Loading game content…";
            phase.Text=stage+"  "+elapsed+"s"+(elapsed>=90?"\nTaking longer than usual. You can hide this screen to inspect Quake.":"");
            if(process.MainWindowHandle!=IntPtr.Zero && (DateTime.UtcNow-started).TotalSeconds>3){ready=true;timer.Stop();loading.Close();WindowState=WindowState.Minimized;status.Text="Game window opened. Experimental rendering may need additional warm-up.";}
        };
        loading.Show();timer.Start();
    }
    void LoadSettings(){try{var p=Path.Combine(Root,"launcher.settings");if(!File.Exists(p))return;var s=File.ReadAllLines(p);campaign.SelectedIndex=Math.Max(0,Math.Min(0,int.Parse(s[0])));resolution.SelectedIndex=Math.Max(0,Math.Min(3,int.Parse(s[1])));fullscreen.IsChecked=s[2]=="True";neural.IsChecked=neural.IsEnabled&&s[3]=="True";passes.SelectedIndex=Math.Max(0,Math.Min(3,int.Parse(s[4])));}catch{}}
    void Play(){try{int g=campaign.SelectedIndex;string game=Path.Combine(Root,"game"),exp=Path.Combine(Root,"game");foreach(var p in Process.GetProcessesByName("q2rtx")){try{if(string.Equals(p.MainModule.FileName,Path.Combine(game,"q2rtx.exe"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("This copy of Quake is already running.");}catch(System.ComponentModel.Win32Exception){}}
        File.WriteAllLines(Path.Combine(Root,"launcher.settings"),new[]{g.ToString(),resolution.SelectedIndex.ToString(),fullscreen.IsChecked.ToString(),neural.IsChecked.ToString(),passes.SelectedIndex.ToString()});
        int[] widths={1280,1920,2560,3840},heights={720,1080,1440,2160};bool nr=neural.IsChecked==true;
        File.WriteAllLines(Path.Combine(game,Dirs[g],"tuin-launch.cfg"),new[]{"exec rt_classic.cfg","set vid_hdr 0","set pt_nearest 2","set pt_freecam 0","set pt_accumulation_rendering 0","set pt_dof 0","set cl_maxfps 60","bind F12 screenshot","set vid_fullscreen "+(fullscreen.IsChecked==true?"1":"0"),"set vid_geometry "+widths[resolution.SelectedIndex]+"x"+heights[resolution.SelectedIndex]});

        string model=Path.Combine(game,"baseq2","z_remastered_models.pkz");
        if(remastered.IsChecked==true&&!File.Exists(model)&&File.Exists(model+".disabled"))File.Move(model+".disabled",model);
        if(remastered.IsChecked!=true&&File.Exists(model)&&!File.Exists(model+".disabled"))File.Move(model,model+".disabled");
        var psi=new ProcessStartInfo(Path.Combine(game,"q2rtx.exe")){WorkingDirectory=game,UseShellExecute=false,Arguments="+set basedir . +set homedir . +set logfile 2 +set vid_fullscreen "+(fullscreen.IsChecked==true?"1":"0")+" +set vid_geometry "+widths[resolution.SelectedIndex]+"x"+heights[resolution.SelectedIndex]+" +exec tuin-launch.cfg"};
        psi.EnvironmentVariables.Remove("VK_INSTANCE_LAYERS");psi.EnvironmentVariables.Remove("VK_LAYER_PATH");psi.EnvironmentVariables["DISABLE_VK_LAYER_reshade_1"]="1";psi.EnvironmentVariables["RG_EXPERIMENTAL_DLSS_NR"]="0";psi.EnvironmentVariables["RG_TRACE_DLSS_SR"]="1";
        if(nr){if(!HasNR(exp))throw new Exception("Optional neural files are missing.");string cfg=Path.Combine(exp,"deep-fried-chicken.cfg");string text=File.Exists(cfg)?File.ReadAllText(cfg):"config_schema=6\r\narm=1\r\nenabled=1\r\nlayers=1\r\n";text=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^layers=.*$","layers="+(passes.SelectedIndex+1));text=System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^enabled=.*$","enabled=1");File.WriteAllText(cfg,text);File.WriteAllText(Path.Combine(Root,"layer","ReShadeApps.ini"),"[GENERAL]\r\nApps="+psi.FileName);ConfigureOverlay(game,exp);psi.EnvironmentVariables["VK_INSTANCE_LAYERS"]="VK_LAYER_quake_swapper";psi.EnvironmentVariables["VK_LAYER_PATH"]=Path.Combine(Root,"layer");}
        if(Environment.GetCommandLineArgs().Contains("--smoke"))psi.Arguments+=" +map base1 +wait 180 +quit";
        long srOffset=LogLength(Path.Combine(game,"native-dlss.log")),bridgeOffset=LogLength(Path.Combine(game,"dlss5-vk-bridge.log"));DateTime started=DateTime.UtcNow;
        var child=Process.Start(psi);status.Text=Games[g]+" started · "+(nr?"experimental NR, "+(passes.SelectedIndex+1)+" pass(es)":"TuinRTX path tracing");
        if(!Environment.GetCommandLineArgs().Contains("--launch-test"))ShowLoading(child,game,nr,srOffset,bridgeOffset,started);
        else WindowState=WindowState.Minimized;
        }catch(Exception ex){status.Text=ex.Message;}}
    [STAThread] static int Main(string[] args){try{
#if !INSTALLER
        if(args.Length>=3&&args[0]=="--launch-test"){var app=new Application();var form=new Tuin();form.campaign.SelectedIndex=int.Parse(args[1]);form.neural.IsChecked=args[2]=="on";form.Play();File.WriteAllText(Path.Combine(Root,"launch-test.txt"),form.status.Text);return 0;}
#endif
#if INSTALLER
        if(args.Length>=4&&args[0]=="--install-test"){Install(args[1],args[2],args[3]=="off"?null:args[3],!Environment.GetCommandLineArgs().Contains("--no-models"),s=>{});return 0;}
#endif
        new Application().Run(new Tuin());return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"Tuin-Quake2-error.txt"),ex.ToString());MessageBox.Show(ex.Message,"Tuin Quake II RTX");return 1;}}
}
