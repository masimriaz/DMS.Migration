using DMS.Migration.Domain.Entities;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Application.Interfaces;

public interface IConnectionVerifier
{
    Task<ConnectionVerificationRun> VerifyAsync(Connection connection, string initiatedBy);
}

public interface IConnectionVerifierFactory
{
    IConnectionVerifier GetVerifier(ConnectionType type);
}
