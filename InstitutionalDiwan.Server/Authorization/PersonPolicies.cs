namespace InstitutionalDiwan.Server.Authorization;

/// <summary>Authorization policies of the persons endpoints that are not a single permission.</summary>
public static class PersonPolicies
{
    /// <summary>Whoever can put a person on a document may be offered existing people to choose from.</summary>
    public const string Suggest = "PERSON_SUGGEST";
}
