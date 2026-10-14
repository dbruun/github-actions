# Recommended additional GitHub Actions

The demo deliberately implements only quality checks, resource verification,
deployment and smoke checks. Add these next, in priority order:

| Priority | Action / workflow | When | Benefit |
| --- | --- | --- | --- |
| 1 | CodeQL for C# and GitHub Actions | Pull requests, main, weekly | Catch code and workflow security weaknesses before deployment |
| 1 | Dependency review plus Dependabot updates | Pull requests; weekly updates | Block vulnerable dependencies and keep SHA-pinned actions and .NET container images current |
| 1 | Secret scanning and push protection (repository settings) | Every push | Prevent credential leaks; OIDC removes deployment secrets but not every possible secret |
| 1 | Unit and integration tests with coverage | Pull requests before deployment | Add behavior-focused tests when the app grows beyond these two smoke-tested endpoints |
| 2 | Container vulnerability scan, e.g. Trivy | After image build, before update | Scan the exact image digest; fail on an agreed severity policy |
| 2 | SBOM generation and GitHub artifact attestations | Image build | Record image contents and provenance; grant attestation permissions only to the build job |
| 2 | Bicep validation/what-if and a separately approved provisioning workflow | Infrastructure pull requests / manual approval | Make Azure setup reproducible without giving every deployment resource-creation rights |
| 2 | Staging deployment, integration checks and approved production promotion | After quality checks | Promote the same digest, with separate OIDC identities and protected environments |
| 2 | Deployment recovery / rollback workflow | Manual dispatch after failure | Restore a recorded known-good digest; verify recovery with the same smoke checks |
| 3 | Scheduled external availability checks and alerts | Every 5–15 minutes, or Azure Monitor | Catch failures after a deployment; monitor latency, errors and replica health |
| 3 | Ephemeral PR environments with teardown | Trusted PRs only | Preview changes without exposing privileged credentials to untrusted fork code |
| 3 | Budget checks, registry retention and demo-resource cleanup | Scheduled / manually approved | Control Azure spend and remove stale images/resources |

Keep third-party actions SHA-pinned and review their permissions and advisories
before adoption. Never use `pull_request_target` to execute untrusted PR code with
deployment privileges. Scan built images and promote immutable digests rather
than rebuilding between staging and production. Limit provisioning and role
assignment permissions to separate administrator workflows.

Branch rules and environment approvals are configuration, not Actions YAML:
make them mandatory alongside these workflows. Some GitHub security features
and environment protections depend on repository visibility and plan.
