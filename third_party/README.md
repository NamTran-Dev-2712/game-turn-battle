# `third_party/` — Mã/asset bên thứ ba

| Mục | Nội dung |
|---|---|
| **Purpose** | Cách ly thư viện/asset bên ngoài + **theo dõi license** rõ ràng. |
| **Responsibilities** | Chứa dependency vendored không lấy qua package manager; kèm file license. |
| **Allowed** | Mã/asset ngoài + `LICENSE`/`NOTICE` tương ứng. |
| **Not allowed** | ❌ asset/mã không rõ license; ❌ chỉnh sửa che giấu nguồn. |
| **Dependencies** | Tham chiếu bởi client/server nếu cần. |
| **Owner** | Người thêm dependency (qua PR, ADR-010 tinh thần). |
| **Future expansion** | Ghi rõ nguồn & phiên bản mỗi mục. |
