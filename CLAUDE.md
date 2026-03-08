# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AiLogoMaker is a .NET 8.0 console application that generates AI-powered logos with multi-platform export (Android, iOS, favicons, social media). It uses OpenAI's gpt-image-1 model for image generation and SixLabors.ImageSharp for image processing.

## Build & Run Commands

```bash
dotnet build                                          # Build entire solution
dotnet run --project AiLogoMaker.Console               # Run the console app
dotnet build AiLogoMaker.Domain                        # Build a single project
```

No test projects exist in the solution currently.

## Architecture

Clean Architecture with 4 projects (dependency flows inward):

```
Console → Application + Infra → Domain
```

- **AiLogoMaker.Domain** — Core models, interfaces, domain services (image generation logic, export presets). No external dependencies except ImageSharp and logging abstractions.
- **AiLogoMaker.Application** — `LogoOrchestrationService` coordinates domain services. Thin orchestration layer.
- **AiLogoMaker.Infra** — External integrations: `ChatGPTAppService` (OpenAI SDK), `FileSystemPromptRepository`, `FileSystemSessionRepository`, `ImageSharpExportService`.
- **AiLogoMaker.Console** — Entry point (`Program.cs`). Interactive workflow with DI setup, session management, and step-by-step user approval loops.

## Key Workflow

The app runs a sequential pipeline with user approval gates at each step:

1. **Base Logo** — AI generates square logo → user approves/adjusts/rejects
2. **Icon** — Symbol-only variant extracted from base
3. **Format Variants** — Horizontal & Vertical versions (each individually approved)
4. **Dark Variants** — Dark/white versions for each approved light variant
5. **Export** — Resizes to all platform sizes (Android 6 densities, iOS 13 sizes, favicons, social)
6. **Brand Guide** — Generates implementation guide with extracted color palette

Sessions persist to `output/[brand]_[timestamp]/session.json` and can be resumed.

## Configuration

Copy `appsettings.example.json` to `appsettings.json` and set:
- `OpenAI:ApiKey` — Required. Checked at startup.
- `Prompts:Path` — Path to prompt templates directory (defaults to `prompts/`).

## Key Directories

- `prompts/` — Markdown prompt templates (6 logo styles + 6 design rules + color-study)
- `output/` — Generated sessions with structured subdirectories (originals, android, ios, favicon, social, prompts)

## DI Registration

- `AddApplication()` in `AiLogoMaker.Application/DependencyInjection.cs` registers domain services
- `AddInfrastructure()` in `AiLogoMaker.Infra/DependencyInjection.cs` registers OpenAI client, repositories, export service

## Domain Models

- **Session** — Tracks brand info, current step (`SessionStep` enum), images, approval status
- **LogoVariant** — 7 variants: Square, Icon, IconDark, HorizontalLight/Dark, VerticalLight/Dark
- **LogoFormat** — Square, Vertical, Horizontal
- **ExportPresets** — Static size definitions for all platform exports

## CI/CD

GitHub Actions on main branch: GitVersion auto-tagging (`version-tag.yml`) and release creation for major/minor bumps (`create-release.yml`).
