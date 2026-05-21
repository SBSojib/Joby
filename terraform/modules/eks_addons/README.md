# EKS add-ons module

Terraform module that installs **Helm-managed cluster software** on Amazon EKS and creates **IRSA** IAM roles for workloads that call AWS APIs. Releases include the AWS Load Balancer Controller, External Secrets Operator, ExternalDNS, Metrics Server, Cluster Autoscaler, AWS for Fluent Bit, and the ADOT collector. This is separate from **EKS managed add-ons** (`aws_eks_addon` in [`module.eks`](../eks/main.tf), such as VPC CNI and CoreDNS).

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "eks_addons"`, always applied with the stack). The Helm provider in [`providers.tf`](../../providers.tf) must reach the cluster API; OIDC trust and scoped IAM policies tie each chart’s service account to AWS.

## Resources in this module

The module defines **twenty-six Terraform constructs** across [`main.tf`](main.tf) and [`iam.tf`](iam.tf): seven `helm_release` resources, six IRSA roles (`for_each`), IAM policy documents and inline policies or attachments, and locals. There are no standalone `aws_*` data sources beyond IAM policy document blocks.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `locals` (`name_prefix`, `oidc_provider`) | Terraform locals | IAM naming prefix and OIDC issuer host for trust **condition keys** | `name_prefix` prefixes role and policy names; `oidc_provider` from `var.oidc_provider_url` (`module.eks.cluster_oidc_issuer_url`) |
| `data.aws_iam_policy_document.assume_role` | IAM trust policies (data, `for_each`) | **`sts:AssumeRoleWithWebIdentity`** for six add-on service account subjects | Federated principal `var.oidc_provider_arn`; one map entry per add-on namespace and SA name (see `iam.tf`) |
| `aws_iam_role.addon` | IAM roles (`for_each`) | **IRSA roles** for charts that annotate `eks.amazonaws.com/role-arn` | Keys: `aws_load_balancer_controller`, `external_secrets`, `external_dns`, `cluster_autoscaler`, `fluent_bit`, `adot_collector`; ARNs passed into matching `helm_release` `set` blocks |
| `data.aws_iam_policy_document.aws_load_balancer_controller` | IAM permissions policy (data) | **ELB/EC2/WAF/ACM** API access for the load balancer controller | Feeds `aws_iam_role_policy.aws_load_balancer_controller` on `aws_iam_role.addon["aws_load_balancer_controller"]` |
| `aws_iam_role_policy.aws_load_balancer_controller` | Inline IAM role policy | Attaches controller permissions | Used by `helm_release.aws_load_balancer_controller` SA annotation |
| `data.aws_iam_policy_document.external_secrets` | IAM permissions policy (data) | **`secretsmanager:GetSecretValue`** and **`DescribeSecret`** on one secret | `resources = [var.application_secret_arn]` from `module.secrets.application_secret_arn` |
| `aws_iam_role_policy.external_secrets` | Inline IAM role policy | Least-privilege Secrets Manager read for ESO | `helm_release.external_secrets` SA annotation |
| `data.aws_iam_policy_document.external_dns` | IAM permissions policy (data) | **Route 53** record changes in the hosted zone plus list APIs | `route53:ChangeResourceRecordSets` on `var.route53_zone_arn` (`module.edge.hosted_zone_arn`) |
| `aws_iam_role_policy.external_dns` | Inline IAM role policy | DNS sync permissions for ExternalDNS | `helm_release.external_dns` SA annotation |
| `data.aws_iam_policy_document.cluster_autoscaler` | IAM permissions policy (data) | **Describe** autoscaling/EKS resources; **scale/terminate** only when tagged for this cluster | `aws:ResourceTag/k8s.io/cluster-autoscaler/${var.cluster_name}` condition on mutating actions |
| `aws_iam_role_policy.cluster_autoscaler` | Inline IAM role policy | Cluster Autoscaler AWS permissions | `helm_release.cluster_autoscaler` SA annotation |
| `aws_iam_role_policy_attachment.fluent_bit` | Managed policy attachment | **`CloudWatchAgentServerPolicy`** on the Fluent Bit role | `aws_iam_role.addon["fluent_bit"]`; `helm_release.aws_for_fluent_bit` |
| `aws_iam_role_policy_attachment.adot_xray` | Managed policy attachment | **`AWSXRayDaemonWriteAccess`** on the ADOT role | `aws_iam_role.addon["adot_collector"]` |
| `aws_iam_role_policy_attachment.adot_cloudwatch` | Managed policy attachment | **`CloudWatchAgentServerPolicy`** on the ADOT role | Same ADOT role as X-Ray attachment |
| `helm_release.aws_load_balancer_controller` | Helm release | **ALB/NLB** reconciliation for Kubernetes Ingress | `clusterName`, `region`, `vpcId` from `var.cluster_name`, `var.aws_region`, `var.vpc_id` (`module.eks`, `module.network`); chart repo `aws.github.io/eks-charts` |
| `helm_release.external_secrets` | Helm release | **External Secrets Operator** and CRDs | `installCRDs = true`; IRSA on `external-secrets` SA in `var.external_secrets_namespace` |
| `helm_release.external_dns` | Helm release | **DNS records** from Ingress hosts | AWS provider, `sync` policy, `ingress` source; IRSA on `external-dns` SA |
| `helm_release.metrics_server` | Helm release | **Resource metrics** API for HPA and `kubectl top` | `kube-system`; **no** IRSA role in this module |
| `helm_release.cluster_autoscaler` | Helm release | **Node group scaling** from pending pods | `autoDiscovery.clusterName`; extra args `balance-similar-node-groups`, `skip-nodes-with-system-pods = false` |
| `helm_release.aws_for_fluent_bit` | Helm release | **Container logs** to CloudWatch | Log group `/aws/eks/${var.cluster_name}/application`, `autoCreateGroup = true`; IRSA via Fluent Bit role |
| `helm_release.adot_collector` | Helm release | **OpenTelemetry** export on EC2 node groups | Chart `adot-exporter-for-eks-on-ec2`; IRSA on `adot-collector` SA in `var.observability_namespace` |

## How this module connects to the rest of the stack

**Inputs from other modules and root (`module "eks_addons"` in [`main.tf`](../../main.tf)):**

- `cluster_name`, `oidc_provider_arn`, and `oidc_provider_url` from `module.eks`.
- `vpc_id` from `module.network.vpc_id` (load balancer controller).
- `route53_zone_arn` from `module.edge.hosted_zone_arn` (ExternalDNS).
- `application_secret_arn` from `module.secrets.application_secret_arn` (External Secrets IAM scope).
- `aws_region`, `project_name`, `environment`, and `tags` from root.
- `depends_on = [module.eks]` at root; Helm still needs a **reachable** Kubernetes API (see [`terraform/README.md`](../../README.md)).

**Not defined inside this module:**

- EKS cluster, node groups, and **`aws_eks_addon`** resources in [`module.eks`](../eks/main.tf).
- [`module.backend_irsa`](../irsa/main.tf) for application S3 access (separate IRSA pattern).
- Kubernetes manifests: [`k8s/eks/clustersecretstore.yaml`](../../../k8s/eks/clustersecretstore.yaml), [`externalsecret.yaml`](../../../k8s/eks/externalsecret.yaml), Ingress, and Deployments (including ALB annotations and DNS hostnames).
- Root [`aws_cloudwatch_log_group.app`](../../main.tf) used by [`module.monitoring`](../monitoring/main.tf) — Fluent Bit here targets **`/aws/eks/<cluster_name>/application`**, not that root log group.

**Downstream consumers:**

- Root outputs (when add-ons enabled): `external_secrets_role_arn`, `aws_load_balancer_controller_role_arn`, `external_dns_role_arn`, `adot_collector_role_arn`.
- In-cluster operators and controllers installed by Helm; ExternalSecret resources use output `cluster_secret_store_name` (`aws-secrets-manager`) once `ClusterSecretStore` exists in the cluster.

**Typical apply order:** `module.eks` (and OIDC provider) → `module.secrets` and `module.edge` (for secret ARN and zone ARN) → this module (IAM roles/policies, then Helm releases) in the same `terraform apply` when the EKS API is reachable. Apply Kubernetes app manifests and confirm SNS or DNS only after controllers are running.

## Notable parameters

### Providers

- **Helm / Kubernetes providers** (root) — Use `aws eks get-token` against `module.eks`; the principal running Terraform needs API access to the cluster endpoint during apply.

### IRSA trust (security)

- **Six bound subjects** — Hard-coded in `data.aws_iam_policy_document.assume_role` map (namespace/SA per add-on); must match chart service account names and namespaces.
- **`aud` = `sts.amazonaws.com`** — Hard-coded on all trust policies.
- **External Secrets** — IAM limited to **`var.application_secret_arn`** only (not `*`).
- **ExternalDNS** — Record changes scoped to **`var.route53_zone_arn`**; list APIs on `*`.
- **Load balancer controller** — Broad `resources = ["*"]` on many ELB/EC2/WAF actions (typical upstream controller policy shape).
- **Cluster Autoscaler** — Scale/terminate conditioned on **`k8s.io/cluster-autoscaler/<cluster_name> = owned`** tag.

### Helm chart settings (fixed vs variables)

- **Namespaces** — Variables with defaults: ALB controller `kube-system`, External Secrets `external-secrets`, ExternalDNS `external-dns`, observability `amazon-cloudwatch`; Metrics Server and Autoscaler use **`kube-system`** in HCL.
- **ExternalDNS** — `policy = sync`, `sources[0] = ingress` (hard-coded).
- **External Secrets** — `installCRDs = true` (hard-coded).
- **Fluent Bit** — Log group name pattern and **`autoCreateGroup = true`** hard-coded; differs from root app log group used by metric filters in monitoring.
- **Cluster Autoscaler** — `skip-nodes-with-system-pods = false` and `balance-similar-node-groups = true` (hard-coded).
- **Chart versions** — Not pinned in Terraform (Helm provider default chart version behavior).

### Observability and cost

- **Fluent Bit + ADOT** — AWS managed policies for CloudWatch and X-Ray; ongoing log and telemetry ingestion charges.
- **Metrics Server** — No extra AWS IAM in this module; cluster CPU/memory metrics only.

### Fixed in HCL (not module variables)

- Controller IAM action lists, autoscaler tag condition key shape, managed policy ARNs for Fluent Bit and ADOT, and most Helm `set` values beyond cluster/region/VPC/role ARNs.

## Module outputs

| Output | Use |
| --- | --- |
| `aws_load_balancer_controller_role_arn` | Root output; verify ALB controller IRSA annotation |
| `external_secrets_role_arn` | Root output; ESO Secrets Manager access |
| `external_dns_role_arn` | Root output; ExternalDNS Route 53 access |
| `cluster_secret_store_name` | Constant `aws-secrets-manager`; `ExternalSecret` `secretStoreRef` name (store manifest is outside this module) |
| `adot_collector_role_arn` | Root output; ADOT collector IRSA |

## Source files

- [`main.tf`](main.tf) — Helm releases for controllers and observability add-ons
- [`iam.tf`](iam.tf) — Locals, IRSA trust, IAM policies and attachments
- [`variables.tf`](variables.tf) — Module inputs and namespace defaults
- [`outputs.tf`](outputs.tf) — IRSA role ARNs and ClusterSecretStore name constant
