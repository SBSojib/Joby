# Joby – AWS Infrastructure (Terraform)

Terraform configuration that provisions a free-tier-friendly AWS environment for the **Joby** Job Application Tracker & Discovery Platform.

## What gets created

| Resource | Purpose |
|---|---|
| **EC2 instance** (Ubuntu 24.04, t3.micro) | Hosts the Docker Compose application stack |
| **RDS PostgreSQL 16** (db.t3.micro, 20 GB gp3) | Managed database, private, not publicly accessible |
| **S3 bucket** | Resume/file uploads with encryption & all public access blocked |
| **IAM role + instance profile** | Gives EC2 least-privilege access to S3 and CloudWatch Logs |
| **Security groups** | EC2: HTTP/HTTPS/SSH inbound; RDS: PostgreSQL only from EC2 |
| **CloudWatch log group** | Ready for log shipping from the application |
| **Elastic IP** *(optional)* | Static public IP that survives instance stop/start |

### Architecture diagram

```
Internet
  │
  ▼
┌──────────────────────────────────┐
│  EC2 (Ubuntu 24.04)              │
│  ├── Docker + Docker Compose     │
│  ├── Joby Backend  (.NET 8)      │
│  ├── Joby Frontend (React/Nginx) │
│  └── IAM Role ──► S3 + CW Logs  │
└──────────┬───────────────────────┘
           │ port 5432 (SG-to-SG)
           ▼
┌──────────────────────────────────┐
│  RDS PostgreSQL 16               │
│  (private, not publicly          │
│   accessible, encrypted)         │
└──────────────────────────────────┘
```

## Prerequisites

