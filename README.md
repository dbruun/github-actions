# Azure Container Apps CI/CD demo

A minimal .NET 10 LTS web application deployed by GitHub Actions to an **existing**
Azure Container App. Authentication uses GitHub OIDC, not a client secret or
registry password.

```text
Pull request → Branch quality (format, analyzers, build, HTTP + container smoke checks)
Merge to main → Branch quality → demo environment approval → OIDC login
              → verify Azure resources → ACR build → deploy image digest → health checks
```

The app serves a greeting at `/` and `Healthy` at `/health`. It has no external
dependencies; the health endpoint checks application availability, not downstream
services. The container runs as the built-in non-root .NET user on port 8080.

## Run locally

Install the .NET 10 SDK, Bash and curl; Docker is needed for container checks.
From the repository root:

```bash
dotnet restore src/DemoWeb/DemoWeb.csproj
dotnet format src/DemoWeb/DemoWeb.csproj --verify-no-changes --no-restore
dotnet publish src/DemoWeb/DemoWeb.csproj -c Release -o /tmp/demo-web
dotnet /tmp/demo-web/DemoWeb.dll --urls http://localhost:8080
# In another terminal:
bash tests/smoke.sh http://localhost:8080
```

```bash
docker build -t demo-web:local .
docker run --rm -p 8080:8080 demo-web:local
# In another terminal:
bash tests/smoke.sh http://localhost:8080
```

## Azure setup (before the live demo)

Use a dedicated demo subscription/resource group. These commands are **one-time
administrator setup**, not part of the deployment workflow. They create billable
resources. You need permission to create resources, identities and role
assignments; allow time for role assignments to propagate.

The following Bash example uses Azure CLI and a user-assigned identity for runtime
image pulls. Replace the subscription ID and globally unique registry name.
Use an ACR with classic registry RBAC (not the ABAC repository-permissions mode)
for these role assignments.

```bash
az login
SUBSCRIPTION_ID="<subscription-id>"
RG="rg-github-actions-demo"
LOCATION="westeurope"
ACR="<globally-unique-alphanumeric-registry-name>"
ACA_ENV="cae-github-actions-demo"
APP="ca-github-actions-demo"

az account set --subscription "$SUBSCRIPTION_ID"
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az extension add --name containerapp --upgrade
az group create --name "$RG" --location "$LOCATION"
az acr create --name "$ACR" --resource-group "$RG" --sku Basic --admin-enabled false
az acr config authentication-as-arm update --registry "$ACR" --status enabled
az containerapp env create --name "$ACA_ENV" --resource-group "$RG" --location "$LOCATION"
az identity create --name demo-image-pull --resource-group "$RG"

ACR_ID=$(az acr show --name "$ACR" --query id -o tsv)
ACR_SERVER=$(az acr show --name "$ACR" --query loginServer -o tsv)
PULL_ID=$(az identity show --name demo-image-pull --resource-group "$RG" --query id -o tsv)
PULL_PRINCIPAL=$(az identity show --name demo-image-pull --resource-group "$RG" --query principalId -o tsv)
az role assignment create --assignee-object-id "$PULL_PRINCIPAL" \
  --assignee-principal-type ServicePrincipal --role AcrPull --scope "$ACR_ID"

# Bootstrap with a public image; the workflow replaces it with this repo's image.
az containerapp create --name "$APP" --resource-group "$RG" \
  --environment "$ACA_ENV" --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --ingress external --target-port 8080 --min-replicas 1 --max-replicas 2 \
  --user-assigned "$PULL_ID" --revisions-mode single
az containerapp registry set --name "$APP" --resource-group "$RG" \
  --server "$ACR_SERVER" --identity "$PULL_ID"
```

Keep the ACR public network endpoint accessible for this hosted-runner demo.
Rehearse `az acr build`: ACR Tasks availability/quota can vary by subscription
(particularly trial subscriptions). Request access or use an eligible
subscription before presenting.

### OIDC deployment identity

Create a **separate** Microsoft Entra app registration and service principal for
GitHub deployment. In the Azure portal, add a federated credential with:

| Field | Value |
| --- | --- |
| Issuer | `https://token.actions.githubusercontent.com` |
| Subject | `repo:dbruun/github-actions:environment:demo` |
| Audience | `api://AzureADTokenExchange` |

