create table [Organization]
(
	OrganizationId int not null identity(1, 1)
		constraint [PK_Organization]
		primary key,

	[Name] varchar(200) not null,
	[IsActive] bit not null,

	[TenantId] int not null
		constraint [FK_Organization_Tenant]
		foreign key references [Tenant],
);

create table [OrganizationRole]
(
	OrganizationRoleId int not null identity(1, 1)
		constraint [PK_OrganizationRole]
		primary key,
	OrganizationId int not null
		constraint [FK_OrganizationRole_Organization]
		foreign key references [Organization],

	[Name] varchar(200) not null,

	[PermissionsJson] varchar(max) not null
		constraint [CK_OrganizationRole_PermissionsJson_IsJson]
		check (isjson([PermissionsJson]) = 1),

	EditedUserId int not null
		constraint [FK_OrganizationRole_EditUser]
		foreign key references [AccessControl].[User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = [dbo].[OrganizationRoleHistory]));

create table [OrganizationRoleUser]
(
	UserId int not null
		constraint [FK_OrganizationRoleUser_User]
		foreign key references [AccessControl].[User],
	OrganizationRoleId int not null
		constraint [FK_OrganizationRoleUser_Role]
		foreign key references [OrganizationRole],

	constraint [PK_OrganizationRoleUser] primary key (UserId, OrganizationRoleId),

	EditedUserId int not null
		constraint [FK_OrganizationRoleUser_EditUser]
		foreign key references [AccessControl].[User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = [dbo].[OrganizationRoleUserHistory]));

create unique index [UIX_OrganizationRoleUser_RoleId_UserId]
on [OrganizationRoleUser](OrganizationRoleId, UserId);
go

create table [OrganizationApiKey]
(
	[OrganizationApiKeyId] int not null
		constraint [PK_OrganizationApiKey]
		primary key
		constraint [FK_OrganizationApiKey_ApiKey]
		foreign key references [AccessControl].[ApiKey],

	[PermissionsJson] varchar(max) not null
		constraint [CK_OrganizationApiKey_PermissionsJson_IsJson]
		check (isjson([PermissionsJson]) = 1),
);
