# Chính sách bảo mật (Security Policy)

## Báo cáo lỗ hổng
**Không** mở issue công khai cho lỗ hổng bảo mật. Liên hệ maintainer qua kênh riêng (xem [SUPPORT.md](SUPPORT.md)) kèm mô tả, bước tái hiện, tác động. Chúng tôi phản hồi trong thời gian hợp lý và phối hợp công bố có trách nhiệm.

## Phạm vi nhạy cảm
- **Server-authoritative:** mọi quyết định phần thưởng/kinh tế/combat ở server (ADR-011) — báo cáo bất kỳ cách nào client thao túng kết quả.
- **Xác thực:** JWT, quản lý phiên (docs/backend/cross-cutting.md).
- **Giao dịch:** idempotency + atomic cho claim/summon/purchase (ADR-007).

## Nguyên tắc bảo mật trong repo
| Nguyên tắc | Chi tiết |
|---|---|
| Không commit secret | `.env`/key/keystore gitignore; dùng GitHub Environments ([ci-cd §5](docs/deployment/ci-cd-pipeline.md)) |
| Least privilege | Token CI quyền tối thiểu |
| Quét phụ thuộc | Dependabot ([.github/dependabot.yml](.github/dependabot.yml)); CodeQL — TODO |
| Không log dữ liệu nhạy cảm | [code-style §3](docs/conventions/code-style.md) |

## Phiên bản được hỗ trợ
Trong giai đoạn phát triển, chỉ nhánh `main`/`dev` được hỗ trợ.
