# GotIssues.Client.Model.Assignee
The person an issue is assigned to.  Carries the display name as well as the subject so a client can render who holds an issue without a second call — which is what the user projection exists for. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Subject** | **string** | The subject claim identifying the person. This is the identifier: the projection stores no email, and a display name is neither unique nor stable.  | 
**DisplayName** | **string** | The person&#39;s display name as their token last carried it, or null if their token carried none.  | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

