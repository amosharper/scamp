using System.Collections.Generic;

namespace Scamp
{
    public class CannedResponse
    {
        public IEnumerable<string> Aliases;
        public string CannedResponseText;
        public bool ContributorOnly;
        public bool WholeMessageTrigger;
        public bool PartialMessageTrigger;

        public CannedResponse()
        {
        }
    }
}
