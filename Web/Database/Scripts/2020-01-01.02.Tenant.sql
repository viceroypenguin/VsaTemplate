create schema [Tenant];
go

create table [Tenant].[Tenant]
(
	TenantId int not null identity(1, 1)
		constraint [PK_Tenant]
		primary key,

	[Name] varchar(200) not null,
	[IsActive] bit not null,

	[RecoveryKeysJson] varchar(max) not null
		constraint [CK_Tenant_RecoveryKeysJson_IsJson]
		check (isjson([RecoveryKeysJson]) = 1),
);

create table [Tenant].[TenantRole]
(
	TenantRoleId int not null identity(1, 1)
		constraint [PK_TenantRole]
		primary key,
	TenantId int not null
		constraint [FK_TenantRole_Tenant]
		foreign key references [Tenant].[Tenant],

	[Name] varchar(200) not null,

	[PermissionsJson] varchar(max) not null
		constraint [CK_Role_PermissionsJson_IsJson]
		check (isjson([PermissionsJson]) = 1),

	EditedUserId int not null
		constraint [FK_Role_EditUser]
		foreign key references [AccessControl].[User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = [Tenant].[TenantRoleHistory]));

create table [Tenant].[TenantRoleUser]
(
	UserId int not null
		constraint [FK_TenantRoleUser_User]
		foreign key references [AccessControl].[User],
	TenantRoleId int not null
		constraint [FK_TenantRoleUser_Role]
		foreign key references [Tenant].[TenantRole],

	constraint [PK_RoleUser] primary key (UserId, TenantRoleId),

	EditedUserId int not null
		constraint [FK_TenantRoleUser_EditUser]
		foreign key references [AccessControl].[User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = [Tenant].[TenantRoleUserHistory]));

create unique index [UIX_TenantRoleUser_RoleId_UserId]
on [Tenant].[TenantRoleUser](TenantRoleId, UserId);
go

create table [Tenant].[TenantApiKey]
(
	[TenantApiKeyId] int not null
		constraint [PK_TenantApiKey]
		primary key
		constraint [FK_TenantApiKey_ApiKey]
		foreign key references [AccessControl].[ApiKey],

	[PermissionsJson] varchar(max) not null
		constraint [CK_ApiKey_PermissionsJson_IsJson]
		check (isjson([PermissionsJson]) = 1),
);
