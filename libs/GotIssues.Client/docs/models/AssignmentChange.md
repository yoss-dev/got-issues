# GotIssues.Client.Model.AssignmentChange
A change of assignment.  There are three ways to send this object and they mean two things:  | Sent | Meaning | | - -- | - -- | | `{\"subject\": \"alice\"}` | assign to `alice` | | `{\"subject\": null}` | unassign | | `{}` | **unassign** — an absent `subject` is read as null |  The third is worth stating plainly rather than leaving to be discovered: a client that forgets to send `subject` unassigns the issue and receives 200. The API cannot distinguish an omitted `subject` from an explicit null — that is the same limitation this object exists to work around one level up — so it is documented rather than rejected. Omit `assignment` entirely to leave the holder alone.  `subject` is deliberately **not** listed as required. In JSON Schema `required` means \"the property must be present\", but the C# generator renders it as `[Required]`, which means \"must not be null\" — and null is precisely the value that unassigns. Listing it would reject the operation this object exists to express. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Subject** | **string** | The subject of the person to assign, or **null to unassign**.  Control characters are excluded, for the reason every other free-text field in this document excludes them: PostgreSQL cannot store &#x60;U+0000&#x60;, so without this the value reaches the database and fails as an unhandled error rather than as validation. A subject is a token claim and spans one line.  A subject with no user record is rejected with 400: this API assigns to people it has seen, and inventing one silently would produce an assignee no client could render.  | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

