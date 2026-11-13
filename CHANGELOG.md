# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-11-13

### Added
- Per-axis cell scaling on `FlexibleGridLayoutGroup` via `widthMode` / `heightMode`
  (`CellSizeMode.Fixed` / `Expand` / `Aspect`). Cells can now stretch to fill the
  container when the UI is resized, instead of keeping a fixed size. `Fixed` on both
  axes preserves the previous behaviour, so existing layouts are unchanged.

## [1.0.0] - 2026-11-12

### Added
- `FlexibleGridLayoutGroup` — grid layout with per-cell column/row spans, CSS-style
  dense auto-placement (Fill Gaps), fixed positions, start corner, and content alignment.
- `FlexibleGridCell` — per-child span and optional fixed grid position.
- Custom inspectors grouped to match Unity's built-in layout components.
- Grid icons on both components so they read as part of the layout ecosystem.
