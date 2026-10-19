namespace InstitutionalDiwan.Domain.Errors;

public static class DomainErrors
{
    public static class Lookup
    {
        public static readonly Error InvalidId = Error.Validation("Lookup.InvalidId", "A lookup entry id must be a positive integer.");
        public static readonly Error LabelEmpty = Error.Validation("Lookup.LabelEmpty", "A lookup entry requires both English and Arabic labels.");

        public static Error UnknownCatalog(string lookup) =>
            Error.NotFound("Lookup.UnknownCatalog", $"The lookup catalog '{lookup}' is not supported.");

        public static Error EntryNotFound(string lookup, int id) =>
            Error.NotFound("Lookup.EntryNotFound", $"Lookup entry {id} was not found in the '{lookup}' catalog.");

        public static Error IdAlreadyExists(string lookup, int id) =>
            Error.Conflict("Lookup.IdAlreadyExists", $"Lookup entry {id} already exists in the '{lookup}' catalog.");

        public static Error EntryInactive(string lookup, int id) =>
            Error.Conflict("Lookup.EntryInactive", $"Lookup entry {id} in the '{lookup}' catalog is inactive and cannot be selected.");

        public static Error EntryInUse(string lookup, int id) =>
            Error.Conflict("Lookup.EntryInUse", $"Lookup entry {id} in the '{lookup}' catalog is referenced by other records and cannot be deactivated.");

        public static Error CatalogReadOnly(string lookup) =>
            Error.Validation("Lookup.CatalogReadOnly", $"The '{lookup}' catalog cannot be extended through the generic lookups endpoint because it requires additional fields.");
    }

    public static class Department
    {
        public static readonly Error CodeEmpty = Error.Validation("Department.CodeEmpty", "Department code is required.");
        public static readonly Error CodeTooLong = Error.Validation("Department.CodeTooLong", "Department code cannot exceed 50 characters.");
        public static readonly Error NameEmpty = Error.Validation("Department.NameEmpty", "Arabic department name is required.");
        public static readonly Error TypeEmpty = Error.Validation("Department.TypeEmpty", "Department type code is required.");
        public static readonly Error SelfParent = Error.Conflict("Department.SelfParent", "A department cannot be its own parent.");
        public static readonly Error CyclicHierarchy = Error.Conflict("Department.CyclicHierarchy", "Cannot move department under one of its own descendants.");
        public static readonly Error InactiveParent = Error.Conflict("Department.InactiveParent", "Cannot move department under an inactive parent department.");
        public static readonly Error HasActiveChildren = Error.Conflict("Department.HasActiveChildren", "Cannot deactivate a department that still has active child departments.");
        public static readonly Error NotFound = Error.NotFound("Department.NotFound", "The department was not found.");
        public static readonly Error CodeAlreadyExists = Error.Conflict("Department.CodeAlreadyExists", "A department with this code already exists.");
        public static readonly Error TypeNotFound = Error.Validation("Department.TypeNotFound", "The specified department type does not exist.");
        public static readonly Error TypeInactive = Error.Conflict("Department.TypeInactive", "Cannot assign an inactive department type.");
        public static readonly Error ParentNotFound = Error.Validation("Department.ParentNotFound", "The specified parent department does not exist.");
    }

