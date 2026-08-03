# Hướng dẫn phong cách (Style Guide) — Điểm vào

> Tóm tắt. **Nguồn đầy đủ:** [`docs/conventions/`](docs/conventions/) — [code-style](docs/conventions/code-style.md), [naming](docs/conventions/naming.md), [git](docs/conventions/git-conventions.md), [data & docs](docs/conventions/data-and-docs-conventions.md). Máy kiểm: [`.editorconfig`](.editorconfig), analyzers, gdlint/gdformat.

## Nguyên tắc chung
SRP · tên nói rõ ý định · guard clause · **không magic number** (dùng hằng/config) · **composition > inheritance** · immutability khi có thể · lỗi tường minh (không nuốt lỗi). **Cấm** God Object, switch-để-mở-rộng gameplay, hardcode config.

## GDScript (client)
Static typing mọi nơi · `snake_case` (biến/hàm), `PascalCase` (`class_name`), `CONSTANT_CASE` · signal đặt tên theo sự kiện · combat sim tách khỏi node (thuần) · **tab** thụt lề (chuẩn Godot).

## C# (backend)
`nullable` bật · `PascalCase`/`camelCase`/`_camelCase` · async I/O toàn bộ (không `.Result`) · DI qua constructor (không service locator ở Domain/Application) · Domain thuần (inject `IClock`) · CQRS tách command/query · `record`/VO cho bất biến.

## Determinism combat (BẮT BUỘC — ADR-011)
**Không float** trong sim; integer/fixed-point · RNG **seeded** truyền vào (không toàn cục) · thứ tự lặp ổn định · cùng (config version, snapshot, seed) ⇒ cùng output · client & server cùng ruleset + golden vector.

## Documentation trong code
GDScript `##` cho public; C# `///` cho public API; comment giải thích **WHY**. Mỗi feature/module có `README.md` ngắn (hỗ trợ AI context).

## Git
Nhánh `feature/<id>-<slug>` · **Conventional Commits** · PR nhỏ + DoD · **squash-merge** · SemVer.
