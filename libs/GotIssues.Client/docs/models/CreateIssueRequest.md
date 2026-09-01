# GotIssues.Client.Model.CreateIssueRequest

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Title** | **string** | A one-line summary. C0 control characters and DEL are excluded — see &#x60;Issue.title&#x60; for both reasons and for the limit this constraint deliberately stops at.  &#x60;U+0085&#x60; and &#x60;U+2028&#x60; are accepted: the constraint is the narrow, checkable one.  | 
**Description** | **string** | Optional free text; multi-line is expected. Explicitly null means the same as omitting it.  | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

