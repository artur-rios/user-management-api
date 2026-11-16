namespace ArturRios.UserManagement.Command.Tests;

/// <summary>
/// The tests whose handler hashes a password. The handlers hash with Argon2id at the production settings, 600 MB and
/// 16 threads a hash, so these classes run one at a time rather than side by side.
/// </summary>
[CollectionDefinition(Name)]
public class PasswordHashingCollection
{
    public const string Name = "Password hashing";
}
