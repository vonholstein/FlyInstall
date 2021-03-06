using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using Vestris.VMWareLib;
using AppUtil;
using VimApi;

namespace VirtualLib
{
    public class CommonInfo
    {
        public static string getProductId(string template)
        {
            return "DYPY4-JYR3F-9THQW-G862M-VYDJG";
        }
    }
    public class VMHost
    {
        //list of templates available on this host can be incorporated here

        private VimService _service;       
        private AppUtil.AppUtil cb = null;        
        private ServiceContent _sic;
        string[] connectString;
        private string dataCenter;
        private string hostName;
        private string url;
        private string server;
        private string userName;
        private string password;        

        public VMWareVirtualHost vixhost;

        public string getUrl()
        {
            return this.url;
        }
        

        public string getUserName()
        {
            return this.userName;
        }

        public string getPassword()
        {
            return this.password;
        }

        public string getDataCenter()
        {
            return this.dataCenter;
        }

        public VMHost(string url, string server, string portNumber, string userName, string password, string dataCenter, string hostName)
        {            
            string tempConnectString;

            tempConnectString = " --url " + url + " --server " + server + " --portnumber " + portNumber + " --username " + userName + " --password " + password + " --ignorecert";
            connectString = tempConnectString.Trim().Split(new char[] { ' ' });
            this.dataCenter = dataCenter;
            this.hostName = hostName;
            this.url = url;
            this.server = server;
            this.userName = userName;
            this.password = password;
            vixhost = new VMWareVirtualHost();
            vixhost.ConnectToVMWareVIServer("localhost:4443",this.userName,this.password);                    
        }

        //Destructor
        ~VMHost()
        {
            if (this.vixhost.IsConnected == true)
            {
                this.vixhost.Disconnect();                
            }
        }


        // use templateExists in deploy to check for existence of guest.templateName
        public bool templateExists(VM guest)            
        {
            return true;
        }

        //public VMWareVirtualHost getHandle()
        //{
        //    return handle;
        //}

