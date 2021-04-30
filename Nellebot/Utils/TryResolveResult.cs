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

    public class TryResolveResultObject<T> where T: class
    {
        public bool Resolved { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public T Result { get; private set; }

        public TryResolveResultObject(bool resolved, string errorMessage)
        {
            Resolved = resolved;
            ErrorMessage = errorMessage;
            Result = null!;
        }

        public TryResolveResultObject(T result)
        {
            Resolved = true;
            Result = result;
        }
    }
}
