# GitHub Pages deployment

The static production site is staged in `docs\`. It includes the website, public GPL addon archive, and update feed; it excludes launcher binaries and local .NET build outputs.

The site is available at `https://highlylikely2169-lgtm.github.io/QuestCompass/`. The custom domain `questcompass.online` is not active until its DNS records are configured.

Monobank checkout and protected launcher delivery are not implemented. GitHub Pages is public static hosting and cannot verify payments or protect a download URL. Keep launcher packages off this site until a payment backend and private delivery flow are in place. The GPL addon and source remain publicly available under their license.

## Publish

1. Create a GitHub repository for QuestCompass. GitHub Pages on the free plan requires a public repository.
2. Add the project files to the repository and push them to its default branch.
3. In the repository settings, open **Pages** and select **Deploy from a branch**. Choose the default branch and `/docs` as the folder.
4. After configuring the domain DNS, add `questcompass.online` as the custom domain in Pages settings and add a `CNAME` file containing that domain to `docs\`.
5. At the domain DNS provider, set these apex A records: `185.199.108.153`, `185.199.109.153`, `185.199.110.153`, and `185.199.111.153`. Optionally add GitHub's recommended IPv6 AAAA records. For `www`, add a CNAME to `highlylikely2169-lgtm.github.io`. Remove conflicting old A/AAAA/CNAME records, wait for DNS propagation, then enable **Enforce HTTPS** in Pages settings.

GitHub Pages deployment and DNS are not configured from this local folder. Verify the site and HTTPS certificate in a browser after GitHub reports the Pages deployment as successful.

## Refreshing a release

1. Update the public GPL addon ZIP and `updates\latest.json` (version, absolute download URL, and SHA-256).
2. Copy the updated site, public addon ZIP, and update manifest into `docs\`.
3. Push the changes to GitHub. The current launcher can check the public manifest at `https://highlylikely2169-lgtm.github.io/QuestCompass/updates/latest.json` and install a newer package.
4. For paid launcher delivery, implement and verify Monobank payments on a server before distributing any launcher through a paid flow. Do not publish private packages or payment secrets in this repository.
