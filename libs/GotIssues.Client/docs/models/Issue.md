# GotIssues.Client.Model.Issue
A unit of work inside a project.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **Guid** | The issue&#39;s identifier. | 
**Key** | **string** | The issue&#39;s key: the project&#39;s key, a hyphen, and the number allocated within that project. This is the identifier people quote.  | 
**ProjectKey** | **string** | The key of the project this issue belongs to. | 
**Number** | **int** | The issue&#39;s number within its project. Allocated by the server, starting at 1 in each project, and never reused.  The maximum is not arbitrary: it is the largest number &#x60;key&#x60; can express, since that pattern allows nine digits. Without it the two fields could disagree — a number of ten digits would produce a key violating the very pattern this document declares, and the issue would be unreadable through the only operation that fetches one. A project reaching this limit is refused with 409 rather than issued a key it cannot use.  | 
**Title** | **string** | A one-line summary. C0 control characters and DEL are excluded, for the same two reasons a project name excludes them: PostgreSQL cannot store &#x60;U+0000&#x60; at all, and the ASCII line breaks and tabs in that range have no place in a title.  Deliberately **not** full Unicode line-break handling: &#x60;U+0085&#x60; and &#x60;U+2028&#x60; are accepted. Refinement asked this ticket to decide rather than inherit the limit, and the decision is to keep the constraint the narrow, checkable one — a title carrying an exotic separator is a cosmetic problem, while a title carrying &#x60;U+0000&#x60; cannot be stored at all.  | 
**Type** | **IssueType** |  | 
**Status** | **IssueStatus** |  | 
**Priority** | **IssuePriority** |  | 
**CreatedAt** | **DateTime** | When the issue was created, in UTC. | 
**Description** | **string** | Optional free text. Multi-line **by design** — unlike a title, this is where the reasoning goes, so line breaks are permitted and only &#x60;U+0000&#x60; is excluded, because PostgreSQL cannot store it in any text column.  That difference is deliberate: the constraint on a title is about being one line, the constraint here is about being storable, and they are not the same rule (recorded on T-0005 from T-0004&#39;s review).  | [optional] 
**Assignee** | [**Assignee**](Assignee.md) |  | [optional] 

[[Back to Model list]](../../README.md#documentation-for-models) [[Back to API list]](../../README.md#documentation-for-api-endpoints) [[Back to README]](../../README.md)

