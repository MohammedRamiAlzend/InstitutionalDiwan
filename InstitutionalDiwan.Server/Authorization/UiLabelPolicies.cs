namespace InstitutionalDiwan.Server.Authorization;

/// <summary>Authorization policies of the UI label endpoints that are not a single permission.</summary>
public static class UiLabelPolicies
{
    /// <summary>Only the seeded superadmin account may replace or reset built-in UI labels.</summary>
    public const string Manage = "UI_LABELS_MANAGE";
}