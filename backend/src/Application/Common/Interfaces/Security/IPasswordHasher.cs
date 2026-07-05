using System;

namespace Application.Common.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