    public static class InstitutionSetting
    {
        public static readonly Error NameEmpty = Error.Validation("InstitutionSetting.NameEmpty", "Institution Arabic name is required.");
        public static readonly Error NumberingModeEmpty = Error.Validation("InstitutionSetting.NumberingModeEmpty", "Numbering mode is required.");
        public static readonly Error ResetPolicyEmpty = Error.Validation("InstitutionSetting.ResetPolicyEmpty", "Numbering reset policy is required.");
        public static readonly Error TimezoneEmpty = Error.Validation("InstitutionSetting.TimezoneEmpty", "A default timezone is required.");
        public static readonly Error NotFound = Error.NotFound("InstitutionSetting.NotFound", "Institution settings have not been configured yet.");
        public static readonly Error CentralDiwanRequired = Error.Validation("InstitutionSetting.CentralDiwanRequired", "The Central Diwan department is required.");
        public static readonly Error CentralDiwanNotFound = Error.Validation("InstitutionSetting.CentralDiwanNotFound", "The designated Central Diwan department does not exist.");
        public static readonly Error CentralDiwanNotRoot = Error.Conflict("InstitutionSetting.CentralDiwanNotRoot", "The designated Central Diwan department must be a root department (no parent).");
        public static readonly Error CentralDiwanTypeInvalid = Error.Conflict("InstitutionSetting.CentralDiwanTypeInvalid", "The designated Central Diwan department must be of type CENTRAL_DIWAN.");
        public static readonly Error CentralDiwanInactive = Error.Conflict("InstitutionSetting.CentralDiwanInactive", "Cannot designate an inactive department as the Central Diwan.");
        public static readonly Error CentralDiwanTransferReasonRequired = Error.Validation("InstitutionSetting.CentralDiwanTransferReasonRequired", "Changing the Central Diwan department requires a documented transfer reason.");
        public static readonly Error NumberingModeNotFound = Error.Validation("InstitutionSetting.NumberingModeNotFound", "The specified numbering mode does not exist.");
        public static readonly Error NumberingModeInactive = Error.Conflict("InstitutionSetting.NumberingModeInactive", "Cannot select an inactive numbering mode.");
        public static readonly Error ResetPolicyNotFound = Error.Validation("InstitutionSetting.ResetPolicyNotFound", "The specified numbering reset policy does not exist.");
        public static readonly Error ResetPolicyInactive = Error.Conflict("InstitutionSetting.ResetPolicyInactive", "Cannot select an inactive numbering reset policy.");
        public static readonly Error ResponsibleEmployeeNotFound = Error.Validation("InstitutionSetting.ResponsibleEmployeeNotFound", "The designated responsible employee does not exist.");
    }

    public static class Identity
    {
        public static readonly Error UsernameEmpty = Error.Validation("Identity.UsernameEmpty", "Username is required.");
        public static readonly Error PasswordHashEmpty = Error.Validation("Identity.PasswordHashEmpty", "Password hash is required.");
        public static readonly Error RoleCodeEmpty = Error.Validation("Identity.RoleCodeEmpty", "Role code is required.");
        public static readonly Error RoleNameEmpty = Error.Validation("Identity.RoleNameEmpty", "Role Arabic name is required.");
        public static readonly Error PermissionCodeEmpty = Error.Validation("Identity.PermissionCodeEmpty", "Permission code is required.");
        public static readonly Error ModuleNameEmpty = Error.Validation("Identity.ModuleNameEmpty", "Permission module name is required.");
        public static readonly Error RoleAlreadyAssigned = Error.Conflict("Identity.RoleAlreadyAssigned", "The specified role is already assigned to this user.");
        public static readonly Error RoleNotAssigned = Error.NotFound("Identity.RoleNotAssigned", "The specified role is not assigned to this user.");
        public static readonly Error SystemRoleProtected = Error.Forbidden("Identity.SystemRoleProtected", "System-defined roles cannot be deactivated or deleted.");
        public static readonly Error PermissionAlreadyGranted = Error.Conflict("Identity.PermissionAlreadyGranted", "Permission is already granted to this role.");
        public static readonly Error EmployeeNameEmpty = Error.Validation("Identity.EmployeeNameEmpty", "Employee full Arabic name is required.");
        public static readonly Error InvalidDateRange = Error.Validation("Identity.InvalidDateRange", "End date cannot be earlier than start date.");
        public static readonly Error AccessLevelInvalid = Error.Validation("Identity.AccessLevelInvalid", "Access level must be VIEW, DOWNLOAD, FORWARD, or MANAGE.");
        public static readonly Error GrantAlreadyRevoked = Error.Conflict("Identity.GrantAlreadyRevoked", "Document access grant has already been revoked.");
        public static readonly Error InvalidCredentials = Error.Unauthorized("Identity.InvalidCredentials", "Invalid username or password.");
        public static readonly Error UserInactive = Error.Forbidden("Identity.UserInactive", "The user account is inactive.");
        public static readonly Error UsernameTaken = Error.Conflict("Identity.UsernameTaken", "The username is already taken.");
        public static readonly Error RoleNotFound = Error.NotFound("Identity.RoleNotFound", "The specified role was not found.");
        public static readonly Error RoleCodeAlreadyExists = Error.Conflict("Identity.RoleCodeAlreadyExists", "A role with this code already exists.");
        public static readonly Error PermissionNotFound = Error.Validation("Identity.PermissionNotFound", "The specified permission does not exist.");
        public static readonly Error UserNotFound = Error.NotFound("Identity.UserNotFound", "The user account was not found.");
        public static readonly Error CurrentPasswordIncorrect = Error.Validation("Identity.CurrentPasswordIncorrect", "The current password is incorrect.");
        public static readonly Error EmployeeNotFound = Error.NotFound("Identity.EmployeeNotFound", "The employee record was not found.");
        public static readonly Error AccessGrantNotFound = Error.NotFound("Identity.AccessGrantNotFound", "The document access grant was not found.");
        public static readonly Error DepartmentAlreadyAssigned = Error.Conflict("Identity.DepartmentAlreadyAssigned", "The user already has an active assignment for this department.");
        public static readonly Error DepartmentAssignmentNotFound = Error.NotFound("Identity.DepartmentAssignmentNotFound", "The department assignment was not found for this user.");
        public static readonly Error AssignmentTypeInvalid = Error.Validation("Identity.AssignmentTypeInvalid", "The specified department assignment type does not exist or is inactive.");
    }

