# GotIssues.Client.Api.PlaceholderApi

All URIs are relative to *http://localhost:8080*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreatePlaceholder**](PlaceholderApi.md#createplaceholder) | **POST** /placeholders | Create a placeholder record. |
| [**ListPlaceholders**](PlaceholderApi.md#listplaceholders) | **GET** /placeholders | List placeholder records. |

<a id="createplaceholder"></a>
# **CreatePlaceholder**
> Placeholder CreatePlaceholder (CreatePlaceholderRequest createPlaceholderRequest)

Create a placeholder record.


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **createPlaceholderRequest** | [**CreatePlaceholderRequest**](CreatePlaceholderRequest.md) |  |  |

### Return type

[**Placeholder**](Placeholder.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | The record was created. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

<a id="listplaceholders"></a>
# **ListPlaceholders**
> PlaceholderPage ListPlaceholders (int page = null, int pageSize = null)

List placeholder records.

Returns a page of placeholder records, newest first.


### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **page** | **int** | 1-based page number. Out-of-range values are rejected with 400 rather than silently adjusted, matching pageSize. The upper bound exists so the constraint is expressible in the contract and enforced by generated clients; this API does not support paging beyond that depth.  | [optional] [default to 1] |
| **pageSize** | **int** | Records per page. The maximum is a declared constraint, so a larger value is rejected with 400 rather than silently reduced — a client asking for 10 000 and receiving 100 without being told is worse. Clients generated from this document enforce it before the request leaves them.  | [optional] [default to 20] |

### Return type

[**PlaceholderPage**](PlaceholderPage.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | A page of placeholder records. |  -  |
| **400** | The request was malformed or failed validation. |  -  |
| **401** | No credentials were supplied, or they were not valid. |  -  |

[[Back to top]](#) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to Model list]](../../README.md#documentation-for-models) [[Back to README]](../../README.md)

