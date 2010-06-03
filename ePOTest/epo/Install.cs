using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using System.Diagnostics;
using PNUnit.Framework;
using System.ServiceProcess;
using White.Core;
using White.Core.UIItems;
using System.Drawing.Imaging;
using System.Drawing;

namespace epo
{
    [TestFixture]
    public class Install
    {       
        /*
         * This method will check for whether the installation folder exists and create
         * a batch file with the name "silent_installer.bat". It will be run to install
         * ePO silently onto the machine. The program will wait for the batch file to
         * finish.
         * 
         * */
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        [Test]
        public void silentInstall()
        {
            string[] testParams = PNUnitServices.Get().GetTestParams();
            //string[] testParams = { "EPOADMIN = admin ", "EPOPASSWORD = epo", "DBSERVER = sql2005sp3", "DBUSERNAME = administrator", "MFSDATABASEINSTANCENAME=Sushanth", "DBDOMAIN = epomfe", "DBPASSWORD = Yv74aL5j", "DBAUTH = 2" };
            Console.WriteLine("testParams is " + testParams);

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            {
                string[] strArray;
                foreach (string strOriginalParam in testParams) //each individual string in the array is of type "textKey=textValue"
                {
                    strArray = strOriginalParam.Split('=');
                    Console.WriteLine("strArray is " + strArray);
                    dictionary.Add(strArray[0].ToString().Trim(), strArray[1].ToString().Trim());
                }
            }

            //Console.WriteLine(" test1 value is " + dictionary["test1"]);



            //Parameters that were passed to this test method:
            //string path = @"C:\EPO 4.6.0 Build 507 IS2010 Package #2 (ENU-EVALUATION-RELEASE-MAIN )\";
            string path = dictionary["EPOPATH"];
            string adminUserName = dictionary["EPOADMIN"];
            string adminPassword = dictionary["EPOPASSWORD"];
            string adminVerifyPassword = adminPassword;
            string databaseServerName = dictionary["DBSERVER"];
            string databasePort = "5500";//We are hardcoding the db port as it will be found dynamically
            string databaseInstance = dictionary["DBINSTANCE"];
            string databaseName = "ePO4_" + System.Environment.MachineName;
            string databaseUserName = dictionary["DBUSERNAME"];
            string databaseDomain = dictionary["DBDOMAIN"];
            string databasePassword = dictionary["DBPASSWORD"];
            string databaseAuth = dictionary["DBAUTH"]; //1 = NT auth, 2 = SQL auth
            string sqlUdpPortIsEnabled = "1"; //1 = enabled, 2 = disabled. We are hardcoding this for now.


            //declarations
            string allParameters = path; //allParameters is the string that would be present in the bat file


            //checking that the installation folder exists
            Assert.IsTrue(System.IO.Directory.Exists(path), "ePO Setup Directory does not exist");

            //creating the script for ePO silent installation
            string silentInstallBatchFile = "silent_installer.bat";

            //combining filename with the path
            path = System.IO.Path.Combine(path, silentInstallBatchFile);

            //creating a blank batch file on the disk
            (System.IO.File.Create(path)).Close();

            //removing last backslash character if it is present
            if (allParameters.EndsWith("\\"))
            {
                allParameters = allParameters.Remove(allParameters.Length - 1);
            }

            //writing into the batch file            
            allParameters = String.Concat("\"" + allParameters + "\"" +

            "\\Setup /qb " +
            "MFSADMINUSERNAME_UE=" + adminUserName + " " +
            "MFSADMINPASSWORD_UE=" + adminPassword + " " +
            "MFSADMINVERIFYPASSWORD_UE=" + adminVerifyPassword + " " +
            "MFSDATABASESERVERNAME=" + databaseServerName + " " +
            "MFSDATABASENAME=" + databaseName + " " +
            "MFSDATABASEUSERNAME_UE=" + databaseUserName + " " +
            "MFSDATABASEPASSWORD_UE=" + databasePassword + " " +
            "MFSDATABASEAUTHENTICATION=" + databaseAuth + " " +
            "SQLUDPPORTISENABLED=" + sqlUdpPortIsEnabled + " ");


            if (databaseAuth == "1")//for NT authentication we need to supply the domain value
            {
                allParameters = String.Concat(allParameters +
                    "MFSDATABASEDOMAIN=" + databaseDomain + " ");
            }

            if ((databaseInstance != null) && (databaseInstance != ""))//add the Instance name only if it is a valid name
            {
                allParameters = String.Concat(allParameters +
                "MFSDATABASEINSTANCENAME=" + databaseInstance + " " +
                "MFSDATABASEPORT=" + databasePort + " ");
            }
            else//if instance name is not specified then hardcode the database port as 1433
            {
                allParameters = String.Concat(allParameters +
                    "MFSDATABASEPORT=" + "1433" + " ");
            }


            allParameters = String.Concat(allParameters +
            "PREVIOUSTOMCATSERVICENAME=MCAFEETOMCATSRV200 IGNOREPROPINI=1");

            System.IO.StreamWriter sw = new System.IO.StreamWriter(path);

            Console.WriteLine("entry into batch is.." + allParameters);
            sw.WriteLine(allParameters);
            //sw.WriteLine("ping -n 10 127.0.0.1");
            sw.Close();



            //running the silent installation batch script
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            Console.WriteLine("executing batch");
            proc.Start();
            Console.WriteLine("batch execution in progress. Waiting for exit..");
            Assert.IsTrue(proc.WaitForExit(1 * 60 * 60 * 1000)); //failing the test if installer takes more than 1 hour
            Console.WriteLine("batch execution done");
            //int exitCode = proc.ExitCode;
            proc.Close();




            //checking the log files in the temp\mcafeelogs folder if it exists
            string tempFolder = System.IO.Path.GetTempPath();
            tempFolder = tempFolder + @"McAfeeLogs\EPO460-Troubleshoot";
            Assert.IsFalse(System.IO.Directory.Exists(tempFolder), "The error log files were found."); //fail the test case if the directory exists



            //checking whether the ePO services are running
            try
            {
                ServiceController sc = new ServiceController();

                sc.ServiceName = "MCAFEETOMCATSRV250";
                Console.WriteLine("The service " + sc.DisplayName + " was found to be in state: " + sc.Status.ToString());
                Assert.IsTrue((sc.Status.ToString().ToLower() == "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

                sc.ServiceName = "MCAFEEEVENTPARSERSRV";
                Console.WriteLine("The service " + sc.DisplayName + " was found to be in state: " + sc.Status.ToString());
                Assert.IsTrue((sc.Status.ToString().ToLower() == "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

                sc.ServiceName = "MCAFEEAPACHESRV";
                Console.WriteLine("The service " + sc.DisplayName + " was found to be in state: " + sc.Status.ToString());
                Assert.IsTrue((sc.Status.ToString().ToLower() == "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

            }
            catch (Exception e)
            {
                Assert.IsTrue(false, "Exception occured while checking the services. " + e.Message);
            }


            //performing an ePO console login to check that it was installed correctly

        }

        [Test]
        public void guiInstall()
        {
            string[] testParams = PNUnitServices.Get().GetTestParams();
            //string[] testParams = { "EPOADMIN = admin ", "EPOPASSWORD = epo", "DBSERVER = DEPSERVER", "DBUSERNAME = administrator", "MFSDATABASEINSTANCENAME=Sushanth", "DBDOMAIN = EPOMFE", "DBPASSWORD = Yv74aL5j", "DBAUTH = 1" };

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            {
                string[] strArray;
                foreach (string strOriginalParam in testParams) //each individual string in the array is of type "textKey=textValue"
                {
                    strArray = strOriginalParam.Split('=');
                    Console.WriteLine("strArray is " + strArray);
                    dictionary.Add(strArray[0].ToString().Trim(), strArray[1].ToString().Trim());
                }
            }

            //Console.WriteLine(" test1 value is " + dictionary["test1"]);
            //Parameters that were passed to this test method:
            //string path = @"C:\EPO 4.6.0 Build 507 IS2010 Package #2 (ENU-EVALUATION-RELEASE-MAIN )\";
            string path = dictionary["EPOPATH"];
            string adminUserName = dictionary["EPOADMIN"];
            string adminPassword = dictionary["EPOPASSWORD"];
            string adminVerifyPassword = adminPassword;            
            string databasePort = "5500";//We are hardcoding the db port as it will be found dynamically
            string databaseInstance = dictionary["DBINSTANCE"];
            string databaseServerName = dictionary["DBSERVER"];
            string databaseName = "ePO4_" + System.Environment.MachineName;
            string databaseUserName = dictionary["DBUSERNAME"];
            string databaseDomain = dictionary["DBDOMAIN"].Split(new char[] { '.' })[0].ToUpper();
            string databasePassword = dictionary["DBPASSWORD"];
            string databaseAuth = dictionary["DBAUTH"]; //1 = NT auth, 2 = SQL auth
            string sqlUdpPortIsEnabled = "1"; //1 = enabled, 2 = disabled. We are hardcoding this for now.

            Assert.IsTrue(System.IO.Directory.Exists(path), "ePO Setup Directory does not exist");

            //Application application;
            Application application = Application.Launch(System.IO.Path.Combine(path,"setup.exe"));

            

            System.Threading.Thread.Sleep(30000); //Wait 30 seconds for installer UI to come up

            Process[] plist = Process.GetProcessesByName("msiexec");
            Process installer = null;

            foreach (Process p in plist)
            {
                if (p.MainWindowTitle.IndexOf("ePolicy") > -1)
                {
                    installer = p;
                }
            }

            Assert.NotNull(installer, "Installer process did not start, test failed");
            application = Application.Attach(installer);

            White.Core.UIItems.WindowItems.Win32Window window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);

            ////Click first next button
            Button next = window.Get<Button>("Next >");
            next.Click();

            ////Setup type
            //window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            //White.Core.UIItems.Label setupType = window.Get<Label>("Setup Type");
            //next = window.Get<Button>("Next >");
            //next.Click();

            //System.Threading.Thread.Sleep(5000);

            ////Choose Database Option
            //window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            //White.Core.UIItems.Label databaseOpt = window.Get<Label>("Choose Database Option");
            //next = window.Get<Button>("Next >");
            //next.Click();

            //Pre-requisites
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label softLabel = window.Get<Label>("Click Next to begin installation of the following software:");

            if (softLabel != null)
            {
                next = window.Get<Button>("Next >");
                next.Click();
                System.Threading.Thread.Sleep(60000); // wait one minute for installation
            }
            else
            {
                System.Threading.Thread.Sleep(5000);
            }

            ////Verify that destination selection screen shows up
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label destLabel = window.Get<Label>("Click Next to install to this folder, or click Change to install to a different folder.");
            Assert.NotNull(destLabel, "Destination Selection screen not present, test failed");

            ////Click next to proceed
            next = window.Get<Button>("Next >");
            next.Click();



            System.Threading.Thread.Sleep(3000);

            //Verify that database information screen shows up
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label databaseLabel = window.Get<Label>("Database Information");
            Assert.NotNull(databaseLabel, "Database selection screen not present, test failed");

            //Input database information                
            //Database server
            White.Core.UIItems.ListBoxItems.Win32ComboBox databaseSelect = (White.Core.UIItems.ListBoxItems.Win32ComboBox)window.Get<White.Core.UIItems.ListBoxItems.Win32ComboBox>("Database Server:");

            if (databaseInstance == null || databaseInstance.Length == 0)
            {
                databaseSelect.Select(databaseServerName);
            }
            else
            {
                databaseSelect.Select(databaseServerName + "\\" + databaseInstance);
            }

            //Database authentication type
            White.Core.UIItems.RadioButton windowsAuth = window.Get<White.Core.UIItems.RadioButton>("Windows authentication");
            White.Core.UIItems.RadioButton sqlAuth = window.Get<White.Core.UIItems.RadioButton>("SQL authentication");

            if (databaseAuth.Equals("1"))
            {
                windowsAuth.Click();
                White.Core.UIItems.ListBoxItems.Win32ComboBox domain = window.Get<White.Core.UIItems.ListBoxItems.Win32ComboBox>("Domain:");
                domain.Select(databaseDomain);
            }
            else
            {
                sqlAuth.Click();
            }

            //Enter db auth user and pass
            White.Core.UIItems.TextBox username = window.Get<White.Core.UIItems.TextBox>("Username:");
            White.Core.UIItems.TextBox password = window.Get<White.Core.UIItems.TextBox>("Password:");

            username.SetValue(databaseUserName);
            password.SetValue(databasePassword);

            //Hit next
            next = window.Get<Button>("Next >");
            next.Click();

            window.WaitWhileBusy();

            //Wait 20 seconds for SQL server validation
            System.Threading.Thread.Sleep(20000);

            //Check if next page loaded
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label portLabel = window.Get<White.Core.UIItems.Label>("HTTP Port Information");
            Assert.NotNull(portLabel, "Ports screen not visible, failing test");

            next = window.Get<White.Core.UIItems.Button>("Next >");
            next.Click();

            //Wait 30 seconds for next screen
            System.Threading.Thread.Sleep(30000);

            //Assert administrator page
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label adminLabel = window.Get<White.Core.UIItems.Label>("Global Administrator Information");
            Assert.NotNull(adminLabel, "Global administrator Information page did not load");

            //Input username and password
            White.Core.UIItems.TextBox ePOUser = window.Get<White.Core.UIItems.TextBox>("Username:");
            White.Core.UIItems.TextBox ePOPassword = window.Get<White.Core.UIItems.TextBox>("Password:");
            White.Core.UIItems.TextBox ePORepeatPassword = window.Get<White.Core.UIItems.TextBox>("Verify Password:");

            ePOUser.SetValue(adminUserName);
            ePOPassword.SetValue(adminPassword);
            ePORepeatPassword.SetValue(adminPassword);

            next = window.Get<White.Core.UIItems.Button>("Next >");
            next.Click();

            System.Threading.Thread.Sleep(10000);

            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Button install = window.Get<White.Core.UIItems.Button>("Install");
            install.Click();

            //Check progress bar in intervals of 30 seconds for upto an hour
            // If it reaches 100% or disappears check for success or failure message


            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);                        
            White.Core.UIItems.ProgressBar pb;            

            for (int i = 0; i < 120; i++)
            {
                pb = window.Get<White.Core.UIItems.ProgressBar>("The program features you selected are being uninstalled.");

                if (pb != null)
                {
                    System.Threading.Thread.Sleep(30000); // Check every 30 seconds
                }
            }

            System.Threading.Thread.Sleep(10000); //Wait further 10 seconds to satisfy edge case of last check falling just after progress bar disappears

            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label passMessage = window.Get<White.Core.UIItems.Label>("The InstallShield Wizard has successfully installed McAfee ePolicy Orchestrator. Click Finish to exit the wizard.");

            if (passMessage != null)
            {
                Assert.IsTrue(true, "Install succeeded");
            }
            else
            {
                Assert.IsTrue(false, "Install failed");
            }                                   
        }

        [Test]
        public void guiInstall2k3()
        {
            string[] testParams = PNUnitServices.Get().GetTestParams();
            //string[] testParams = { "EPOADMIN = admin ", "EPOPASSWORD = epo", "DBSERVER = DEPSERVER", "DBUSERNAME = administrator", "MFSDATABASEINSTANCENAME=Sushanth", "DBDOMAIN = EPOMFE", "DBPASSWORD = Yv74aL5j", "DBAUTH = 1" };

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            {
                string[] strArray;
                foreach (string strOriginalParam in testParams) //each individual string in the array is of type "textKey=textValue"
                {
                    strArray = strOriginalParam.Split('=');
                    Console.WriteLine("strArray is " + strArray);
                    dictionary.Add(strArray[0].ToString().Trim(), strArray[1].ToString().Trim());
                }
            }

            //Console.WriteLine(" test1 value is " + dictionary["test1"]);
            //Parameters that were passed to this test method:
            //string path = @"C:\EPO 4.6.0 Build 507 IS2010 Package #2 (ENU-EVALUATION-RELEASE-MAIN )\";
            string path = dictionary["EPOPATH"];
            string adminUserName = dictionary["EPOADMIN"];
            string adminPassword = dictionary["EPOPASSWORD"];
            string adminVerifyPassword = adminPassword;
            string databasePort = "5500";//We are hardcoding the db port as it will be found dynamically
            string databaseInstance = dictionary["DBINSTANCE"];
            string databaseServerName = dictionary["DBSERVER"];
            string databaseName = "ePO4_" + System.Environment.MachineName;
            string databaseUserName = dictionary["DBUSERNAME"];
            string databaseDomain = dictionary["DBDOMAIN"].Split(new char[] { '.' })[0].ToUpper();
            string databasePassword = dictionary["DBPASSWORD"];
            string databaseAuth = dictionary["DBAUTH"]; //1 = NT auth, 2 = SQL auth
            string sqlUdpPortIsEnabled = "1"; //1 = enabled, 2 = disabled. We are hardcoding this for now.

            Assert.IsTrue(System.IO.Directory.Exists(path), "ePO Setup Directory does not exist");

            //Application application;
            Application application = Application.Launch(System.IO.Path.Combine(path, "setup.exe"));

            

            System.Threading.Thread.Sleep(30000); //Wait 15 seconds for installer UI to come up

            Process[] plist = Process.GetProcessesByName("msiexec");
            Process installer = null;

            foreach (Process p in plist)
            {
                if (p.MainWindowTitle.IndexOf("ePolicy") > -1)
                {
                    installer = p;
                }
            }

            Assert.NotNull(installer, "Installer process did not start, test failed");
            application = Application.Attach(installer);

            White.Core.UIItems.WindowItems.Win32Window window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);

            ////Click first next button
            Button next = window.Get<Button>("Next >");
            next.Click();

            System.Threading.Thread.Sleep(5000);
            //Setup type
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label setupType = window.Get<Label>("Setup Type");
            next = window.Get<Button>("Next >");
            next.Click();

            System.Threading.Thread.Sleep(5000);

            ////Choose Database Option
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label databaseOpt = window.Get<Label>("Choose Database Option");
            next = window.Get<Button>("Next >");
            next.Click();

            System.Threading.Thread.Sleep(5000);
            //Pre-requisites
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label softLabel = window.Get<Label>("Click Next to begin installation of the following software:");

            if (softLabel != null)
            {
                next = window.Get<Button>("Next >");
                next.Click();
                System.Threading.Thread.Sleep(60000); // wait one minute for installation
            }
            else
            {
                System.Threading.Thread.Sleep(5000);
            }

            ////Verify that destination selection screen shows up
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label destLabel = window.Get<Label>("Click Next to install to this folder, or click Change to install to a different folder.");
            Assert.NotNull(destLabel, "Destination Selection screen not present, test failed");

            ////Click next to proceed
            next = window.Get<Button>("Next >");
            next.Click();



            System.Threading.Thread.Sleep(3000);

            //Wait 3 minutes for database information screen to show up checking every 10 seconds
            White.Core.UIItems.Label databaseLabel;
            bool databaseLoad = false;
            for (int i = 0; i < 18; i++)
            {
                window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
                databaseLabel = window.Get<Label>("Database Information");
                if (databaseLabel != null)
                {
                    databaseLoad = true;
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(10000);
                }
            }

            Assert.IsTrue(databaseLoad, "Database selection screen not present, test failed");
            

            //Input database information                
            //Database server
            White.Core.UIItems.ListBoxItems.Win32ComboBox databaseSelect = (White.Core.UIItems.ListBoxItems.Win32ComboBox)window.Get<White.Core.UIItems.ListBoxItems.Win32ComboBox>("Database Server:");

            if (databaseInstance == null || databaseInstance.Length == 0)
            {
                databaseSelect.Select(databaseServerName);
            }
            else
            {
                databaseSelect.Select(databaseServerName + "\\" + databaseInstance);
            }

            //Database authentication type
            White.Core.UIItems.RadioButton windowsAuth = window.Get<White.Core.UIItems.RadioButton>("Windows authentication");
            White.Core.UIItems.RadioButton sqlAuth = window.Get<White.Core.UIItems.RadioButton>("SQL authentication");

            if (databaseAuth.Equals("1"))
            {
                windowsAuth.Click();
                White.Core.UIItems.ListBoxItems.Win32ComboBox domain = window.Get<White.Core.UIItems.ListBoxItems.Win32ComboBox>("Domain:");
                domain.Select(databaseDomain);
            }
            else
            {
                sqlAuth.Click();
            }

            //Enter db auth user and pass
            White.Core.UIItems.TextBox username = window.Get<White.Core.UIItems.TextBox>("Username:");
            White.Core.UIItems.TextBox password = window.Get<White.Core.UIItems.TextBox>("Password:");

            username.SetValue(databaseUserName);
            password.SetValue(databasePassword);

            //Hit next
            next = window.Get<Button>("Next >");
            next.Click();

            window.WaitWhileBusy();

            //Wait 20 seconds for SQL server validation
            System.Threading.Thread.Sleep(20000);

            //Check if next page loaded
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label portLabel = window.Get<White.Core.UIItems.Label>("HTTP Port Information");
            Assert.NotNull(portLabel, "Ports screen not visible, failing test");

            next = window.Get<White.Core.UIItems.Button>("Next >");
            next.Click();

            //Wait 30 seconds for next screen
            System.Threading.Thread.Sleep(30000);

            //Assert administrator page
            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label adminLabel = window.Get<White.Core.UIItems.Label>("Global Administrator Information");
            Assert.NotNull(adminLabel, "Global administrator Information page did not load");

            //Input username and password
            White.Core.UIItems.TextBox ePOUser = window.Get<White.Core.UIItems.TextBox>("Username:");
            White.Core.UIItems.TextBox ePOPassword = window.Get<White.Core.UIItems.TextBox>("Password:");
            White.Core.UIItems.TextBox ePORepeatPassword = window.Get<White.Core.UIItems.TextBox>("Verify Password:");

            ePOUser.SetValue(adminUserName);
            ePOPassword.SetValue(adminPassword);
            ePORepeatPassword.SetValue(adminPassword);

            next = window.Get<White.Core.UIItems.Button>("Next >");
            next.Click();

            System.Threading.Thread.Sleep(10000);

            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Button install = window.Get<White.Core.UIItems.Button>("Install");
            install.Click();

            //Check progress bar in intervals of 30 seconds for upto an hour
            // If it reaches 100% or disappears check for success or failure message


            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.ProgressBar pb;

            for (int i = 0; i < 120; i++)
            {
                pb = window.Get<White.Core.UIItems.ProgressBar>("The program features you selected are being uninstalled.");

                if (pb != null)
                {
                    System.Threading.Thread.Sleep(30000); // Check every 30 seconds
                }
            }

            System.Threading.Thread.Sleep(10000); //Wait further 10 seconds to satisfy edge case of last check falling just after progress bar disappears

            window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
            White.Core.UIItems.Label passMessage = window.Get<White.Core.UIItems.Label>("The InstallShield Wizard has successfully installed McAfee ePolicy Orchestrator. Click Finish to exit the wizard.");

            if (passMessage != null)
            {
                Assert.IsTrue(true, "Install succeeded");
                //window = (White.Core.UIItems.WindowItems.Win32Window)application.GetWindow("McAfee ePolicy Orchestrator - InstallShield Wizard", White.Core.Factory.InitializeOption.NoCache);
                //White.Core.UIItems.Button finish = window.Get<White.Core.UIItems.Button>("Finish");
                //finish.Click();
            }
            else
            {
                Assert.IsTrue(false, "Install failed");
            }
        }

        [TearDown]
        public void cleanup()
        {
            //Initialize test artifact folder
            string logDirName = @"c:\" + PNUnitServices.Get().GetTestName();

            Directory.CreateDirectory(logDirName);

            string tempPath = Environment.GetEnvironmentVariable("TEMP");
            //copy log files
            Install.CopyFilesRecursively(new DirectoryInfo((Path.Combine(tempPath, "McAfeeLogs"))), new DirectoryInfo(logDirName));


            //Take Screenshot
            int screenWidth = System.Windows.Forms.Screen.GetBounds(new Point(0, 0)).Width;
            int screenHeight = System.Windows.Forms.Screen.GetBounds(new Point(0, 0)).Height;
            System.Drawing.Bitmap bmpScreenShot = new Bitmap(screenWidth, screenHeight);
            System.Drawing.Graphics gfx = Graphics.FromImage((System.Drawing.Image)bmpScreenShot);
            gfx.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
            bmpScreenShot.Save(Path.Combine(logDirName, PNUnitServices.Get().GetTestName() + ".jpg"), ImageFormat.Jpeg);

            System.Threading.Thread.Sleep(3000);
        }
    }
}

    
    
    

