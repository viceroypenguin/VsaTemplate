create table [ApiKey]
(
	ApiKeyId int not null
		constraint [PK_ApiKey]
		primary key
		constraint [FK_ApiKey_User]
		foreign key references [User],

	OwnerUserId int not null
		constraint [FK_ApiKey_Owner]
		foreign key references [User],
);
