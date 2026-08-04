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

- [ ] Thêm bước tải Godot 4.7 headless (cache binary) trong `ci-client.yml`.
- [ ] `godot --headless --import` để build import cache; fail nếu lỗi import.
- [ ] Thêm gdUnit4 (addon) + 1 test mẫu; chạy headless, xuất kết quả JUnit.
- [ ] `validate-config.yml`: giữ parse-check; thêm step gọi `tools/config-validator` sau cờ `--if-present` (tới 07 mới bắt buộc).
- [ ] `release.yml`: build+test server → `docker build` → `softprops/action-gh-release` tạo draft, generate notes.
- [ ] Pin `GODOT_VERSION` biến môi trường; đồng bộ với `project.godot` feature `4.7`.
- [ ] Cập nhật `../godot/tooling-and-testing.md` + `../deployment/release-operations.md`.

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

---

## Liên kết
- [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md) · [`../testing/godot-testing.md`](../testing/godot-testing.md) · [`../deployment/release-operations.md`](../deployment/release-operations.md)
- ADR: [`../adr/ADR-001-engine-choice.md`](../adr/ADR-001-engine-choice.md) · [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md)
- Roadmap: [`README.md`](README.md) → kế: [`04-dev-environment-tooling.md`](04-dev-environment-tooling.md)
