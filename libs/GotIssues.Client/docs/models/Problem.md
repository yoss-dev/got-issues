# GotIssues.Client.Model.Problem
An RFC 9457 problem document. Every failure in this API uses this shape, so clients get one error type generated rather than guessing per endpoint. 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Type** | **string** | A URI identifying the problem type. | [optional] 
**Title** | **string** | A short, human-readable summary of the problem type. | [optional] 
**Status** | **int** | The HTTP status code. | [optional] 
**Detail** | **string** | A human-readable explanation specific to this occurrence. | [optional] 
**Instance** | **string** | A URI identifying this specific occurrence. | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

