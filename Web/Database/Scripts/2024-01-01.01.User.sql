create table [User]
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

	Roles nvarchar(max) not null
		constraint [CK_User_Roles_IsJson]
		check (isjson(Roles) = 1),
);

create unique index [UK_User_Auth0UserId]
on [User](Auth0UserId)
where Auth0UserId is not null;
