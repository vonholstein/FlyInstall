using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace Utilities
{
    public class TestModel
    {        
        private static Dictionary<string, StripeTest> testList = new Dictionary<string, StripeTest>();

        private TestModel()
        {
        }

        public static Dictionary<string, StripeTest> getTests()
        {
            return testList;
        }

        public static void initialize(string testListFile)
        {
            int lineCount = 0;
            string line;
            string[] lineItems;
            try
            {             
                using (StreamReader sr = new StreamReader(testListFile))
                {                
                    while ((line = sr.ReadLine()) != null)
                    {
                        ++lineCount;
                        if (line.Trim() == "")
                        {
                            Console.WriteLine("Skipping empty line in test list");
                            continue;
                        }
                                                
                        lineItems = line.Split(new char[] { ':' });
                        StripeTest st = new StripeTest();

                        foreach (string s in lineItems)
                        {
                            string[] itemSplit = s.Split(new char[] { '=' });
                            if (itemSplit.Length != 2)
                            {
                                Console.WriteLine("Invalid pairing at line " + lineCount.ToString());
                                break;
                            }
                            else
                            {
                                switch (itemSplit[0])
                                {
                                    case "ID":
                                        st.id = itemSplit[1];
                                        break;
                                    case "OS":
                                        st.os = itemSplit[1];
                                        break;
                                    case "DB":
                                        st.db = itemSplit[1];
                                        break;
                                    case "DBLOC":
                                        st.dbloc = itemSplit[1];
                                        break;
                                    case "AUTH":
                                        st.auth = itemSplit[1];
                                        break;
                                    case "BITNESS":
                                        st.bitness = itemSplit[1];
                                        break;
                                    default:
                                        Console.WriteLine("Skipping case: Invalid parameter " + itemSplit[0]);
                                        break;
                                }                                
                            }                            
                        }
                        testList.Add(st.id, st);                               
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                throw e;
            }

        }
    }

    public class StripeTest
    {
        public string id;
        public string os;
        public string db;
        public string dbloc;
        public string auth;
        public string bitness;

        public StripeTest()
        {
        }

        public StripeTest(string id, string os, string db, string dbloc, string auth, string bitness)
        {
            this.id = id;
            this.os = os;
            this.db = db;
            this.dbloc = dbloc;
            this.auth = auth;
            this.bitness = bitness;
        }
    }
}
