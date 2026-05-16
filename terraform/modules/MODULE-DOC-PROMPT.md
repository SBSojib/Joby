# Prompt: document a Terraform module (Joby style)

Copy the block below into a new chat. Replace the placeholders, attach or `@`-reference the module folder, and run one module at a time.

---

```text
You are documenting a Terraform module in the Joby repo for someone learning AWS. I have on-prem data-center experience (e.g. SQL Server clustering) but am newer to AWS.

Module path: @terraform/modules/<MODULE_NAME>/

Also read how root wires this module in terraform/main.tf (and any related root resources that are NOT inside the module but required for it to work).

Deliver a markdown file at terraform/modules/<MODULE_NAME>/README.md with this structure only:

1. Title and short overview (2–4 sentences): what the module provisions and its role in the stack.
2. "Resources in this module" — a table with columns: Resource / data source | What it is | Purpose | How it connects.
   - Include every resource, data source, and other Terraform object defined in the module .tf files.
   - "How it connects" should name other resources in the module, root module inputs/outputs, and other Joby modules (e.g. module.network) where relevant.
3. "How this module connects to the rest of the stack" — prose (no diagrams): upstream inputs, downstream consumers, resources defined outside the module but required for correct operation, and typical apply/dependency order.
4. "Notable parameters" — only important or non-obvious settings (group by theme: network, security, scaling, lifecycle, observability, etc.). Skip exhaustive line-by-line HCL commentary.
   - Call out values hard-coded in the module vs exposed as variables.
   - Call out defaults that matter for cost, security, or availability.
5. "Module outputs" — short table: output name | what it is used for.
6. "Source files" — bullet list linking to main.tf, variables.tf, outputs.tf (and any other .tf files in the module).

Constraints:
- Text only; no mermaid, ASCII art, or other diagrams.
- No line-by-line walkthrough of every attribute.
- Use relative markdown links to sibling .tf files and to ../../main.tf where appropriate.
- Match the tone and depth of terraform/modules/rds/README.md in this repo.
- Do not edit unrelated modules or root Terraform unless you find a factual error needed for the doc.
- Write only the README.md for this module; do not create extra learning guides unless I ask.

After writing the file, reply briefly with what you documented and any gaps (e.g. behavior only visible in root main.tf).
```

---

## Tips

- Run **one module per chat** so the table stays accurate.
- If the module is thin (only outputs or a wrapper), say so in the overview and keep the resources table honest.
- For modules split across multiple `.tf` files, still use one README in the module root.
