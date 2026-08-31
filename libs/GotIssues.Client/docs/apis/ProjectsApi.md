# GotIssues.Client.Api.ProjectsApi

All URIs are relative to *http://localhost:8080*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreateProject**](ProjectsApi.md#createproject) | **POST** /projects | Create a project. |
| [**ListProjects**](ProjectsApi.md#listprojects) | **GET** /projects | List projects. |

<a id="createproject"></a>
# **CreateProject**
> Project CreateProject (CreateProjectRequest createProjectRequest)

Create a project.

Creates a project with a name and a key.  **Requires the `admin` role.** Creating a project is one of the three administrative acts in this system; a caller holding only `member` receives 403. The restriction is a property of the caller's role claim rather than of an OAuth scope, so it cannot be expressed in this document's security requirements — it is declared here, and in the 403 response, so that a client generating from this contract knows the endpoint can refuse an authenticated caller.  The key must be unique across the deployment. A key already in use is rejected with 409 rather than silently reused. 


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **createProjectRequest** | [**CreateProjectRequest**](CreateProjectRequest.md) |  |  |

### Return type

[**Project**](Project.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | The project was created. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |
| **403** | The credentials were valid, but the caller&#39;s role does not permit this operation. Distinct from 401: the caller is known, and still refused.  |  -  |
| **409** | The request conflicts with something that already exists — for a project, a key already in use.  |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

<a id="listprojects"></a>
# **ListProjects**
> ProjectPage ListProjects (int page = null, int pageSize = null)

List projects.

Returns a page of projects, newest first.  Any caller holding a recognised role may list projects; there is no per-project visibility, because roles in this system are global. 


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **page** | **int** | 1-based page number. Out-of-range values are rejected with 400 rather than silently adjusted, matching pageSize.  Enforcement is the server&#39;s. Declaring a bound does not mean a generated client checks it before dispatching — the C# client generated from this document does not.  | [optional] [default to 1] |
| **pageSize** | **int** | Projects per page. The maximum is a declared constraint, so a larger value is rejected with 400 rather than silently reduced — a client asking for 10 000 and receiving 100 without being told is worse.  | [optional] [default to 20] |

### Return type

[**ProjectPage**](ProjectPage.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | A page of projects. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |
| **403** | The credentials were valid, but the caller&#39;s role does not permit this operation. Distinct from 401: the caller is known, and still refused.  |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

