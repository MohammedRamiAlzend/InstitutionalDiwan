namespace InstitutionalDiwan.Domain.Events;

// Module 01: Organization
public sealed record DepartmentCreatedEvent(Guid DepartmentId, string Code, string NameAr, Guid? ParentDepartmentId) : DomainEventBase;
public sealed record DepartmentMovedEvent(Guid DepartmentId, Guid? OldParentId, Guid? NewParentId) : DomainEventBase;
public sealed record DepartmentDeactivatedEvent(Guid DepartmentId) : DomainEventBase;

// Module 02: Identity & Access
public sealed record UserCreatedEvent(Guid UserId, string Username, Guid PrimaryDepartmentId) : DomainEventBase;
public sealed record UserLoggedInEvent(Guid UserId, string Username) : DomainEventBase;
public sealed record UserDeactivatedEvent(Guid UserId) : DomainEventBase;
public sealed record UserPasswordResetEvent(Guid UserId, Guid? ResetByUserId) : DomainEventBase;
public sealed record UserRoleAssignedEvent(Guid UserId, Guid RoleId, Guid? AssignedByUserId) : DomainEventBase;
public sealed record UserRoleRevokedEvent(Guid UserId, Guid RoleId) : DomainEventBase;
public sealed record DocumentAccessGrantedEvent(Guid GrantId, Guid DocumentId, Guid UserId, int AccessLevelId) : DomainEventBase;
public sealed record DocumentAccessRevokedEvent(Guid GrantId, Guid DocumentId, Guid UserId) : DomainEventBase;

// Module 03 & 07: Correspondence & Archiving
public sealed record DocumentCreatedEvent(Guid DocumentId, int DocumentTypeId, int? DirectionId, Guid OwningDepartmentId, Guid CreatedByUserId) : DomainEventBase;
public sealed record DocumentRegisteredEvent(Guid DocumentId, Guid OfficialNumberId, string OfficialNumberText, DateTime RegistrationDate) : DomainEventBase;
public sealed record DocumentArchivedEvent(Guid DocumentId, int PreviousStatusId, Guid ActorUserId, string? Reason, int ArchiveLocationId) : DomainEventBase;
public sealed record DocumentUnarchivedEvent(Guid DocumentId, int RestoredStatusId, Guid ActorUserId, string? Reason) : DomainEventBase;
public sealed record DocumentCanceledEvent(Guid DocumentId, int PreviousStatusId, Guid ActorUserId, string Reason) : DomainEventBase;
public sealed record DocumentParticipantAddedEvent(Guid DocumentId, Guid ParticipantId, int RoleId) : DomainEventBase;
public sealed record PersonCreatedEvent(Guid PersonId, Guid ActorUserId) : DomainEventBase;
public sealed record PersonDetailsUpdatedEvent(Guid PersonId, IReadOnlyList<string> ChangedFields, Guid ActorUserId) : DomainEventBase;
public sealed record DocumentPersonLinkedEvent(Guid DocumentId, Guid DocumentPersonId, Guid PersonId, int RoleId, Guid ActorUserId) : DomainEventBase;
public sealed record DocumentPersonRoleChangedEvent(Guid DocumentId, Guid DocumentPersonId, Guid PersonId, int PreviousRoleId, int RoleId, Guid ActorUserId) : DomainEventBase;
public sealed record DocumentPersonUnlinkedEvent(Guid DocumentId, Guid DocumentPersonId, Guid PersonId, int RoleId, Guid ActorUserId) : DomainEventBase;
public sealed record DocumentSchemaVersionPublishedEvent(Guid SchemaVersionId, int DocumentTypeId, int VersionNumber, Guid ActorUserId) : DomainEventBase;

// Module 04: Official Numbering
public sealed record OfficialNumberIssuedEvent(Guid DocumentNumberId, Guid DocumentId, Guid RegistryId, string OfficialNumberText, int NumberingYear, long SequenceValue) : DomainEventBase;
public sealed record OfficialNumberCancelledEvent(Guid DocumentNumberId, Guid DocumentId, string OfficialNumberText, Guid ActorUserId, string Reason) : DomainEventBase;
public sealed record OfficialNumberReplacedEvent(Guid OldDocumentNumberId, Guid NewDocumentNumberId, Guid DocumentId, Guid ActorUserId, string Reason) : DomainEventBase;

// Module 05: Movements & Routing
public sealed record MovementCreatedEvent(Guid MovementId, Guid DocumentId, Guid SourceDepartmentId, Guid DestinationDepartmentId) : DomainEventBase;
public sealed record MovementSentEvent(Guid MovementId, Guid DocumentId, int StepNo, Guid ActorUserId) : DomainEventBase;
public sealed record MovementReceivedEvent(Guid MovementId, Guid DocumentId, int StepNo, Guid ActorUserId) : DomainEventBase;
public sealed record MovementCompletedEvent(Guid MovementId, Guid DocumentId, int StepNo, Guid ActorUserId) : DomainEventBase;
public sealed record MovementRejectedEvent(Guid MovementId, Guid DocumentId, int StepNo, Guid ActorUserId, string Reason) : DomainEventBase;
public sealed record MovementReturnedEvent(Guid MovementId, Guid DocumentId, int StepNo, Guid ActorUserId, string Reason) : DomainEventBase;
public sealed record MovementRedirectedEvent(Guid MovementId, Guid DocumentId, int NewStepNo, Guid NewDestinationDepartmentId, Guid ActorUserId) : DomainEventBase;
public sealed record MovementClosedEvent(Guid MovementId, Guid DocumentId, Guid ActorUserId) : DomainEventBase;
public sealed record MovementCancelledEvent(Guid MovementId, Guid DocumentId, Guid ActorUserId, string Reason) : DomainEventBase;

// Module 06: Attachments
public sealed record DocumentFileCreatedEvent(Guid FileId, Guid DocumentId, int FileTypeId, string Title) : DomainEventBase;
public sealed record DocumentFileVersionUploadedEvent(Guid FileId, Guid VersionId, Guid DocumentId, int VersionNumber, string ChecksumSha256) : DomainEventBase;
public sealed record DocumentFileArchivedEvent(Guid FileId, Guid DocumentId) : DomainEventBase;

// Module 08: Relations
public sealed record DocumentRelationCreatedEvent(Guid RelationId, Guid SourceDocumentId, Guid TargetDocumentId, int RelationTypeId, Guid ActorUserId) : DomainEventBase;
public sealed record DocumentRelationRemovedEvent(Guid RelationId, Guid SourceDocumentId, Guid TargetDocumentId, int RelationTypeId, Guid ActorUserId) : DomainEventBase;

// Decisions
public sealed record DecisionIssuedEvent(Guid DecisionId, Guid DocumentId, Guid IssuingDepartmentId, int EffectId, Guid ActorUserId) : DomainEventBase;
public sealed record DecisionRevokedEvent(Guid DecisionId, Guid DocumentId, int StatusId, Guid ActorUserId, string Reason) : DomainEventBase;
public sealed record DecisionAppealedEvent(Guid DecisionId, Guid AppealId, Guid DocumentId, Guid AppellantDepartmentId, Guid ActorUserId) : DomainEventBase;

// Module 10: Notifications
public sealed record NotificationCreatedEvent(Guid NotificationId, int NotificationTypeId, Guid? RelatedDocumentId) : DomainEventBase;
public sealed record NotificationReadEvent(Guid NotificationId, Guid UserId, DateTime ReadAtUtc) : DomainEventBase;
