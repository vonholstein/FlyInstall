using System;
using System.Collections.Generic;
using System.Text;
using TDAPIOLELib;

namespace QCInterface
{
    public class NoTestException : Exception
    {
    }

    public class NoTestSetException : Exception
    {
    }
    
    public class QualityCenter
    {
        private TDConnection connection;

        public QualityCenter(string qcHostName, string qcDomain, string qcProject, string qcUser, string qcPassword, string qcPort)
        {
            string qcServer = qcHostName;

            connection = new TDConnection();
            
            //if qc HostName starts with http:// do not append, else append
            if(!qcHostName.StartsWith(@"http://"))
            {
                qcServer = String.Concat(@"http://",qcHostName);
            }

            //Concat :port
            if (qcPort != null)
            {
                qcServer = String.Concat(qcServer, ":", qcPort);
            }

            //Concat /qcbin
            qcServer = String.Concat(qcServer, "/qcbin");

            connection.InitConnectionEx(qcServer);

            connection.Login(qcUser, qcPassword);

            connection.Connect(qcDomain, qcProject);           

        }

        public string getQCUserName()
        {
            return connection.UserName;
        }

        public void disConnect()
        {
            connection.Disconnect();
        }

        public void addTestSet(string pathToTestSet, string testSetName)
        {
            TestSetTreeManager treeManager = (TestSetTreeManager)connection.TestSetTreeManager;
            TestSetFolder testSetFolder = (TestSetFolder)treeManager.get_NodeByPath(pathToTestSet);
            TestSetFactory testSetFactory = (TestSetFactory)testSetFolder.TestSetFactory;
            TestSet testSet = (TestSet)testSetFactory.AddItem(DBNull.Value);            
            testSet.Name = testSetName;
            testSet.Status = "Open";
            testSet.Post();            
        }

        public bool addTestToTestSet(string pathToTestSet, string testSetName, string testID)
        {
            TestSetTreeManager treeManager = (TestSetTreeManager)connection.TestSetTreeManager;
            TestSetFolder testSetFolder = (TestSetFolder)treeManager.get_NodeByPath(pathToTestSet);
            TestSetFactory testSetFactory = (TestSetFactory)testSetFolder.TestSetFactory;
            TestSet testSet = null;                      
            Test test = null;

            //Get list of test sets satisfying name criteria
            List testSetList = testSetFolder.FindTestSets(testSetName, false, "");

            foreach (TestSet ts in testSetList)
            {
                testSet = ts;
            }

            if (testSet == null)
            {
                throw new NoTestSetException();
            }            

            //Get test             
            TestFactory testFactory = (TestFactory)connection.TestFactory;
            TDFilter filter = (TDFilter)testFactory.Filter;
            
            filter["TS_TEST_ID"] = testID;            
            List testList = testFactory.NewList(filter.Text);

            foreach (Test t in testList)
            {
                test = t;
            }

            if (test == null)
            {
                throw new NoTestException();
            }           
            
            TSTestFactory factory = (TSTestFactory)testSet.TSTestFactory;

            //if test does not exist in testset add test
            string checkTest = testSet.CheckTestInstances(testID);
            if ( checkTest == null)
            {
                TSTest test1 = (TSTest)factory.AddItem(Int32.Parse(testID));
                test1.Post();
            }

            return true;
             
        }

        private void setTestPassOrFail(string pathToTestSet, string testSetName, string testID, bool passOrFail)
        {
            TestSetTreeManager treeManager = (TestSetTreeManager)connection.TestSetTreeManager;
            TestSetFolder testSetFolder = (TestSetFolder)treeManager.get_NodeByPath(pathToTestSet);
            TestSetFactory testSetFactory = (TestSetFactory)testSetFolder.TestSetFactory;
            TestSet testSet = null;
            Test test = null;

            //Get list of test sets satisfying name criteria
            List testSetList = testSetFolder.FindTestSets(testSetName, false, "");

            foreach (TestSet ts in testSetList)
            {
                testSet = ts;
            }

            if (testSet == null)
            {
                throw new NoTestSetException();
            }

            //Get test             
            TestFactory testFactory = (TestFactory)connection.TestFactory;
            TDFilter filter = (TDFilter)testFactory.Filter;

            filter["TS_TEST_ID"] = testID;
            List testList = testFactory.NewList(filter.Text);

            foreach (Test t in testList)
            {
                test = t;
            }

            if (test == null)
            {
                throw new NoTestException();
            }

            //Set run status
            TSTestFactory tsTestFactory = (TSTestFactory)testSet.TSTestFactory;
            List tsTestList = tsTestFactory.NewList(filter.Text);

            foreach (TSTest ts in tsTestList)
            {
                if (passOrFail)
                {
                    ts.Status = "PASSED";
                }
                else
                {
                    ts.Status = "FAILED";
                }
                ts.Post();
            }

        }

        public void passTest(string pathToTestSet, string testSetName, string testID)            
        {
            this.setTestPassOrFail(pathToTestSet, testSetName, testID, true);
        }

        public void failTest(string pathToTestSet, string testSetName, string testID)
        {
            this.setTestPassOrFail(pathToTestSet, testSetName, testID, false);
        }
    }
}
