using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Utils
{
    public class TryResolveResult
    {
        public bool Resolved { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        public TryResolveResult(bool resolved, string errorMessage)
        {
            Resolved = resolved;
            ErrorMessage = errorMessage;
        }

        public TryResolveResult(bool resolved)
        {
            Resolved = resolved;
        }
    }
}
