# GotIssues.Client.Model.Project
A project, which groups related work and owns the key its issues are numbered under.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | The project&#39;s identifier. | 
**Key** | **string** | The project&#39;s key: 2-10 characters, uppercase letters and digits, starting with a letter. Unique across the deployment and immutable once set.  | 
**Name** | **string** | The project&#39;s display name. Names need not be unique — the key is the identifier, and requiring unique names would be a constraint on people rather than on data.  | 
**CreatedAt** | **DateTime** | When the project was created, in UTC. | 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

