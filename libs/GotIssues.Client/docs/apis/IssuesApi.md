# GotIssues.Client.Api.IssuesApi

All URIs are relative to *http://localhost:8080*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreateIssue**](IssuesApi.md#createissue) | **POST** /projects/{projectKey}/issues | Create an issue in a project. |
| [**GetIssue**](IssuesApi.md#getissue) | **GET** /issues/{issueKey} | Read an issue by its key. |
| [**UpdateIssue**](IssuesApi.md#updateissue) | **PATCH** /issues/{issueKey} | Change an issue&#39;s lifecycle fields. |

<a id="createissue"></a>
# **CreateIssue**
> Issue CreateIssue (string projectKey, CreateIssueRequest createIssueRequest)

Create an issue in a project.

Creates an issue and allocates its number within the given project.  The number is allocated by the server and cannot be chosen.  **Requires a recognised role.** Any caller holding `admin` or `member` may create an issue; a token carrying neither receives 403. Unlike creating a project, this is not an administrative act.  A project that has exhausted its issue numbers — 999 999 999 of them, the most a key can express — is refused with 409. Issuing a key beyond that would produce an identifier this document's own pattern rejects. 


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **projectKey** | **string** | The key of the project the issue belongs to, for example &#x60;GOTI&#x60;. |  |
| **createIssueRequest** | [**CreateIssueRequest**](CreateIssueRequest.md) |  |  |

### Return type

[**Issue**](Issue.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | The issue was created. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |
| **403** | The credentials were valid, but the caller&#39;s role does not permit this operation. Distinct from 401: the caller is known, and still refused.  |  -  |
| **404** | The addressed resource does not exist — a project that was never created, or an issue key that corresponds to nothing.  |  -  |
| **409** | The request conflicts with the state of the system — for a project, a key already in use; for an issue, a project that has exhausted its issue numbers.  |  -  |
| **500** | The request could not be completed because of an unexpected failure.  Declared because the API can return it: an operation that reaches the database can fail in ways no validation anticipates, and a contract that lists only the outcomes it likes is as wrong as one that promises a body it does not send. The response is a problem document like every other failure — never an empty body, which is what a caller received before this was declared.  |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

<a id="getissue"></a>
# **GetIssue**
> Issue GetIssue (string issueKey)

Read an issue by its key.

Returns the issue with the given key, for example `GOTI-1`.  **Requires a recognised role.** Any caller holding `admin` or `member` may read an issue; a token carrying neither receives 403. There is no per-project visibility — roles in this system are global.  Issues are addressed by the key people quote rather than by project and number separately, because that string is the thing written in commit messages and chat. 


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **issueKey** | **string** | The issue&#39;s key, for example &#x60;GOTI-1&#x60;. |  |

### Return type

[**Issue**](Issue.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The issue. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |
| **403** | The credentials were valid, but the caller&#39;s role does not permit this operation. Distinct from 401: the caller is known, and still refused.  |  -  |
| **404** | The addressed resource does not exist — a project that was never created, or an issue key that corresponds to nothing.  |  -  |
| **500** | The request could not be completed because of an unexpected failure.  Declared because the API can return it: an operation that reaches the database can fail in ways no validation anticipates, and a contract that lists only the outcomes it likes is as wrong as one that promises a body it does not send. The response is a problem document like every other failure — never an empty body, which is what a caller received before this was declared.  |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

<a id="updateissue"></a>
# **UpdateIssue**
> Issue UpdateIssue (string issueKey, UpdateIssueRequest updateIssueRequest)

Change an issue's lifecycle fields.

Changes any of an issue's type, status, priority or assignee. Every field is optional; omitting one leaves it as it was.  **Any declared status may follow any other.** There are no transition rules — an issue may move from `done` back to `open`. Configurable workflows and validated transitions are a later product goal, and enforcing them here would pre-empt that decision.  **Requires a recognised role.** Any caller holding `admin` or `member` may change these fields; a token carrying neither receives 403. Moving an issue is not one of this system's administrative acts. 


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **issueKey** | **string** | The issue&#39;s key, for example &#x60;GOTI-1&#x60;. |  |
| **updateIssueRequest** | [**UpdateIssueRequest**](UpdateIssueRequest.md) |  |  |

### Return type

[**Issue**](Issue.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The issue as it now stands. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |
| **403** | The credentials were valid, but the caller&#39;s role does not permit this operation. Distinct from 401: the caller is known, and still refused.  |  -  |
| **404** | The addressed resource does not exist — a project that was never created, or an issue key that corresponds to nothing.  |  -  |
| **500** | The request could not be completed because of an unexpected failure.  Declared because the API can return it: an operation that reaches the database can fail in ways no validation anticipates, and a contract that lists only the outcomes it likes is as wrong as one that promises a body it does not send. The response is a problem document like every other failure — never an empty body, which is what a caller received before this was declared.  |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

