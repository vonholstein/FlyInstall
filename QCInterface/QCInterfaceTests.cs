using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;


namespace QCInterface
{
    [TestFixture]
    public class QCInterfaceTests
    {
        [Test]
        public void connectQCValid()
        {
            QualityCenter qc = new QualityCenter("plaengqc1.corp.nai.org", "MCAFEE", "ePO", "QCUser", "Welcome2mcafee", null);

            Assert.AreEqual(qc.getQCUserName(), "QCUser", "Login failed");

            qc.disConnect();
        }

        [Test]
        public void connectQCInvalidHostName()
        {
            QualityCenter qc = new QualityCenter("", "MCAFEE", "ePO", "QCUser", "Welcome2mcafee", null);

            Assert.AreEqual(qc.getQCUserName(), null, "Failed");

            qc.disConnect();
        }

        [Test]
        public void addTestSet()
        {
            string testSetPath = @"Root\QCTest";
            string testSetName = "ePOInstallTest";

            QualityCenter qc = new QualityCenter("plaengqc1.corp.nai.org", "MCAFEE", "ePO", "hnahas", "Kierkegaard4", null);

            qc.addTestSet(testSetPath, testSetName);

            qc.disConnect();

        }

        [Test]
        public void addTestToTestSet()
        {
            string testSetPath = @"Root\QCTest";
            string testSetName = @"ePOInstallTest";
            string testID = "8375";
            string[] testIDs = { "8374", "8375", "8376", "8377", "8378" };
            

            QualityCenter qc = new QualityCenter("plaengqc1.corp.nai.org", "MCAFEE", "ePO", "hnahas", "Kierkegaard4", null);

            foreach(string id in testIDs)
            {
                qc.addTestToTestSet(testSetPath, testSetName, id);
            }

            qc.disConnect();

        }

        [Test]
        public void passTest()
        {
            string testSetPath = @"Root\QCTest";
            string testSetName = @"ePOInstallTest";
            string testID = "8375";
            string[] testIDs = { "8374", "8375", "8376", "8377", "8378" };


            QualityCenter qc = new QualityCenter("plaengqc1.corp.nai.org", "MCAFEE", "ePO", "hnahas", "Kierkegaard4", null);

            qc.passTest(testSetPath, testSetName, testID);

            qc.disConnect();
        }
            
    }
}
