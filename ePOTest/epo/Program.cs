using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;

namespace epo
{
    [TestFixture]
    class Program
    {
        /*
         * This method will check for whether the installation folder exists and create
         * a batch file with the name "silent_installer.bat". It will be run to install
         * ePO silently onto the machine. The program will wait for the batch file to
         * finish.
         * 
         * */
        [Test]
        public void myAdditionTest()
        {

            string[] testParams = PNUnitServices.GetTestParams();
            //string[] testParams = { "EPOADMIN = admin ", "EPOPASSWORD = epo", "DBSERVER = sql2005sp3", "DBUSERNAME = administrator", "MFSDATABASEINSTANCENAME=Sushanth", "DBDOMAIN = epomfe", "DBPASSWORD = Yv74aL5j", "DBAUTH = 2" };
            Console.WriteLine("testParams is " + testParams);

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            {string[] strArray;
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
            string databaseName = "ePO4_"+ System.Environment.MachineName;
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
                    "MFSDATABASEPORT=" + databasePort + " ");
            }

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

            Console.WriteLine("entry into batch is.."+allParameters);
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
                Assert.IsTrue((sc.Status.ToString().ToLower != "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

                sc.ServiceName = "MCAFEEEVENTPARSERSRV";
                Console.WriteLine("The service " + sc.DisplayName + " was found to be in state: " + sc.Status.ToString());
                Assert.IsTrue((sc.Status.ToString().ToLower != "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

                sc.ServiceName = "MCAFEEAPACHESRV";
                Console.WriteLine("The service " + sc.DisplayName + " was found to be in state: " + sc.Status.ToString());
                Assert.IsTrue((sc.Status.ToString().ToLower != "running"), sc.DisplayName + " is in the state: " + sc.Status.ToString());

            }
            catch (Exception e)
            {
                Assert.IsTrue(false, "Exception occured while checking the services. "+ e.Message);
            }


            //performing an ePO console login to check that it was installed correctly


        }

    }
}
