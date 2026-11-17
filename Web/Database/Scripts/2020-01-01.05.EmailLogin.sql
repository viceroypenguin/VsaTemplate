create table [AccessControl].[EmailLoginChallenge]
(
	[SessionIdHash] binary(32) not null
		constraint [PK_EmailLoginChallenge] primary key,
	[EmailAddress] nvarchar(500) not null,
	[TokenHash] binary(32) not null,
	[CreatedAt] datetimeoffset not null,
	[ExpiresAt] datetimeoffset not null,
	[FailedAttempts] int not null,
	[IsConsumed] bit not null,

	constraint [CK_EmailLoginChallenge_FailedAttempts]
		check ([FailedAttempts] between 0 and 5),
);

create index [IX_EmailLoginChallenge_EmailAddress_CreatedAt]
on [AccessControl].[EmailLoginChallenge]([EmailAddress], [CreatedAt]);

create index [IX_EmailLoginChallenge_ExpiresAt]
on [AccessControl].[EmailLoginChallenge]([ExpiresAt]);
