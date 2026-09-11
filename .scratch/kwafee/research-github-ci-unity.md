# GitHub Actions CI/CD for Unity 6 (6000.6.0f1) — primary-source research

Target engine: **Unity 6000.6.0f1** (this repo's pin, `kwaffee/ProjectSettings/ProjectVersion.txt`). Unity project lives in `kwaffee/` subtree, so `projectPath: kwaffee` everywhere.

Verification legend: **[V]** = verified first-hand against the cited source while writing this; **[V-API]** = verified via Docker Hub/GitHub API (listings below are real query output); **[NV]** = could not be verified against a primary source (says which source was missing); claims without a tag are direct quotes/paraphrases of the cited page.

---

## 1. Does GameCI (game-ci org) support Unity 6 / 6000.x?
- Unity Actions (unity-builder, unity-test-runner, unity-activate, unity-return-license) are the maintained GameCI set, and any Unity version in the supported list can be used — "Supported Unity versions: Unity Actions are using game-ci/docker since unity-builder version 2. Any version in this list can be used." https://game.ci/docs/github/getting-started/
- GameCI publishes ready-to-run Docker images (`unityci/editor:<version>-<module>-<imagever>`) with Unity preinstalled; that is how Unity runs on GitHub-hosted Linux runners. https://game.ci/docs/faq/
- Images are auto-built for every new non-alpha/non-beta Unity release: pipelines poll Unity's archive every 15 minutes; a new version typically appears on Docker Hub a few hours after release. https://game.ci/docs/faq/
- Alpha/beta Unity versions are NOT published — "Do you have docker images for alpha and beta versions of Unity? No" (tracked in game-ci/docker#50). https://game.ci/docs/faq/
- **[V-API]** Image tag for this exact engine exists: `unityci/editor:ubuntu-6000.6.0f1-base-3`, `-3.2`, `-3.2.2`, plus module variants (`-webgl-3`, `-linux-il2cpp-3`, `-android-3`, `-ios-3`, `-mac-mono-3`, `-windows-mono-3`, …), listed by the Docker Hub tags API (queried 2026-09-10). https://hub.docker.com/r/unityci/editor/tags
- **[V]** Image version suffix: `-0`/`-1`/`-2` tags are no longer published; current line is version `3` (latest `3.2.2`), and actions default `containerRegistryImageVersion: "3"`. https://game.ci/docs/troubleshooting/common-issues/ , https://github.com/game-ci/docker/releases (v3.2.2, 2026-03-18)
- **[V]** The "unity-version-manager" action named in the task does NOT exist in the game-ci GitHub org. Org repo listing (queried via API) shows the activation-related repos `unity-activate`, `unity-return-license`, `unity-license-activate`, `versioning-backend` — no `unity-version-manager`, no `unity-versions`. https://github.com/orgs/game-ci/repositories
- **[V]** Action tags: `game-ci/unity-builder` latest release is v6.0.0 with v4/v5 lines still published; `game-ci/unity-test-runner` latest is v4.4.0 (v5.0.0-beta.1 exists). Official docs examples use `@v4` for both. https://github.com/game-ci/unity-builder/releases , https://github.com/game-ci/unity-test-runner/releases
- If a Unity version is not (yet) on Docker Hub, GameCI's documented answer is: roll back to a nearby version or wait for the 15-min build pipeline; there is no generic fallback image that "adapts" to an unlisted version. https://game.ci/docs/faq/

## 2. Canonical GameCI workflow: headless EditMode tests on a GitHub-hosted runner
- Canonical shape (checkout with LFS → cache `Library` → `game-ci/unity-test-runner@v4` → upload artifacts) is the documented "Getting started" example, which runs tests and a WebGL build on `ubuntu-latest`. https://game.ci/docs/github/getting-started/
- Exact action inputs (from the action's own `action.yml`, the primary source): `projectPath` (relative to repo root), `testMode` (default `all`), `artifactsPath` (default `artifacts`), `githubToken` (defaults to `${{ github.token }}`), `checkName` (default `Test Results`), `unityVersion` (default `auto`, reads `ProjectSettings/ProjectVersion.txt`), `customImage`, `coverageEnabled` (default `true`), `coverageOptions`, `containerRegistryRepository` (`unityci/editor`), `containerRegistryImageVersion` (`3`), plus license env vars. https://github.com/game-ci/unity-test-runner/blob/main/action.yml
- `testMode` options are `All`, `PlayMode`, `EditMode`, `Standalone` (case-insensitive in practice; docs spell them `all`/`playmode`/`editmode`/`standalone`); `All` = PlayMode + EditMode only — "You must explicitly specify `Standalone` for it to run." https://game.ci/docs/github/test-runner/
- Personal license job (this repo's case): the test step passes `UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}` plus `UNITY_EMAIL`/`UNITY_PASSWORD` env vars; `ENV` vars are required because GameCI reactivates the license in the container on every step. https://game.ci/docs/github/test-runner/
- For this repo pass `projectPath: kwaffee` (project is in a subdirectory); the documented full-example matrix uses `projectPath: <subdir>` exactly this way. https://game.ci/docs/github/test-runner/
- Cache `kwaffee/Library` keyed on `hashFiles('Assets/**','Packages/**','ProjectSettings/**')`; docs claim >50% faster reruns. https://game.ci/docs/github/test-runner/ , https://game.ci/docs/github/builder/
- **Unity 6-specific:** set `coverageEnabled: false`. The action's own docs: coverage "has been the root cause of crashes and compile errors on some Unity versions (obsolete-API-as-error issues in com.unity.testtools.codecoverage, and a PlayMode SIGSEGV on Unity 6)". https://game.ci/docs/github/test-runner/ , https://github.com/game-ci/unity-test-runner/blob/main/action.yml

## 3. Unity 6 headless EditMode testing on Linux (Docker on GitHub-hosted runner)
- The test runner runs Unity inside a Docker container pulled from Docker Hub on the Linux runner; the Linux implementation historically always appended `-enableCodeCoverage` (that unconditional flag was the reported bug). https://github.com/game-ci/unity-test-runner/issues/302
- **[V]** Issue #301 — Unity 6000.3.8f1 PlayMode crashes with SIGSEGV exit code 139 before any test code runs; on the SAME project "EditMode tests pass without issue". Conclusion: EditMode is the reliable mode for Unity 6 in GameCI today. https://github.com/game-ci/unity-test-runner/issues/301
- **[V]** Issue #302 — the Linux runner forced code coverage on, which crashes certain Unity versions before tests execute; fixed by the `coverageEnabled: false` input (default remains `true`). https://github.com/game-ci/unity-test-runner/issues/302
- **[V]** Issue #306 — Unity 6000.5.1f1 package-mode tests fail compiling the Code Coverage package (obsolete `TreeView` API as fatal `CS0619`); again coverage-related, not a `-nographics` problem. https://github.com/game-ci/unity-test-runner/issues/306
- **[V]** Issue #307 — Unity 6.6 **beta** / 6.7 **alpha** fail in Docker with "Insufficient shared memory" (Unity asks 1 GiB, Docker gives 64 MiB); images up to 6.5 complete fine. Unity 6000.6.0f1 is a stable 6.6 release; whether the stable 6.6 image hits this was NOT verified in a tracked issue. https://github.com/game-ci/unity-test-runner/issues/307
- **[NV]** No GameCI issue was found that shows EditMode tests failing specifically because of `-nographics`. Unity's docs do note `-nographics` skips graphics-device init, which can affect tests that need rendering/GPU — but that's a general caveat, never tied to Unity 6 EditMode in a tracked GameCI issue. https://docs.unity3d.com/6000.6/Documentation/Manual/EditorCommandLineArguments.html
- Unity officially supports headless batchmode CI (`-batchmode -quit -projectPath …`); the Manual's canonical URL for this page is versioned 6000.6, i.e. current for this engine. https://docs.unity3d.com/6000.6/Documentation/Manual/build-command-line.html

## 4. Alternatives to GameCI, and when to prefer them
- **Raw Unity CLI in a workflow** — Unity's official Manual documents `-batchmode`/`-quit`/`-projectPath`/`-buildTarget`/`-activeBuildProfile`/`-executeMethod` command-line builds for CI. Preferred when you want zero community-dependency and full control of the Unity invocation; you still must install Unity on the runner every time (or use a container) and still need license activation. https://docs.unity3d.com/6000.6/Documentation/Manual/build-command-line.html
- **[V]** **uzak/unity-activation does not exist** — the GitHub repo 404s (checked 2026-09-10). Related real alternatives: `mob-sakai/unity-activate` (archived, `archived: true` per GitHub API), and `RageAgainstThePixel/activate-unity-license` (active, activates by username/password/serial instead of a .ulf secret). https://github.com/mob-sakai/unity-activate , https://github.com/RageAgainstThePixel/activate-unity-license
- **Custom/self-hosted runner image** — GameCI's own images are MIT-licensed and built from `game-ci/docker`; you can fork the Dockerfiles, or point the actions at your own registry via `customImage` / `containerRegistryRepository` / `containerRegistryImageVersion` (supports `ghcr.io/...`). Preferred when you need preinstalled extra tooling, airtight image provenance, or to avoid Docker Hub pull volume; costs are image maintenance and build time. https://game.ci/docs/github/test-runner/ , https://github.com/game-ci/docker (MIT license)
- **Self-hosted runners** — GitHub's docs position them for when hosted runner hardware/OS/networking isn't adequate (or to avoid per-minute billing on private repos); GameCI's FAQ explicitly links self-hosted runners as the "own machines" option and notes `runAsHostUser` exists for self-hosted permission issues. https://docs.github.com/en/actions/hosting-your-own-runners/about-self-hosted-runners , https://game.ci/docs/faq/
- Trade-off summary (our situation): hosted Linux runner + GameCI = zero installation time, only cost is the ~GB-scale image pull each run; personal license activation is one-time manual; nothing else needed. A raw-CLI workflow only makes sense after you already committed to a custom image — at which point GameCI would still do the same thing with less YAML.
- GameCI does not publish macOS Docker images (impossible), so macOS work runs directly on the host runner, not in Docker. https://game.ci/docs/faq/

## 5. License activation flow for GitHub-hosted runners
- Personal license flow (canonical): activate a free Personal license once via Unity Hub (Preferences → Licenses → Add → "Get a free personal license") → a `.ulf` file is written (Windows `C:\ProgramData\Unity\Unity_lic.ulf`; macOS `/Library/Application Support/Unity/Unity_lic.ulf`; Linux `~/.local/share/unity3d/Unity/Unity_lic.ulf`) → store its full contents as the `UNITY_LICENSE` GitHub Actions secret, plus `UNITY_EMAIL` and `UNITY_PASSWORD` secrets. https://game.ci/docs/github/activation/
- "Licenses are not tied to a specific Unity version or platform" — you can activate on any OS and use the same `.ulf` for builds on another (e.g., activated on macOS arm64 dev machine, used on Ubuntu x64 CI runner). https://game.ci/docs/github/activation/
- **[V]** Unity hub no longer exposes the manual Personal activation option in the UI; documented workaround: upload the `.alf` file at license.unity3d.com/manual and unhide the Personal option in the page HTML, or activate from the command line per Unity's Manual. https://game.ci/docs/troubleshooting/common-issues/ (references game-ci/documentation#408 and https://docs.unity3d.com/Manual/ManagingYourUnityLicense.html)
- The old `unity-request-activation-file` action ("createManualActivationFile") is DEPRECATED: its README says "This action is no longer maintained and should not be used." The v4 flow is the manual .ulf-secret method above. https://github.com/game-ci/unity-request-activation-file/blob/main/README.md
- Professional (paid Plus/Pro) licenses instead use `UNITY_SERIAL` + email/password secrets, no .ulf; `unity-activate`/`unity-return-license` are optional verification steps — "Test runner and Builder already include these steps." https://game.ci/docs/github/activation/
- Floating/team licenses: provide `unityLicensingServer: <url>` and the action acquires a floating license before the step and returns it after; this is the only path that avoids committing a long-lived credential secret. https://game.ci/docs/github/builder/ , https://game.ci/docs/github/test-runner/
- Re-activation per run: GameCI hardcodes a stable `machine-id` into every published image precisely so the license does NOT need re-activation when the runner or Unity version changes; each run is on a fresh ephemeral runner, and the action writes/activates the license from the secrets at the start of the step. https://game.ci/docs/faq/
- Caveat from GameCI docs: `UNITY_PASSWORD` with special characters has caused issues; mixed-case alphanumeric passwords recommended. https://game.ci/docs/github/builder/

## 6. Unity 6 on macOS arm64 (Apple Silicon) hosted runners vs Linux
- [`V]` All current macOS hosted-runner labels are ARM64: `macos-latest` and `macos-15` are macOS 26 / 15 **arm64 (M1-family)** VMs, and `macos-14` is also arm64; x64 macOS is only `macos-15-intel`/`macos-26-intel`. https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- **[V]** `macos-14` (and `macos-14-xlarge`/`-large`) are deprecated: deprecation July 6, 2026, full retirement + job failures November 2, 2026 — don't start new workflows on macOS 14. https://github.com/actions/runner-images/issues/13518
- GameCI's `unity-builder` supports a macOS host path (runs directly on the runner instead of a Docker container) for `StandaloneOSX`-style targets; but there is no macOS Docker image. For a tests-only workflow, Linux + the `unityci/editor` Linux container is the supported path. https://game.ci/docs/github/builder/ , https://game.ci/docs/faq/
- Unity 6 officially supports building for Apple Silicon (ARM64) macOS targets (Player Settings macOS build architecture: "Apple Silicon", "Intel 64-bit", or universal). https://docs.unity3d.com/6000.6/Documentation/Manual/macos-building.html (status 200 verified)
- macOS IL2CPP builds require Xcode on the build machine (system requirements), and GameCI documents only "limited IL2CPP support" in its images — neither affects EditMode testing. https://game.ci/docs/faq/ , https://game.ci/docs/docker/docker-images/
- **[NV]** Could not verify any primary source stating a behavioral difference for Unity 6 **EditMode testing** between Linux containers and macOS arm64 hosts. GameCI's own CI tests run on Ubuntu/Windows/macOS (see unity-builder's `build-tests-mac.yml` using Unity 6 `StandaloneOSX`), but no doc compares test-mode behavior across OSes. https://github.com/game-ci/unity-builder/blob/main/.github/workflows/build-tests-mac.yml
- Practical consequence for this repo (dev on M4 arm64 + macOS): CI on Linux runs the Linux editor — a different binary than the dev machine, which is fine for headless EditMode; if you ever need macOS-specific validation, `macos-15` is the current arm64 hosted label. https://docs.github.com/en/actions/reference/runners/github-hosted-runners

## 7. WebGL deployment (future) — canonical GitHub Pages shape
- GameCI's documented WebGL flow: `unity-builder@v4` with `targetPlatform: WebGL` writes the game to `build/`; upload it with `actions/upload-artifact@v4`. https://game.ci/docs/github/getting-started/ , https://game.ci/docs/github/builder/
- **[V]** WebGL editor image exists for this engine: `unityci/editor:ubuntu-6000.6.0f1-webgl-3.2.2` (Docker Hub tags API). https://hub.docker.com/r/unityci/editor/tags
- Publishing to Pages from Actions (official): a build job uploads with `actions/upload-pages-artifact`, then a deploy job (with `permissions: pages: write, id-token: write`, `environment: github-pages`) runs `actions/deploy-pages@v4`; the action deploys artifacts previously uploaded, outputs `page_url`. https://github.com/actions/deploy-pages/blob/main/README.md
- So the future flow is just: `unity-builder` (WebGL) → `upload-pages-artifact` from `build/` → `deploy-pages`. No extra GameCI machinery needed beyond what's already in section 2. (Also note: repo currently lacks the WebGL module on the dev machine — that only affects local builds; GameCI's WebGL image is self-contained.)

## 8. Unity-6-specific official docs on CI/CD and GitHub Actions runner docs
- Unity's official CI entry point, current for 6000.6: "Build a player from the command line" — `-batchmode -quit -projectPath` required args; `-logFile`, `-buildTarget`/`-activeBuildProfile` recommended; one target per invocation; several build APIs don't work in batch mode. https://docs.unity3d.com/6000.6/Documentation/Manual/build-command-line.html
- Unity Editor command-line arguments reference (6000.6), including `-nographics`, `-batchmode`, `-buildTarget`, `-executeMethod`. https://docs.unity3d.com/6000.6/Documentation/Manual/EditorCommandLineArguments.html (status 200 verified)
- GitHub official hosted-runner reference (labels/hardware/arch, incl. arm64 macOS): https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- GitHub official self-hosted-runner docs (only relevant if Unity's runtime/hardware needs outgrow hosted runners): https://docs.github.com/en/actions/hosting-your-own-runners/about-self-hosted-runners
- GameCI's own docs explicitly point GitHub-hosted-runner decisions back to these two GitHub docs from its FAQ. https://game.ci/docs/faq/

---

## Recommended stack (concrete, for this exact repo)

**Stack:** GitHub-hosted **Linux** runner + `game-ci/unity-test-runner@v4` in its Docker container. EditMode-only, coverage off. Personal license via `UNITY_LICENSE` (.ulf) + email/password secrets. This matches every primary source above: EditMode is the Unity-6-stable mode (#301), coverage off (#302/#306), project in subdirectory via `projectPath` (test-runner docs).

Optional post-setup: `web_search 6000.6.0f1` image availability check — already done, `ubuntu-6000.6.0f1-base-3.2.2` exists.

```yaml
# .github/workflows/ci.yml
name: Unity CI

on: [push, pull_request]

jobs:
  editmode:
    name: Unity EditMode Tests (6000.6.0f1)
    runs-on: ubuntu-latest          # host runner; Unity runs inside GameCI's Docker image
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          lfs: true

      - name: Cache Unity Library
        uses: actions/cache@v3
        with:
          path: kwaffee/Library
          key: Library-6000.6.0f1-${{ hashFiles('kwaffee/Assets/**', 'kwaffee/Packages/**', 'kwaffee/ProjectSettings/**') }}
          restore-keys: |
            Library-6000.6.0f1-

      - name: Run EditMode tests
        id: tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}    # full .ulf contents, one-time manual activation
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: kwaffee                           # repo-relative; Version.txt auto-read otherwise
          unityVersion: 6000.6.0f1                       # exact pin incl. tag letter + build number
          testMode: EditMode                             # NOT All: PlayMode SIGSEGVs on Unity 6 (#301)
          coverageEnabled: false                         # coverage crashes Unity 6 (#302, #306)
          artifactsPath: editmode-artifacts
          githubToken: ${{ secrets.GITHUB_TOKEN }}       # creates a Check run w/ results
          checkName: Unity EditMode Tests

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: EditMode test results
          path: ${{ steps.tests.outputs.artifactsPath }}
```

Rationale per input (all cited above):
- `projectPath: kwaffee` — project lives in subdirectory; path is repo-relative (test-runner docs).
- `unityVersion: 6000.6.0f1` — full version string with suffix+build (`6000.6.0f1`); "auto" would also resolve from `ProjectSettings/ProjectVersion.txt`, pinning is explicit (builder docs, action.yml).
- `testMode: EditMode` — `All` includes PlayMode, which crashes on Unity 6 (#301).
- `coverageEnabled: false` — default `true` is the documented Unity 6 crash/compile-failure root cause (#302, #306; action.yml description).
- `UNITY_LICENSE` secret — Personal license manual .ulf flow, GameCI's current canonical v4 method (activation docs; request-activation-file deprecated).
- `runs-on: ubuntu-latest` — x64 Linux hosted runner; GameCI has no macOS images, macOS arm64 adds no value for headless EditMode and has no primary-source-proven behavioral difference (section 6).

Future WebGL: same job shape, replace test step with `game-ci/unity-builder@v4` (`targetPlatform: WebGL`), then `actions/upload-pages-artifact` from `build/` and `actions/deploy-pages@v4` in a deploy job (section 7). The `-webgl-3.2.2` image for 6000.6.0f1 already exists.

---

### Verification status of the two facts the task flagged
- "Engine pin 6000.6.0f1 supported by GameCI" — **[V-API]** confirmed directly on Docker Hub (tags `ubuntu-6000.6.0f1-base-3`, `-3.2`, `-3.2.2`, `-webgl-3.2.2`, etc.).
- "uzak/unity-activation approach" — **[V]** repo does not exist (HTTP 404); nearest maintained alternative is `RageAgainstThePixel/activate-unity-license` (active; activates with credentials instead of a .ulf secret).