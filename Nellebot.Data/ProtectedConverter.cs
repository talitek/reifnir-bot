using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nellebot.Data;

public class ProtectedConverter : ValueConverter<string?, string?>
{
    public ProtectedConverter(IDataProtectionProvider provider, string purpose)
    : this(new Wrapper(provider, purpose))
    { }

    private ProtectedConverter(Wrapper wrapper)
    : base(wrapper.To, wrapper.From)
    { }

    private class Wrapper
    {
        private readonly IDataProtector _dataProtector;

        public Wrapper(IDataProtectionProvider dataProtectionProvider, string purpose)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(purpose);
        }

        public Expression<Func<string?, string?>> To => x => x != null ? _dataProtector.Protect(x) : null;

        public Expression<Func<string?, string?>> From => x => x != null ? _dataProtector.Unprotect(x) : null;
    }
}