        public bool deploy(VM guest)
        {
            cb = AppUtil.AppUtil.initialize("VMDeploy", this.connectString);
            cb.connect();            
            /************Start Deploy Code*****************/
            _service = cb.getConnection()._service;
            _sic = cb.getConnection()._sic;
            
            // ManagedObjectReferences
            ManagedObjectReference datacenterRef;
            ManagedObjectReference vmFolderRef;
            ManagedObjectReference vmRef; 
            ManagedObjectReference hfmor; // hostFolder reference
            ArrayList crmors; // ArrayList of ComputeResource references
            ManagedObjectReference hostmor;
            ManagedObjectReference crmor = null; // ComputeResource reference
            ManagedObjectReference resourcePool;

            // Find the Datacenter reference by using findByInventoryPath().
            datacenterRef = _service.FindByInventoryPath(_sic.searchIndex, this.dataCenter);

            if (datacenterRef == null)
            {
                Console.WriteLine("The specified datacenter is not found");
                return false;
            }

            // Find the virtual machine folder for this datacenter.
            vmFolderRef = (ManagedObjectReference)cb.getServiceUtil().GetMoRefProp(datacenterRef, "vmFolder");
            if (vmFolderRef == null)
            {
                Console.WriteLine("The virtual machine is not found");
                return false;
            }

            vmRef = _service.FindByInventoryPath(_sic.searchIndex, guest.getVmPath());
            if (vmRef == null)
            {
                Console.WriteLine("The virtual machine is not found");
                return false;
            }

            // Code for obtaining managed object reference to resource root

            hfmor = cb.getServiceUtil().GetMoRefProp(datacenterRef, "hostFolder");   
            crmors = cb.getServiceUtil().GetDecendentMoRefs(hfmor, "ComputeResource", null);         

            if (this.hostName != null)
            {
                hostmor = cb.getServiceUtil().GetDecendentMoRef(hfmor, "HostSystem", this.hostName);
                if (hostmor == null)
                {
                    Console.WriteLine("Host " + this.hostName + " not found");
                    return false;
                }
            }
            else
            {
                hostmor = cb.getServiceUtil().GetFirstDecendentMoRef(datacenterRef, "HostSystem");
            }
            
            hostName = (String)cb.getServiceUtil().GetDynamicProperty(hostmor, "name");
            for (int i = 0; i < crmors.Count; i++)
            {

                ManagedObjectReference[] hrmors
                   = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty((ManagedObjectReference)crmors[i], "host");
                if (hrmors != null && hrmors.Length > 0)
                {
                    for (int j = 0; j < hrmors.Length; j++)
                    {
                        String hname = (String)cb.getServiceUtil().GetDynamicProperty(hrmors[j], "name");
                        if (hname.Equals(this.hostName))
                        {
                            crmor = (ManagedObjectReference)crmors[i];
                            i = crmors.Count + 1;
                            j = hrmors.Length + 1;
                        }

                    }
                }
            }

            if (crmor == null)
            {
                Console.WriteLine("No Compute Resource Found On Specified Host");
                return false;
            }
            resourcePool = cb.getServiceUtil().GetMoRefProp(crmor, "resourcePool");

            /***********************************/
            /*Setup cloning sysprep preferences*/
            /***********************************/

            VirtualMachineCloneSpec cloneSpec = new VirtualMachineCloneSpec();
            VirtualMachineRelocateSpec relocSpec = new VirtualMachineRelocateSpec();

            // Set resource pool for relocspec(compulsory since deploying template)
            relocSpec.pool = resourcePool;

            cloneSpec.location = relocSpec;
            cloneSpec.powerOn = true; //Specifies whether or not the new VirtualMachine should be powered on after creation. As part of a customization, this flag is normally set to true, since the first power-on operation completes the customization process. This flag is ignored if a template is being created. 
            cloneSpec.template = false; //Specifies whether or not the new virtual machine should be marked as a template. 

            // Customization
            CustomizationSpec custSpec = new CustomizationSpec();

            // Make NIC settings
            CustomizationAdapterMapping[] custAdapter = new CustomizationAdapterMapping[1];
            custAdapter[0] = new CustomizationAdapterMapping();
            CustomizationIPSettings custIPSettings = new CustomizationIPSettings();
            CustomizationDhcpIpGenerator custDhcp = new CustomizationDhcpIpGenerator();
            custIPSettings.ip = custDhcp;
            custAdapter[0].adapter = custIPSettings;
            // Set NIC settings
            custSpec.nicSettingMap = custAdapter;

            // Make DNS entry
            CustomizationGlobalIPSettings custIP = new CustomizationGlobalIPSettings();
            custIP.dnsServerList = guest.getDnsList(); ;
            // Set DNS entry
            custSpec.globalIPSettings = custIP;
            
            // Make Sysprep entries
            CustomizationSysprep custPrep = new CustomizationSysprep(); //An object representation of a Windows sysprep.inf answer file. The sysprep type encloses all the individual keys listed in a sysprep.inf file

            // Make guiRunOnce entries(to change autologon settings to login to domain)

            //CustomizationGuiRunOnce custGuiRunOnce = new CustomizationGuiRunOnce();

            //string deleteKey = "reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /f";
            //string addKey = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /t REG_SZ /d " + this.joinDomain;
            //string shutdownKey = "shutdown -r -t 00 -c \"Rebooting computer\"";

            //custGuiRunOnce.commandList = new string[] { deleteKey, addKey, shutdownKey };
        
            // Set guiRunOnce
            //custPrep.guiRunOnce = custGuiRunOnce;

            // Make guiUnattended settings
            CustomizationGuiUnattended custGui = new CustomizationGuiUnattended(); //The GuiUnattended type maps to the GuiUnattended key in the sysprep.inf answer file
            //The GuiUnattended type maps to the GuiUnattended key in the sysprep.inf answer file            

            if (Int32.Parse(guest.getAutoLogonCount()) == 0)
            {
                custGui.autoLogon = false;
            }
            else
            {
                custGui.autoLogon = true;
                custGui.autoLogonCount = Int16.Parse(guest.getAutoLogonCount()); //If the AutoLogon flag is set, then the AutoLogonCount property specifies the number of times the machine should automatically log on as Administrator
            }
                         
            
            CustomizationPassword custWorkPass = new CustomizationPassword();

            if (guest.getWorkGroupPassword() != null)
            {
                custWorkPass.plainText = true; //Flag to specify whether or not the password is in plain text, rather than encrypted. 
                custWorkPass.value = guest.getWorkGroupPassword();
                custGui.password = custWorkPass;
            }

            custGui.timeZone = 190; //IST The time zone for the new virtual machine. Numbers correspond to time zones listed in sysprep documentation at  in Microsoft Technet. Taken from unattend.txt
            
            // Set guiUnattend settings
            custPrep.guiUnattended = custGui;

            // Make identification settings
            CustomizationIdentification custId = new CustomizationIdentification();
            custId.domainAdmin = guest.getDomainAdmin();
            CustomizationPassword custPass = new CustomizationPassword();
            custPass.plainText = true; //Flag to specify whether or not the password is in plain text, rather than encrypted. 
            custPass.value = guest.getDomainPassword();
            custId.domainAdminPassword = custPass;
            custId.joinDomain = guest.getJoinDomain();
            // Set identification settings
            custPrep.identification = custId;

            // If 2003 add licenseFilePrintData settings
            if (guest is Win2003VM)
            {
                CustomizationLicenseFilePrintData clfpd = new CustomizationLicenseFilePrintData();
                clfpd.autoMode = CustomizationLicenseDataMode.perServer;
                clfpd.autoUsers = 5;
                clfpd.autoUsersSpecified = true;
                custPrep.licenseFilePrintData = clfpd;
            }            

            // Make userData settings
            CustomizationUserData custUserData = new CustomizationUserData();
            CustomizationFixedName custName = new CustomizationFixedName();
            custName.name = guest.getName();
            custUserData.computerName = custName;
            custUserData.fullName = "ePO";
            custUserData.orgName = "McAfee";

            if (guest.getProductId() != null)
            {
                custUserData.productId = guest.getProductId();
            }

            // Set userData settings
            custPrep.userData = custUserData;

            // Set sysprep
            custSpec.identity = custPrep;

            // clonespec customization
            cloneSpec.customization = custSpec;

            // clone power on
            cloneSpec.powerOn = true;

            String clonedName = guest.getName();
            Console.WriteLine("Launching clone task to create a clone: " + clonedName);

            try
            {
                ManagedObjectReference cloneTask
                   = _service.CloneVM_Task(vmRef, vmFolderRef, clonedName, cloneSpec);
                String status = cb.getServiceUtil().WaitForTask(cloneTask);
                if (status.Equals("failure"))
                {
                    Console.WriteLine("Failure -: Virtual Machine cannot be cloned");
                    return false;
                }
                if (status.Equals("sucess"))
                {
                    Console.WriteLine("Virtual Machine Cloned  successfully.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Virtual Machine Cloned cannot be cloned");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;

            }
            finally
            {
                cb.disConnect();
            }
            /************End Deploy Code*******************/
            
            return true;
        }

        public bool delete(VM vmRef)
        {
            cb = AppUtil.AppUtil.initialize("VMDelete", this.connectString);
            cb.connect();

            try
            {
                String errmsg = "";

                ManagedObjectReference memor
                   = cb.getServiceUtil().GetDecendentMoRef(null, "ManagedEntity", vmRef.getName());
                if (memor == null)
                {
                    errmsg = "Unable to find a Managed Entity named : " + vmRef.getName()
                           + " in Inventory";
                    Console.WriteLine(errmsg);
                    return false;
                }

                ManagedObjectReference taskmor
                   = cb.getConnection()._service.Destroy_Task(memor);

                // If we get a valid task reference, monitor the task for success or failure
                // and report task completion or failure.
                if (taskmor != null)
                {
                    Object[] result =
                    cb.getServiceUtil().WaitForValues(
                       taskmor, new String[] { "info.state", "info.error" },
                       new String[] { "state" }, // info has a property - 
                        //state for state of the task
                       new Object[][] { new Object[] { 
                     TaskInfoState.success, TaskInfoState.error } 
                  }
                    );

                    // Wait till the task completes.
                    if (result[0].Equals(TaskInfoState.success))
                    {
                        //log.LogLine(cb.getAppName() + " : Successful delete of Managed Entity : "
                        //          + vmRef.getName());
                    }
                    else
                    {
                        //log.LogLine(cb.getAppName() + " : Failed delete of Managed Entity : "
                        //          + vmRef.getName());
                        if (result.Length == 2 && result[1] != null)
                        {
                            if (result[1].GetType().Equals("MethodFault"))
                            {
                                cb.getUtil().LogException((Exception)result[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                cb.getUtil().LogException(e);
                //log.LogLine(cb.getAppName() + " : Failed delete of Managed Entity : "
                //          + getMeName());
                throw e;
            }
            finally
            {
                cb.disConnect();
            }
            return true;
        }
    } 

    
    public abstract class VM
    {        
        //If systemName is given as a parameter then use that otherwise autogenerate
        protected VMHost hostRef = null;
        protected string templateName;
        protected string datacenterName;
        protected string[] dnsList;
        protected string workGroupPassword;
        protected string domainAdmin;
        protected string domainPassword;
        protected string joinDomain;
        protected string name; //Machine name
        protected string productId;
        protected string autoLogonCount;
        protected string cloneName;
        protected string vmPath;
        protected string[] guiRunOnce;
        protected string storageLocation;
        protected string testname;

        public VM(VMHost hostRef)
        {
            this.hostRef = hostRef;
            
        }

        public string getTestName()
        {
            return this.testname;
        }

        public void setTestName(string testname)
        {
            this.testname = testname;
        }

        public string getName()
        {
            return this.name;
        }

        public void setName(string name)
        {
            this.name = name;
            //this.storageLocation = "[storage]/" + this.name + "/" + this.name + ".vmx";
            this.storageLocation = "[LocalDataStore] " + this.name + "/" + this.name + ".vmx";
        }

        public string getProductId()
        {
            return this.productId;
        }

        public string[] getDnsList()
        {
            return this.dnsList;
        }

        public string getWorkGroupPassword()
        {
            return this.workGroupPassword;
        }

        public string getDomainAdmin()
        {
            return this.domainAdmin;
        }

        public void setDomainAdmin(string domainAdmin)
        {
            this.domainAdmin = domainAdmin;
        }        

        public string getDomainPassword()
        {
            return this.domainPassword;
        }

        public void setDomainPassword(string domainPassword)
        {
            this.domainPassword = domainPassword;
        }

        public string getJoinDomain()
        {
            return this.joinDomain;
        }

        public void setJoinDomain(string joinDomain)
        {
            this.joinDomain = joinDomain;
        }

        public string getAutoLogonCount()
        {
            return this.autoLogonCount;
        }

        public string getCloneName()
        {
            return this.cloneName;
        }

        public string getVmPath()
        {
            return this.vmPath;
        }

        /* given a path to a storage location the following function loads the VM */

        public void defineSysprepParameters(string templateName, string systemName, string[] dnsList, string workGroupPassword, string domainAdmin, string domainPassword, string joinDomain, string productId)
        {
            this.templateName = templateName;

            if (systemName != null)
            {
                this.name = systemName;
                this.cloneName = systemName;
            }
            else
            {
                this.name = this.cloneName = DateTime.Now.Second.ToString() + templateName;
            }
            this.dnsList = dnsList;
            this.workGroupPassword = workGroupPassword;
            this.domainAdmin = domainAdmin;
            this.domainPassword = domainPassword;
            this.joinDomain = joinDomain;
            this.productId = productId;
            this.vmPath = "/" + this.hostRef.getDataCenter() +"/vm/" + this.templateName;
            this.storageLocation = "[LocalDataStore] " + this.name + "/" + this.name + ".vmx";
            
        }

        public virtual void stage()
        {
            // Create registry strings to:
            // 1. Setup autologon settings to domain
            // 2. Startup agent on reboot
            // Add shutdown command
            // Create batch file with the strings
            // Copy file to vm
            // Execute file
            
            System.IO.FileInfo fi;
            string fileName = new Random().Next(100000000, 999999999).ToString() + ".bat";

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                if (virtualMachine.IsRunning)
                {
                    virtualMachine.WaitForToolsInGuest();
                    //virtualMachine.LoginInGuest(this.getJoinDomain().Split(new char[] { '.' })[0] + "\\" + this.getDomainAdmin(), this.getDomainPassword());
                    virtualMachine.LoginInGuest(this.joinDomain + "\\" + this.domainAdmin, this.domainPassword);

                    // Build reg file to change autologon settings to domain
                    string deleteKey = "reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /f";
                    string addKey = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /t REG_SZ /d " + this.joinDomain.Split(new char[]{'.'})[0];
                    //string disableScreenSaverKey = "reg add \"HKCU\\Control Panel\\Desktop\" /v \"ScreenSaveActive\" /t REG_SZ /d 0 /f";
                    string disableScreenSaverKey = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\" /v \"ScreenSaveDisable\" /t REG_SZ /d \"reg add \\\"HKCU\\Control Panel\\Desktop\\\" /v \\\"ScreenSaveActive\\\" /t REG_SZ /d 0 /f\"";
                    //string startAgent = "cmd /C \"start c:\\agent\\agent.exe c:\\agent\\agent.conf";
                    string startAgent = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\" /v \"StartAgent\" /t REG_SZ /d \"cmd /C start c:\\agent\\agent.exe c:\\agent\\agent.conf";
                    string sqlAdminAdd = "sqlcmd -S localhost\\SQL2008 -U sa -P sa -Q \"EXEC master..sp_addsrvrolemember @loginame = N'" + this.joinDomain.Split(new char[] { '.' })[0] + "\\" + this.domainAdmin + "', @rolename = N'sysadmin'\"";
                    string shutdownKey = "shutdown -r -t 10 -c \"Rebooting computer\"";

                    //Build a file containing the domain settings
                    fi = new System.IO.FileInfo(@"c:\windows\temp\" + fileName);
                    StreamWriter sw = fi.CreateText();
                    sw.WriteLine(deleteKey);
                    sw.WriteLine(addKey);
                    sw.WriteLine(disableScreenSaverKey);
                    sw.WriteLine(startAgent);
                    sw.WriteLine(sqlAdminAdd);
                    sw.WriteLine(shutdownKey);                       
                    sw.Close();

                    //Copy said file guest
                    virtualMachine.CopyFileFromHostToGuest(@"c:\windows\temp\" + fileName, @"c:\" + fileName);
                    System.Threading.Thread.Sleep(2000); // wait 2 seconds for copy
                    virtualMachine.RunProgramInGuest(@"c:\" + fileName, "");
                    virtualMachine.LogoutFromGuest(); 
                    //virtualMachine.ShutdownGuest();

                    //Wait every two seconds and check power state until machine is powered off
                    /*
                    while (virtualMachine.IsRunning == true)
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                    */

                    //virtualMachine.PowerOn();                                                                            
                }                
            }
            //Wait 20 seconds for reboot
            System.Threading.Thread.Sleep(20000);

            //Creating a connection again for running the batch script, for some reason running the batch within the previous block causes a timeout
            //using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            //{
                //virtualMachine.LoginInGuest(this.joinDomain + "\\" + this.domainAdmin, this.domainPassword);
                //virtualMachine.WaitForToolsInGuest();                
                //System.Threading.Thread.Sleep(15000); // wait 15 seconds for personal settings to apply and agent to start
                //virtualMachine.LogoutFromGuest();
            //}

        }

        public void delete()
        {
            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                virtualMachine.LoginInGuest(this.domainAdmin, this.domainPassword);
                virtualMachine.PowerOff();
                System.Threading.Thread.Sleep(5000);
                hostRef.delete(this);
            }
        }

        public void waitForLogon(int timeout)
        {
            string commandString = @"C:\psloggedon.exe -accepteula -l > user.txt";
            string fileName = this.name + @"\user.txt";
            int count = 0;
            string currentUser = this.getCurrentUser();

            while( (currentUser == null || currentUser.Equals("System")) & count < timeout)
            {
                ++count;
                System.Threading.Thread.Sleep(10000);

                try
                {
                    currentUser = this.getCurrentUser();
                }
                catch (Exception e)
                {
                    //Returned error, inconsequential for now, define various types of exceptions later
                    // Retry
                }
            }
            //Wait 20 seconds and try again to see if system is still logged on
            System.Threading.Thread.Sleep(20000);
            currentUser = this.getCurrentUser();
            while ((currentUser == null || currentUser.Equals("System")) & count < timeout)
            {
                ++count;
                System.Threading.Thread.Sleep(10000);

                try
                {
                    currentUser = this.getCurrentUser();
                }
                catch (Exception e)
                {
                    //Returned error, inconsequential for now, define various types of exceptions later
                    // Retry
                }
            }
            Console.WriteLine("Logon complete with user {0}", currentUser);
        }

        //public void waitForWorkGroupLogon(int timeout)
        //{
        //    waitForLogon(String.Concat(this.name, "\\", this.domainAdmin), timeout);
        //}

        //public void waitForDomainLogon(int timeout)
        //{
        //    waitForLogon(String.Concat(this.joinDomain, "\\", this.domainAdmin), timeout);
        //}

        public string getCurrentUser() 
        {            
            string program = @"c:\psloggedon.exe  -accepteula -l > c:\user.txt";           
            
            string batchFileName = this.name + @"user.bat";
            string batchFilePath = @"f:\autoinstallproject\temp\";
            string localUserFileName = @"f:\autoinstallproject\temp\" + this.name + "user.txt";
            string[] users;

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                if (!virtualMachine.IsRunning)
                {
                    return null;
                }

                try
                {                    
                    virtualMachine.LoginInGuest("administrator", this.workGroupPassword);
                }
                catch (Exception e)
                {
                    return null;
                }                

                try
                {
                    if(!virtualMachine.FileExistsInGuest(@"c:\getuser.bat"))
                    {
                        virtualMachine.CopyFileFromHostToGuest(@"f:\autoinstallproject\psloggedon.exe", @"c:\psloggedon.exe", 10);
                        //Build a batch file containing the psloggedon command
                        System.IO.FileInfo fi = new System.IO.FileInfo(batchFilePath + batchFileName);
                        StreamWriter sw = fi.CreateText();
                        sw.WriteLine(program);
                        sw.Close();
                        virtualMachine.CopyFileFromHostToGuest(batchFilePath + batchFileName, @"c:\getuser.bat");                
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }


                try
                {
                    VMWareVirtualMachine.Process vp = virtualMachine.RunProgramInGuest(@"c:\getuser.bat");
                    if (vp.ExitCode == -1)
                    {
                        return null;
                    }
                    System.Threading.Thread.Sleep(5000); //Wait 5 seconds

                    virtualMachine.CopyFileFromGuestToHost(@"c:\user.txt", localUserFileName);

                    FileStream fs = new FileStream(localUserFileName, FileMode.Open, FileAccess.Read);
                    StreamReader sr = new StreamReader(fs);
                    string con = sr.ReadToEnd().ToLower();
                    //int lastSpaceIndex = con.Trim().LastIndexOf(' ');
                    //string user = con.Substring(lastSpaceIndex, con.Length-1).Trim();              

                    //close stream
                    sr.Close();
                    fs.Close();
                    File.Delete(localUserFileName);
                    //There should be two user entries, one for current session and one interactive
                    string[] userEntries = con.Split(new char[] { '\n' });
                    users = new string[5];

                    foreach (string s in userEntries)
                    {
                        if (s.IndexOf('\\') > -1) // if current line is a user entry
                        {
                            if (s.IndexOf("unknown time") == -1) // skip entry for api user
                            {
                                //get user name
                                int lastTabIndex = s.LastIndexOf("\t"); //username is delimited by a \t and \r
                                users[0] = s.Substring(lastTabIndex).Trim();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return null;
                }
                
                return users[0];
            }
        }

        public void getePOLogs()
        {
        }

        public bool copyRequiredFilesToVM(string ePOZip, string agentZip, string testZip, string zipExe, string compExe)
        {
            //Files to copy
            // 1. ePO Build
            // 2. agent and test files
            // 3. uzext unzipper

            string batchFilePath = @"f:\autoinstallproject\temp\unzip.bat";

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                //virtualMachine.LoginInGuest(this.joinDomain + "\\" + this.domainAdmin, this.domainPassword);
                virtualMachine.LoginInGuest("administrator", this.workGroupPassword);
                if (File.Exists(ePOZip) && File.Exists(agentZip) && File.Exists(zipExe) && File.Exists(zipExe) && File.Exists(compExe))
                {
                    virtualMachine.CopyFileFromHostToGuest(ePOZip, @"c:\epo.zip");
                    virtualMachine.CopyFileFromHostToGuest(agentZip, @"C:\agent.zip");
                    virtualMachine.CopyFileFromHostToGuest(zipExe, @"C:\uzext.exe");
                    virtualMachine.CopyFileFromHostToGuest(testZip, @"C:\test.zip");
                    virtualMachine.CopyFileFromHostToGuest(compExe, @"c:\uzcomp.exe");
                    System.Threading.Thread.Sleep(5000);

                    //Construct batchfile for unzipping
                    //G:\test>uzext -e -o -d -pc:\epo c:\epo.zip
                    string[] zipCommands = new string[3];
                    zipCommands[0] = @"c:\uzext.exe -e -o -d -pc:\epo c:\epo.zip";
                    zipCommands[1] = @"c:\uzext.exe -e -o -d -pc:\agent c:\agent.zip";
                    zipCommands[2] = @"c:\uzext.exe -e -o -d -pc:\test c:\test.zip";

                    System.IO.FileInfo fi = new System.IO.FileInfo(batchFilePath);
                    StreamWriter sw = fi.CreateText();
                    sw.WriteLine(zipCommands[0]);
                    sw.WriteLine(zipCommands[1]);
                    sw.WriteLine(zipCommands[2]);
                    sw.Close();

                    virtualMachine.CopyFileFromHostToGuest(batchFilePath, @"c:\unzip.bat");
                    virtualMachine.RunProgramInGuest(@"c:\unzip.bat");
                    //wait 60 seconds for unzipping to comlete
                    //System.Threading.Thread.Sleep(60000);
                    return true;
                    virtualMachine.LogoutFromGuest();
                }
                else
                {
                    return false;
                }
            }
        }

        public bool isLoggedIn()
        {
            bool directoryExists = false;

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                if (virtualMachine.IsRunning)
                {
                    virtualMachine.WaitForToolsInGuest();
                    //virtualMachine.LoginInGuest(this.getJoinDomain().Split(new char[] { '.' })[0] + "\\" + this.getDomainAdmin(), this.getDomainPassword());
                    if (this.joinDomain == null)
                    {
                        virtualMachine.LoginInGuest(this.domainAdmin, this.domainPassword);
                    }
                    else
                    {
                        virtualMachine.LoginInGuest(this.joinDomain + "\\" + this.domainAdmin, this.domainPassword);
                    }
                        
                    //Check for existence of Windows directory, if it returns true then guest is logged in
                    directoryExists = virtualMachine.DirectoryExistsInGuest(@"C:\Windows");
                    virtualMachine.LogoutFromGuest();                    
                }
                return directoryExists;                
            }
        }

        public bool deploy()
        {
            return hostRef.deploy(this);
        }

        public void copyLogsToHost(string guestLogFolder, string hostLogFolder)
        {
            string zipLogsBatchFile = @"f:\autoinstallproject\temp\ziplogs.bat";

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                if (virtualMachine.IsRunning)
                {
                    virtualMachine.LoginInGuest(this.domainAdmin, this.domainPassword);
                    virtualMachine.WaitForToolsInGuest();
                    
                    //Check for existence of logs directory                    
                    bool directoryExists = virtualMachine.DirectoryExistsInGuest(guestLogFolder);                    

                    if (directoryExists)
                    {
                        string zipCommand = @"c:\uzcomp.exe -r " + guestLogFolder + ".zip " + guestLogFolder + "\\*";
                        System.IO.FileInfo fi = new System.IO.FileInfo(zipLogsBatchFile);
                        StreamWriter sw = fi.CreateText();
                        sw.WriteLine(zipCommand);
                        sw.Close();
                        virtualMachine.CopyFileFromHostToGuest(zipLogsBatchFile, @"c:\ziplogs.bat");
                        virtualMachine.RunProgramInGuest(@"c:\ziplogs.bat");
                    }

                    bool zipFileExists = virtualMachine.FileExistsInGuest(guestLogFolder + ".zip");

                    if(zipFileExists)
                    {
                        virtualMachine.CopyFileFromGuestToHost(guestLogFolder + ".zip",Path.Combine(hostLogFolder,this.getTestName()+".zip"));
                    }

                    virtualMachine.LogoutFromGuest();
                }                
            }
        }

    }

    public class Win2003VM : VM
    {
        public Win2003VM(VMHost hostRef)
            : base(hostRef)
        {
            this.autoLogonCount = "4";
        }      
    }

    public class Win2008VM : VM
    {
        public Win2008VM(VMHost hostRef)
            : base(hostRef)
        {
            this.autoLogonCount = "4";
        }        
    }

    public class Win2008R2VM : VM
    {
        public Win2008R2VM(VMHost hostRef)
            : base(hostRef)
        {
            this.autoLogonCount = "4";
        }

        public override void stage()
        {
            // Create registry strings to:
            // 1. Setup autologon settings to domain
            // 2. Startup agent on reboot
            // Add shutdown command
            // Create batch file with the strings
            // Copy file to vm
            // Execute file

            System.IO.FileInfo fi;
            string fileName = new Random().Next(100000000, 999999999).ToString() + ".bat";

            using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            {
                if (virtualMachine.IsRunning)
                {
                    virtualMachine.WaitForToolsInGuest();
                    //virtualMachine.LoginInGuest(this.getJoinDomain().Split(new char[] { '.' })[0] + "\\" + this.getDomainAdmin(), this.getDomainPassword());
                    virtualMachine.LoginInGuest("administrator", this.workGroupPassword);

                    // Build reg file to change autologon settings to domain
                    string deleteKey = "reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /f";
                    string addKey = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v \"DefaultDomainName\" /t REG_SZ /d " + this.joinDomain.Split(new char[] { '.' })[0];
                    //string disableScreenSaverKey = "reg add \"HKCU\\Control Panel\\Desktop\" /v \"ScreenSaveActive\" /t REG_SZ /d 0 /f";
                    string disableScreenSaverKey = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\" /v \"ScreenSaveDisable\" /t REG_SZ /d \"reg add \\\"HKCU\\Control Panel\\Desktop\\\" /v \\\"ScreenSaveActive\\\" /t REG_SZ /d 0 /f\"";
                    //string startAgent = "cmd /C \"start c:\\agent\\agent.exe c:\\agent\\agent.conf";
                    string startAgent = "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\" /v \"StartAgent\" /t REG_SZ /d \"cmd /C start c:\\agent\\agent.exe c:\\agent\\agent.conf";
                    string sqlAdminAdd = "sqlcmd -S localhost\\SQL2008 -U sa -P sa -Q \"EXEC master..sp_addsrvrolemember @loginame = N'" + this.joinDomain.Split(new char[] { '.' })[0] + "\\" + this.domainAdmin + "', @rolename = N'sysadmin'\"";
                    //string shutdownKey = "shutdown -r -t 10 -c \"Rebooting computer\"";
                    string shutdownKey = "netdom join localhost /domain:" + this.joinDomain + " /userd:" + this.domainAdmin + " /passwordd:" + this.domainPassword + " /REboot:10";

                    //Build a file containing the domain settings
                    fi = new System.IO.FileInfo(@"c:\windows\temp\" + fileName);
                    StreamWriter sw = fi.CreateText();
                    sw.WriteLine(deleteKey);
                    sw.WriteLine(addKey);
                    sw.WriteLine(disableScreenSaverKey);
                    sw.WriteLine(startAgent);
                    sw.WriteLine(sqlAdminAdd);                    
                    sw.WriteLine(shutdownKey);                    
                    sw.Close();

                    //Copy said file guest
                    virtualMachine.CopyFileFromHostToGuest(@"c:\windows\temp\" + fileName, @"c:\" + fileName);
                    System.Threading.Thread.Sleep(2000); // wait 2 seconds for copy
                    virtualMachine.RunProgramInGuest(@"c:\" + fileName, "");
                    virtualMachine.LogoutFromGuest();
                    //virtualMachine.ShutdownGuest();

                    //Wait every two seconds and check power state until machine is powered off
                    /*
                    while (virtualMachine.IsRunning == true)
                    {
                        System.Threading.Thread.Sleep(2000);
                    }
                    */

                    //virtualMachine.PowerOn();                                                                            
                }
            }
            //Wait 20 seconds for reboot
            System.Threading.Thread.Sleep(20000);

            //Creating a connection again for running the batch script, for some reason running the batch within the previous block causes a timeout
            //using (VMWareVirtualMachine virtualMachine = hostRef.vixhost.Open(this.storageLocation, 20))
            //{
            //virtualMachine.LoginInGuest(this.joinDomain + "\\" + this.domainAdmin, this.domainPassword);
            //virtualMachine.WaitForToolsInGuest();                
            //System.Threading.Thread.Sleep(15000); // wait 15 seconds for personal settings to apply and agent to start
            //virtualMachine.LogoutFromGuest();
            //}

        }

    }

}