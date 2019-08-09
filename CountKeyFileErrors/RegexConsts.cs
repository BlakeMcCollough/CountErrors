using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CountKeyFileErrors
{
    class RegexConsts
    {
        public readonly Regex ProcessedFileAccept;
        public readonly Regex KeyLineFound;
        public readonly Regex ErrorLineFound;
        public readonly Regex KeyLineFound6789; //for errors 86, 87, 88, 89
        public readonly Regex ErrorLineFound6789;
        public readonly Regex CustomerNameAccept;

        public RegexConsts()
        {
            //used to make sure a file is a .log file that's been processed with proper time.time.time format
            ProcessedFileAccept = new Regex(@"^pQS1LOG(\.\d{6}){3}\.log$");
            //used to find an error line in the format C2A008x2 where x is 0-D
            KeyLineFound = new Regex(@"\|=C2A008[0-5A-D]0\|");
            KeyLineFound6789 = new Regex(@"\|=C2A008[6-9]1\|");
            ErrorLineFound = new Regex(@"\|=C2A008[0-5A-D]2\|");
            ErrorLineFound6789 = new Regex(@"\|=C2A008[6-9]3\|");

            //used to check if a string is in the customer format
            CustomerNameAccept = new Regex(@"^\w{4}$");
        }

    }
}
