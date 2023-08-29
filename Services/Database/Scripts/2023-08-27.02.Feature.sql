create table FeatureType
(
	FeatureTypeId int not null
		constraint [PK_FeatureType]
		primary key,
	Name varchar(200) not null,
);

create table Feature
(
	FeatureId int not null identity
		constraint [PK_Feature]
		primary key,
	Name nvarchar(200) not null,
	FeatureTypeId int not null
		constraint [FK_Feature_FeatureType]
		foreign key references FeatureType,
	CreatorUserId int not null
		constraint [FK_Feature_User]
		foreign key references [User],
	LastUpdatedTimestamp datetimeoffset not null,
	ValueA int not null,
	ValueB nvarchar(max) null,
);
