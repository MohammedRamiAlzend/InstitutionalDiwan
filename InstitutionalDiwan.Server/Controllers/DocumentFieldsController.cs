using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Dynamic document fields. Administrators define fields per document type and publish them in
/// immutable schema versions (<c>LOOKUP_MANAGE</c>); a document is pinned to the version published
/// when it was created, and its values are read with <c>DOCUMENT_VIEW</c> and edited, while the
/// document is a draft, with <c>DOCUMENT_EDIT</c>.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
public sealed class DocumentFieldsController : ControllerBase
{
    private const string TypeIdDoc = "Document type id (integer, route). Example: 1.";
    private const string VersionIdDoc = "Schema version unique identifier (GUID, route). Example: 3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438.";
    private const string FieldIdDoc = "Document field unique identifier (GUID, route). Example: b2a47e90-6c13-4d58-8f21-0e9d5a7c3b64.";
    private const string DocumentIdDoc = "Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.";

    private readonly ISender _sender;

    public DocumentFieldsController(ISender sender) => _sender = sender;

    /// <summary>Lists every dynamic field defined for a document type.</summary>
    /// <param name="documentTypeId">Document type id (integer, route). Example: <c>1</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The fields, oldest first.</response>
    /// <response code="404">The document type does not exist.</response>
    [HttpGet("document-types/{documentTypeId:int}/fields")]
    [EndpointSummary("Lists every dynamic field defined for a document type.")]
    [EndpointDescription("Lists every dynamic field defined for a document type, whether or not it is in a published schema. Requires the LOOKUP_MANAGE permission. Responses: 200 (The fields, oldest first.), 404 (The document type does not exist.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<IReadOnlyList<DocumentFieldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListFields(
        [Description(TypeIdDoc)] int documentTypeId, CancellationToken cancellationToken)
        => (await _sender.Send(new ListDocumentFieldsQuery(documentTypeId), cancellationToken)).ToActionResult();

    /// <summary>Defines a new dynamic field (key and data type) for a document type.</summary>
    /// <param name="documentTypeId">Document type id (integer, route). Example: <c>1</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>key</term><description>Stable field key, lowercase letters, digits and underscores (string, required). Example: <c>session_no</c>.</description></item>
    /// <item><term>dataType</term><description>1 Text, 2 LongText, 3 Number, 4 Date, 5 Boolean, 6 Select (integer, required). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created field.</response>
    /// <response code="400">The key or data type is invalid.</response>
    /// <response code="404">The document type does not exist.</response>
    /// <response code="409">The key is already used for this document type.</response>
    [HttpPost("document-types/{documentTypeId:int}/fields")]
    [EndpointSummary("Defines a new dynamic field (key and data type) for a document type.")]
    [EndpointDescription("Defines a new dynamic field for a document type. The field appears on documents only after it is added to a schema draft and that draft is published. The data type can never change. Requires the LOOKUP_MANAGE permission. Responses: 200 (The created field.), 400 (The key or data type is invalid.), 404 (The document type does not exist.), 409 (The key is already used for this document type.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<DocumentFieldResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateField(
        [Description(TypeIdDoc)] int documentTypeId,
        [FromBody] CreateDocumentFieldRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        return (await _sender.Send(
            new CreateDocumentFieldCommand(userId, documentTypeId, request.Key, request.DataType),
            cancellationToken)).ToActionResult();
    }

    /// <summary>Fetches the published schema that new documents of a type are pinned to.</summary>
    /// <param name="documentTypeId">Document type id (integer, route). Example: <c>1</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The published schema, or an empty one when the type has no published fields.</response>
    /// <response code="404">The document type does not exist.</response>
    [HttpGet("document-types/{documentTypeId:int}/schema")]
    [EndpointSummary("Fetches the published schema that new documents of a type are pinned to.")]
    [EndpointDescription("Fetches the published field schema of a document type, used to render the create-document form. Requires the DOCUMENT_VIEW permission. Responses: 200 (The published schema, or an empty one when the type has no published fields.), 404 (The document type does not exist.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<DocumentTypeSchemaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedSchema(
        [Description(TypeIdDoc)] int documentTypeId, CancellationToken cancellationToken)
        => (await _sender.Send(new GetDocumentTypeSchemaQuery(documentTypeId), cancellationToken)).ToActionResult();

    /// <summary>Lists a document type's schema version history.</summary>
    /// <param name="documentTypeId">Document type id (integer, route). Example: <c>1</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The versions, newest first.</response>
    /// <response code="404">The document type does not exist.</response>
    [HttpGet("document-types/{documentTypeId:int}/schema-versions")]
    [EndpointSummary("Lists a document type's schema version history.")]
    [EndpointDescription("Lists the schema versions (draft, published and retired) of a document type, newest first. Requires the LOOKUP_MANAGE permission. Responses: 200 (The versions, newest first.), 404 (The document type does not exist.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<IReadOnlyList<SchemaVersionSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListVersions(
        [Description(TypeIdDoc)] int documentTypeId, CancellationToken cancellationToken)
        => (await _sender.Send(new ListSchemaVersionsQuery(documentTypeId), cancellationToken)).ToActionResult();

    /// <summary>Starts the editable schema draft of a document type.</summary>
    /// <param name="documentTypeId">Document type id (integer, route). Example: <c>1</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The new draft, copied from the published version when there is one.</response>
    /// <response code="404">The document type does not exist.</response>
    /// <response code="409">The type already has a draft.</response>
    [HttpPost("document-types/{documentTypeId:int}/schema-versions")]
    [EndpointSummary("Starts the editable schema draft of a document type.")]
    [EndpointDescription("Starts the editable schema draft of a document type as a copy of the published version, or empty when there is none. A type has at most one draft. Requires the LOOKUP_MANAGE permission. Responses: 200 (The new draft, copied from the published version when there is one.), 404 (The document type does not exist.), 409 (The type already has a draft.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDraft(
        [Description(TypeIdDoc)] int documentTypeId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        return (await _sender.Send(new CreateSchemaDraftCommand(userId, documentTypeId), cancellationToken))
            .ToActionResult();
    }

    /// <summary>Fetches one schema version with its fields.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The schema version.</response>
    /// <response code="404">No schema version with that id exists.</response>
    [HttpGet("schema-versions/{schemaVersionId:guid}")]
    [EndpointSummary("Fetches one schema version with its fields.")]
    [EndpointDescription("Fetches one schema version with its fields. Requires the LOOKUP_MANAGE permission. Responses: 200 (The schema version.), 404 (No schema version with that id exists.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(
        [Description(VersionIdDoc)] Guid schemaVersionId, CancellationToken cancellationToken)
        => (await _sender.Send(new GetSchemaVersionQuery(schemaVersionId), cancellationToken)).ToActionResult();

    /// <summary>Discards an unpublished schema draft.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The draft was discarded.</response>
    /// <response code="404">No schema version with that id exists.</response>
    /// <response code="409">The version is already published or retired.</response>
    [HttpDelete("schema-versions/{schemaVersionId:guid}")]
    [EndpointSummary("Discards an unpublished schema draft.")]
    [EndpointDescription("Discards an unpublished schema draft; published and retired versions cannot be discarded. Requires the LOOKUP_MANAGE permission. Responses: 200 (The draft was discarded.), 404 (No schema version with that id exists.), 409 (The version is already published or retired.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DiscardDraft(
        [Description(VersionIdDoc)] Guid schemaVersionId, CancellationToken cancellationToken)
        => (await _sender.Send(new DiscardSchemaDraftCommand(schemaVersionId), cancellationToken)).ToActionResult();

    /// <summary>Adds an existing field to a schema draft.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>documentFieldId</term><description>Field unique identifier (GUID, required). Example: <c>b2a47e90-6c13-4d58-8f21-0e9d5a7c3b64</c>.</description></item>
    /// <item><term>settings</term><description>Label, requiredness, order and type-specific rules (object, required). Example: <c>{"labelAr":"رقم الجلسة","isRequired":true}</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated draft.</response>
    /// <response code="400">The settings are invalid for the field's data type.</response>
    /// <response code="404">The draft or the field does not exist.</response>
    /// <response code="409">The version is not a draft, or the field is already in it.</response>
    [HttpPost("schema-versions/{schemaVersionId:guid}/fields")]
    [EndpointSummary("Adds an existing field to a schema draft.")]
    [EndpointDescription("Adds an existing field to a schema draft with its settings. Requires the LOOKUP_MANAGE permission. Responses: 200 (The updated draft.), 400 (The settings are invalid for the field's data type.), 404 (The draft or the field does not exist.), 409 (The version is not a draft, or the field is already in it.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFieldToDraft(
        [Description(VersionIdDoc)] Guid schemaVersionId,
        [FromBody] AddSchemaFieldRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(
            new AddFieldToDraftCommand(schemaVersionId, request.DocumentFieldId, request.Settings.ToInput()),
            cancellationToken)).ToActionResult();

    /// <summary>Replaces the settings of a field inside a schema draft.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="documentFieldId">Document field unique identifier (GUID, route). Example: <c>b2a47e90-6c13-4d58-8f21-0e9d5a7c3b64</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>labelAr</term><description>Arabic label (string, required). Example: <c>رقم الجلسة</c>.</description></item>
    /// <item><term>isRequired</term><description>Whether a value is required to register (boolean, optional). Example: <c>true</c>.</description></item>
    /// <item><term>displayOrder</term><description>Position in the form (integer, optional). Example: <c>10</c>.</description></item>
    /// <item><term>regexPattern</term><description>Text pattern (string, optional). Example: <c>^\d{4}$</c>.</description></item>
    /// <item><term>regexErrorMessageAr</term><description>Arabic message shown when the pattern fails (string, optional). Example: <c>يجب أن يتكون من أربعة أرقام</c>.</description></item>
    /// <item><term>minNumber</term><description>Lowest allowed number (decimal, optional). Example: <c>0</c>.</description></item>
    /// <item><term>maxNumber</term><description>Highest allowed number (decimal, optional). Example: <c>1000</c>.</description></item>
    /// <item><term>maxLength</term><description>Longest allowed text (integer, optional). Example: <c>50</c>.</description></item>
    /// <item><term>options</term><description>Select options as key and Arabic label (array, optional). Example: <c>[{"key":"urgent","labelAr":"عاجل"}]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated draft.</response>
    /// <response code="400">The settings are invalid for the field's data type.</response>
    /// <response code="404">The draft or the field in it does not exist.</response>
    /// <response code="409">The version is not a draft.</response>
    [HttpPut("schema-versions/{schemaVersionId:guid}/fields/{documentFieldId:guid}")]
    [EndpointSummary("Replaces the settings of a field inside a schema draft.")]
    [EndpointDescription("Replaces the settings of a field already in a schema draft; its data type never changes. Requires the LOOKUP_MANAGE permission. Responses: 200 (The updated draft.), 400 (The settings are invalid for the field's data type.), 404 (The draft or the field in it does not exist.), 409 (The version is not a draft.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraftField(
        [Description(VersionIdDoc)] Guid schemaVersionId,
        [Description(FieldIdDoc)] Guid documentFieldId,
        [FromBody] DocumentFieldSettingsRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(
            new UpdateDraftFieldCommand(schemaVersionId, documentFieldId, request.ToInput()),
            cancellationToken)).ToActionResult();

    /// <summary>Removes a field from a schema draft.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="documentFieldId">Document field unique identifier (GUID, route). Example: <c>b2a47e90-6c13-4d58-8f21-0e9d5a7c3b64</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated draft.</response>
    /// <response code="404">The draft or the field in it does not exist.</response>
    /// <response code="409">The version is not a draft.</response>
    [HttpDelete("schema-versions/{schemaVersionId:guid}/fields/{documentFieldId:guid}")]
    [EndpointSummary("Removes a field from a schema draft.")]
    [EndpointDescription("Removes a field from a schema draft; the field itself and values stored under earlier versions are untouched. Requires the LOOKUP_MANAGE permission. Responses: 200 (The updated draft.), 404 (The draft or the field in it does not exist.), 409 (The version is not a draft.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveDraftField(
        [Description(VersionIdDoc)] Guid schemaVersionId,
        [Description(FieldIdDoc)] Guid documentFieldId,
        CancellationToken cancellationToken)
        => (await _sender.Send(
            new RemoveDraftFieldCommand(schemaVersionId, documentFieldId), cancellationToken)).ToActionResult();

    /// <summary>Publishes a schema draft, retiring the previously published version.</summary>
    /// <param name="schemaVersionId">Schema version unique identifier (GUID, route). Example: <c>3f8b2c61-9d47-4e15-a6b0-5c1e7d92f438</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The published version.</response>
    /// <response code="404">No schema version with that id exists.</response>
    /// <response code="409">The version is not a draft.</response>
    [HttpPost("schema-versions/{schemaVersionId:guid}/publish")]
    [EndpointSummary("Publishes a schema draft, retiring the previously published version.")]
    [EndpointDescription("Freezes a schema draft and makes it the version new documents are pinned to. Documents already created keep their own version. Requires the LOOKUP_MANAGE permission. Responses: 200 (The published version.), 404 (No schema version with that id exists.), 409 (The version is not a draft.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<SchemaVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(
        [Description(VersionIdDoc)] Guid schemaVersionId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        return (await _sender.Send(new PublishSchemaVersionCommand(schemaVersionId, userId), cancellationToken))
            .ToActionResult();
    }

    /// <summary>Fetches a document's pinned schema with the values it stores.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The schema version the document is pinned to and its values.</response>
    /// <response code="404">No document with that id exists.</response>
    [HttpGet("documents/{documentId:guid}/fields")]
    [EndpointSummary("Fetches a document's pinned schema with the values it stores.")]
    [EndpointDescription("Fetches the schema version a document is pinned to together with the dynamic-field values it stores. Requires the DOCUMENT_VIEW permission. Responses: 200 (The schema version the document is pinned to and its values.), 404 (No document with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<DocumentFieldsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentFields(
        [Description(DocumentIdDoc)] Guid documentId, CancellationToken cancellationToken)
        => (await _sender.Send(new GetDocumentFieldsQuery(documentId), cancellationToken)).ToActionResult();

    /// <summary>Replaces the dynamic-field values of a DRAFT document.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>values</term><description>Field key and value as text; a blank value clears the field (array, required). Example: <c>[{"fieldKey":"session_no","value":"2026-14"}]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's schema and its new values.</response>
    /// <response code="400">A value failed validation or names a field outside the pinned schema.</response>
    /// <response code="404">No document with that id exists.</response>
    /// <response code="409">The document is no longer in DRAFT status.</response>
    [HttpPut("documents/{documentId:guid}/fields")]
    [EndpointSummary("Replaces the dynamic-field values of a DRAFT document.")]
    [EndpointDescription("Replaces the dynamic-field values of a DRAFT document, validated against the schema version it is pinned to. Values are locked once the document is registered. Requires the DOCUMENT_EDIT permission. Responses: 200 (The document's schema and its new values.), 400 (A value failed validation or names a field outside the pinned schema.), 404 (No document with that id exists.), 409 (The document is no longer in DRAFT status.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentFieldsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetDocumentFields(
        [Description(DocumentIdDoc)] Guid documentId,
        [FromBody] SetDocumentFieldValuesRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(
            new SetDocumentFieldValuesCommand(documentId, request.Values.ToInputs() ?? []),
            cancellationToken)).ToActionResult();

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