    public static class Correspondence
    {
        public static readonly Error TitleEmpty = Error.Validation("Correspondence.TitleEmpty", "Document title is required.");
        public static readonly Error TitleTooLong = Error.Validation("Correspondence.TitleTooLong", "Document title cannot exceed 500 characters.");
        public static readonly Error TypeCodeEmpty = Error.Validation("Correspondence.TypeCodeEmpty", "Document type code is required.");
        public static readonly Error DocumentTypeNotFound = Error.Validation("Correspondence.DocumentTypeNotFound", "The specified document type does not exist.");
        public static readonly Error DocumentTypeInactive = Error.Conflict("Correspondence.DocumentTypeInactive", "Cannot use an inactive document type.");
        public static readonly Error DirectionInvalid = Error.Validation("Correspondence.DirectionInvalid", "Direction must be INTERNAL, INCOMING, or OUTGOING.");
        public static readonly Error OnlyDraftEditable = Error.Conflict("Correspondence.OnlyDraftEditable", "Only documents in DRAFT status can have their core metadata modified directly.");
        public static readonly Error OnlyDraftRegisterable = Error.Conflict("Correspondence.OnlyDraftRegisterable", "Only documents in DRAFT status can be officially registered.");
        public static readonly Error AlreadyArchived = Error.Conflict("Correspondence.AlreadyArchived", "Document is already archived.");
        public static readonly Error CannotArchiveCanceled = Error.Conflict("Correspondence.CannotArchiveCanceled", "Canceled documents cannot be archived.");
        public static readonly Error HasActiveMovements = Error.Conflict("Correspondence.HasActiveMovements", "Cannot archive or cancel a document while it has active movements.");
        public static readonly Error NotArchived = Error.Conflict("Correspondence.NotArchived", "Document is not currently archived.");
        public static readonly Error AlreadyCanceled = Error.Conflict("Correspondence.AlreadyCanceled", "Document is already canceled.");
        public static readonly Error CannotCancelArchived = Error.Conflict("Correspondence.CannotCancelArchived", "Archived documents must be unarchived before cancellation.");
        public static readonly Error CancelReasonEmpty = Error.Validation("Correspondence.CancelReasonEmpty", "A cancellation reason is required.");
        public static readonly Error DocumentNotFound = Error.NotFound("Correspondence.DocumentNotFound", "The document was not found.");
        public static readonly Error SingleExternalPartyLimit = Error.Conflict("Correspondence.SingleExternalPartyLimit", "A document can have at most one ExternalOrganization participant.");
        public static readonly Error ParticipantRoleEmpty = Error.Validation("Correspondence.ParticipantRoleEmpty", "Participant role code is required.");
        public static readonly Error ParticipantTargetInvalid = Error.Validation("Correspondence.ParticipantTargetInvalid", "Participant target must specify exactly one External organization, Internal department, or Person name.");
        public static readonly Error ExternalOrgCodeEmpty = Error.Validation("Correspondence.ExternalOrgCodeEmpty", "External organization code is required.");
        public static readonly Error ExternalOrgNameEmpty = Error.Validation("Correspondence.ExternalOrgNameEmpty", "External organization Arabic name is required.");
        public static readonly Error ExternalOrgCodeTooLong = Error.Validation("Correspondence.ExternalOrgCodeTooLong", "External organization code cannot exceed 50 characters.");
        public static readonly Error ExternalOrgCodeAlreadyExists = Error.Conflict("Correspondence.ExternalOrgCodeAlreadyExists", "An external organization with this code already exists.");
        public static readonly Error ExternalOrgNotFound = Error.NotFound("Correspondence.ExternalOrgNotFound", "The external organization was not found.");
        public static readonly Error ExternalOrgContactInvalid = Error.Validation("Correspondence.ExternalOrgContactInvalid", "Contact information must be a valid e-mail address or phone number.");
        public static readonly Error ParticipantRoleNotFound = Error.Validation("Correspondence.ParticipantRoleNotFound", "The specified participant role does not exist.");
        public static readonly Error ParticipantDepartmentNotFound = Error.Validation("Correspondence.ParticipantDepartmentNotFound", "The specified internal participant department does not exist.");
        public static readonly Error DocumentHasActiveNumber = Error.Conflict("Correspondence.DocumentHasActiveNumber", "Cancel or replace the existing official number before issuing a new one.");
    }

