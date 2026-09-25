# Unraid Community Applications template

`gamarr.xml` is the Community Applications (CA) container template for Gamarr.
It defines the image, the web UI port, and the `/config`, `/games` and
`/downloads` paths CA prompts for on install.

## Getting it listed in CA

Two routes; either works, and the first is faster to get accepted.

1. **PR to the selfhosters community template repo** — fork
   [selfhosters/unRAID-CA-templates](https://github.com/selfhosters/unRAID-CA-templates),
   copy `gamarr.xml` into `templates/`, and open a PR. That repo is already
   indexed by CA, so once merged Gamarr appears in the app store.

2. **Register our own template repository** — post in the unraid forums thread
   for new template repositories (Community Applications → "Template
   repositories") pointing at this repo. CA's moderators add the repo to the
   appfeed and it gets scraped from `distribution/unraid/`.

## Testing locally

On an unraid box, either:

- Docker tab → Add Container → Template dropdown, and paste the raw URL of
  `gamarr.xml` (the `TemplateURL` value inside the file:
  `https://raw.githubusercontent.com/gamarr-app/Gamarr/main/distribution/unraid/gamarr.xml`).
- Or drop the file into `/boot/config/plugins/dockerMan/templates-user/`.

Keep `TemplateURL` pointing at this file's raw `main` URL so CA can pick up
template updates.
