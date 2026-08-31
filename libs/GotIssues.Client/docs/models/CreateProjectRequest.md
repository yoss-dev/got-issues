# GotIssues.Client.Model.CreateProjectRequest

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Key** | **string** | The key this project&#39;s issues are numbered under, for example &#x60;GOTI&#x60;. Cannot be changed afterwards, so it is worth choosing deliberately.  | 
**Name** | **string** | The project&#39;s display name. C0 control characters and DEL are excluded: &#x60;U+0000&#x60; cannot be stored by PostgreSQL at all, and the rest of the range carries tabs and line breaks a display name has no use for.  | 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