    public static class Numbering
    {
        public static readonly Error OfficialNumberEmpty = Error.Validation("Numbering.OfficialNumberEmpty", "Official number text is required.");
        public static readonly Error SequenceInvalid = Error.Validation("Numbering.SequenceInvalid", "Sequence number must be greater than zero.");
        public static readonly Error YearInvalid = Error.Validation("Numbering.YearInvalid", "Numbering year must be between 1900 and 2200.");
        public static readonly Error AlreadyCancelled = Error.Conflict("Numbering.AlreadyCancelled", "Official number is already cancelled.");
        public static readonly Error CancelReasonRequired = Error.Validation("Numbering.CancelReasonRequired", "A reason is required to cancel or replace an official number.");
        public static readonly Error ReplacementIdInvalid = Error.Validation("Numbering.ReplacementIdInvalid", "A distinct replacement number ID is required.");
        public static readonly Error RegistryCodeEmpty = Error.Validation("Numbering.RegistryCodeEmpty", "Numbering registry code is required.");
        public static readonly Error RegistryInactive = Error.Conflict("Numbering.RegistryInactive", "Cannot allocate numbers from an inactive numbering registry.");
        public static readonly Error OfficialNumberTaken = Error.Conflict("Numbering.OfficialNumberTaken", "The official number is already taken for the specified numbering year.");
        public static readonly Error RegistryNotFound = Error.NotFound("Numbering.RegistryNotFound", "The numbering registry was not found.");
        public static readonly Error RegistryCodeAlreadyExists = Error.Conflict("Numbering.RegistryCodeAlreadyExists", "A numbering registry with this code already exists.");
        public static readonly Error RegistryTemplateLocked = Error.Conflict("Numbering.RegistryTemplateLocked", "The registry format cannot be changed after numbers have been issued from it.");
        public static readonly Error NumberNotFound = Error.NotFound("Numbering.NumberNotFound", "The official number was not found for this document.");
        public static readonly Error NumberNotActive = Error.Conflict("Numbering.NumberNotActive", "Only an active official number can be cancelled or replaced.");
        public static readonly Error ReplacementNumberNotFound = Error.Validation("Numbering.ReplacementNumberNotFound", "The replacement number was not found for this document.");
        public static readonly Error DocumentAlreadyNumbered = Error.Conflict("Numbering.DocumentAlreadyNumbered", "The document already has an active official number.");
    }

    public static class Movements
    {
        public static readonly Error SameSourceAndDestination = Error.Validation("Movements.SameSourceAndDestination", "Source and destination departments cannot be identical unless an assignee user is specified.");
        public static readonly Error InvalidStatusTransition = Error.Conflict("Movements.InvalidStatusTransition", "The requested movement workflow transition is invalid for the current status.");
        public static readonly Error NoActiveStep = Error.NotFound("Movements.NoActiveStep", "No active movement step was found for the current step number.");
        public static readonly Error ReasonRequired = Error.Validation("Movements.ReasonRequired", "A reason or note is required for rejecting, returning, or cancelling a movement.");
        public static readonly Error AlreadyTerminal = Error.Conflict("Movements.AlreadyTerminal", "Cannot modify a movement that is already CLOSED or CANCELED.");
    }

