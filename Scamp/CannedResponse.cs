using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Scamp
{
    public class CannedResponse
    {
        public IEnumerable<string> Aliases;
        public IEnumerable<Regex> RegExTriggers;
        public string CannedResponseText;
        public bool ContributorOnly;

        public CannedResponse()
        {
        }
    }
}
