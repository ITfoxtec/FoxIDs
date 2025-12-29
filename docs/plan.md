<!--
{
    "title":  "Plans",
    "description":  "Overview of FoxIDs plans for shared cloud deployments, including how plans are defined in the master tenant and linked to tenants for billing.",
    "ogTitle":  "Plans",
    "ogDescription":  "Overview of FoxIDs plans for shared cloud deployments, including how plans are defined in the master tenant and linked to tenants for billing.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "plan, FoxIDs docs"
                       }
}
-->

# Plans

FoxIDs is a cloud application designed as a container with multi-tenant support. FoxIDs can be deployed and use by e.g., a single company or deployed as a shared cloud container and used by multiple organisations, companies or everyone with the need.

Plans is for a shared cloud deployment like on [FoxIDs.com](https://foxids.com) to be able to calculate payments and send invoices in an external system.

Plans is defined and connected to tenants in the `master` tenant `master` environment.

A plan is configured with:

- Plan name and a text
- Currency and cost per month
- Included usage in two levels for
	- Users
	- Logins per month
	- Token requests per month
	- Control API gets per month
	- Control API updates per month
- Optionally plan specific Application Insights and Log Analytics Workspace

![Plan configuration](images/configure-plan.png)

Tenants can optionally be connected to a plan

![Configure plan on tenant](images/configure-plan-tenant.png)


