create table TodoStatus
(
	TodoStatusId int not null
		constraint [PK_TodoStatus]
		primary key,
	Name varchar(200) not null,
);

create table TodoPriority
(
	TodoPriorityId int not null
		constraint [PK_TodoPriority]
		primary key,
	Name varchar(200) not null,
);

create table Todo
(
	TodoId int not null identity
		constraint [PK_Todo]
		primary key,
	Name varchar(200) not null,
	Comment varchar(max) null,
	TodoPriorityId int not null
		constraint [FK_Todo_TodoPriority]
		foreign key references TodoPriority,
	TodoStatusId int not null
		constraint [FK_Todo_TodoStatus]
		foreign key references TodoStatus,

	UserId int not null
		constraint [FK_Todo_User]
		foreign key references [User],

	ValidFrom datetime2 generated always as row start not null,
	ValidTo datetime2 generated always as row end not null,
	period for system_time (ValidFrom, ValidTo),
)
with (system_versioning = on (history_table = dbo.TodoHistory));