1. **AWS account** – sign up at <https://aws.amazon.com> if you don't have one
2. **Terraform** ≥ 1.5 – [install guide](https://developer.hashicorp.com/terraform/install)
3. **AWS CLI** – [install guide](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
4. **An EC2 key pair** – see [Create a key pair](#create-a-key-pair) below

## Set up AWS credentials

Configure the AWS CLI with your IAM user credentials:

```bash
aws configure
# AWS Access Key ID:     <your-access-key>
# AWS Secret Access Key: <your-secret-key>
# Default region name:   us-east-1
# Default output format: json
```

Terraform uses the same credentials stored in `~/.aws/credentials`.

> **Tip:** For better security, use [IAM Identity Center (SSO)](https://docs.aws.amazon.com/singlesignon/latest/userguide/what-is.html) or short-lived credentials instead of long-lived access keys.

## Create a key pair

1. Open the [EC2 Console → Key Pairs](https://console.aws.amazon.com/ec2/home#KeyPairs:)
2. Click **Create key pair**
3. Name: `joby-key` (or whatever you like)
4. Type: **RSA**, Format: **.pem**
5. Download the `.pem` file and move it to `~/.ssh/`
6. Restrict permissions: `chmod 400 ~/.ssh/joby-key.pem`
7. Set `key_pair_name = "joby-key"` in your `terraform.tfvars`

## Quick start

```bash
cd terraform

# 1. Create your variables file
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values (key pair, SSH CIDR, DB password, …)

# 2. Initialize Terraform (downloads providers)
terraform init

# 3. Preview what will be created
terraform plan

# 4. Create the infrastructure
terraform apply

# 5. Review outputs
terraform output
```

## Connecting to your EC2 instance

After `terraform apply` completes, use the SSH command from the outputs:

```bash
# Copy the ssh_command output, or run:
ssh -i ~/.ssh/joby-key.pem ubuntu@$(terraform output -raw ec2_public_ip)
```

> **Note:** SSH access requires that your IP is in `allowed_ssh_cidrs`. Find your IP at <https://checkip.amazonaws.com>.

## Verify RDS connectivity from EC2

Once SSH'd into the instance:

```bash
# psql was installed by the bootstrap script
psql -h $(grep DATABASE_HOST /opt/joby/.env | cut -d= -f2) \
     -U joby_admin \
     -d joby
# Enter your database password when prompted
```

## Test S3 access from EC2

The instance role gives you S3 access without any AWS keys:

```bash
# List the bucket
aws s3 ls s3://$(grep S3_BUCKET_NAME /opt/joby/.env | cut -d= -f2)

# Upload a test file
echo "test upload" > /tmp/test.txt
aws s3 cp /tmp/test.txt s3://$(grep S3_BUCKET_NAME /opt/joby/.env | cut -d= -f2)/test.txt

# Clean up
aws s3 rm s3://$(grep S3_BUCKET_NAME /opt/joby/.env | cut -d= -f2)/test.txt
```

## Deploy the application

After infrastructure is up:

1. SSH into the EC2 instance
2. Edit `/opt/joby/.env` — replace `CHANGE_ME_TO_YOUR_DB_PASSWORD` with your real password
3. Clone the application repo into `/opt/joby`
4. If you cloned into a subfolder, copy the bootstrap `.env` into the repo folder
5. Build or pull images, then start:

```bash
cd /opt/joby

# Option A (recommended): clone directly into /opt/joby
git clone https://github.com/SBSojib/Joby.git .

# Option B: clone into a subfolder (creates /opt/joby/Joby)
git clone https://github.com/SBSojib/Joby

# If you used Option B, copy the bootstrap env file into the repo folder
cp /opt/joby/.env /opt/joby/Joby/.env

# Start the app (run from the repo folder)
cd /opt/joby/Joby
docker compose up -d
docker compose ps
docker compose logs -f
```

## Tear down

```bash
terraform destroy
```

> If `db_deletion_protection = true`, you must first set it to `false` and run `terraform apply` before you can destroy.

## Estimated costs & free tier notes

| Resource | Free tier (first 12 months) | After free tier |
|---|---|---|
| EC2 t3.micro | 750 hrs/month | ~$7.60/month |
| RDS db.t3.micro | 750 hrs/month + 20 GB storage | ~$13/month |
| S3 | 5 GB + 20k GET + 2k PUT | ~$0.02/GB/month |
| EBS (gp3, 20 GB) | 30 GB/month | ~$1.60/month |
| Elastic IP | Free while attached to running instance | $3.65/month if idle |
| CloudWatch Logs | 5 GB ingest + 5 GB storage | $0.50/GB ingest |
| Data transfer | 100 GB/month outbound | $0.09/GB after |

**Within free tier:** effectively $0/month if you stay within limits.

> **Warning:** Leaving resources running after free tier expiration costs ~$25/month. Always `terraform destroy` when not in use.

## Terraform state

This project uses **local state** (stored in `terraform.tfstate`). This is fine for solo learning.

To migrate to remote state later (recommended for teams):

```hcl
# Add to versions.tf
terraform {
  backend "s3" {
    bucket         = "your-terraform-state-bucket"
    key            = "joby/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "terraform-locks"
    encrypt        = true
  }
}
```

Then run `terraform init -migrate-state`.

## Default VPC trade-off

This setup uses the **default VPC** for simplicity. All subnets in the default VPC are public (have internet gateway routes). RDS is placed in these subnets but configured with `publicly_accessible = false`, which means:

- RDS has no public DNS / no public IP
- Only the EC2 security group can reach port 5432
- This is secure enough for learning and small projects

For production workloads, create a **custom VPC** with truly private subnets for the database.

## Next phase — what to add later

### Route53 + ACM + HTTPS
- Register or transfer a domain in Route53
- Request a free ACM certificate for your domain
- Configure Nginx on EC2 to terminate TLS, or add an ALB with the ACM cert

### CI/CD
- GitHub Actions workflow: build images → push to ECR → SSH deploy or `docker compose pull && up`
- Or use AWS CodePipeline / CodeDeploy

### CloudWatch Agent
- Install the CloudWatch agent on EC2 to ship system metrics and application logs
- The IAM role already has CloudWatch Logs permissions

### SSM Session Manager
- Attach the `AmazonSSMManagedInstanceCore` policy to the EC2 role
- Connect to the instance without SSH keys or open port 22

### Secrets Manager / Parameter Store
- Store `db_password` and other secrets in AWS Secrets Manager or SSM Parameter Store
- Retrieve them at container startup instead of baking into `.env`

### Monitoring & Alerts
- CloudWatch alarms for CPU, disk, RDS connections
- SNS topic for email notifications

### Custom VPC
- Create a VPC with public + private subnets
- Move RDS into private subnets with no internet route
- Add a NAT gateway only if the private subnet needs outbound internet

## Files

```
terraform/
├── main.tf                     # Root module – data sources, locals, module calls
├── variables.tf                # All input variables with validation
├── outputs.tf                  # Useful outputs and next-steps guide
├── providers.tf                # AWS provider config with default tags
├── versions.tf                 # Terraform and provider version constraints
├── terraform.tfvars.example    # Example variable values (copy to terraform.tfvars)
├── README.md                   # This file
└── modules/
    ├── security/               # EC2 and RDS security groups
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── iam/                    # IAM role, policies, instance profile
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── s3/                     # S3 bucket with encryption and public access block
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── ec2/                    # EC2 instance, EIP, user data bootstrap
    │   ├── main.tf
    │   ├── variables.tf
    │   ├── outputs.tf
    │   └── user_data.sh.tftpl
    └── rds/                    # RDS PostgreSQL instance and subnet group
        ├── main.tf
        ├── variables.tf
        └── outputs.tf
```
