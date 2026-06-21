# Make Bold Spark Project Brand Guide

Version: 2.0
Updated: 2026-06-15

## Purpose

This guide defines the visual identity, messaging, content strategy, information architecture, and implementation standards for every project within the Make Bold Spark ecosystem.

### Primary Positioning

> Spec-Driven Systems. Real Applications. Practical AI.

### Supporting Description

> Make Bold Spark is an ecosystem of open-source tools, reference implementations, and practical AI-assisted development patterns for building better software.

---

## Ecosystem Architecture

```text
Make Bold Spark
│
├── Systems
│   ├── DevSpark
│   ├── ApiTestSpark
│   ├── WebSpark
│   ├── ApiSpark
│   └── Future Spark Projects
│
├── Insights
├── Documentation
├── GitHub Repositories
└── Packages
```

## Site Responsibilities

### makeboldspark.com

Purpose:

- Discovery
- Product positioning
- SEO
- Ecosystem navigation
- Thought leadership
- Release announcements

Content:

- /systems/*
- /insights/*
- /releases/*

### Project Subdomains

Examples:

- dev.makeboldspark.com
- apitestspark.makeboldspark.com
- webspark.makeboldspark.com

Purpose:

- Documentation
- Tutorials
- Examples
- References
- Installation guides

Rule:

**Main site = discovery, insights, ecosystem, positioning**
**Subdomains = documentation, tutorials, references, usage**

---

## Project Identity

Every Spark Project must define:

- Name
- Description
- Status
- Accent Color
- Repository
- Documentation URL
- System Page

## Status Values

- Active
- Evolving
- Experimental
- Legacy
- Archived

---

## Information Architecture

## Systems

Every project receives a system page.

Examples:

- /systems/devspark
- /systems/apitestspark
- /systems/webspark
- /systems/apispark

## Insights

Insights belong on the parent site.

Examples:

- /insights/devspark/why-i-built-devspark
- /insights/devspark/spec-driven-development
- /insights/apitestspark/openapi-implementation-context

Insights explain:

- Decisions
- Architecture
- Evolution
- Lessons learned
- Case studies

Insights are not documentation.

## Documentation

Documentation belongs on project sites.

Examples:

- /getting-started
- /install
- /tutorials
- /reference

---

## Markdown Publishing Standard

Markdown is the preferred content format.

Benefits:

- Git-friendly
- AI-friendly
- Portable
- Searchable
- Easy migration
- Easy review

Database-backed CMS systems are discouraged.

## Required Front Matter

```yaml
---
title: "Title"
summary: "Short summary"

published: "2026-06-15"
updated: "2026-06-15"

status: "published"

system: "devspark"

contentType: "insight"

category: "architecture"

tags:
  - ai-assisted-development
  - architecture

featured: false

canonicalUrl: ""
originalUrl: ""

source: "makeboldspark"
---
```

## Content Types

- system
- documentation
- insight
- tutorial
- release
- reference
- case-study

---

## Voice and Tone

Spark Projects should be:

- Practical
- Builder-focused
- Architect-friendly
- Direct
- Credible
- Helpful

Preferred language:

- spec-driven
- practical
- reference implementation
- AI-assisted
- developer tooling
- architecture
- delivery pattern

Avoid:

- revolutionary
- disruptive
- AI magic
- fully autonomous
- instant software

---

## Brand Governance

Projects may customize:

- Accent color
- Product screenshots
- Documentation examples
- Product-specific diagrams

Projects may NOT customize:

- Core typography
- Parent logo proportions
- Ecosystem structure
- Voice and tone principles
- Footer attribution

---

## SEO and Migration

When migrating content from markhazleton.com:

1. Move article to Make Bold Spark.
2. Preserve publication date.
3. Set canonical URL.
4. Create 301 redirect.
5. Update internal links.

Preferred URLs:

```text
/systems/devspark

/insights/devspark/why-i-built-devspark

/insights/apitestspark/openapi-implementation-context

/releases/devspark-1-0
```

Avoid date-based URLs.

---

## Repository Standards

Every repository should contain:

```text
README.md
CHANGELOG.md
LICENSE
brand-guide.md
```

Recommended:

```text
/docs
/content
/assets
```

---

## Success Criteria

A Spark Project is compliant when:

- It clearly identifies its purpose.
- It follows the ecosystem architecture.
- It uses approved visual standards.
- It supports Markdown-first publishing.
- It links back to Make Bold Spark.
- It has a documented status.
- It explains how it fits into the ecosystem.
- It feels like part of a larger family of systems.
