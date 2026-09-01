# GotIssues.Client.Model.Project
A project, which groups related work and owns the key its issues are numbered under.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | The project&#39;s identifier. | 
**Key** | **string** | The project&#39;s key: 2-10 characters, uppercase letters and digits, starting with a letter. Unique across the deployment and immutable once set.  | 
**Name** | **string** | The project&#39;s display name. Names need not be unique — the key is the identifier, and requiring unique names would be a constraint on people rather than on data.  C0 control characters and DEL are excluded. Two reasons, and they are not the same reason: PostgreSQL cannot store &#x60;U+0000&#x60; in text at all, so a name carrying one fails as an unhandled error rather than as validation; and the rest of the range carries tabs and line breaks, which a display name has no use for.  This is not full Unicode line-break normalisation — &#x60;U+0085&#x60; and &#x60;U+2028&#x60; are accepted. The constraint is deliberately the narrow, checkable one.  | 
**CreatedAt** | **DateTime** | When the project was created, in UTC. | 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

