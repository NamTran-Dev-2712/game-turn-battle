# `.githooks/` — Git hooks dùng chung

> Hook chia sẻ trong repo. Kích hoạt một lần cho máy dev:
>
> ```bash
> git config core.hooksPath .githooks
> ```

| Hook | Việc (bootstrap stub) |
|---|---|
| `pre-commit` | Chặn commit chứa dấu vết secret rõ ràng; nhắc format (editorconfig). |

## Hai lựa chọn
- **Đơn giản (mặc định):** dùng `pre-commit` script trong thư mục này (không cần cài thêm).
- **Framework (tuỳ chọn):** dùng [pre-commit](https://pre-commit.com) qua `../.pre-commit-config.yaml`.

> **Bootstrap:** hook cố ý nhẹ để không cản trở; siết dần khi có lint/format thật (gdformat, dotnet format). Không thay thế gate CI (`../.github/workflows/`).
