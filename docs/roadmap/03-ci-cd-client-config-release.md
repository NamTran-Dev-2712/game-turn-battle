# 03 — CI/CD client (Godot headless) + validate-config + release

> Mục đích: Nâng 3 workflow còn ở mức skeleton (`ci-client`, `validate-config`, `release`) lên chạy thật ở mức nền, để client, config và quy trình phát hành có cổng kiểm.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 0 Nền tảng & Chuẩn hoá | P0 | S0 | hạ tầng |

# Mục tiêu

`ci-client.yml` import Godot **4.7 headless** + chạy test gdUnit4 mẫu; `validate-config.yml` bắc khung gọi `tools/config-validator` (bật đầy đủ ở phase 07); `release.yml` build+test+docker + tạo draft release theo tag `v*`.

# Lý do

Client và config là hai nhánh chưa được CI bảo vệ. Cần cổng tối thiểu ngay để khi client core (nhóm 3) và config schema (nhóm 1) bắt đầu có nội dung thì không hồi quy âm thầm.

# Phụ thuộc

- **Trước:** 01, 02.
- **Sau:** nhóm 3 (client), nhóm 1 (config validator 06–07), phase 55 (release hoàn chỉnh).

# Phạm vi

- `ci-client.yml`: cài Godot 4.7, `--headless --import`, chạy gdUnit4 smoke test (1 test mẫu), báo lỗi import.
- `validate-config.yml`: giữ JSON parse-check hiện có, thêm hook gọi `tools/config-validator` (no-op tới phase 07).
- `release.yml`: on tag `v*` → build/test server, `docker build -f server/Dockerfile`, tạo **draft** GitHub Release.
- Pin `GODOT_VERSION=4.7`; path filter `client/**`, `config/**`, `shared/config-schema/**`.

# Không thuộc phạm vi

- Android export/signing (phase 55).
- Registry push image (phase 55).
- Referential-integrity validation đầy đủ (phase 07).
- Test client feature thực (các phase feature).

# Deliverables

- `ci-client.yml` xanh: import Godot headless + 1 gdUnit4 test mẫu pass.
- `validate-config.yml` với hook validator (bật/tắt bằng cờ).
- `release.yml` tạo được draft release trên tag thử.
- Ghi chú phiên bản Godot pin ở [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md).

# Công việc cần thực hiện

> Ghi chú xác minh: item chỉ chạy được trên CI runner (tải Godot, import headless, gdUnit4, docker,
> draft release) được đánh dấu **⏳ CI-pending** — đã hiện thực + xác minh tĩnh cục bộ, nhưng **giữ `[ ]`**
> cho tới khi có kết quả GitHub Actions thật (theo `CLAUDE.md` §4.5, `.claude/checklists/post-task.md`).

- [ ] ⏳ **CI-pending** — Thêm bước tải Godot 4.7 headless (cache binary) trong `ci-client.yml`. *(Đã hiện thực: tải binary chính thức `4.7-stable` + verify `SHA512-SUMS.txt` + `actions/cache@v4`; YAML hợp lệ. Sandbox chặn CDN tải binary → chưa chạy được cục bộ.)*
- [ ] ⏳ **CI-pending** — `godot --headless --import` để build import cache; fail nếu lỗi import. *(Bước import + guard exit-code đã có; cần runner có Godot để xác minh.)*
- [ ] ⏳ **CI-pending** — Thêm gdUnit4 (addon) + 1 test mẫu; chạy headless, xuất kết quả JUnit. *(Addon **vendored** `client/addons/gdUnit4` pin v6.2.0 — 599 file + LICENSE, đã xác minh đầy đủ cục bộ; test `client/tests/smoke/example_smoke_test.gd` đã tạo; discovery/headless-guard/JUnit-path đã kiểm chứng qua đọc mã. Chạy headless + xuất JUnit là CI-pending.)*
- [x] `validate-config.yml`: giữ parse-check; thêm step gọi `tools/config-validator` sau cờ `--if-present` (tới 07 mới bắt buộc). *(Xác minh cục bộ: parse-check pass trên config mẫu; hook no-op exit 0; negative test JSON hỏng ⇒ đỏ rồi revert.)*
- [ ] ⏳ **CI-pending** — `release.yml`: build+test server → `docker build` → `softprops/action-gh-release` tạo draft, generate notes. *(Server build Release 0 warning + test 6/6 pass **đã xác minh cục bộ**; `docker build`/draft-release là CI-pending — Docker daemon tắt cục bộ, draft cần tag trên GitHub.)*
- [x] Pin `GODOT_VERSION` biến môi trường; đồng bộ với `project.godot` feature `4.7`. *(Xác minh tĩnh: `ci-client.yml env.GODOT_VERSION="4.7"` khớp `client/project.godot config/features "4.7"`; CI có guard `--version`.)*
- [x] Cập nhật `../godot/tooling-and-testing.md` + `../deployment/release-operations.md`. *(Đã cập nhật + `ci-cd-pipeline.md` §4c/§4d/§4e, `../testing/godot-testing.md`, `.github/workflows/README.md`.)*

