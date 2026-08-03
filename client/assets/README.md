# `client/assets/` — Asset đã import (runtime)

> Asset **đã import** dùng trực tiếp trong game. Asset thô/nguồn (psd, wav gốc) nằm ở `../../assets/` (root) hoặc `../../design/`.

| Mục | Nội dung |
|---|---|
| **Purpose** | Lưu art/audio/vfx/font dùng trong scene. |
| **Responsibilities** | Tổ chức theo loại; tuân quy tắc import Godot. |
| **Allowed** | `.png`/`.webp`, `.ogg`/`.wav`, particle/shader, `.ttf`/`.otf` + `.import`. |
| **Not allowed** | ❌ file nguồn nặng chưa xử lý; ❌ asset không rõ license (dùng `../../third_party/`). |
| **Dependencies** | Được scene/feature tham chiếu qua `res://`. |
| **Owner** | Art/Audio team. |
| **Future expansion** | Thêm nhóm asset; cân nhắc Git LFS cho binary lớn. |

## Thư mục con
`art/` · `audio/` · `vfx/` · `fonts/`

Chi tiết: `../../docs/godot/resources-and-assets.md`, quy tắc import: `../../docs/conventions/naming.md`.
