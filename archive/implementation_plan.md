# GitHub Preparation & Cleanup Plan

The goal is to prepare the repository for a professional GitHub presence by cleaning up redundant files and creating high-quality documentation.

## User Review Required

> [!IMPORTANT]
> I will be removing `GEMINI.md` as it contains internal development rules that are typically not shared in a public repository. I will also move the detailed documentation into a `docs/` folder.

## Proposed Changes

### 1. Cleanup
- **[DELETE]** `GEMINI.md`: Internal requirements file.
- **[DELETE]** `AuthApi.Tests/UnitTest1.cs`: Redundant template file.
- **[DELETE]** `project_review.md`, `implementation_plan.md`, `task.md`, `walkthrough.md`: These are assistant-generated management files. They should stay in the assistant's brain or be deleted from the repo.

### 2. Documentation [NEW STRUCTURE]
- **[MODIFY]** `README.md`: Create a professional, comprehensive README in English (standard for GitHub) or Bilingual (Turkish/English) as per your preference. I will default to a high-quality Bilingual version unless specified.
- **[NEW] [docs/api_documentation.md](file:///c:/Users/pasaa/Desktop/backend/docs/api_documentation.md)**: Move and translate/refine `api_dokumantasyonu.md`.
- **[NEW] [docs/project_structure.md](file:///c:/Users/pasaa/Desktop/backend/docs/project_structure.md)**: Move and refine `tanim.md`.

### 3. Code Polish
- Remove unused `using` statements across the project.
- Ensure consistent naming and formatting.

## Verification Plan
- Ensure the project still builds and tests pass after cleanup.
- Verify all links in the new `README.md` work.

## Open Questions
- Do you want the README to be strictly English, or Bilingual (Turkish & English)?
- Should I keep the assistant-generated files (`project_review.md` etc.) in a separate `archive/` folder or delete them entirely?
