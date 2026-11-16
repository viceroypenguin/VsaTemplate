# Repository guides

These guides describe durable repository architecture and working conventions for human contributors and LLM agents,
including agents outside the original development environment. Read the public [README](../readme.md) for the project's
purpose, architectural rationale, and instructions for building and running it.

- [Architecture](architecture.md): project relationships, feature ownership, persistence, authorization, and frontend hosting.
- [Authentication](authentication.md): email-code sign-in, tab binding, delivery, and protection boundaries.
- [Contributing](contributing.md): code placement, reading style, change validation, and maintaining these guides.

Keep these files about decisions that remain useful across tasks. Do not add chat transcripts, individual query answers,
implementation diaries, temporary workarounds, task progress, or descriptions of abandoned approaches. Those records create
churn and can make future agents mistake an old local choice for a lasting constraint.

These guides describe the current repository and its intended boundaries. They may be revised when the architecture or
user requirements change; they do not prohibit justified changes. Update the relevant explanation with the implementation
so later contributors can understand both the decision and its purpose.
