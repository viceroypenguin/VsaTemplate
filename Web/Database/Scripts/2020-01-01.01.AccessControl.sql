create schema [AccessControl];
go

create table [AccessControl].[User]
(
	UserId int not null identity
		constraint [PK_User]
		primary key,

	Auth0UserId nvarchar(100) null,
	EmailAddress nvarchar(500) not null
		constraint [UK_User_EmailAddress] unique,

	Name nvarchar(200) null,
	IsActive bit not null,
	LastLogin datetimeoffset null,
);

create unique index [UK_User_Auth0UserId]
on [AccessControl].[User](Auth0UserId)
where Auth0UserId is not null;

set identity_insert [AccessControl].[User] on;

insert [AccessControl].[User](UserId, EmailAddress, Name, IsActive)
values (-1, 'system@vsatemplate.com', 'System', 1);

set identity_insert [AccessControl].[User] off;
go

create table [AccessControl].[Role]
(
	RoleId int not null identity(1, 1)
		constraint [PK_Role]
		primary key,

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
with (system_versioning = on (history_table = [AccessControl].[RoleHistory]));

create table [AccessControl].[RoleUser]
(
	UserId int not null
		constraint [FK_RoleUser_User]
		foreign key references [AccessControl].[User],
	RoleId int not null
		constraint [FK_RoleUser_Role]
		foreign key references [AccessControl].[Role],

	constraint [PK_RoleUser] primary key (UserId, RoleId),

	EditedUserId int not null
		constraint [FK_RoleUser_EditUser]
		foreign key references [AccessControl].[User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = [AccessControl].[RoleUserHistory]));

create unique index [UIX_RoleUser_RoleId_UserId]
on [AccessControl].[RoleUser](RoleId, UserId);
go

set identity_insert [AccessControl].[Role] on;

insert [AccessControl].[Role]([RoleId], [Name], [PermissionsJson], [EditedUserId])
values (-1, 'Administrators', '[1]', -1);

set identity_insert [AccessControl].[Role] off;

insert [AccessControl].[RoleUser](UserId, RoleId, EditedUserId)
values (-1, -1, -1);
go

create table [AccessControl].[ApiKey]
(
	[ApiKeyId] int not null
		constraint [PK_ApiKey]
		primary key
		constraint [FK_ApiKey_User]
		foreign key references [AccessControl].[User],

	[OwnerUserId] int not null
		constraint [FK_ApiKey_Owner]
		foreign key references [AccessControl].[User],

	[PermissionsJson] varchar(max) not null
		constraint [CK_ApiKey_PermissionsJson_IsJson]
		check (isjson([PermissionsJson]) = 1),

	[CreatedDateTime] datetimeoffset not null,
	[ExpirationDateTime] datetimeoffset not null,
);

create unique index [IX_ApiKey_OwnerUserId]
on [AccessControl].[ApiKey](OwnerUserId);
