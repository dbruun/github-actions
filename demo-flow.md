# One-hour demo: GitHub Actions → Azure Container Apps

## Rehearsal checklist (before the session)

- Complete the README Azure/OIDC setup and protected `demo` environment.
- Require the `Branch quality` check and a review on `main`.
- Run a complete deployment; verify ACR Tasks access, role propagation, image
  pulls and the public HTTPS URL. Record a known-good main commit.
- Have .NET 10, Docker, Azure CLI, repository access and a browser ready.
- Open tabs for the pull request, Actions run, GitHub environment, Entra federated
  credential, Azure Container App revisions and registry. Never display tokens.
- Prepare a small greeting change and an intentional formatting error on a demo
  branch. If changing the greeting, update `tests/smoke.sh` in the same PR.
- Keep a successful run and its logs available as a fallback if Azure is slow.

## Agenda (60 minutes)

| Time | Activity | Talking points / expected result |
| --- | --- | --- |
| 00–05 | Explain the architecture | PR → quality gate → main → protected environment → OIDC → preflight → ACR → Container App |
| 05–12 | Tour and run the app | Show `Program.cs`, `/health`, strict compiler/analyzer settings and the non-root Dockerfile; run the README local smoke check |
| 12–22 | Demonstrate branch health | Open the prepared PR with a formatting error; show `Branch quality` failing and deployment skipped; fix with `dotnet format`, push and show green checks |
| 22–30 | Explain trust and resources | Show environment branch restrictions/approval, federated issuer/subject/audience, separate deployment/runtime identities and RBAC; inspect existing Azure resources |
| 30–42 | Merge and deploy | Review/merge the PR, watch quality rerun, approve `demo`, then follow resource verification, ACR build, digest deployment and revision readiness |
| 42–48 | Verify the result | Open the run-summary HTTPS URL; show `/health`, the updated greeting, ACR SHA tag and the new ready Container App revision |
| 48–54 | Discuss failures and recovery | Explain missing-resource preflight failures, image-pull permissions and logs; show how to revert to a known-good commit through a reviewed PR and redeploy |
| 54–60 | Recommendations and Q&A | Prioritize recommendations.md, distinguish demo vs production, recap OIDC/no stored credentials, explain cleanup and costs |

## Presenter notes

- Start the prepared PR promptly: runner queues, approvals and image builds can
  consume several minutes. Explain OIDC while checks run; use recorded successful
  logs if necessary instead of provisioning Azure live.
- The checked-in greeting and smoke-test expectations must stay in sync. Emphasize
  that behavior changes deserve corresponding tests, not just a green build.
- Resource creation is intentionally outside CD. The preflight detects missing or
  incompatible resources before building/deploying; it does not fix infrastructure.
- If demonstrating a missing resource, use an isolated demo environment/config and
  restore it immediately. Do not delete a live app or alter RBAC during the talk.
- Deployment serialization avoids overlapping updates. Single-revision mode keeps
  the demo understandable; discuss staged traffic and rollback as production work.
- A failed deployment is not an automatic rollback. Show the recorded known-good
  commit and the reviewed revert/redeploy path; leave destructive cleanup until
  after Q&A and confirm the resource group is demo-only.
