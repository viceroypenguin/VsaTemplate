using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Features.AccessControl.Models;

[ValueObject]
[AlternativeColumnNames("ApiKeyId")]
public readonly partial struct UserId;

[ValueObject]
public readonly partial struct RoleId;
