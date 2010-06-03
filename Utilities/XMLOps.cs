using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Utilities
{
    public class XMLOps
    {
        public static void WriteXML(StripeTest[] stripes, string outfile, int parallelTestRuns)
        {
            // Objective: Return pnunit xml test specification
            try
            {

                XmlDocument xmlDoc = new XmlDocument();
                XmlElement[] parallelTestNodes = new XmlElement[parallelTestRuns];

                //if file is not found, create a new xml file
                XmlTextWriter xmlWriter = new XmlTextWriter(outfile, System.Text.Encoding.UTF8);
                xmlWriter.Formatting = Formatting.Indented;
                //xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                xmlWriter.WriteStartElement("TestGroup");
                //If WriteProcessingInstruction is used as above,
                //Do not use WriteEndElement() here
                //xmlWriter.WriteEndElement();
                //it will cause the &ltRoot></Root> to be &ltRoot />
                xmlWriter.Close();
                //xmlDoc.Load(filename);

                XmlNode root = xmlDoc.DocumentElement;
                XmlElement childNode = xmlDoc.CreateElement("ParallelTests");
                XmlElement childNode2 = xmlDoc.CreateElement("ParallelTest");
                XmlText textNode = xmlDoc.CreateTextNode("hello");
                textNode.Value = "hello, world";

                root.AppendChild(childNode);
                childNode.AppendChild(childNode2);
                childNode2.SetAttribute("Name", "Value");
                childNode2.AppendChild(textNode);

                textNode.Value = "replacing hello world";
                //xmlDoc.Save(filename);

                XmlElement parallelTest;
                XmlElement parallelTestName;

                for (int i = 0; i < parallelTestRuns; i++)
                {
                    parallelTest = xmlDoc.CreateElement("ParallelTest");
                    parallelTestName = xmlDoc.CreateTextNode("Name");
                    parallelTestName.Value = 
                    parallelTest.AppendChild(parallelTestName);
                    for (int j = 0; j < stripes.Length / parallelTestRuns; j++)
                    {

                    }
                }
                
            }
            catch (Exception ex)
            {
            }
        }
    }
}