The subject must match the actual repository owner/name and GitHub environment
exactly. No client secret is required. Assign these roles to the deployment
service principal (not the runtime image-pull identity):

| Scope | Role | Purpose |
| --- | --- | --- |
| Demo resource group | Reader | Verify resource group, registry, environment and app |
| Demo registry | Contributor | Run ACR Tasks builds |
| Demo registry | AcrPull | Read image digest after the build |
| Demo container app | Container Apps Contributor | Update the app and inspect revisions |

For example, an administrator can assign each role using
`az role assignment create --assignee-object-id "<deployment-service-principal-object-id>" --assignee-principal-type ServicePrincipal --role "<role>" --scope "<resource-id>"`.
Use the service principal **object ID** for assignments, and the app registration
**client ID** for GitHub configuration. For production, replace the registry
Contributor role with a reviewed custom role limited to necessary build operations.

### GitHub configuration

Create a GitHub environment named **`demo`** under Settings → Environments.
Restrict its deployment branches to **`main`**, and enable required reviewers
where supported by your GitHub plan. These protections matter: environment-based
OIDC trust does not itself restrict a branch. Do not allow arbitrary branches to
use this environment.

Add the following environment **variables** (Settings → Environments → demo):

| Variable | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | Deployment app registration client ID |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target subscription ID |
| `AZURE_RESOURCE_GROUP` | `RG` from setup |
| `AZURE_CONTAINER_REGISTRY` | `ACR` name, not its login-server hostname |
| `AZURE_CONTAINER_APP_ENVIRONMENT` | `ACA_ENV` from setup |
| `AZURE_CONTAINER_APP` | `APP` from setup |

These identifiers are not credentials. Do not add `AZURE_CREDENTIALS`, a client
secret, or an ACR admin password. Only the deployment job can request an OIDC
token; pull-request quality checks have read-only repository permissions.

Create a branch protection rule/ruleset for `main`: require pull requests, a
review, resolved conversations, and the **Branch quality** status check before
merging. Run the workflow once so that the status-check name is available.
Protect workflow changes with trusted review. The workflow supports manual
dispatch; deployment is still restricted to `main` and depends on passing quality
checks. Deployments are serialized and an in-flight deployment is not cancelled.

## What the pipeline verifies

Before building or updating Azure resources, it verifies configuration, tenant,
resource group, ACR, Container Apps environment and app. It also requires:

- Successfully provisioned resources and ACR ARM-audience authentication enabled.
- The app in the specified environment, with one container and single-revision mode.
- External ingress targeting port 8080 and an attached managed identity configured
  for the registry. Its `AcrPull` authorization is a setup prerequisite; an actual
  pull/role-propagation failure is caught during rollout.

Missing resources fail the deployment; the workflow does **not** silently
provision them. ACR builds the Dockerfile and tags the image with the commit SHA.
Deployment uses its immutable digest, waits for the new revision to be running
and ready, then smoke-tests the public HTTPS endpoint. The run summary includes
the URL, commit and revision. There is no automatic rollback; rehearse recovery
using a known-good commit and manual dispatch on `main`.

## Troubleshooting and cleanup

- OIDC errors: check issuer, audience, exact environment subject and client/tenant
  IDs; inspect the environment branch restrictions.
- Authorization failures: check each role's scope and allow propagation time.
- Image pull failures: check the runtime identity, `AcrPull`, registry network
  access and ARM-audience setting; this identity is different from GitHub's.
- Readiness failures: inspect revisions and `az containerapp logs show --name
  "$APP" --resource-group "$RG" --type console --follow`. The app must listen on 8080.
- Quality failures: run the README commands locally. Use `dotnet format
  src/DemoWeb/DemoWeb.csproj` to fix formatting, then review the diff.

After the demo, delete **only the dedicated demo resource group**:
`az group delete --name "$RG" --yes --no-wait`. Also remove the demo Entra app
registration/federated credential and any GitHub variables no longer needed.

See [the one-hour demo flow](demo-flow.md) and
[recommended additional actions](recommendations.md).

References: [Azure Login OIDC](https://learn.microsoft.com/azure/developer/github/connect-from-azure-openid-connect),
[managed-identity image pulls](https://learn.microsoft.com/azure/container-apps/managed-identity-image-pull),
[ACR quick builds](https://learn.microsoft.com/azure/container-registry/container-registry-tutorial-quick-task),
[GitHub environment protection](https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment).