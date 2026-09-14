create schema [DataProtectionKeys];
go

create table [DataProtectionKeys].[DataProtectionKey]
(
	DataProtectionKeyId int not null identity(1, 1)
		constraint [PK_DataProtectionKey]
		primary key,

	FriendlyName varchar(200) null,
	[Xml] varchar(max) null,
);