    public static class Attachments
    {
        public static readonly Error TitleEmpty = Error.Validation("Attachments.TitleEmpty", "Document file title is required.");
        public static readonly Error FileTypeEmpty = Error.Validation("Attachments.FileTypeEmpty", "Document file type code is required.");
        public static readonly Error FileArchived = Error.Conflict("Attachments.FileArchived", "Cannot upload a new version to an archived document file.");
        public static readonly Error InvalidVersionNumber = Error.Validation("Attachments.InvalidVersionNumber", "Version number must be positive and strictly sequential.");
        public static readonly Error StoragePathEmpty = Error.Validation("Attachments.StoragePathEmpty", "Relative storage path is required.");
        public static readonly Error ChecksumInvalid = Error.Validation("Attachments.ChecksumInvalid", "SHA-256 checksum must be exactly 64 hex characters.");
        public static readonly Error FileSizeInvalid = Error.Validation("Attachments.FileSizeInvalid", "File size in bytes must be greater than zero.");
        public static readonly Error ProviderNameEmpty = Error.Validation("Attachments.ProviderNameEmpty", "Storage provider name is required.");
        public static readonly Error ProviderPathEmpty = Error.Validation("Attachments.ProviderPathEmpty", "Storage provider base path or endpoint is required.");
    }

    public static class Relations
    {
        public static readonly Error SelfRelation = Error.Validation("Relations.SelfRelation", "A document cannot be linked to itself.");
        public static readonly Error RelationTypeEmpty = Error.Validation("Relations.RelationTypeEmpty", "Relation type code is required.");
        public static readonly Error RelationTypeInactive = Error.Conflict("Relations.RelationTypeInactive", "Cannot create a link using an inactive relation type.");
        public static readonly Error CycleDetected = Error.Conflict("Relations.CycleDetected", "Linking these documents would create a cycle in a cycle-restricted relation type.");
        public static readonly Error DuplicateRelation = Error.Conflict("Relations.DuplicateRelation", "This document relation already exists.");
    }

    public static class Archiving
    {
        public static readonly Error EventTypeInvalid = Error.Validation("Archiving.EventTypeInvalid", "Archive event type must be ARCHIVED, UNARCHIVED, or RETAINED.");
        public static readonly Error SearchNameEmpty = Error.Validation("Archiving.SearchNameEmpty", "Saved search name is required.");
        public static readonly Error CriteriaJsonEmpty = Error.Validation("Archiving.CriteriaJsonEmpty", "Saved search criteria JSON is required.");
    }

    public static class Audit
    {
        public static readonly Error HashMismatch = Error.Conflict("Audit.HashMismatch", "Computed SHA-256 event hash does not match the recorded hash.");
        public static readonly Error ChainBroken = Error.Conflict("Audit.ChainBroken", "Audit log hash chain link to previous event is broken.");
        public static readonly Error RequiredFieldsMissing = Error.Validation("Audit.RequiredFieldsMissing", "Audit log module, action, entity type, and hashes are required.");
        public static readonly Error InvalidCheckpointRange = Error.Validation("Audit.InvalidCheckpointRange", "Checkpoint ToLogId must be greater than or equal to FromLogId.");
    }

    public static class Notifications
    {
        public static readonly Error TypeCodeEmpty = Error.Validation("Notifications.TypeCodeEmpty", "Notification type code is required.");
        public static readonly Error TitleEmpty = Error.Validation("Notifications.TitleEmpty", "Notification title is required.");
        public static readonly Error BodyEmpty = Error.Validation("Notifications.BodyEmpty", "Notification body is required.");
        public static readonly Error RecipientDuplicate = Error.Conflict("Notifications.RecipientDuplicate", "User is already added as a recipient for this notification.");
        public static readonly Error ChannelEmpty = Error.Validation("Notifications.ChannelEmpty", "Notification delivery channel is required.");
    }
}
