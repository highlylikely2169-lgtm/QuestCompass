# GitHub Pages deployment

The static production site is staged in `docs\`. It includes only the website, public addon downloads, launcher download, and update feed; it excludes local .NET build outputs.

## Publish

1. Create a GitHub repository for QuestCompass. GitHub Pages on the free plan requires a public repository.
2. Add the project files to the repository and push them to its default branch.
3. In the repository settings, open **Pages** and select **Deploy from a branch**. Choose the default branch and `/docs` as the folder.
4. Configure `questcompass.online` as the custom domain in Pages settings. The `docs\CNAME` file is already staged.
5. At the domain DNS provider, set these apex A records: `185.199.108.153`, `185.199.109.153`, `185.199.110.153`, and `185.199.111.153`. Optionally add GitHub's recommended IPv6 AAAA records. For `www`, add a CNAME to `highlylikely2169-lgtm.github.io`. Remove conflicting old A/AAAA/CNAME records, wait for DNS propagation, then enable **Enforce HTTPS** in Pages settings.

GitHub Pages deployment and DNS are not configured from this local folder. Verify the site and HTTPS certificate in a browser after GitHub reports the Pages deployment as successful.

## Refreshing a release

1. Update the public addon ZIP and `updates\latest.json` (version, absolute download URL, and SHA-256).
2. Update the bundled ZIP/version/hash in the launcher project as needed, then publish a new self-contained launcher and replace its ZIP under `downloads\`.
3. Copy the updated `index.html`, `downloads\`, and `updates\` into `docs\`, preserving the existing `CNAME` and `.nojekyll`.
4. Push the changes to GitHub. The launcher checks `https://questcompass.online/updates/latest.json`.
