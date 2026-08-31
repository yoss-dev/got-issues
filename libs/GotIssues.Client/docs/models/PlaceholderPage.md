# GotIssues.Client.Model.PlaceholderPage
One page of results. Collection endpoints are always paginated — an unbounded result set is forbidden by the project's engineering standards. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Items** | [**List&lt;Placeholder&gt;**](Placeholder.md) | The records on this page. | 
**Page** | **int** | The 1-based page number this response represents. | 
**PageSize** | **int** | How many records this page can hold. | 
**TotalCount** | **int** | Total records matching the query, across all pages. | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

