using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace Disk_To_Disk
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        string backuptime;
        string target;
        string folder;
        string docpath = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        List<string> backupfile = new List<string>();
        Timer Timer;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;//WPF가 로드되면 실행
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)//창이 닫쳤을때
        {
            ni.Dispose();//트레이 아이콘 닫기
        }

        private void Backup_Start()
        {
            target = backup_target_path.Text;
            folder = backup_folder_path.Text;
            Timer = new Timer(1000);
            Timer.Elapsed += check_time;
            //백그라운드 동작 시작
            Timer.Start();
            this.Hide();
            Console.WriteLine("backup start");
        }

        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        private void Window_Loaded(object sender, RoutedEventArgs e)//wpf가 로드됐다면
        {
            try
            {
                /*트레이 아이콘 생성*/
                System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
                System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem();
                System.Windows.Forms.MenuItem show = new System.Windows.Forms.MenuItem();
                menu.MenuItems.Add(exit);//아이템 추가
                exit.Index = 0;
                exit.Text = "프로그램 종료";
                exit.Click += delegate (object click, EventArgs eClick)//exit클릭시 실행할 명령
                {
                    ni.Dispose();
                    Application.Current.Shutdown();
                };
                menu.MenuItems.Add(show);//아이템 추가
                show.Index = 1;
                show.Text = "백업 종료및 프로그램 띄우기";
                show.Click += delegate (object click, EventArgs eClick)//show클릭시 실행할 명령
                {
                    try
                    {
                        if (Timer.Enabled)
                        {
                            Timer.Enabled = false;
                            //Console.WriteLine("stop call");
                        }
                        else
                        {
                            MessageBox.Show("백업이 진행중이지 않습니다.", "Disk To Disk", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        this.Show();//메인윈도 띄우기
                    }
                };
                ni.Icon = Properties.Resources.icon;
                ni.Visible = true;
                ni.ContextMenu = menu;
                ni.Text = "DTD Menu";
            }
            catch
            {
                MessageBox.Show("시스템 트레이 아이콘을 불러오지 못했습니다.", "Disk To Disk", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //저장된 설정 파일 불러오기
            try
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Disk To Disk");
                if (reg == null)
                {
                    RegistryKey reg1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Disk To Disk");
                    reg1.SetValue("is_frist", true);
                    reg1.Close();
                    throw new Exception("setting value is not set");
                }
                if (Convert.ToBoolean(reg.GetValue("is_setting_save", false)))
                {
                    backup_target_path.Text = (string)reg.GetValue("target");
                    backup_folder_path.Text = (string)reg.GetValue("folder");
                    is_setting_save.IsChecked = Convert.ToBoolean(reg.GetValue("is_setting_save"));
                    is_startup.IsChecked = Convert.ToBoolean(reg.GetValue("is_startup"));
                    is_faststartup.IsChecked = Convert.ToBoolean(reg.GetValue("is_faststartup"));
                    is_poweroff.IsChecked = Convert.ToBoolean(reg.GetValue("is_poweroff"));
                    H.SelectedIndex = Convert.ToInt32(reg.GetValue("hour"));
                    M.SelectedIndex = Convert.ToInt32(reg.GetValue("minute"));
                }
                reg.Close();
            }
            catch(Exception ex)
            {
                if(ex.Message != "setting value is not set")
                    MessageBox.Show("설정값을 불러오지 못했습니다.\n설정값을 저장하는 과정에서 설정값이 손상된것 같습니다.\n\n자세한 오류 :\n" + ex.ToString(), "Disk To Disk", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //is_faststartup체크시 바로 백업 시작
            if (is_faststartup.IsChecked.Value)
            {
                Backup_Start();
            }
        }

        private void backup_target_sel_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = @"C:\";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)//폴더선택을 했으면
            {
                backup_target_path.Text = dialog.FileName;//textbox텍스트 변경
            }
        }

        private void backup_folder_sel_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = @"C:\";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                backup_folder_path.Text = dialog.FileName;
            }
        }

        private void backup_start_Click(object sender, RoutedEventArgs e)
        {
            backuptime = H.Text + M.Text;//백업시간 형변환
            //백업 대상 폴더와 백업을 저장할 폴더 지정
            target = backup_target_path.Text;
            folder = backup_folder_path.Text;
            if (!Directory.Exists(target) || !Directory.Exists(folder))//만약 올바른 경로가 아니면
            {
                //오류후 리턴
                MessageBox.Show("올바른 경로를 입력해주세요.", "Disk To Disk", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                //백그라운드 동작전 json값 생성 및 저장
                if (is_setting_save.IsChecked.Value)
                {
                    RegistryKey reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Disk To Disk");
                    reg.SetValue("target", target);
                    reg.SetValue("folder", folder);
                    reg.SetValue("is_setting_save", is_setting_save.IsChecked.Value);
                    reg.SetValue("is_startup", is_startup.IsChecked.Value);
                    reg.SetValue("is_faststartup", is_faststartup.IsChecked.Value);
                    reg.SetValue("is_poweroff", is_poweroff.IsChecked.Value);
                    reg.SetValue("hour", H.SelectedIndex);
                    reg.SetValue("minute", M.SelectedIndex);
                    reg.Close();
                } else
                {
                    RegistryKey reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Disk To Disk");
                    reg.SetValue("is_setting_save", false);
                    reg.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("설정값 저장에 실패하였습니다.\n다음에 프로그램을 킬때 설정값이 초기화 될수 있습니다.\n\n자세한 오류 : " + ex.ToString(), "Disk To Disk", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            if (is_startup.IsChecked.Value || is_faststartup.IsChecked.Value)
            {
                try
                {
                    RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    reg.SetValue("Disk To Disk", AppDomain.CurrentDomain.BaseDirectory);
                    reg.Close();
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    reg.DeleteValue("Disk To Disk");
                    reg.Close();
                }
                catch
                {
                }
            }

            //백업 동작 시작
            //TODO : 폴더가 같이 감지되는 경우 수정
            FileSystemWatcher watcher = new FileSystemWatcher()
            {
                Path = backup_target_path.Text,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            watcher.Changed += onChanged;
            watcher.Created += onChanged;
            watcher.Deleted += onChanged;
            watcher.Renamed += onRenamed;
            Backup_Start();
        }

        private bool isDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(@"c:\Temp");

            //detect whether its a directory or file
            return attr.HasFlag(FileAttributes.Directory);
        }

        private void onChanged(object source, FileSystemEventArgs e)
        {
            if (!Directory.Exists(e.FullPath))
            {
                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    Console.WriteLine("\n\nBackupFile Removed!");
                    Console.WriteLine("File Name : {0}", e.Name);
                    Console.WriteLine("FullPath : {0}\n", e.FullPath);
                    backupfile.Remove(e.FullPath);
                }
                else if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)
                {
                    Console.WriteLine("\n\nBackupFile Add!");
                    Console.WriteLine("File Name : {0}", e.Name);
                    Console.WriteLine("FullPath : {0}\n", e.FullPath);
                    backupfile.Add(e.FullPath);
                }
                Console.WriteLine(String.Join(", ", backupfile.ToArray()));
            }
        }
        private void onRenamed(object source, RenamedEventArgs e)
        {
            if (!Directory.Exists(e.FullPath))
            {
                Console.WriteLine("\n\nBackupFile Add!");
                Console.WriteLine("File Name : {0}", e.Name);
                Console.WriteLine("FullPath : {0}\n", e.FullPath);
                Console.WriteLine("BackupFile Removed!");
                Console.WriteLine("File Name : {0}", e.OldName);
                Console.WriteLine("FullPath : {0}\n", e.OldFullPath);
                backupfile.Remove(e.OldFullPath);
                backupfile.Add(e.FullPath);
                Console.WriteLine(String.Join(", ", backupfile.ToArray()));
            }
        }

        private void CopyTree(string source, string dest)
        {
            try
            {
                Console.WriteLine(Path.GetDirectoryName(dest));
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(source, dest, true);//파일 복사
            }
            catch(Exception ex) { Console.WriteLine(ex); }
        }

        private void backupEnd()//백업이 완료했을때 호출되는 함수
        {
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    backupfile = new List<string>();
                    if (is_poweroff.IsChecked.Value)
                    {
                        ProcessStartInfo cmd = new ProcessStartInfo();
                        Process process = new Process();
                        cmd.FileName = @"cmd";
                        cmd.WindowStyle = ProcessWindowStyle.Hidden;
                        cmd.CreateNoWindow = true;
                        cmd.UseShellExecute = false;
                        process.EnableRaisingEvents = false;
                        process.StartInfo = cmd;
                        process.Start();
                        process.StandardInput.Write(@"shutdown /s /t 00" + Environment.NewLine);//컴퓨터 끄기
                        process.StandardInput.Close();
                    }
                }));
            }
            catch { }
        }

        private void check_time(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(backuptime + "0");
            Console.WriteLine(DateTime.Now.ToString("Hmms"));
            if (backuptime + "0" == DateTime.Now.ToString("Hmms"))
            {
                foreach (string s in backupfile)
                {
                    if (Directory.Exists(s))
                    {
                        backupfile.Remove(s);
                    }
                }
                backupfile = backupfile.Distinct().ToList();
                RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Disk To Disk");
                if(reg == null)
                {
                    backupEnd();
                    return;
                }
                if (Convert.ToBoolean(reg.GetValue("is_first",false)))
                {
                    CopyTree(target, folder);
                    Console.WriteLine("back up all");
                    reg.SetValue("is_first", false);
                } else
                {
                    foreach (string s in backupfile)
                    {
                        CopyTree(s, folder + s.Replace(target, ""));
                        Console.WriteLine(s);
                    }
                }
                reg.Close();
                backupEnd();
            }
        }

        private void is_faststartup_Checked(object sender, RoutedEventArgs e)
        {
            is_startup.IsChecked = false;
            is_startup.IsEnabled = false;
        }
        private void is_faststartup_UnChecked(object sender, RoutedEventArgs e)
        {
            is_startup.IsEnabled = true;
        }

        private void backup_target_path_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            backup_target_path.Text = "";
        }

        private void backup_folder_path_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            backup_folder_path.Text = "";
        }
    }
}
