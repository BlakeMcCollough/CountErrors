using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CountSupvErrors
{
    class RegexConsts
    {
        public readonly Regex ProcessedFileAccept;
        public readonly Regex ErrorLineFound;
        public readonly Regex CustomerNameAccept;

        public RegexConsts()
        {
            //used to make sure a file is a .log file that's been processed with proper time.time.time format
            ProcessedFileAccept = new Regex(@"^pQS1LOG(\.\d{6}){3}\.log$");
            //used to find an error line in the format C2A00x0 where x is the error code, hex is CASE SENSITIVE
            ErrorLineFound = new Regex(@"\|=C2A00[0-9A-F][0-9A-F]0\|");

            //used to check if a string is in the customer format
            CustomerNameAccept = new Regex(@"^\w{4}$");
        }

    }
}
