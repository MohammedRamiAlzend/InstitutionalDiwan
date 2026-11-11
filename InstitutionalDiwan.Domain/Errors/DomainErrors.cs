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

        public static Error DefaultMustBeActive(string lookup, int id) =>
            Error.Conflict("Lookup.DefaultMustBeActive", $"Lookup entry {id} in the '{lookup}' catalog is inactive and cannot be the default.");

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
        public static readonly Error ArchiveDiwanProtected = Error.Conflict("Department.ArchiveDiwanProtected", "Cannot deactivate the designated archive diwan department. Choose another archive diwan in institution settings first.");
        public static readonly Error CentralDiwanProtected =Error.Conflict("Department.CentralDiwanProtected", "Cannot reparent or deactivate the designated Central Diwan department. Transfer the Central Diwan through institution settings first.");
        public static readonly Error NotFound = Error.NotFound("Department.NotFound", "The department was not found.");
        public static readonly Error CodeAlreadyExists = Error.Conflict("Department.CodeAlreadyExists", "A department with this code already exists.");
        public static readonly Error TypeNotFound = Error.Validation("Department.TypeNotFound", "The specified department type does not exist.");
        public static readonly Error TypeInactive = Error.Conflict("Department.TypeInactive", "Cannot assign an inactive department type.");
        public static readonly Error ParentNotFound = Error.Validation("Department.ParentNotFound", "The specified parent department does not exist.");
        public static readonly Error ManagerNotFound = Error.Validation("Department.ManagerNotFound", "The specified manager employee does not exist.");
        public static readonly Error Inactive = Error.Conflict("Department.Inactive", "The department is inactive and cannot take part in a document movement.");
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
        public static readonly Error ArchiveDiwanNotFound = Error.Validation("InstitutionSetting.ArchiveDiwanNotFound", "The designated archive diwan department does not exist.");
        public static readonly Error ArchiveDiwanInactive = Error.Conflict("InstitutionSetting.ArchiveDiwanInactive", "Cannot designate an inactive department as the archive diwan.");
        public static readonly Error ArchiveDiwanSameAsCentral = Error.Conflict("InstitutionSetting.ArchiveDiwanSameAsCentral", "The archive diwan cannot be the Central Diwan department.");
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
        public static readonly Error NotAuthenticated = Error.Unauthorized("Identity.NotAuthenticated", "No authenticated caller is attached to the current request.");
        public static readonly Error UserInactive = Error.Forbidden("Identity.UserInactive", "The user account is inactive.");
        public static readonly Error UsernameTaken = Error.Conflict("Identity.UsernameTaken", "The username is already taken.");
        public static readonly Error RoleNotFound = Error.NotFound("Identity.RoleNotFound", "The specified role was not found.");
        public static readonly Error RoleInactive = Error.Conflict("Identity.RoleInactive", "Cannot assign an inactive role.");
        public static readonly Error PasswordChangeRequired = Error.Forbidden("Identity.PasswordChangeRequired", "Change your password before accessing this endpoint.");
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

    public static class Persons
    {
        public static readonly Error Empty = Error.Validation("Persons.Empty", "A person needs at least a name or a national number.");
        public static readonly Error NotFound = Error.NotFound("Persons.NotFound", "The person was not found.");
        public static readonly Error ReferenceInvalid = Error.Validation("Persons.ReferenceInvalid", "A person entry must give either an existing person id or a full name, not both.");
        public static readonly Error NationalNumberTaken = Error.Conflict("Persons.NationalNumberTaken", "Another person already has this national number.");
        public static readonly Error DuplicateInDocument = Error.Conflict("Persons.DuplicateInDocument", "This person is already named on the document.");
        public static readonly Error DocumentCanceled = Error.Conflict("Persons.DocumentCanceled", "The people named on a canceled document cannot be changed.");
        public static readonly Error LinkNotFound = Error.NotFound("Persons.LinkNotFound", "This person is not named on the document.");
        public static readonly Error RoleNotFound = Error.Validation("Persons.RoleNotFound", "The specified person role does not exist or is inactive.");
        public static readonly Error DefaultRoleMissing = Error.Conflict("Persons.DefaultRoleMissing", "No default person role is active; choose a role or set a default in the lookups.");
        public static readonly Error GenderNotFound = Error.Validation("Persons.GenderNotFound", "The specified gender does not exist or is inactive.");
        public static readonly Error NationalityNotFound = Error.Validation("Persons.NationalityNotFound", "The specified nationality does not exist or is inactive.");
        public static readonly Error BirthDateInvalid = Error.Validation("Persons.BirthDateInvalid", "The birth date is outside the accepted range.");
        public static readonly Error ValueTooLong = Error.Validation("Persons.ValueTooLong", "A person value exceeds its maximum length.");
    }

    public static class Correspondence
    {
        public static readonly Error TitleEmpty = Error.Validation("Correspondence.TitleEmpty", "Document title is required.");
        public static readonly Error DisplayNameEmpty = Error.Validation("Correspondence.DisplayNameEmpty", "A document needs a name to be listed under.");
        public static readonly Error TitleTooLong = Error.Validation("Correspondence.TitleTooLong", "Document title cannot exceed 500 characters.");
        public static readonly Error TypeCodeEmpty = Error.Validation("Correspondence.TypeCodeEmpty", "Document type code is required.");
        public static readonly Error DocumentTypeNotFound = Error.Validation("Correspondence.DocumentTypeNotFound", "The specified document type does not exist.");
        public static readonly Error DocumentTypeInactive = Error.Conflict("Correspondence.DocumentTypeInactive", "Cannot use an inactive document type.");
        public static readonly Error DocumentCreationRestricted = Error.Forbidden("Correspondence.DocumentCreationRestricted", "Only the Central Diwan can create documents; the institution settings do not allow other departments to.");
        public static readonly Error DirectionInvalid = Error.Validation("Correspondence.DirectionInvalid", "Direction must be INTERNAL, INCOMING, or OUTGOING.");
        public static readonly Error OnlyDraftEditable = Error.Conflict("Correspondence.OnlyDraftEditable", "Only documents in DRAFT status can have their core metadata modified directly.");
        public static readonly Error OnlyDraftRegisterable = Error.Conflict("Correspondence.OnlyDraftRegisterable", "Only documents in DRAFT status can be officially registered.");
        public static readonly Error AlreadyArchived = Error.Conflict("Correspondence.AlreadyArchived", "Document is already archived.");
        public static readonly Error CannotArchiveCanceled = Error.Conflict("Correspondence.CannotArchiveCanceled", "Canceled documents cannot be archived.");
        public static readonly Error HasActiveMovements = Error.Conflict("Correspondence.HasActiveMovements", "Cannot archive or cancel a document while it has active movements.");
        public static readonly Error NotArchived = Error.Conflict("Correspondence.NotArchived", "Document is not currently archived.");
        public static readonly Error OnlyRegisteredArchivable = Error.Conflict("Correspondence.OnlyRegisteredArchivable", "Only documents in REGISTERED status can be archived.");
        public static readonly Error DocumentArchived = Error.Conflict("Correspondence.DocumentArchived", "Archived documents cannot be changed; unarchive the document first.");
        public static readonly Error AlreadyCanceled = Error.Conflict("Correspondence.AlreadyCanceled", "Document is already canceled.");
        public static readonly Error CannotCancelArchived = Error.Conflict("Correspondence.CannotCancelArchived", "Archived documents must be unarchived before cancellation.");
        public static readonly Error CancelReasonEmpty = Error.Validation("Correspondence.CancelReasonEmpty", "A cancellation reason is required.");
        public static readonly Error DocumentNotFound = Error.NotFound("Correspondence.DocumentNotFound", "The document was not found.");
        public static readonly Error DocumentAccessDenied = Error.Forbidden("Correspondence.DocumentAccessDenied", "The caller does not have the required access level on this document.");
        public static readonly Error DepartmentOutOfScope = Error.Forbidden("Correspondence.DepartmentOutOfScope", "The requested department's documents are outside the caller's organizational scope.");
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
        public static readonly Error OnlyOwningDepartmentCanClose = Error.Forbidden("Correspondence.OnlyOwningDepartmentCanClose", "Only the department that created the document can archive, unarchive or cancel it.");
        public static readonly Error OnlyRegisteredRoutable =Error.Conflict("Correspondence.OnlyRegisteredRoutable", "Only documents in REGISTERED status can be routed to another department.");
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
        public static readonly Error RegistryDateInvalid = Error.Validation("Numbering.RegistryDateInvalid", "The register date must be a real date between 1900 and 2200.");
        public static readonly Error DepartmentScopeMismatch = Error.Validation("Numbering.DepartmentScopeMismatch", "This number does not belong to a department register.");
        public static readonly Error DepartmentRegistryNotConfigured = Error.Conflict("Numbering.DepartmentRegistryNotConfigured", "The department has no active numbering registry configured.");
        public static readonly Error DepartmentRegistrationRequired = Error.Conflict("Numbering.DepartmentRegistrationRequired", "Record the department register number and date before receiving the document.");
        public static readonly Error RegistryNumberTaken = Error.Conflict("Numbering.RegistryNumberTaken", "This number is already used in the department register for that year.");
        public static readonly Error VisitDateDuplicate = Error.Conflict("Numbering.VisitDateDuplicate", "This document already has a visit with that date in the department register.");
        public static readonly Error VisitYearMismatch = Error.Validation("Numbering.VisitYearMismatch", "The visit date must be in the same year as the department register number.");
        public static readonly Error DepartmentNumberLocked = Error.Conflict("Numbering.DepartmentNumberLocked", "The document is already registered in this department under a different number.");
        public static readonly Error DocumentNotRegistered = Error.Conflict("Numbering.DocumentNotRegistered", "Issue the central official number before recording a department register entry.");
        public static readonly Error DepartmentRegistrationOwnDepartmentOnly = Error.Forbidden("Numbering.DepartmentRegistrationOwnDepartmentOnly", "A department register number can only be recorded for the caller's own department.");
        public static readonly Error DepartmentRegistryMismatch = Error.Conflict("Numbering.DepartmentRegistryMismatch", "The chosen register is inactive or does not belong to the department.");
        public static readonly Error DepartmentNumberRequired = Error.Validation("Numbering.DepartmentNumberRequired", "The department register number is required.");
    }

    public static class Movements
    {
        public static readonly Error SameSourceAndDestination = Error.Validation("Movements.SameSourceAndDestination", "Source and destination departments cannot be identical unless an assignee user is specified.");
        public static readonly Error InvalidStatusTransition = Error.Conflict("Movements.InvalidStatusTransition", "The requested movement workflow transition is invalid for the current status.");
        public static readonly Error NoActiveStep = Error.NotFound("Movements.NoActiveStep", "No active movement step was found for the current step number.");
        public static readonly Error ReasonRequired = Error.Validation("Movements.ReasonRequired", "A reason or note is required for rejecting, returning, or cancelling a movement.");
        public static readonly Error AlreadyTerminal = Error.Conflict("Movements.AlreadyTerminal", "Cannot modify a movement that is already CLOSED or CANCELED.");
        public static readonly Error MovementNotFound = Error.NotFound("Movements.MovementNotFound", "The document movement was not found.");
        public static readonly Error ActiveMovementExists = Error.Conflict("Movements.ActiveMovementExists", "The document already has an active movement; close or cancel it before routing the document again.");
        public static readonly Error DepartmentOutOfScope = Error.Forbidden("Movements.DepartmentOutOfScope", "The requested department is outside the caller's organizational scope.");
        public static readonly Error CentralDiwanRoutingRequired = Error.Conflict("Movements.CentralDiwanRoutingRequired", "A movement between two non-Central-Diwan departments must be routed through the Central Diwan.");
        public static readonly Error CentralDiwanNotConfigured = Error.Conflict("Movements.CentralDiwanNotConfigured", "The Central Diwan department is not configured in the institution settings.");
        public static readonly Error StepNotCurrent = Error.Conflict("Movements.StepNotCurrent", "The requested movement step is not the movement's current step.");
    }

    public static class Attachments
    {
        public static readonly Error TitleEmpty = Error.Validation("Attachments.TitleEmpty", "Document file title is required.");
        public static readonly Error TitleTooLong = Error.Validation("Attachments.TitleTooLong", "Document file title cannot exceed 300 characters.");
        public static readonly Error FileTypeEmpty = Error.Validation("Attachments.FileTypeEmpty", "Document file type code is required.");
        public static readonly Error FileArchived = Error.Conflict("Attachments.FileArchived", "Cannot upload a new version to an archived document file.");
        public static readonly Error InvalidVersionNumber = Error.Validation("Attachments.InvalidVersionNumber", "Version number must be positive and strictly sequential.");
        public static readonly Error StoragePathEmpty = Error.Validation("Attachments.StoragePathEmpty", "Relative storage path is required.");
        public static readonly Error ChecksumInvalid = Error.Validation("Attachments.ChecksumInvalid", "SHA-256 checksum must be exactly 64 hex characters.");
        public static readonly Error FileSizeInvalid = Error.Validation("Attachments.FileSizeInvalid", "File size in bytes must be greater than zero.");
        public static readonly Error ProviderNameEmpty = Error.Validation("Attachments.ProviderNameEmpty", "Storage provider name is required.");
        public static readonly Error ProviderPathEmpty = Error.Validation("Attachments.ProviderPathEmpty", "Storage provider base path or endpoint is required.");
        public static readonly Error FileNotFound = Error.NotFound("Attachments.FileNotFound", "The document file was not found.");
        public static readonly Error VersionNotFound = Error.NotFound("Attachments.VersionNotFound", "The document file version was not found.");
        public static readonly Error FileTypeNotFound = Error.NotFound("Attachments.FileTypeNotFound", "The specified document file type does not exist.");
        public static readonly Error FileTypeInactive = Error.Conflict("Attachments.FileTypeInactive", "The specified document file type is inactive.");
        public static readonly Error StorageProviderNotFound = Error.NotFound("Attachments.StorageProviderNotFound", "No file storage provider is configured.");
        public static readonly Error StorageProviderInactive = Error.Conflict("Attachments.StorageProviderInactive", "The file storage provider is inactive.");
        public static readonly Error FileEmpty = Error.Validation("Attachments.FileEmpty", "The uploaded file is empty.");
        public static readonly Error FileTooLarge = Error.Validation("Attachments.FileTooLarge", "The uploaded file exceeds the maximum size allowed for its file type.");
        public static readonly Error ExtensionNotAllowed = Error.Validation("Attachments.ExtensionNotAllowed", "The file extension is not allowed for this document file type.");
        public static readonly Error SignatureMismatch = Error.Validation("Attachments.SignatureMismatch", "The file content does not match its extension (magic-byte check failed).");
        public static readonly Error FileAlreadyArchived = Error.Conflict("Attachments.FileAlreadyArchived", "The document file is already archived.");
    }

    public static class Relations
    {
        public static readonly Error SelfRelation = Error.Validation("Relations.SelfRelation", "A document cannot be linked to itself.");
        public static readonly Error RelationTypeEmpty = Error.Validation("Relations.RelationTypeEmpty", "Relation type code is required.");
        public static readonly Error RelationTypeInactive = Error.Conflict("Relations.RelationTypeInactive", "Cannot create a link using an inactive relation type.");
        public static readonly Error CycleDetected = Error.Conflict("Relations.CycleDetected", "Linking these documents would create a cycle in a cycle-restricted relation type.");
        public static readonly Error DuplicateRelation = Error.Conflict("Relations.DuplicateRelation", "This document relation already exists.");
        public static readonly Error RelationNotFound = Error.NotFound("Relations.RelationNotFound", "The document relation was not found.");
        public static readonly Error RelationTypeNotFound = Error.Validation("Relations.RelationTypeNotFound", "The specified relation type does not exist.");
        public static readonly Error ReasonRequired = Error.Validation("Relations.ReasonRequired", "A reason is required to remove a document relation.");
        public static readonly Error ReasonTooLong = Error.Validation("Relations.ReasonTooLong", "A relation removal reason cannot exceed 500 characters.");
        public static readonly Error NotesTooLong = Error.Validation("Relations.NotesTooLong", "Relation notes cannot exceed 500 characters.");
        public static readonly Error DocumentCancelled = Error.Conflict("Relations.DocumentCancelled", "A cancelled document cannot be linked to another document.");
        public static readonly Error GraphDepthInvalid = Error.Validation("Relations.GraphDepthInvalid", "The relations graph depth must be between 1 and 3.");
    }

    public static class SystemErrors
    {
        public static readonly Error NotFound = Error.NotFound("SystemErrors.NotFound", "The captured error was not found.");
        public static readonly Error StatusInvalid = Error.Validation("SystemErrors.StatusInvalid", "The work status is not supported.");
        public static readonly Error NotesTooLong = Error.Validation("SystemErrors.NotesTooLong", "The notes cannot exceed 4000 characters.");
        public static readonly Error PurgeDaysInvalid = Error.Validation("SystemErrors.PurgeDaysInvalid", "The number of days must be between 1 and 3650.");
        public static readonly Error ReportInvalid = Error.Validation("SystemErrors.ReportInvalid", "A client error report needs a browser error kind and a message.");
    }

    public static class Archiving
    {
        public static readonly Error EventTypeInvalid = Error.Validation("Archiving.EventTypeInvalid", "Archive event type must be ARCHIVED, UNARCHIVED, or RETAINED.");
        public static readonly Error SearchNameEmpty = Error.Validation("Archiving.SearchNameEmpty", "Saved search name is required.");
        public static readonly Error CriteriaJsonEmpty = Error.Validation("Archiving.CriteriaJsonEmpty", "Saved search criteria JSON is required.");
        public static readonly Error LocationNotFound = Error.Validation("Archiving.LocationNotFound", "The specified archive location does not exist or is inactive.");
        public static readonly Error UnarchiveReasonEmpty = Error.Validation("Archiving.UnarchiveReasonEmpty", "A reason is required to unarchive a document.");
        public static readonly Error ReasonTooLong = Error.Validation("Archiving.ReasonTooLong", "An archive reason cannot exceed 500 characters.");
        public static readonly Error SearchNotFound = Error.NotFound("Archiving.SearchNotFound", "The saved search was not found.");
        public static readonly Error SearchNameTooLong = Error.Validation("Archiving.SearchNameTooLong", "A saved search name cannot exceed 150 characters.");
        public static readonly Error SearchNameTaken = Error.Conflict("Archiving.SearchNameTaken", "You already have a saved search with this name.");
        public static readonly Error SearchLimitReached = Error.Conflict("Archiving.SearchLimitReached", "You have reached the maximum of 50 saved searches.");
        public static readonly Error CriteriaInvalid = Error.Validation("Archiving.CriteriaInvalid", "The saved search criteria are not valid or exceed 4000 characters.");
        public static readonly Error ArchiveDiwanNotConfigured = Error.Conflict("Archiving.ArchiveDiwanNotConfigured", "No archive diwan is designated in the institution settings, so a document cannot be sent for archiving.");
        public static readonly Error ArchiveRouteViaOwningDepartment = Error.Conflict("Archiving.ArchiveRouteViaOwningDepartment", "A document sent for archiving must go to the department that created it; only that department sends it to the archive diwan.");
        public static readonly Error OnlyOwningDepartmentSendsToArchive = Error.Conflict("Archiving.OnlyOwningDepartmentSendsToArchive", "Only the department that created the document can send it to the archive diwan.");
        public static readonly Error ArchiveDiwanAcceptsArchiveMovementsOnly = Error.Conflict("Archiving.ArchiveDiwanAcceptsArchiveMovementsOnly", "The archive diwan only receives documents sent for archiving.");
        public static readonly Error DocumentNotAtArchiveDiwan = Error.Conflict("Archiving.DocumentNotAtArchiveDiwan", "The document must be received by the archive diwan before it can be archived.");
        public static readonly Error OnlyArchiveDiwanCanArchive = Error.Forbidden("Archiving.OnlyArchiveDiwanCanArchive", "Only the archive diwan can archive or unarchive a document it holds.");
    }

    public static class Audit
    {
        public static readonly Error HashMismatch = Error.Conflict("Audit.HashMismatch", "Computed SHA-256 event hash does not match the recorded hash.");
        public static readonly Error ChainBroken = Error.Conflict("Audit.ChainBroken", "Audit log hash chain link to previous event is broken.");
        public static readonly Error RequiredFieldsMissing = Error.Validation("Audit.RequiredFieldsMissing", "Audit log module, action, entity type, and hashes are required.");
        public static readonly Error InvalidCheckpointRange = Error.Validation("Audit.InvalidCheckpointRange", "Checkpoint ToLogId must be greater than or equal to FromLogId.");
        public static readonly Error NoAuditLogs = Error.NotFound("Audit.NoAuditLogs", "No audit log entries are available.");
        public static readonly Error NothingToCheckpoint = Error.Conflict("Audit.NothingToCheckpoint", "No new audit log entries have been recorded since the last checkpoint.");
        public static readonly Error InvalidIntegrityRange = Error.Validation("Audit.InvalidIntegrityRange", "The audit integrity range is invalid.");
        public static readonly Error PreviousLogNotFound = Error.NotFound("Audit.PreviousLogNotFound", "The audit log entry immediately before the requested range was not found.");
    }

    public static class Notifications
    {
        public static readonly Error TypeCodeEmpty = Error.Validation("Notifications.TypeCodeEmpty", "Notification type code is required.");
        public static readonly Error TitleEmpty = Error.Validation("Notifications.TitleEmpty", "Notification title is required.");
        public static readonly Error BodyEmpty = Error.Validation("Notifications.BodyEmpty", "Notification body is required.");
        public static readonly Error RecipientDuplicate = Error.Conflict("Notifications.RecipientDuplicate", "User is already added as a recipient for this notification.");
        public static readonly Error ChannelEmpty = Error.Validation("Notifications.ChannelEmpty", "Notification delivery channel is required.");
        public static readonly Error TitleTooLong = Error.Validation("Notifications.TitleTooLong", "A notification title cannot exceed 300 characters.");
        public static readonly Error BodyTooLong = Error.Validation("Notifications.BodyTooLong", "A notification body cannot exceed 2000 characters.");
        public static readonly Error ActionUrlInvalid = Error.Validation("Notifications.ActionUrlInvalid", "A notification action URL must be a relative application path of at most 500 characters.");
        public static readonly Error PayloadTooLong = Error.Validation("Notifications.PayloadTooLong", "A notification outbox payload cannot exceed 4000 characters.");
        public static readonly Error TooManyRecipients = Error.Conflict("Notifications.TooManyRecipients", "A notification cannot have more than 500 recipients.");
        public static readonly Error RecipientNotFound = Error.NotFound("Notifications.RecipientNotFound", "The notification was not found.");
    }

    public static class DocumentFields
    {
        public static readonly Error KeyInvalid = Error.Validation("DocumentFields.KeyInvalid", "Field key must start with a lowercase letter and contain only lowercase letters, digits, and underscores (max 50 characters).");
        public static readonly Error KeyAlreadyExists = Error.Conflict("DocumentFields.KeyAlreadyExists", "A field with this key already exists for the document type.");
        public static readonly Error FieldNotFound = Error.NotFound("DocumentFields.FieldNotFound", "The document field was not found.");
        public static readonly Error DataTypeInvalid = Error.Validation("DocumentFields.DataTypeInvalid", "The field data type is not supported.");
        public static readonly Error DocumentTypeInvalid = Error.Validation("DocumentFields.DocumentTypeInvalid", "A valid document type is required.");
        public static readonly Error LabelEmpty = Error.Validation("DocumentFields.LabelEmpty", "The Arabic field label is required.");
        public static readonly Error LabelTooLong = Error.Validation("DocumentFields.LabelTooLong", "The Arabic field label cannot exceed 200 characters.");
        public static readonly Error RegexNotAllowed = Error.Validation("DocumentFields.RegexNotAllowed", "A pattern can only be set on text fields.");
        public static readonly Error RegexInvalid = Error.Validation("DocumentFields.RegexInvalid", "The pattern is not a valid regular expression.");
        public static readonly Error RegexTooLong = Error.Validation("DocumentFields.RegexTooLong", "The pattern cannot exceed 500 characters.");
        public static readonly Error RegexMessageRequired = Error.Validation("DocumentFields.RegexMessageRequired", "An Arabic error message is required when a pattern is set.");
        public static readonly Error RegexMessageTooLong = Error.Validation("DocumentFields.RegexMessageTooLong", "The pattern error message cannot exceed 500 characters.");
        public static readonly Error RegexTimedOut = Error.Validation("DocumentFields.RegexTimedOut", "Evaluating the field pattern took too long.");
        public static readonly Error MaxLengthNotAllowed = Error.Validation("DocumentFields.MaxLengthNotAllowed", "A maximum length can only be set on text fields.");
        public static readonly Error MaxLengthInvalid = Error.Validation("DocumentFields.MaxLengthInvalid", "The maximum length must be between 1 and the limit of the field type (500 for text, 4000 for long text).");
        public static readonly Error NumberRangeNotAllowed = Error.Validation("DocumentFields.NumberRangeNotAllowed", "A minimum or maximum can only be set on number fields.");
        public static readonly Error NumberRangeInvalid = Error.Validation("DocumentFields.NumberRangeInvalid", "The minimum cannot be greater than the maximum.");
        public static readonly Error OptionsRequired = Error.Validation("DocumentFields.OptionsRequired", "A select field requires at least one option.");
        public static readonly Error OptionsNotAllowed = Error.Validation("DocumentFields.OptionsNotAllowed", "Options can only be set on select fields.");
        public static readonly Error OptionInvalid = Error.Validation("DocumentFields.OptionInvalid", "Every option needs a key (lowercase letters, digits, underscores) and an Arabic label.");
        public static readonly Error OptionKeyDuplicate = Error.Validation("DocumentFields.OptionKeyDuplicate", "Option keys must be unique within a field.");
        public static readonly Error TooManyOptions = Error.Validation("DocumentFields.TooManyOptions", "A select field cannot have more than 100 options.");
        public static readonly Error SchemaVersionNotFound = Error.NotFound("DocumentFields.SchemaVersionNotFound", "The schema version was not found.");
        public static readonly Error SchemaVersionNotDraft = Error.Conflict("DocumentFields.SchemaVersionNotDraft", "Only a schema version in DRAFT status can be changed.");
        public static readonly Error SchemaVersionAlreadyHasDraft = Error.Conflict("DocumentFields.SchemaVersionAlreadyHasDraft", "This document type already has a draft schema version.");
        public static readonly Error SchemaVersionNotPublished = Error.Conflict("DocumentFields.SchemaVersionNotPublished", "Only a published schema version can be retired.");
        public static readonly Error SchemaVersionMismatch = Error.Conflict("DocumentFields.SchemaVersionMismatch", "The schema version does not belong to this document.");
        public static readonly Error FieldAlreadyInVersion = Error.Conflict("DocumentFields.FieldAlreadyInVersion", "The field is already part of this schema version.");
        public static readonly Error FieldNotInVersion = Error.NotFound("DocumentFields.FieldNotInVersion", "The field is not part of this schema version.");
        public static readonly Error FieldTypeMismatch = Error.Validation("DocumentFields.FieldTypeMismatch", "The settings data type does not match the field data type, and a field data type cannot be changed.");
        public static readonly Error FieldDocumentTypeMismatch = Error.Validation("DocumentFields.FieldDocumentTypeMismatch", "The field belongs to a different document type than the schema version.");
        public static readonly Error FilterRequiresDocumentType = Error.Validation("DocumentFields.FilterRequiresDocumentType", "Filtering by field values requires a document type.");
        public static readonly Error FilterMalformed = Error.Validation("DocumentFields.FilterMalformed", "A field filter must look like key:operator:value with a supported operator (eq, contains, gt, gte, lt, lte).");
        public static readonly Error FilterFieldNotFound = Error.Validation("DocumentFields.FilterFieldNotFound", "A field filter names a field that does not exist for this document type.");
        public static readonly Error FilterOperatorNotSupported = Error.Validation("DocumentFields.FilterOperatorNotSupported", "The operator is not supported for this field data type.");
        public static readonly Error FilterValueInvalid = Error.Validation("DocumentFields.FilterValueInvalid", "The filter value does not match the field data type.");
        public static readonly Error TooManyFilters = Error.Validation("DocumentFields.TooManyFilters", "A registry query accepts at most 5 field filters.");
        public static readonly Error TooManyFields = Error.Conflict("DocumentFields.TooManyFields", "A schema version cannot have more than 50 fields.");
        public static readonly Error DuplicateValue = Error.Validation("DocumentFields.DuplicateValue", "A field value was supplied more than once.");
        public static readonly Error BehaviorTextTooLong = Error.Validation("DocumentFields.BehaviorTextTooLong", "The placeholder (200), help text (500) or section title (100) is too long.");
        public static readonly Error MinLengthNotAllowed = Error.Validation("DocumentFields.MinLengthNotAllowed", "A minimum length can only be set on text fields.");
        public static readonly Error MinLengthInvalid = Error.Validation("DocumentFields.MinLengthInvalid", "The minimum length must be at least 1 and not exceed the maximum length.");
        public static readonly Error DateRangeNotAllowed = Error.Validation("DocumentFields.DateRangeNotAllowed", "A date range can only be set on date fields.");
        public static readonly Error DateRangeInvalid = Error.Validation("DocumentFields.DateRangeInvalid", "A date limit must be a valid date or @today, and the earliest cannot be after the latest.");
        public static readonly Error DefaultValueInvalid = Error.Validation("DocumentFields.DefaultValueInvalid", "The default value does not satisfy the field's own rules, or the field takes its options from a source.");
        public static readonly Error ReadOnlyRequiresDefault = Error.Validation("DocumentFields.ReadOnlyRequiresDefault", "A read-only field needs a default value.");
        public static readonly Error OptionSourceNotAllowed = Error.Validation("DocumentFields.OptionSourceNotAllowed", "An option source can only be set on select and multi-select fields.");
        public static readonly Error OptionSourceInvalid = Error.Validation("DocumentFields.OptionSourceInvalid", "The option source is not supported or its lookup catalog is unknown.");
        public static readonly Error ConditionIncomplete = Error.Validation("DocumentFields.ConditionIncomplete", "A visibility condition needs both a field and a value.");
        public static readonly Error ConditionFieldInvalid = Error.Validation("DocumentFields.ConditionFieldInvalid", "A visibility condition must name another boolean, select or multi-select field of this version.");
        public static readonly Error ConditionValueInvalid = Error.Validation("DocumentFields.ConditionValueInvalid", "The visibility condition value is not a value of the field it depends on.");
        public static readonly Error ConditionCycle = Error.Validation("DocumentFields.ConditionCycle", "Visibility conditions cannot depend on each other in a circle.");
        public static readonly Error FieldHasDependents = Error.Conflict("DocumentFields.FieldHasDependents", "Another field's visibility depends on this field.");

        public static Error FieldReadOnly(string key) =>
            Error.Validation("DocumentFields.FieldReadOnly", $"The field '{key}' is read-only and keeps its fixed value.");

        public static Error ValueTooShort(string key, int minLength) =>
            Error.Validation("DocumentFields.ValueTooShort", $"The value of field '{key}' must be at least {minLength} characters.");

        public static Error ValueDateOutOfRange(string key) =>
            Error.Validation("DocumentFields.ValueDateOutOfRange", $"The value of field '{key}' is outside its allowed dates.");

        public static Error TooManySelections(string key) =>
            Error.Validation("DocumentFields.TooManySelections", $"The value of field '{key}' holds too many choices.");

        public static Error UnknownField(string key) =>
            Error.Validation("DocumentFields.UnknownField", $"The field '{key}' is not part of the document's schema version.");

        public static Error ValueRequired(string key) =>
            Error.Validation("DocumentFields.ValueRequired", $"The field '{key}' is required.");

        public static Error ValueTypeInvalid(string key) =>
            Error.Validation("DocumentFields.ValueTypeInvalid", $"The value of field '{key}' does not match its data type.");

        public static Error ValueTooLong(string key, int maxLength) =>
            Error.Validation("DocumentFields.ValueTooLong", $"The value of field '{key}' cannot exceed {maxLength} characters.");

        public static Error ValueBelowMinimum(string key, decimal minimum) =>
            Error.Validation("DocumentFields.ValueBelowMinimum", $"The value of field '{key}' cannot be less than {minimum}.");

        public static Error ValueAboveMaximum(string key, decimal maximum) =>
            Error.Validation("DocumentFields.ValueAboveMaximum", $"The value of field '{key}' cannot be greater than {maximum}.");

        public static Error ValueOptionInvalid(string key) =>
            Error.Validation("DocumentFields.ValueOptionInvalid", $"The value of field '{key}' is not one of its options.");

        /// <summary>The detail is the administrator-supplied Arabic message of the field's pattern.</summary>
        public static Error ValuePatternMismatch(string messageAr) =>
            Error.Validation("DocumentFields.ValuePatternMismatch", messageAr);
    }

    /// <summary>The record a department keeps on each visit of a document (its record form values).</summary>
    public static class DepartmentRecords
    {
        public static readonly Error VisitNotFound = Error.NotFound("DepartmentRecords.VisitNotFound", "The register visit was not found.");
        public static readonly Error VisitLocked = Error.Conflict("DepartmentRecords.VisitLocked", "The document has left the department, so this record is locked; only an administrator can correct it.");
        public static readonly Error VisitNotLocked = Error.Conflict("DepartmentRecords.VisitNotLocked", "The record is still open; edit it directly instead of correcting it.");
        public static readonly Error NotOwnerDepartment = Error.Forbidden("DepartmentRecords.NotOwnerDepartment", "Only the department that keeps this record can edit it.");
        public static readonly Error SchemaNotFound = Error.Conflict("DepartmentRecords.SchemaNotFound", "This visit has no department record form.");
        public static readonly Error FormNotFound = Error.NotFound("DepartmentRecords.FormNotFound", "The department has no published record form with that id.");
        public static readonly Error CorrectionReasonRequired = Error.Validation("DepartmentRecords.CorrectionReasonRequired", "A reason is required to correct a locked record.");
        public static readonly Error CorrectionReasonTooLong = Error.Validation("DepartmentRecords.CorrectionReasonTooLong", "The correction reason cannot exceed 1000 characters.");
    }

    /// <summary>Decision types and the decisions issued on documents.</summary>
    public static class Decisions
    {
        public static readonly Error NameEmpty = Error.Validation("Decisions.NameEmpty", "A decision type needs an Arabic name and an English name.");
        public static readonly Error NameTooLong = Error.Validation("Decisions.NameTooLong", "A decision type name cannot exceed 200 characters.");
        public static readonly Error EffectInvalid = Error.Validation("Decisions.EffectInvalid", "The decision effect is not one of the known effects.");
        public static readonly Error TypeNotFound = Error.NotFound("Decisions.TypeNotFound", "The decision type was not found.");
        public static readonly Error TypeInactive = Error.Conflict("Decisions.TypeInactive", "The decision type is inactive and cannot be issued.");
        public static readonly Error TypeNotAllowedForDocument = Error.Conflict("Decisions.TypeNotAllowedForDocument", "This decision type is not offered for the document's type.");
        public static readonly Error TypeDestinationRequired = Error.Conflict("Decisions.TypeDestinationRequired", "A referral decision type needs a destination department before it can be activated.");
        public static readonly Error TypeDestinationNotAllowed = Error.Validation("Decisions.TypeDestinationNotAllowed", "Only a referral decision type may name a destination department.");
        public static readonly Error TypeInUse = Error.Conflict("Decisions.TypeInUse", "A decision type that has been used is deactivated, not deleted.");
        public static readonly Error NotHoldingDepartment = Error.Forbidden("Decisions.NotHoldingDepartment", "A decision can be issued only by the department that currently holds the document.");
        public static readonly Error ActiveDecisionExists = Error.Conflict("Decisions.ActiveDecisionExists", "The document already has a decision in force. Revoke or cancel it before issuing another.");
        public static readonly Error DecisionNumberEmpty = Error.Validation("Decisions.DecisionNumberEmpty", "The decision number is required.");
        public static readonly Error DecisionNumberTooLong = Error.Validation("Decisions.DecisionNumberTooLong", "The decision number cannot exceed 100 characters.");
        public static readonly Error DecisionNumberTaken = Error.Conflict("Decisions.DecisionNumberTaken", "This decision number is already used by the issuing department in that year.");
        public static readonly Error DecisionDateInvalid = Error.Validation("Decisions.DecisionDateInvalid", "The decision date is not a valid year.");
        public static readonly Error SummaryTooLong = Error.Validation("Decisions.SummaryTooLong", "The decision summary cannot exceed 2000 characters.");
        public static readonly Error DecisionNotFound = Error.NotFound("Decisions.DecisionNotFound", "The decision was not found.");
        public static readonly Error DecisionFileInvalid = Error.Validation("Decisions.DecisionFileInvalid", "The file must be a decision copy attached to the same document.");
        public static readonly Error CancelReasonRequired = Error.Validation("Decisions.CancelReasonRequired", "A reason is required to cancel or revoke a decision.");
        public static readonly Error ReasonTooLong = Error.Validation("Decisions.ReasonTooLong", "The reason cannot exceed 1000 characters.");
        public static readonly Error NotSystemAdmin = Error.Forbidden("Decisions.NotSystemAdmin", "Only a system administrator can cancel a decision that was entered by mistake.");
        public static readonly Error NotAppealable = Error.Conflict("Decisions.NotAppealable", "This decision cannot be appealed.");
        public static readonly Error AppealAlreadyPending = Error.Conflict("Decisions.AppealAlreadyPending", "An appeal is already pending on this decision.");
        public static readonly Error AppealOutcomeInvalid = Error.Validation("Decisions.AppealOutcomeInvalid", "The appeal outcome must be accepted or rejected.");
        public static readonly Error AppealNotFound = Error.NotFound("Decisions.AppealNotFound", "The appeal was not found.");
        public static readonly Error AppealOutOfScope = Error.Forbidden("Decisions.AppealOutOfScope", "An appeal can be recorded only when the caller's scope covers the issuing department or the appellant department.");
        public static readonly Error AppealDateInvalid = Error.Validation("Decisions.AppealDateInvalid", "The appeal date or the outcome date is not a valid year.");
        public static readonly Error AppealReferenceTooLong = Error.Validation("Decisions.AppealReferenceTooLong", "The appeal reference cannot exceed 100 characters.");
        public static readonly Error AppealNotesTooLong = Error.Validation("Decisions.AppealNotesTooLong", "The appeal notes cannot exceed 2000 characters.");
        public static readonly Error DocumentJoined = Error.Conflict("Decisions.DocumentJoined", "This file was joined into another and cannot take a new separating decision.");
        public static readonly Error JoinTargetInvalid = Error.Conflict("Decisions.JoinTargetInvalid", "The principal file cannot be used: it is missing, is this same file, is archived or canceled, is itself joined into another file, cannot be viewed, or the link would create a cycle. A decision that does not unify files cannot name a principal file.");
    }
}
