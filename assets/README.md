# `assets/` (root) — Asset nguồn thô (pre-import)

> Asset **nguồn/thô** trước khi import vào game (psd, wav gốc, sprite sheet nguồn). Asset đã import dùng runtime nằm ở `client/assets/`.

| Mục | Nội dung |
|---|---|
| **Purpose** | Lưu bản gốc chất lượng cao để xử lý/xuất lại. |
| **Responsibilities** | Kho nguồn nghệ thuật/âm thanh; không nạp trực tiếp bởi Godot. |
| **Allowed** | File nguồn nặng (psd/ai/wav/…); cân nhắc **Git LFS**. |
| **Not allowed** | ❌ asset thiếu license (→ `../third_party/`); ❌ dùng làm asset runtime. |
| **Dependencies** | Pipeline export → `client/assets/`. |
| **Owner** | Art/Audio team. |
| **Future expansion** | Chuẩn hoá pipeline export, LFS. |
