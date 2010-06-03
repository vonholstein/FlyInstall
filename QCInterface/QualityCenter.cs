using System;
using System.Collections.Generic;
using System.Text;
using TDAPIOLELib;

namespace QCInterface
{
    public class QualityCenter
    {
        public QualityCenter()
        {
            TDConnection connection = new TDConnection();
            connection.InitConnection();
        }

    }
}