# Tiêu chí hoàn thành

- `ci-client.yml` xanh: import không lỗi + test mẫu pass headless.
- `validate-config.yml` xanh trên config mẫu; hook validator gọi được (no-op ok).
- Tag thử `v0.0.0-ci` tạo **draft** release thành công rồi xoá.
- Godot version pin nhất quán giữa CI và `project.godot`.

# Cách kiểm tra

- PR đụng `client/**` → Actions ci-client xanh.
- PR đụng `config/**` → validate-config xanh.
- Đẩy tag thử → kiểm draft release sinh ra (rồi gỡ tag+draft).
- Local (nếu có Godot): `godot --headless --import` không lỗi.

# Rủi ro

- **Godot 4.7 headless không sẵn trên runner** → tải binary chính thức + cache; ghim URL/hash.
- **gdUnit4 flaky headless** → chạy deterministic, tắt animation/real-time trong test.
- **Draft release rác** → dùng tag namespace `-ci` và dọn sau kiểm.

# Ghi chú

Client CI sẽ được các phase nhóm 3+ mở rộng (feature tests). Đây chỉ dựng cổng. Xem [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md).

# Technical Debt Review

- **Maintainability:** cache Godot binary + addon versioned (ADR-010) giảm bất định.
- **Scalability:** JUnit output cho phép ghép nhiều test sau.
- **Testing:** mới ở mức smoke; feature test thêm dần.
- **Security:** release chỉ tạo draft; không lộ signing key (chưa cấu hình).
- **Nợ:** referential validation & Android export để phase 07/55.

# Phase Review

Đóng khi cả 3 workflow chạy thật ở mức nền (client import+test mẫu, config hook, release draft), version Godot đồng bộ.

## Bằng chứng xác minh (checkpoint hiện tại)

**Đã xác minh cục bộ (local):**
- `validate-config.yml`: parse-check `python3 json.load` pass trên `shared/config-schema/config-bundle.schema.json`; hook `--if-present` no-op exit 0; **negative test**: chèn JSON hỏng ⇒ parse-check đỏ → đã revert.
- `release.yml` (phần server): `dotnet build server/GameTeam.sln -c Release` → **0 warning / 0 error**; `dotnet test` → **6/6 pass** (Domain 1, Infrastructure 1, Application 3, Api.Integration 1). SDK 9.0.306.
- Version pin: `ci-client.yml env.GODOT_VERSION="4.7"` khớp `client/project.godot` (`config/features` chứa `"4.7"`).
- gdUnit4 vendored: 599 file + LICENSE (MIT) tại `client/addons/gdUnit4`, `plugin.cfg version="6.2.0"`; đã sửa `.gitignore` để rule `[Bb]in/` không nuốt `addons/gdUnit4/bin/` (CLI runner). Discovery (`GdUnitTestSuiteScanner` quét mọi `.gd extends GdUnitTestSuite`), headless-guard (cần `xvfb-run`), JUnit path (`reports/report_<n>/results.xml`) đã kiểm chứng qua đọc mã v6.2.0.
- YAML 4 workflow parse hợp lệ (PyYAML).

**⏳ Chờ xác minh GitHub Actions (blocker để đóng phase):**
- `ci-client.yml`: tải+verify Godot 4.7, `--headless --import`, gdUnit4 smoke test pass, artifact JUnit. *(Sandbox chặn CDN tải binary Godot + không có Godot cục bộ.)*
- `release.yml`: `docker build -f server/Dockerfile` *(Docker daemon tắt cục bộ)* + tạo **draft** release trên tag thử `v0.0.0-ci` (rồi xoá tag+draft).

**Kết luận:** Hiện thực đầy đủ theo phạm vi, bounded đúng scope. **CHƯA đủ điều kiện đóng** theo Strict
Phase Gate §5 (điểm 2/4: acceptance đo được + CI xanh) cho tới khi 4 item **CI-pending** ở trên có
bằng chứng Actions xanh. Trạng thái: **PASS WITH BLOCKER (GitHub-verification pending)**.

---

## Liên kết
- [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md) · [`../testing/godot-testing.md`](../testing/godot-testing.md) · [`../deployment/release-operations.md`](../deployment/release-operations.md)
- ADR: [`../adr/ADR-001-engine-choice.md`](../adr/ADR-001-engine-choice.md) · [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → kế: [`04-dev-environment-tooling.md`](04-dev-environment-tooling.md)
