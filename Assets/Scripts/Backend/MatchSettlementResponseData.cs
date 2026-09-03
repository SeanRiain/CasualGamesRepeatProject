using System;

[Serializable]
public class MatchRelationshipSnapshotData
{
    public int schemaVersion;

    public string playerAId;
    public string playerBId;

    public int playerAWins;
    public int playerBWins;

    public int matchesPlayed;

    public string lastSessionId;
    public int lastMatchNumber;
}

[Serializable]
public class MatchSettlementResponseData
{
    public bool success;

    public bool alreadySettled;

    public string settlementId;

    public MatchRelationshipSnapshotData relationship;
}