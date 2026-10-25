using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Tests.Application;

public sealed class AuditDomainTests
{
    private static AuditLog NewLog(string previousHash, DateTime? occurredAt = null, string? newValues = null)
    {
        var result = AuditLog.Record(
            "DOCUMENTS",
            AuditActionType.UpdateId,
            AuditEntityType.DocumentId,
            Guid.NewGuid().ToString(),
            previousHash,
            occurredAtUtc: occurredAt ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            newValuesJson: newValues);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public void Record_FirstEntry_ChainsToGenesisAndProducesSixtyFourCharacterHash()
    {
        var log = NewLog(AuditLog.GenesisHash);

        Assert.Equal(AuditLog.GenesisHash, log.PreviousHash);
        Assert.Equal(64, log.EventHash.Length);
        Assert.True(log.VerifyIntegrity(AuditLog.GenesisHash).IsSuccess);
    }

    [Fact]
    public void Reseal_ChainsTheEntryToANewPredecessorAndRecomputesTheHash()
    {
        var log = NewLog(AuditLog.GenesisHash);
        var originalHash = log.EventHash;
        var newPredecessor = new string('b', 64);

        log.Reseal(newPredecessor);

        Assert.Equal(newPredecessor, log.PreviousHash);
        Assert.NotEqual(originalHash, log.EventHash);
        Assert.True(log.VerifyIntegrity(newPredecessor).IsSuccess);
    }

    [Fact]
    public void VerifyIntegrity_AfterTamperingWithAField_FailsWithHashMismatch()
    {
        var log = NewLog(AuditLog.GenesisHash);

        typeof(AuditLog)
            .GetProperty(nameof(AuditLog.ModuleName))!
            .SetValue(log, "TAMPERED");

        var verification = log.VerifyIntegrity(AuditLog.GenesisHash);

        Assert.True(verification.IsFailure);
        Assert.Equal(DomainErrors.Audit.HashMismatch.Code, verification.Error.Code);
    }

    [Fact]
    public void VerifyIntegrity_WhenPreviousHashDoesNotMatch_FailsWithChainBroken()
    {
        var log = NewLog(AuditLog.GenesisHash);

        var verification = log.VerifyIntegrity(new string('a', 64));

        Assert.True(verification.IsFailure);
        Assert.Equal(DomainErrors.Audit.ChainBroken.Code, verification.Error.Code);
    }

    [Fact]
    public void VerifyIntegrity_AfterTimestampRoundTrip_StillMatches()
    {
        var timestamp = new DateTime(2026, 3, 4, 5, 6, 7, 123, DateTimeKind.Utc);
        var log = NewLog(AuditLog.GenesisHash, timestamp);

        // Simulate SQL Server datetime2 reading the value back with DateTimeKind.Unspecified.
        typeof(AuditLog)
            .GetProperty(nameof(AuditLog.OccurredAtUtc))!
            .SetValue(log, DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified));

        Assert.True(log.VerifyIntegrity(AuditLog.GenesisHash).IsSuccess);
    }

    [Fact]
    public void Record_WhenPreviousHashMissing_FailsValidation()
    {
        var result = AuditLog.Record(
            "DOCUMENTS",
            AuditActionType.CreateId,
            AuditEntityType.DocumentId,
            "id",
            previousHash: " ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
